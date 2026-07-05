using FT891S_CatControl;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using static YAESU_FT_891_Front_End.MyStructs;

namespace YAESU_FT_891_Front_End
{
    public class Sprite
    {
        private MainWindow mainWindow;
        private Canvas historyCanvas;
        private Canvas bandScopeCanvas;
        private double ypos = 0;
        private const double LineHeight = 1; // Height of each waterfall step
        private double MaxHeight = 54; // Wrap-around point

        private byte _lastSignalStrength = 0;
        private double _rippleIntensity = 1.0; // Acts as our kinetic energy tracker

        public Sprite(MainWindow mainWindow, Canvas historyCanvas, Canvas bandScopeCanvas)
        {
            this.mainWindow = mainWindow;
            this.historyCanvas = historyCanvas;
            this.bandScopeCanvas = bandScopeCanvas;

            MaxHeight = mainWindow.WaterfallCanvas.Height;
        }

        public void GenerateCombinedSignalSprite(byte signalStrength, double xCenter, double maxSpanWidth, double heightMultiplier)
        {
            // 1. Clear old elements from UI
            bandScopeCanvas.Children.Clear();
            ClearWaterfallArea(bandScopeCanvas);

            if (signalStrength == 0)
            {
                _lastSignalStrength = 0;
                _rippleIntensity = 0.0;
                return;
            }

            // --- HYSTERESIS & MOMENTUM ENGINE ---
            if (signalStrength != _lastSignalStrength)
            {
                // Signal changed! Instantly inject energy to cause a violent burst
                _rippleIntensity = 1.0;
            }
            else
            {
                // Signal is static! Cool down and decay the ripple intensity exponentially
                // Adjust 0.85 to change decay speed (lower = calms down faster)
                _rippleIntensity *= 0.85;

                // Prevent it from micro-calculating infinitely near zero
                if (_rippleIntensity < 0.01) _rippleIntensity = 0.0;
            }

            // Save current read for the next cycle comparison
            _lastSignalStrength = signalStrength;

            // 2. HEIGHT & LAYER CALCULATIONS
            const double MaxCanvasHeight = 60.0;
            const double BlockHeight = 3.0;

            double signalRatio = (double)signalStrength / 255.0;
            double acceleratedRatio = Math.Sqrt(signalRatio);
            double totalCalculatedHeight = acceleratedRatio * MaxCanvasHeight;

            int totalBlocksToDraw = (int)Math.Ceiling(totalCalculatedHeight / BlockHeight);

            // System clock for baseline dynamic movement
            double timeOffset = (DateTime.Now.Ticks / (double)TimeSpan.TicksPerMillisecond) * 0.004;

            // 3. RENDER STACK LOOP
            for (int i = 0; i < totalBlocksToDraw; i++)
            {
                double localProgress = totalBlocksToDraw > 1 ? (double)i / (totalBlocksToDraw - 1) : 0.0;

                // --- DAMPENED RIPPLE ENGINE ---
                double baselineScale = 0.85;

                // Raw chaotic frequencies
                double wave1 = Math.Sin((i * 0.73) - timeOffset);
                double wave2 = Math.Sin((i * 1.37) + (timeOffset * 1.5));
                double wave3 = Math.Cos((i * 0.29) - (timeOffset * 0.5));

                // CRITICAL FIX: The entire wave payload is multiplied by our _rippleIntensity tracker.
                // As the signal remains static, this noise modifier drops to 0, locking the column flat.
                double combinedRandomNoise = ((wave1 * 0.12) + (wave2 * 0.08) + (wave3 * 0.10)) * _rippleIntensity;

                double widthScale = baselineScale + combinedRandomNoise;

                double baseWidth = signalRatio * maxSpanWidth;
                double layerWidth = Math.Max(4.0, baseWidth * widthScale);

                // B. REACTIVE BURST COLORING
                Color blockColor;
                double blueCeiling = 0.35 * (1.0 - (signalRatio * 0.2));
                double goldCeiling = 0.65 * (1.0 - (signalRatio * 0.2));
                double orangeCeiling = 0.85 * (1.0 - (signalRatio * 0.1));

                if (localProgress < blueCeiling)
                {
                    blockColor = Colors.DodgerBlue;
                }
                else if (localProgress < goldCeiling)
                {
                    blockColor = Colors.Gold;
                }
                else if (localProgress < orangeCeiling)
                {
                    blockColor = Colors.DarkOrange;
                }
                else
                {
                    blockColor = Colors.Red;
                }

                // C. CREATE POLYGON SEGMENT
                Polygon blockPolygon = new Polygon
                {
                    Fill = new SolidColorBrush(blockColor),
                    Opacity = signalRatio,
                    Points = new PointCollection
            {
                new Point(0, 0),
                new Point(layerWidth, 0),
                new Point(layerWidth, BlockHeight),
                new Point(0, BlockHeight)
            }
                };

                // 4. POSITION THE SEGMENT
                Canvas.SetLeft(blockPolygon, xCenter - (layerWidth / 2.0));

                double currentLayerBottom = i * BlockHeight;
                Canvas.SetBottom(blockPolygon, currentLayerBottom);

                bandScopeCanvas.Children.Add(blockPolygon);
            }
        }

        private void ClearWaterfallArea(Canvas canvas)
        {
            // Clears only the Polygons added by this visualizer, leaving other UI elements intact
            for (int i = canvas.Children.Count - 1; i >= 0; i--)
            {
                if (canvas.Children[i] is Polygon)
                {
                    canvas.Children.RemoveAt(i);
                }
            }
        }

        public static double ByteToOpacity(byte value)
        {
            const double minOpacity = 0.4;
            const double maxOpacity = 1.0;
            return minOpacity + (value / 255.0) * (maxOpacity - minOpacity);
        }
    }
}