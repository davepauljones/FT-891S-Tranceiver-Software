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

        public Sprite(MainWindow mainWindow, Canvas historyCanvas, Canvas bandScopeCanvas)
        {
            this.mainWindow = mainWindow;
            this.historyCanvas = historyCanvas;
            this.bandScopeCanvas = bandScopeCanvas;

            MaxHeight = mainWindow.WaterfallCanvas.Height;
        }

        public void GenerateBandScopeSprite(byte signalStrength, double xCenter, double heightMultiplier)
        {
            ClearWaterfallArea(bandScopeCanvas);
            
            // 2. Generate the single line segment
            Polygon lineSegment = GenerateCurrentFrequencyPolygon(Convert.ToByte(MainWindow.GetSMeterIntegerForBandScope(signalStrength)), heightMultiplier);

            // 3. Position the segment on the MAIN canvas
            // We position X so that the center of the line aligns with xCenter
            //double actualWidth = signalStrength * heightMultiplier;
            Canvas.SetLeft(lineSegment, xCenter - (SimulatedWaterfall.currentBandScopeSpriteRectangleWidth / 2));
            Canvas.SetBottom(lineSegment, 0);

            // 4. Add to view
            bandScopeCanvas.Children.Add(lineSegment);

            if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
            {
                Console.WriteLine($"SignalStrength = {signalStrength} at Y = {ypos}");
            }
        }
        public void GenerateHistorySprite(byte signalStrength, double xCenter, double widthMultiplier)
        {
            // 1. Wrap around if we hit the bottom of the waterfall area
            if (ypos >= MaxHeight)
            {
                ClearWaterfallArea(historyCanvas);
                ypos = 0;
            }

            // 2. Generate the single line segment
            Polygon lineSegment = GenerateHistoryPolygon(signalStrength, widthMultiplier);

            // 3. Position the segment on the MAIN canvas
            // We position X so that the center of the line aligns with xCenter
            double actualWidth = signalStrength * widthMultiplier;
            Canvas.SetLeft(lineSegment, xCenter - (actualWidth / 2));
            Canvas.SetTop(lineSegment, ypos);

            // 4. Add to view
            historyCanvas.Children.Add(lineSegment);

            // 5. Move down for the next signal sweep
            ypos += LineHeight;

            if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
            {
                Console.WriteLine($"SignalStrength = {signalStrength} at Y = {ypos}");
            }
        }

        private Polygon GenerateHistoryPolygon(byte signalStrength, double widthMultiplier)
        {
            Polygon polygon = new Polygon();

            // Map signal strength to actual rendering width
            double calculatedWidth = signalStrength * widthMultiplier;

            polygon.Fill = new SolidColorBrush(Colors.DodgerBlue);

            // Optional: Uncomment this if you want stronger signals to be brighter
            polygon.Opacity = ByteToOpacity(signalStrength);

            // Simple rectangle definition starting from (0,0) locally
            polygon.Points = new PointCollection
            {
                new Point(0, 0),
                new Point(calculatedWidth, 0),
                new Point(calculatedWidth, LineHeight),
                new Point(0, LineHeight)
            };

            return polygon;
        }
        private Polygon GenerateCurrentFrequencyPolygon(byte signalStrength, double heightMultiplier)
        {
            Polygon polygon = new Polygon();

            // Map signal strength to actual rendering width
            double calculatedHeight = signalStrength * heightMultiplier;

            polygon.Fill = new SolidColorBrush(Colors.DodgerBlue);

            // Optional: Uncomment this if you want stronger signals to be brighter
            polygon.Opacity = ByteToOpacity(signalStrength);

            // Simple rectangle definition starting from (0,0) locally
            polygon.Points = new PointCollection
            {
                new Point(0, 0),
                new Point(0, calculatedHeight),
                new Point(SimulatedWaterfall.currentBandScopeSpriteRectangleWidth ,calculatedHeight),
                new Point(SimulatedWaterfall.currentBandScopeSpriteRectangleWidth, 0)
            };

            return polygon;
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