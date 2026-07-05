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
                        // Set alpha transparency to ramp up quickly so faint signals form distinct dots
                        alpha = (int)(intensity * 5.0 * 255);
                        if (alpha > 255) alpha = 255;

                        // Color mapping targeting Dodger Blue (R:30, G:144, B:255)
                        r = (int)(intensity * 30);
                        g = (int)(intensity * 144);

                        b = 30 + (int)(intensity * 225);
                        if (b > 255) b = 255;
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