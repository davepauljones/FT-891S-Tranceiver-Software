using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using NAudio.Wave;

namespace CWDecoder
{
    public readonly struct CWDecoderDiagnostic
    {
        public CWDecoderDiagnostic(
            string stage,
            double power,
            double threshold,
            bool toneState,
            int durationMs,
            double dotLength,
            string symbol,
            string lastDecoded)
        {
            Stage = stage;
            Power = power;
            Threshold = threshold;
            ToneState = toneState;
            DurationMs = durationMs;
            DotLength = dotLength;
            Symbol = symbol;
            LastDecoded = lastDecoded;
        }

        public string Stage { get; }
        public double Power { get; }
        public double Threshold { get; }
        public bool ToneState { get; }
        public int DurationMs { get; }
        public double DotLength { get; }
        public string Symbol { get; }
        public string LastDecoded { get; }
    }

    public class CWDecoderEngine : IDisposable
    {
        public event EventHandler<string> TextDecoded;
        public event EventHandler<double> SignalPowerUpdated;
        public event EventHandler<CWDecoderDiagnostic> DiagnosticUpdated;

        public bool IsRunning { get; private set; }

        private WaveInEvent waveIn;
        private GoertzelDetector detector;
        private Timer flushTimer;

        public int SampleRate { get; set; } = 8000;
        public int TargetFrequency { get; set; } = 600;
        public double Threshold { get; set; } = 0.020;

        private bool toneState = false;
        private double lastPower = 0.0;
        private int lastDurationMs = 0;
        private string lastDecoded = string.Empty;

        private readonly Stopwatch stateClock = new Stopwatch();
        private readonly Stopwatch idleClock = new Stopwatch();
        private readonly Stopwatch toneHoldClock = new Stopwatch();

        private readonly List<int> toneTimes = new List<int>();
        private readonly StringBuilder symbol = new StringBuilder();
        private double dotLength = 70;
        private bool wordSpaceSent = true;

        private int sessionId = 0;
        private long lastAudioTicks = 0;
        private int isResyncing = 0;

        private static readonly Dictionary<string, string> morse = new Dictionary<string, string>
        {
            {".-","A"},{"-...","B"},{"-.-.","C"},{"-..","D"},{".","E"},
            {"..-.","F"},{"--.","G"},{"....","H"},{"..","I"},{".---","J"},
            {"-.-","K"},{".-..","L"},{"--","M"},{"-.","N"},{"---","O"},
            {".--.","P"},{"--.-","Q"},{".-.","R"},{"...","S"},{"-","T"},
            {"..-","U"},{"...-","V"},{".--","W"},{"-..-","X"},{"-.--","Y"},
            {"--..","Z"},
            {"-----","0"},{".----","1"},{"..---","2"},{"...--","3"},{"....-","4"},
            {".....","5"},{"-....","6"},{"--...","7"},{"---..","8"},{"----.","9"}
        };

        public void Start()
        {
            Stop();

            int currentSession = Interlocked.Increment(ref sessionId);

            lock (symbol)
            {
                detector = new GoertzelDetector(SampleRate, TargetFrequency);
                toneState = false;
                lastPower = 0.0;
                lastDurationMs = 0;
                lastDecoded = string.Empty;
                dotLength = 70;
                symbol.Clear();
                toneTimes.Clear();
                wordSpaceSent = true;

                stateClock.Restart();
                idleClock.Restart();
                toneHoldClock.Reset();
                lastAudioTicks = Stopwatch.GetTimestamp();
            }

            WaveInEvent newWaveIn = null;
            Timer newTimer = null;

            try
            {
                newWaveIn = new WaveInEvent
                {
                    DeviceNumber = 0,
                    WaveFormat = new WaveFormat(SampleRate, 16, 1),
                    BufferMilliseconds = 10
                };
                newWaveIn.DataAvailable += OnAudio;
                newWaveIn.StartRecording();

                waveIn = newWaveIn;
                IsRunning = true;

                newTimer = new Timer(CheckForSilenceTimeout, currentSession, 20, 20);
                flushTimer = newTimer;

                PublishDiagnostic("START");
            }
            catch
            {
                IsRunning = false;
                newTimer?.Dispose();
                if (newWaveIn != null)
                {
                    newWaveIn.DataAvailable -= OnAudio;
                    try { newWaveIn.StopRecording(); } catch { }
                    newWaveIn.Dispose();
                }
                throw;
            }
        }

        public void Stop()
        {
            Interlocked.Increment(ref sessionId);
            IsRunning = false;

            Timer oldTimer = Interlocked.Exchange(ref flushTimer, null);
            oldTimer?.Dispose();

            WaveInEvent oldWaveIn = Interlocked.Exchange(ref waveIn, null);
            if (oldWaveIn != null)
            {
                oldWaveIn.DataAvailable -= OnAudio;
                try { oldWaveIn.StopRecording(); } catch { }
                try { oldWaveIn.Dispose(); } catch { }
            }

            lock (symbol)
            {
                stateClock.Stop();
                toneHoldClock.Reset();
            }

            PublishDiagnostic("STOP");
        }

