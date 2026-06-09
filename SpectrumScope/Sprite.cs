using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace YAESU_FT_891_Front_End
{
    public class Sprite
    {
        private MainWindow mainWindow;
        private Canvas canvas;
        private double ypos = 0;
        private const double LineHeight = 1; // Height of each waterfall step
        private const double MaxHeight = 114; // Wrap-around point

        public Sprite(MainWindow mainWindow, Canvas canvas)
        {
            this.mainWindow = mainWindow;
            this.canvas = canvas;
        }

        public void GenerateSprite(byte signalStrength, double xCenter, double widthMultiplier)
        {
            // 1. Wrap around if we hit the bottom of the waterfall area
            if (ypos >= MaxHeight)
            {
                ClearWaterfallArea();
                ypos = 0;
            }

            // 2. Generate the single line segment
            Polygon lineSegment = GeneratePolygon(signalStrength, widthMultiplier);

            // 3. Position the segment on the MAIN canvas
            // We position X so that the center of the line aligns with xCenter
            double actualWidth = signalStrength * widthMultiplier;
            Canvas.SetLeft(lineSegment, xCenter - (actualWidth / 2));
            Canvas.SetTop(lineSegment, ypos);

            // 4. Add to view
            canvas.Children.Add(lineSegment);

            // 5. Move down for the next signal sweep
            ypos += LineHeight;

            Console.WriteLine($"SignalStrength = {signalStrength} at Y = {ypos}");
        }

        private Polygon GeneratePolygon(byte signalStrength, double widthMultiplier)
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

        private void ClearWaterfallArea()
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