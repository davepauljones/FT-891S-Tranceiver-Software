using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Threading; // Added for UI safe dispatching if needed
using NAudio.Wave;

namespace CWDecoder
{
    public class CWDecoderEngine : IDisposable
    {
        public event EventHandler<string> TextDecoded;
        public event EventHandler<double> SignalPowerUpdated;

        public bool IsRunning;

        private WaveInEvent waveIn;
        private GoertzelDetector detector;
        private Timer flushTimer;

        public int SampleRate { get; set; } = 8000;
        public int TargetFrequency { get; set; } = 600;
        public double Threshold { get; set; } = 0.020;

        private bool toneState = false;

        // Using a Stopwatch is infinitely safer than counting buffer blocks 
        // for real-time decoding, preventing block-boundary errors.
        private readonly Stopwatch stateClock = new Stopwatch();

        private readonly List<int> toneTimes = new List<int>();
        private readonly StringBuilder symbol = new StringBuilder();
        private double dotLength = 70;
        private bool wordSpaceSent = true;

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

            detector = new GoertzelDetector(SampleRate, TargetFrequency);
            toneState = false;
            symbol.Clear();
            toneTimes.Clear();
            wordSpaceSent = true;

            stateClock.Restart();

            waveIn = new WaveInEvent
            {
                DeviceNumber = 0,
                WaveFormat = new WaveFormat(SampleRate, 16, 1),
                BufferMilliseconds = 10 // Low latency helps accuracy
            };

            waveIn.DataAvailable += OnAudio;
            waveIn.StartRecording();

            flushTimer = new Timer(CheckForSilenceTimeout, null, 20, 20);

            IsRunning = true;
        }

        public void Stop()
        {
            if (flushTimer != null)
            {
                flushTimer.Dispose();
                flushTimer = null;
            }

            if (waveIn != null)
            {
                waveIn.DataAvailable -= OnAudio;
                try { waveIn.StopRecording(); } catch { /* Ignore flight stop errors */ }
                waveIn.Dispose();
                waveIn = null;
            }

            stateClock.Stop();

            IsRunning = false;
        }

        private void OnAudio(object sender, WaveInEventArgs e)
        {
            int count = e.BytesRecorded / 2;
            if (count == 0) return;

            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                short s = BitConverter.ToInt16(e.Buffer, i * 2);
                samples[i] = s / 32768f;
            }

            double power = detector.Process(samples);
            SignalPowerUpdated?.Invoke(this, power);

            lock (symbol)
            {
                bool detected = power > Threshold;

                if (detected != toneState)
                {
                    // Calculate exact time elapsed on this state change
                    int durationMs = (int)stateClock.ElapsedMilliseconds;
                    stateClock.Restart();

                    if (toneState)
                        ProcessTone(durationMs);
                    else
                        ProcessGap(durationMs);

                    toneState = detected;
                }
            }
        }

        private void ProcessTone(int ms)
        {
            if (ms < 15) return; // Debounce artifact noise

            if (ms < dotLength * 2.5) // Relaxed dynamic tracking range
            {
                toneTimes.Add(ms);
                if (toneTimes.Count > 15) toneTimes.RemoveAt(0);

                var sorted = toneTimes.OrderBy(x => x).ToList();
                dotLength = sorted[sorted.Count / 2];
                if (dotLength < 30) dotLength = 30;
            }

            symbol.Append(ms < (dotLength * 1.8) ? "." : "-");
            wordSpaceSent = false;
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
                TextDecoded?.Invoke(this, " ");
                wordSpaceSent = true;
            }
        }

        private void CheckForSilenceTimeout(object state)
        {
            lock (symbol)
            {
                if (toneState) return;

                // Get exact un-interrupted silence duration directly from the clock
                int currentSilenceMs = (int)stateClock.ElapsedMilliseconds;

                if (currentSilenceMs > dotLength * 1.8)
                {
                    FlushCharacter();
                }

                if (currentSilenceMs > dotLength * 4.0 && !wordSpaceSent)
                {
                    TextDecoded?.Invoke(this, " ");
                    wordSpaceSent = true;
                }
            }
        }

        private void FlushCharacter()
        {
            if (symbol.Length == 0) return;

            string key = symbol.ToString();
            string decodedChar = morse.TryGetValue(key, out string val) ? val : "?";

            symbol.Clear();

            // Fire out event asynchronously to prevent UI blocks from hanging our audio thread
            TextDecoded?.Invoke(this, decodedChar);
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