        private void OnAudio(object sender, WaveInEventArgs e)
        {
            if (!IsRunning) return;

            int count = e.BytesRecorded / 2;
            if (count == 0) return;

            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                short s = BitConverter.ToInt16(e.Buffer, i * 2);
                samples[i] = s / 32768f;
            }

            GoertzelDetector currentDetector = detector;
            if (currentDetector == null) return;

            double power = currentDetector.Process(samples);
            lastAudioTicks = Stopwatch.GetTimestamp();
            lastPower = power;
            SignalPowerUpdated?.Invoke(this, power);

            lock (symbol)
            {
                if (!IsRunning) return;

                bool detected = power > Threshold;

                if (detected)
                {
                    idleClock.Restart();
                    if (!toneState)
                        toneHoldClock.Restart();
                }
                else
                {
                    toneHoldClock.Reset();
                }

                if (detected != toneState)
                {
                    int durationMs = (int)stateClock.ElapsedMilliseconds;
                    lastDurationMs = durationMs;
                    stateClock.Restart();

                    if (toneState)
                        ProcessTone(durationMs);
                    else
                        ProcessGap(durationMs);

                    toneState = detected;
                    PublishDiagnostic(toneState ? "TONE_ON" : "TONE_OFF");
                }
            }
        }

        private readonly List<int> dotTimes = new List<int>();
        private readonly List<int> dashTimes = new List<int>();
        private int outlierStreak = 0;
        private int lastOutlierSign = 0;

        private void ProcessTone(int ms)
        {
            if (ms < 12) return;

            bool isDash = ms >= dotLength * 1.8;

            // Detect a sustained speed change (sender speeds up or slows down)
            int sign = ms < dotLength * 0.55 ? -1 : (ms > dotLength * 2.6 ? 1 : 0);
            if (sign != 0)
            {
                if (sign == lastOutlierSign)
                    outlierStreak++;
                else
                {
                    lastOutlierSign = sign;
                    outlierStreak = 1;
                }

                if (outlierStreak >= 3)
                {
                    dotTimes.Clear();
                    dashTimes.Clear();
                    dotLength = Clamp(sign < 0 ? ms : ms / 3.0, 20.0, 250.0);
                    isDash = ms >= dotLength * 1.8;
                    outlierStreak = 0;
                    lastOutlierSign = 0;
                }
            }
            else
            {
                outlierStreak = 0;
                lastOutlierSign = 0;
            }

            if (!isDash && ms >= dotLength * 0.45 && ms <= dotLength * 1.8)
            {
                AddSample(dotTimes, ms, 8);
                double dotMedian = Median(dotTimes);
                dotLength = Clamp((dotLength * 0.7) + (dotMedian * 0.3), 20.0, 250.0);
            }
            else if (isDash && ms <= dotLength * 5.0)
            {
                AddSample(dashTimes, ms, 8);
                double dashMedian = Median(dashTimes);
                double impliedDot = dashMedian / 3.0;
                double weight = dotTimes.Count < 3 ? 0.35 : 0.12;
                dotLength = Clamp((dotLength * (1.0 - weight)) + (impliedDot * weight), 20.0, 250.0);
            }

            symbol.Append(isDash ? "-" : ".");
            wordSpaceSent = false;
        }

        private static void AddSample(List<int> list, int value, int maxCount)
        {
            list.Add(value);
            if (list.Count > maxCount) list.RemoveAt(0);
        }

