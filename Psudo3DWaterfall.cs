using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace YAESU_FT_891_Front_End
{
    public class Psudo3DWaterfall
    {
        MainWindow mainWindow;
        Image WaterfallImage;

        public WriteableBitmap Bitmap { get; private set; }
        private int _bitmapWidth = 600;
        private int _bitmapHeight = 60;

        // Flat 1D array representing the pixel buffer (Width * Height)
        private int[] _pixelBuffer;
        private int _stride;

        private List<byte[]> _history = new List<byte[]>();
        private const int MaxHistory = 50;

        // --- High-Speed CAT Tracking Mechanics ---
        public long StartFrequencyHz { get; set; } = 14239000; // Default: 20m band start
        public long FrequencyStepHz { get; set; } = 1000;    // 1 kHz steps
        public int TotalScanSteps { get; private set; } = 64;   // Matches 64 cmds/sec limit

        private int _currentScanIndex = 0;
        private byte[] _liveSweepArray;

        public bool IsSweepComplete => _currentScanIndex == 0 && _history.Count > 0;

        public Psudo3DWaterfall(MainWindow mainWindow, Image WaterfallImage)
        {
            this.mainWindow = mainWindow;
            this.WaterfallImage = WaterfallImage;

            Bitmap = new WriteableBitmap(_bitmapWidth, _bitmapHeight, 96, 96, PixelFormats.Bgra32, null);
            WaterfallImage.Source = Bitmap;

            _pixelBuffer = new int[_bitmapWidth * _bitmapHeight];
            _stride = _bitmapWidth;

            _liveSweepArray = new byte[TotalScanSteps];
        }

        public long GetNextTargetFrequency()
        {
            return StartFrequencyHz + (_currentScanIndex * FrequencyStepHz);
        }

        public void ProcessMeterReading(int rawMeterValue)
        {
            // 1. Double check bounds to prevent overflow color blocks
            // Ensures a standard 0-255 scaling without capping out instantly at bright red
            byte scaledHeight = (byte)Math.Max(0, Math.Min(255, rawMeterValue * 2.5));

            // 2. Safely inject into the specific column we are sweeping
            if (_currentScanIndex >= 0 && _currentScanIndex < _liveSweepArray.Length)
            {
                _liveSweepArray[_currentScanIndex] = scaledHeight;
            }

            _currentScanIndex++;

            // 3. Once we hit 64 discrete samples, push the line down the screen
            if (_currentScanIndex >= TotalScanSteps)
            {
                _currentScanIndex = 0;

                // Perform a deep copy to decouple memory references
                byte[] snapshot = new byte[TotalScanSteps];
                Buffer.BlockCopy(_liveSweepArray, 0, snapshot, 0, TotalScanSteps);

                AddSweepToHistory(snapshot);

                // CRITICAL FIX: Clear the live array to base noise level 
                // So a single red peak doesn't bleed horizontally into subsequent scans
                for (int i = 0; i < _liveSweepArray.Length; i++)
                {
                    _liveSweepArray[i] = 10; // Low blue noise floor default
                }
            }
        }

        public void AddSweepToHistory(byte[] newSweep)
        {
            // Insert new data at index 0 (Top row of screen)
            _history.Insert(0, newSweep);

            // Limit history memory structure directly to the physical screen height
            while (_history.Count > _bitmapHeight)
            {
                _history.RemoveAt(_history.Count - 1);
            }
        }

        // ==========================================
        //   RENDERING & GRAPHICS ENGINE (FIXED)
        // ==========================================

        public void Render3DWaterfall()
        {
            if (_history == null || _history.Count == 0) return;

            // 1. Clear the buffer manually (Replaces Array.Fill for .NET Framework 4.8)
            int baseNoiseColor = unchecked((int)0xFF050515);
            for (int i = 0; i < _pixelBuffer.Length; i++)
            {
                _pixelBuffer[i] = baseNoiseColor;
            }

            // 2. Loop through each available row of history (up to our 60-pixel canvas height)
            int visibleRows = Math.Min(_history.Count, _bitmapHeight);

            for (int y = 0; y < visibleRows; y++)
            {
                byte[] sweep = _history[y]; // y=0 is the newest sweep, y=59 is oldest

                for (int x = 0; x < _bitmapWidth; x++)
                {
                    // Map screen X (0 to 599) smoothly to our 64 data points (0 to 63)
                    double horizontalPct = (double)x / (_bitmapWidth - 1);
                    double dataPosition = horizontalPct * (TotalScanSteps - 1);

                    int indexLow = (int)Math.Floor(dataPosition);
                    int indexHigh = (int)Math.Ceiling(dataPosition);
                    double t = dataPosition - indexLow;

                    // Smooth out the 64 steps across the 600px width via linear interpolation
                    byte signalStrength = (byte)((1.0 - t) * sweep[indexLow] + t * sweep[indexHigh]);

                    // 3. Dynamic Heatmap Palette Generation (Flat 2D Spectrograph Style)
                    int r = 0, g = 0, b = 0;

                    if (signalStrength < 64) // Weak signals / Noise floor: Deep Blue
                    {
                        b = (int)(signalStrength * 2.5);
                    }
                    else if (signalStrength < 128) // Medium signals: Green to Cyan
                    {
                        g = (int)((signalStrength - 64) * 3.5);
                        b = 255 - g;
                    }
                    else if (signalStrength < 192) // Strong signals: Yellow to Orange
                    {
                        r = (int)((signalStrength - 128) * 4);
                        g = 255;
                    }
                    else // Peak Overload signals: Bright Red/White
                    {
                        r = 255;
                        g = 255 - (int)((signalStrength - 192) * 4);
                        b = (int)((signalStrength - 192) * 2);
                    }

                    // Pack the components into standard BGRA format
                    int color = (255 << 24) | (r << 16) | (g << 8) | b;

                    // 4. Write directly to the pixel buffer
                    _pixelBuffer[y * _stride + x] = color;
                }
            }

            // 5. Blast the safe managed buffer into the UI graphics memory
            Int32Rect rect = new Int32Rect(0, 0, _bitmapWidth, _bitmapHeight);
            Bitmap.WritePixels(rect, _pixelBuffer, _bitmapWidth * 4, 0);
        }
    }
}