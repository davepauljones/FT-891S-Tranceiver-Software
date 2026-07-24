using FT891S_CatControl;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using YAESU_FT_891_Front_End.Models;
using static YAESU_FT_891_Front_End.MyStructs;
using static YAESU_FT_891_Front_End.RigStateChanges;
using static YAESU_FT_891_Front_End.SimulatedWaterfall;

namespace YAESU_FT_891_Front_End
{
    public class WaterFallSweep
    {
        MainWindow mainWindow;
        Canvas bandScopeCanvas;
        Canvas canvas;

        Double canvasFullLeftPos = 14;
        Double canvasFullRightPos = 613;

        public bool SweepActive = false;
        public bool ScopeOnOff = false;

        // --- FEATURE CONFIGURATION TOGGLE ---
        public bool UseTimeSlicing { get; set; } = false;

        public long currentQsoCenterFrequency;

        private const int TimeSliceHopDelayMs = 15;
        private const int TimeSliceListenDwellMs = 150;

        private CancellationTokenSource _cts;
        private int _rfGainBeforeBandScopeScan;

        public WaterFallSweep(MainWindow mainWindow, Canvas bandScopeCanvas, Canvas canvas)
        {
            this.mainWindow = mainWindow;
            this.canvas = canvas;
            this.bandScopeCanvas = bandScopeCanvas;

            canvas.Visibility = Visibility.Collapsed;
        }

        // --- CLEANED METHOD: Parameter removed, relies purely on the property value ---
        public async Task ToggleSweepOnOff(bool forceSingleSweep = false)
        {
            if (ScopeOnOff && !forceSingleSweep)
            {
                ScopeOnOff = false;
                mainWindow.ScopeOnOffTextBlock.Text = "OFF";

                if (SweepActive && _cts != null)
                {
                    _cts.Cancel();
                    while (SweepActive)
                    {
                        await Task.Delay(5);
                    }
                }
                ClearBandScope();
            }
            else
            {
                ScopeOnOff = true;
                mainWindow.ScopeOnOffTextBlock.Text = forceSingleSweep ? "SINGLE" : "ON";

                long centerFreq = FT891S_CatManager.currentRadioState.VfoAFrequency;

                // Wait for the complete sweep cycle to process completely
                await Sweep(centerFreq - 25000, centerFreq + 25000, 500, 6);

                // If it was triggered as a forced single shot, drop state and clean up immediately
                if (forceSingleSweep)
                {
                    ScopeOnOff = false;
                    mainWindow.ScopeOnOffTextBlock.Text = "OFF";
                    ClearBandScope();
                }
            }
        }

        public async void ClearBandScope()
        {
            if (SweepActive && _cts != null)
            {
                _cts.Cancel();
                while (SweepActive)
                {
                    await Task.Delay(5);
                }
            }

            bandScopeCanvas?.Children.Clear();

            if (canvas != null)
            {
                Canvas.SetLeft(canvas, canvasFullLeftPos);
            }
        }