        private static double Median(List<int> values)
        {
            var sorted = values.OrderBy(x => x).ToList();
            return sorted[sorted.Count / 2];
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private void ProcessGap(int ms)
        {
            if (ms < 15) return;

            if (ms > dotLength * 1.8)
            {
                FlushCharacter();
            }

            if (ms > dotLength * 4.0 && !wordSpaceSent)
            {
                lastDecoded = " ";
                TextDecoded?.Invoke(this, " ");
                wordSpaceSent = true;
            }
        }

        private void CheckForSilenceTimeout(object state)
        {
            int expectedSession = state is int id ? id : 0;
            if (!IsRunning || Volatile.Read(ref sessionId) != expectedSession) return;

            double msSinceAudio = (Stopwatch.GetTimestamp() - Interlocked.Read(ref lastAudioTicks)) * 1000.0 / Stopwatch.Frequency;
            if (msSinceAudio > 1500)
            {
                ResyncAudioStream(expectedSession);
                return;
            }

            lock (symbol)
            {
                if (!IsRunning || Volatile.Read(ref sessionId) != expectedSession) return;

                if (toneState && toneHoldClock.IsRunning && toneHoldClock.ElapsedMilliseconds > 2500)
                {
                    toneState = false;
                    toneHoldClock.Reset();
                    symbol.Clear();
                    wordSpaceSent = true;
                    stateClock.Restart();
                    PublishDiagnostic("RESYNC_TONE");
                    return;
                }

                if (toneState) return;

                int currentSilenceMs = (int)stateClock.ElapsedMilliseconds;

                if (currentSilenceMs > dotLength * 1.8 && symbol.Length > 0)
                {
                    lastDurationMs = currentSilenceMs;
                    FlushCharacter();
                    PublishDiagnostic("SILENCE_FLUSH");
                }

                if (currentSilenceMs > dotLength * 4.0 && !wordSpaceSent)
                {
                    lastDurationMs = currentSilenceMs;
                    lastDecoded = " ";
                    TextDecoded?.Invoke(this, " ");
                    wordSpaceSent = true;
                    PublishDiagnostic("WORD_SPACE");
                }

                if (idleClock.ElapsedMilliseconds > 4000 &&
                    (symbol.Length > 0 || toneTimes.Count > 0 || Math.Abs(dotLength - 70) > 0.01))
                {
                    symbol.Clear();
                    toneTimes.Clear();
                    dotLength = 70;
                    wordSpaceSent = true;
                    stateClock.Restart();
                    idleClock.Restart();
                    PublishDiagnostic("RESYNC_IDLE");
                }
            }
        }

        private void ResyncAudioStream(int expectedSession)
        {
            if (Interlocked.Exchange(ref isResyncing, 1) != 0) return;

            try
            {
                if (!IsRunning || Volatile.Read(ref sessionId) != expectedSession) return;

                WaveInEvent oldWaveIn = Interlocked.Exchange(ref waveIn, null);
                if (oldWaveIn != null)
                {
                    oldWaveIn.DataAvailable -= OnAudio;
                    try { oldWaveIn.StopRecording(); } catch { }
                    try { oldWaveIn.Dispose(); } catch { }
                }

                if (!IsRunning || Volatile.Read(ref sessionId) != expectedSession) return;

                WaveInEvent newWaveIn = new WaveInEvent
                {
                    DeviceNumber = 0,
                    WaveFormat = new WaveFormat(SampleRate, 16, 1),
                    BufferMilliseconds = 10
                };
                newWaveIn.DataAvailable += OnAudio;
                newWaveIn.StartRecording();

                waveIn = newWaveIn;

                lock (symbol)
                {
                    detector = new GoertzelDetector(SampleRate, TargetFrequency);
                    toneState = false;
                    symbol.Clear();
                    toneTimes.Clear();
                    dotLength = 70;
                    wordSpaceSent = true;
                    stateClock.Restart();
                    idleClock.Restart();
                    toneHoldClock.Reset();
                    Interlocked.Exchange(ref lastAudioTicks, Stopwatch.GetTimestamp());
                }

                PublishDiagnostic("RESYNC_AUDIO");
            }
            catch
            {
                // Ignore transient device resync failures; next watchdog tick will retry if still running
            }
            finally
            {
                Interlocked.Exchange(ref isResyncing, 0);
            }
        }

        private void FlushCharacter()
        {
            if (symbol.Length == 0) return;

            string key = symbol.ToString();
            string decodedChar = morse.TryGetValue(key, out string val) ? val : "?";
            lastDecoded = decodedChar;

            symbol.Clear();

            TextDecoded?.Invoke(this, decodedChar);
            DiagnosticUpdated?.Invoke(this, new CWDecoderDiagnostic(
                "CHAR",
                lastPower,
                Threshold,
                toneState,
                lastDurationMs,
                dotLength,
                key,
                decodedChar));
        }

        private void PublishDiagnostic(string stage)
        {
            DiagnosticUpdated?.Invoke(this, new CWDecoderDiagnostic(
                stage,
                lastPower,
                Threshold,
                toneState,
                lastDurationMs,
                dotLength,
                symbol.ToString(),
                lastDecoded));
        }

        public void Dispose() => Stop();
    }

    public class GoertzelDetector
    {
        private readonly int sr;
        private readonly int f;

        public GoertzelDetector(int sr, int f) { this.sr = sr; this.f = f; }

        public double Process(float[] samples)
        {
            int n = samples.Length;
            if (n == 0) return 0;
            double k = 0.5 + ((n * f) / (double)sr);
            double w = (2 * Math.PI * k) / n;
            double c = 2 * Math.Cos(w);
            double q0 = 0, q1 = 0, q2 = 0;
            for (int i = 0; i < n; i++)
            {
                q0 = c * q1 - q2 + samples[i];
                q2 = q1; q1 = q0;
            }
            return Math.Sqrt(q1 * q1 - q1 * q2 * c + q2 * q2) / n;
        }
    }
}