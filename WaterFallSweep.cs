using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using static YAESU_FT_891_Front_End.MyStructs;
using static YAESU_FT_891_Front_End.RigStateChanges;

namespace YAESU_FT_891_Front_End
{
    public class WaterFallSweep
    {
        MainWindow mainWindow;
        Canvas canvas;
        Double canvasFullLeftPos = 14;
        Double canvasFullRightPos = 613;
        public WaterFallSweep(MainWindow mainWindow, Canvas canvas)
        {
            this.mainWindow = mainWindow;
            this.canvas = canvas;
        }

        public async void Sweep(long startFreq, long endFreq, long step, int levelThreshold)
        {
            mainWindow.fT891S_SerialPort.StopSerialLoop();

            mainWindow.yAESU_FT_891_CAT_Dictionary.SetRfGain(mainWindow.fT891S_SerialPort._port, 30);
            await Task.Delay(20);

            double canvasCurrentPosition = canvasFullLeftPos;
            double totalCanvasWidth = canvasFullRightPos - canvasFullLeftPos;

            // 1. KEEP YOUR VARIABLE HERE: Fetch the actual Hz value of the currently selected span
            long totalSpanHz = mainWindow.simulatedWaterfall.GetSpanHz(SimulatedWaterfall.currentFrequencySpan);

            // Safeguard against division by zero
            if (totalSpanHz <= 0) totalSpanHz = 50000;

            // 2. DYNAMIC MATH: Calculate pixel step using explicit double casting
            // This forces C# to use decimal math so it won't resolve to 0 or overshoot
            double pixelStep = ((double)step / (double)totalSpanHz) * totalCanvasWidth;


            for (long freq = startFreq; freq <= endFreq; freq += step)
            {
                Canvas.SetLeft(canvas, canvasCurrentPosition);

                mainWindow.yAESU_FT_891_CAT_Dictionary.FreqA(mainWindow.fT891S_SerialPort._port, freq);
                await Task.Delay(10);

                mainWindow.yAESU_FT_891_CAT_Dictionary.SMeter(mainWindow.fT891S_SerialPort._port, SMeters.S);
                await Task.Delay(20);

                if (mainWindow.stationSeek.LastSMeterReading >= levelThreshold)
                {
                    // Handle threshold detection here
                }

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

            mainWindow.yAESU_FT_891_CAT_Dictionary.SetRfGain(mainWindow.fT891S_SerialPort._port, 0);
            await Task.Delay(20);

            mainWindow.fT891S_SerialPort.StartSerialLoop();

            if (RigMode != RigModes.FM)
                mainWindow.yAESU_FT_891_CAT_Dictionary.SetRfGain(mainWindow.fT891S_SerialPort._port, 0);
            else
                mainWindow.fT891S_SerialPort.SendCAT(mainWindow.fT891S_SerialPort._port, "SQ015");

            await Task.Delay(20);
        }
    }
}