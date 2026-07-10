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
        private int _bitmapHeight = 59;

        private int[] _pixelBuffer;
        private int _stride;

        private List<byte[]> _history = new List<byte[]>();

        public Psudo3DWaterfall(MainWindow mainWindow, Image WaterfallImage)
        {
            this.mainWindow = mainWindow;
            this.WaterfallImage = WaterfallImage;

            Bitmap = new WriteableBitmap(_bitmapWidth, _bitmapHeight, 96, 96, PixelFormats.Bgra32, null);
            WaterfallImage.Source = Bitmap;

            _pixelBuffer = new int[_bitmapWidth * _bitmapHeight];
            _stride = _bitmapWidth;

            ClearWaterfall();
        }

        public void AddSweepToHistory(byte[] newSweep)
        {
            _history.Insert(0, newSweep);

            while (_history.Count > _bitmapHeight)
            {
                _history.RemoveAt(_history.Count - 1);
            }
        }

        public void ClearWaterfall()
        {
            _history.Clear();

            int transparentColor = 0x00000000;
            for (int i = 0; i < _pixelBuffer.Length; i++)
            {
                _pixelBuffer[i] = transparentColor;
            }

            Int32Rect rect = new Int32Rect(0, 0, _bitmapWidth, _bitmapHeight);
            Bitmap.WritePixels(rect, _pixelBuffer, _bitmapWidth * 4, 0);
        }

        // Pre-calculated color palette: Index 0 is weak (dark blue), Index 255 is hot (red/white)
        private static readonly int[] ColorPalette = InitializePalette();

        private static int[] InitializePalette()
        {
            int[] palette = new int[256];
            for (int i = 0; i < 256; i++)
            {
                int r = 0, g = 0, b = 0;
                int alpha = (int)Math.Min(255, i * 4); // Faint signals stay slightly transparent, strong signals solid

                if (i < 85) // 0 to 84: Weak signals (Dark Blue to Cyan)
                {
                    r = 0;
                    g = (int)(i * 3.0);                 // Green ramps up to 255
                    b = 50 + (int)(i * 2.4);            // Blue starts at 50, ramps to 255
                }
                else if (i < 170) // 85 to 169: Medium signals (Cyan to Green to Yellow)
                {
                    int localIdx = i - 85;
                    r = (int)(localIdx * 3.0);          // Red ramps up to 255
                    g = 255;                            // Green stays maxed
                    b = 255 - (int)(localIdx * 3.0);    // Blue drops down to 0
                }
                else // 170 to 255: Strong signals (Yellow to Bright Red/White)
                {
                    int localIdx = i - 170;
                    r = 255;                            // Red stays maxed
                    g = 255 - (int)(localIdx * 3.0);    // Green drops down
                    b = (int)(localIdx * 3.0);          // Blue ramps back up at the very peak to make it white
                }

                // Clip values manually for .NET Framework 4.8 safety
                r = Math.Max(0, Math.Min(255, r));
                g = Math.Max(0, Math.Min(255, g));
                b = Math.Max(0, Math.Min(255, b));

                // Pack into BGRA integer format
                palette[i] = (alpha << 24) | (r << 16) | (g << 8) | b;
            }
            return palette;
        }

        // FT-710 Style Speckled Rendering Engine (Nearest Neighbor)
        public void Render3DWaterfall()
        {
            if (_history == null || _history.Count == 0) return;

            int baseNoiseColor = 0x00000000;
            for (int i = 0; i < _pixelBuffer.Length; i++)
            {
                _pixelBuffer[i] = baseNoiseColor;
            }

            int visibleRows = Math.Min(_history.Count, _bitmapHeight);

            for (int y = 0; y < visibleRows; y++)
            {
                byte[] sweep = _history[y];

                if (sweep == null || sweep.Length == 0) continue;

                for (int x = 0; x < _bitmapWidth; x++)
                {
                    // Map the visual pixel X coordinate to the exact sample length
                    double horizontalPct = (double)x / (_bitmapWidth - 1);
                    double dataPosition = horizontalPct * (sweep.Length - 1);

                    // FT-710 FIXED LOOK: Use Math.Round to grab the nearest exact raw data sample.
                    // This eliminates the smooth blending effect and leaves sharp, distinct dots/speckles.
                    int nearestIndex = (int)Math.Round(dataPosition);
                    nearestIndex = Math.Max(0, Math.Min(sweep.Length - 1, nearestIndex));

                    byte signalStrength = sweep[nearestIndex];

                    int r = 0, g = 0, b = 0;
                    int alpha = 0;

                    double intensity = (double)signalStrength / 255.0;

                    if (signalStrength > 0)
                    {
                        // 1. Alpha (Transparency): We want a distinct "dot". 
                        // If there is any signal at all, make it mostly solid so it snaps into a dot.
                        alpha = 100 + (int)((signalStrength / 255.0) * 155);
                        if (alpha > 255) alpha = 255;

                        // 2. Base Dodger Blue Mix: R:30, G:144, B:255
                        // Weak signals will be darker blue, strong signals will be bright neon blue.
                        double factor = signalStrength / 255.0;

                        r = (int)(factor * 30);    // Max Red is 30 (keeps it deep blue, never pink!)
                        g = (int)(factor * 144);   // Max Green is 144
                        b = 100 + (int)(factor * 155); // Blue starts bright (100) and maxes out at 255

                        // 3. .NET 4.8 Safety Check
                        r = Math.Max(0, Math.Min(255, r));
                        g = Math.Max(0, Math.Min(255, g));
                        b = Math.Max(0, Math.Min(255, b));
                    }

                    int color = (alpha << 24) | (r << 16) | (g << 8) | b;
                    _pixelBuffer[y * _stride + x] = color;
                }
            }

            Int32Rect rect = new Int32Rect(0, 0, _bitmapWidth, _bitmapHeight);
            Bitmap.WritePixels(rect, _pixelBuffer, _bitmapWidth * 4, 0);
        }
    }
}