        public async Task Sweep(long startFreq, long endFreq, long step, int levelThreshold)
        {
            if (SweepActive) return;

            SweepActive = true;

            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            currentQsoCenterFrequency = FT891S_CatManager.currentRadioState.VfoAFrequency;
            FT891S_CatManager.currentRadioState.VfoALastFrequency = currentQsoCenterFrequency;

            long vfoAFreq = FT891S_CatManager.currentRadioState.VfoAFrequency;
            startFreq = vfoAFreq - (SimulatedWaterfall.currentFrequencySpanHz / 2);
            endFreq = vfoAFreq + (SimulatedWaterfall.currentFrequencySpanHz / 2);

            mainWindow._catManager.StopOutgoingDataLoop();

            await mainWindow._catManager.SendCatCommandAsync("RG", "0", mainWindow._catManager.OutGoingDataLoopDelay);
            _rfGainBeforeBandScopeScan = FT891S_CatManager.currentRadioState.RFGain;

            if (UseTimeSlicing)
            {
                await mainWindow._catManager.SendCatCommandAsync("RG", new object[] { 0, _rfGainBeforeBandScopeScan }, mainWindow._catManager.OutGoingDataLoopDelay);
            }
            else
            {
                await mainWindow._catManager.SendCatCommandAsync("RG", new object[] { 0, 30 }, mainWindow._catManager.OutGoingDataLoopDelay);
            }

            double totalCanvasWidth = canvasFullRightPos - canvasFullLeftPos;
            long totalSpanHz = mainWindow.simulatedWaterfall.GetSpanHz(SimulatedWaterfall.currentFrequencySpan);
            if (totalSpanHz <= 0) totalSpanHz = 50000;

            double pixelStep = ((double)step / (double)totalSpanHz) * totalCanvasWidth;

            // Track sweep direction: true = forward (start to end), false = backward (end to start)
            bool sweepForward = true;

            try
            {
                while (!token.IsCancellationRequested && ScopeOnOff)
                {
                    // Set initial position based on direction
                    double canvasCurrentPosition = sweepForward ? canvasFullLeftPos : canvasFullRightPos;
                    bandScopeCanvas.Children.Clear();
                    canvas.Visibility = Visibility.Visible;

                    List<byte> currentLineData = new List<byte>();
                    double currentLevelSetting = FunctionMenuClass.FunctionMenuMinMaxScaleTypeList[FunctionMenu.Level].currentValue;

                    // Define loop boundaries based on current direction
                    long loopStart = sweepForward ? startFreq : endFreq;
                    long loopEnd = sweepForward ? endFreq : startFreq;
                    long currentStep = sweepForward ? step : -step;

                    for (long freq = loopStart; sweepForward ? freq <= loopEnd : freq >= loopEnd; freq += currentStep)
                    {
                        if (token.IsCancellationRequested || !ScopeOnOff)
                        {
                            break;
                        }

                        Canvas.SetLeft(canvas, canvasCurrentPosition);

                        int rawMeterReading = 0;

                        if (UseTimeSlicing)
                        {
                            await mainWindow._catManager.SendCatCommandAsync("FA", new object[] { freq }, TimeSliceHopDelayMs);
                            await mainWindow._catManager.SendCatCommandAsync("RM", new object[] { (int)MeterTypes.DependsOnFrontPanelMETER }, TimeSliceHopDelayMs);
                            rawMeterReading = FT891S_CatManager.currentRadioState.CurrentMeterReading;
                            await mainWindow._catManager.SendCatCommandAsync("FA", new object[] { currentQsoCenterFrequency }, TimeSliceHopDelayMs);
                        }
                        else
                        {
                            await mainWindow._catManager.SendCatCommandAsync("FA", new object[] { freq }, 5);
                            await mainWindow._catManager.SendCatCommandAsync("RM", new object[] { (int)MeterTypes.DependsOnFrontPanelMETER }, 5);
                            rawMeterReading = FT891S_CatManager.currentRadioState.CurrentMeterReading;
                        }

                        // --- PROCESS & DRAW DATAPOINT ---
                        double baseSignal = rawMeterReading * 2.5;
                        double colorScaleShift = currentLevelSetting * 4.25;
                        int adjustedSignal = (int)(baseSignal + colorScaleShift);
                        byte finalizedSignal = (byte)Math.Max(0, Math.Min(255, adjustedSignal));
                        currentLineData.Add(finalizedSignal);

                        if (UseTimeSlicing)
                        {
                            mainWindow.frequencyManagement.SetFrequencyUIForBandScope(freq, mainWindow.MainFrequencyTextBlock);
                        }
                        else
                        {
                            mainWindow.frequencyManagement.SetFrequencyUIForBandScope(freq, mainWindow.MainFrequencyTextBlock);
                            mainWindow.LargeFrequencyDisplay.Frequency = freq;
                        }

                        if (freq != loopStart)
                        {
                            Double height = Convert.ToDouble(MainWindow.GetSMeterIntegerForBandScope(rawMeterReading));
                            height += currentLevelSetting;
                            height = Math.Max(0, height);

                            Rectangle r = new Rectangle { Width = SimulatedWaterfall.currentFrequencySpanRectangleWidth, Height = height, Fill = new SolidColorBrush(Colors.DodgerBlue) };
                            bandScopeCanvas.Children.Add(r);

                            // Adjust placement offset based on sweep direction
                            double rectPosition = sweepForward ? canvasCurrentPosition - 14 : canvasCurrentPosition - 19;

                            switch (SimulatedWaterfall.currentFrequencySpan)
                            {
                                case FrequencySpans._1K:
                                    rectPosition = sweepForward ? canvasCurrentPosition - 14 : canvasCurrentPosition - 290;
                                    break;
                                case FrequencySpans._2K:
                                    rectPosition = sweepForward ? canvasCurrentPosition - 14 : canvasCurrentPosition - 151;
                                    break;
                                case FrequencySpans._5K:
                                    rectPosition = sweepForward ? canvasCurrentPosition - 14 : canvasCurrentPosition - 69;
                                    break;
                                case FrequencySpans._10K:
                                    rectPosition = sweepForward ? canvasCurrentPosition - 14 : canvasCurrentPosition - 41;
                                    break;
                                case FrequencySpans._20K:
                                    rectPosition = sweepForward ? canvasCurrentPosition - 14 : canvasCurrentPosition - 28;
                                    break;
                                case FrequencySpans._50K:
                                    rectPosition = sweepForward ? canvasCurrentPosition - 14 : canvasCurrentPosition - 19;
                                    break;
                                case FrequencySpans._100K:
                                    rectPosition = sweepForward ? canvasCurrentPosition - 14 : canvasCurrentPosition - 17;
                                    break;
                                case FrequencySpans._200K:
                                    rectPosition = sweepForward ? canvasCurrentPosition - 14 : canvasCurrentPosition - 15;
                                    break;
                                case FrequencySpans._500K:
                                    rectPosition = sweepForward ? canvasCurrentPosition - 14 : canvasCurrentPosition - 14;
                                    break;
                                case FrequencySpans._1000K:
                                    rectPosition = sweepForward ? canvasCurrentPosition - 14 : canvasCurrentPosition - 13;
                                    break;
                            }
                            
                            Canvas.SetLeft(r, rectPosition);
                            Canvas.SetBottom(r, 1);

                            if (sweepForward)
                            {
                                if (canvasCurrentPosition < canvasFullRightPos) canvasCurrentPosition += pixelStep;
                                else break;
                            }
                            else
                            {
                                if (canvasCurrentPosition > canvasFullLeftPos) canvasCurrentPosition -= pixelStep;
                                else break;
                            }
                        }

                        if (freq == loopEnd)
                        {
                            Canvas.SetLeft(canvas, canvasCurrentPosition);
                        }

                        if (UseTimeSlicing)
                        {
                            await Task.Delay(TimeSliceListenDwellMs, token);
                        }
                    }

                    if (!token.IsCancellationRequested && ScopeOnOff && mainWindow.psudo3DWaterfall != null && currentLineData.Count > 0)
                    {
                        // If sweeping backward, reverse the data array so the waterfall aligns correctly left-to-right
                        if (!sweepForward)
                        {
                            currentLineData.Reverse();
                        }

                        mainWindow.psudo3DWaterfall.AddSweepToHistory(currentLineData.ToArray());
                        mainWindow.psudo3DWaterfall.Render3DWaterfall();
                    }

                    // Toggle direction for the next iteration
                    sweepForward = !sweepForward;
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sweep Loop Error: {ex.Message}");
            }
            finally
            {
                await mainWindow._catManager.SendCatCommandAsync("FA", new object[] { currentQsoCenterFrequency }, mainWindow._catManager.OutGoingDataLoopDelay);
                mainWindow.LargeFrequencyDisplay.Frequency = currentQsoCenterFrequency;

                await mainWindow._catManager.SendCatCommandAsync("RG", new object[] { 0, _rfGainBeforeBandScopeScan }, mainWindow._catManager.OutGoingDataLoopDelay);
                mainWindow._catManager.StartOutgoingDataLoop();

                if (RigMode != RadioMode.FM)
                {
                    await mainWindow._catManager.SendCatCommandAsync("RG", new object[] { 0, _rfGainBeforeBandScopeScan }, mainWindow._catManager.OutGoingDataLoopDelay);
                }
                else
                {
                    mainWindow.fT891S_SerialPort.SendCAT(mainWindow.fT891S_SerialPort._port, "SQ015");
                }

                _cts?.Dispose();
                _cts = null;
                SweepActive = false;
                ScopeOnOff = false;
                mainWindow.ScopeOnOffTextBlock.Text = "OFF";
                canvas.Visibility = Visibility.Collapsed;
            }

            await Task.Delay(20);
        }
    }
}