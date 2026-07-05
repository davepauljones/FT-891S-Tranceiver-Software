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
using static YAESU_FT_891_Front_End.MyStructs;
using static YAESU_FT_891_Front_End.RigStateChanges;
using YAESU_FT_891_Front_End.Models;

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

            long currentQsoCenterFrequency = FT891S_CatManager.currentRadioState.VfoAFrequency;
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

            double canvasCurrentPosition = canvasFullLeftPos;
            double totalCanvasWidth = canvasFullRightPos - canvasFullLeftPos;
            long totalSpanHz = mainWindow.simulatedWaterfall.GetSpanHz(SimulatedWaterfall.currentFrequencySpan);
            if (totalSpanHz <= 0) totalSpanHz = 50000;

            double pixelStep = ((double)step / (double)totalSpanHz) * totalCanvasWidth;
            bandScopeCanvas.Children.Clear();

            canvas.Visibility = Visibility.Visible;

            List<byte> currentLineData = new List<byte>();
            double currentLevelSetting = FunctionMenuClass.FunctionMenuMinMaxScaleTypeList[FunctionMenu.Level].currentValue;

            try
            {
                for (long freq = startFreq; freq <= endFreq; freq += step)
                {
                    if (token.IsCancellationRequested || !ScopeOnOff)
                    {
                        break;
                    }

                    Canvas.SetLeft(canvas, canvasCurrentPosition);

                    int rawMeterReading = 0;

                    if (UseTimeSlicing)
                    {
                        // Hop out to sample frequency
                        await mainWindow._catManager.SendCatCommandAsync("FA", new object[] { freq }, TimeSliceHopDelayMs);

                        // Query S-Meter
                        await mainWindow._catManager.SendCatCommandAsync("RM", new object[] { (int)MeterTypes.DependsOnFrontPanelMETER }, TimeSliceHopDelayMs);
                        rawMeterReading = FT891S_CatManager.currentRadioState.CurrentMeterReading;

                        // Hop straight back to QSO audio center
                        await mainWindow._catManager.SendCatCommandAsync("FA", new object[] { currentQsoCenterFrequency }, TimeSliceHopDelayMs);
                    }
                    else
                    {
                        // High-Speed Mode
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

                    if (!(freq == startFreq))
                    {
                        Double height = Convert.ToDouble(MainWindow.GetSMeterIntegerForBandScope(rawMeterReading));
                        height += currentLevelSetting;
                        height = Math.Max(0, height);

                        Rectangle r = new Rectangle { Width = SimulatedWaterfall.currentFrequencySpanRectangleWidth, Height = height, Fill = new SolidColorBrush(Colors.DodgerBlue) };
                        bandScopeCanvas.Children.Add(r);

                        Canvas.SetLeft(r, canvasCurrentPosition - 14);
                        Canvas.SetBottom(r, 0);

                        if (canvasCurrentPosition < canvasFullRightPos)
                        {
                            canvasCurrentPosition += pixelStep;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (freq == endFreq)
                    {
                        Canvas.SetLeft(canvas, canvasCurrentPosition);
                    }

                    if (UseTimeSlicing)
                    {
                        await Task.Delay(TimeSliceListenDwellMs);
                    }
                }

                if (!token.IsCancellationRequested && ScopeOnOff && mainWindow.psudo3DWaterfall != null && currentLineData.Count > 0)
                {
                    mainWindow.psudo3DWaterfall.AddSweepToHistory(currentLineData.ToArray());
                    mainWindow.psudo3DWaterfall.Render3DWaterfall();
                }
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

                _cts.Dispose();
                _cts = null;
                SweepActive = false;
                canvas.Visibility = Visibility.Collapsed;
            }

            await Task.Delay(20);
        }
    }
}