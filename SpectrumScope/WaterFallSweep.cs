using FT891S_CatControl;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using static YAESU_FT_891_Front_End.MyStructs;
using static YAESU_FT_891_Front_End.RigState;
using static YAESU_FT_891_Front_End.RigStateChanges;

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
        public WaterFallSweep(MainWindow mainWindow, Canvas bandScopeCanvas, Canvas canvas)
        {
            this.mainWindow = mainWindow;
            this.canvas = canvas;
            this.bandScopeCanvas = bandScopeCanvas;
        }

        public void ToggleSweepOnOff()
        {
            if (ScopeOnOff)
            {
                ScopeOnOff = false;
                mainWindow.ScopeOnOffTextBlock.Text = "OFF";
            }
            else
            {
                ScopeOnOff = true;
                mainWindow.ScopeOnOffTextBlock.Text = "ON";
                Sweep(14252500, 14380000, 500, 6);
            }
        }
        public async void Sweep(long startFreq, long endFreq, long step, int levelThreshold)
        {
            if (SweepActive) return;

            SweepActive = true;

            FT891S_CatManager.currentRadioState.VfoALastFrequency = FT891S_CatManager.currentRadioState.VfoAFrequency;

            long vfoAFreq = FT891S_CatManager.currentRadioState.VfoAFrequency;

            startFreq = vfoAFreq - (SimulatedWaterfall.currentFrequencySpanHz / 2);

            endFreq = vfoAFreq + (SimulatedWaterfall.currentFrequencySpanHz / 2);

            if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.CurrentDebug)
            {
                Console.Write("startFreq is ");
                Console.Write(startFreq);
                Console.Write(" - endFreq is ");
                Console.WriteLine(endFreq);
            }

            mainWindow._catManager.StopOutgoingDataLoop();

            //await mainWindow._catManager.SendCatCommandAsync("AG", "0", mainWindow._catManager.OutGoingDataLoopDelay);

            //int afGainBeforeBandScopeScan = FT891S_CatManager.currentRadioState.AFGain;

            //await mainWindow._catManager.SendCatCommandAsync("AG", new object[] { 0, 0 }, mainWindow._catManager.OutGoingDataLoopDelay);

            await mainWindow._catManager.SendCatCommandAsync("RG", "0", mainWindow._catManager.OutGoingDataLoopDelay);

            int rfGainBeforeBandScopeScan = FT891S_CatManager.currentRadioState.RFGain;

            await mainWindow._catManager.SendCatCommandAsync("RG", new object[] { 0, 30 }, mainWindow._catManager.OutGoingDataLoopDelay);

            double canvasCurrentPosition = canvasFullLeftPos;
            double totalCanvasWidth = canvasFullRightPos - canvasFullLeftPos;

            // 1. KEEP YOUR VARIABLE HERE: Fetch the actual Hz value of the currently selected span
            long totalSpanHz = mainWindow.simulatedWaterfall.GetSpanHz(SimulatedWaterfall.currentFrequencySpan);

            // Safeguard against division by zero
            if (totalSpanHz <= 0) totalSpanHz = 50000;

            // 2. DYNAMIC MATH: Calculate pixel step using explicit double casting
            // This forces C# to use decimal math so it won't resolve to 0 or overshoot
            double pixelStep = ((double)step / (double)totalSpanHz) * totalCanvasWidth;

            bandScopeCanvas.Children.Clear();

            for (long freq = startFreq; freq <= endFreq; freq += step)
            {
                Canvas.SetLeft(canvas, canvasCurrentPosition);

                await mainWindow._catManager.SendCatCommandAsync("FA", new object[] { freq }, 5);

                mainWindow.frequencyManagement.SetFrequencyUIForBandScope(freq, mainWindow.MainFrequencyTextBlock);
                //mainWindow.frequencyManagement.SetFrequencyUI(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, freq, mainWindow.MainFrequencyTextBlock);

                await mainWindow._catManager.SendCatCommandAsync("RM", new object[] { (int)MeterTypes.DependsOnFrontPanelMETER }, 5);

                if (mainWindow.stationSeek.LastSMeterReading >= levelThreshold)
                {
                    // Handle threshold detection here
                }

                if (!(freq == startFreq))
                {
                    //85-135 height of active band span scope 50px
                    Double height = Convert.ToDouble(MainWindow.GetSMeterIntegerForBandScope(FT891S_CatManager.currentRadioState.CurrentMeterReading));

                    height += FunctionMenuClass.FunctionMenuMinMaxScaleTypeList[FunctionMenu.Level].currentValue;

                    Rectangle r = new Rectangle { Width = SimulatedWaterfall.currentFrequencySpanRectangleWidth, Height = height, Fill = new SolidColorBrush(Colors.DodgerBlue) };

                    bandScopeCanvas.Children.Add(r);

                    Canvas.SetLeft(r, canvasCurrentPosition - 14);
                    Canvas.SetBottom(r, 0);

                    // 3. Move the canvas position by the dynamically scaled pixel step
                    if (canvasCurrentPosition < canvasFullRightPos)
                    {
                        canvasCurrentPosition += pixelStep;
                    }
                    else
                    {
                        break; // Stop if we physically run out of canvas space
                    }
                }

                if (freq == endFreq)
                {
                    Canvas.SetLeft(canvas, canvasCurrentPosition);
                }
            }

            await mainWindow._catManager.SendCatCommandAsync("FA", new object[] { FT891S_CatManager.currentRadioState.VfoALastFrequency }, mainWindow._catManager.OutGoingDataLoopDelay);

            await mainWindow._catManager.SendCatCommandAsync("RG", new object[] { 0, rfGainBeforeBandScopeScan }, mainWindow._catManager.OutGoingDataLoopDelay);

            //await mainWindow._catManager.SendCatCommandAsync("AG", new object[] { 0, FT891S_CatManager.currentRadioState.AFGain }, mainWindow._catManager.OutGoingDataLoopDelay);

            mainWindow._catManager.StartOutgoingDataLoop();

            SweepActive = false;

            if (RigMode != RigModes.FM)
            {
                await mainWindow._catManager.SendCatCommandAsync("RG", new object[] { 0, rfGainBeforeBandScopeScan }, mainWindow._catManager.OutGoingDataLoopDelay);
                //await mainWindow._catManager.SendCatCommandAsync("AG", new object[] { 0, FT891S_CatManager.currentRadioState.AFGain }, mainWindow._catManager.OutGoingDataLoopDelay);
            }
            else
                mainWindow.fT891S_SerialPort.SendCAT(mainWindow.fT891S_SerialPort._port, "SQ015");

            await Task.Delay(20);
        }
    }
}