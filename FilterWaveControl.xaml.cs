using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace YAESU_FT_891_Front_End
{
    public partial class FilterWaveControl : UserControl
    {
        // =========================================================================
        // DEPENDENCY PROPERTIES
        // =========================================================================

        // Handle Value Properties (Values range from 0 to 100, or 0 to 15 for DNR)
        public static readonly DependencyProperty NbValueProperty =
            DependencyProperty.Register("NbValue", typeof(int), typeof(FilterWaveControl), new PropertyMetadata(0));

        public static readonly DependencyProperty WidthValueProperty =
            DependencyProperty.Register("WidthValue", typeof(int), typeof(FilterWaveControl), new PropertyMetadata(0));

        public static readonly DependencyProperty DnrValueProperty =
            DependencyProperty.Register("DnrValue", typeof(int), typeof(FilterWaveControl), new PropertyMetadata(0));

        // Binary Toggle Properties (True = On, False = Off)
        public static readonly DependencyProperty DnfEnabledProperty =
            DependencyProperty.Register("DnfEnabled", typeof(bool), typeof(FilterWaveControl), new PropertyMetadata(false));

        public static readonly DependencyProperty ApfEnabledProperty =
            DependencyProperty.Register("ApfEnabled", typeof(bool), typeof(FilterWaveControl), new PropertyMetadata(false));

        public static readonly DependencyProperty ContourEnabledProperty =
            DependencyProperty.Register("ContourEnabled", typeof(bool), typeof(FilterWaveControl), new PropertyMetadata(false));

        // Standard C# Property Wrappers
        public int NbValue
        {
            get { return (int)GetValue(NbValueProperty); }
            set { SetValue(NbValueProperty, value); }
        }

        public int WidthValue
        {
            get { return (int)GetValue(WidthValueProperty); }
            set { SetValue(WidthValueProperty, value); }
        }

        public int DnrValue
        {
            get { return (int)GetValue(DnrValueProperty); }
            set { SetValue(DnrValueProperty, value); }
        }

        public bool DnfEnabled
        {
            get { return (bool)GetValue(DnfEnabledProperty); }
            set { SetValue(DnfEnabledProperty, value); }
        }

        public bool ApfEnabled
        {
            get { return (bool)GetValue(ApfEnabledProperty); }
            set { SetValue(ApfEnabledProperty, value); }
        }

        public bool ContourEnabled
        {
            get { return (bool)GetValue(ContourEnabledProperty); }
            set { SetValue(ContourEnabledProperty, value); }
        }

        // =========================================================================
        // LAYOUT CONSTANTS & LOGIC
        // =========================================================================

        private const double CanvasHeight = 102; // Shrunk wave canvas height
        private const double CenterY = 51;       // Middle baseline zero-point

        public FilterWaveControl()
        {
            InitializeComponent();

            // Draw the initial straight-line waveform once the control loads safely
            Loaded += (s, e) => UpdateWaveform();
        }

        // Left Handle: Noise Blanker (Drags strictly UP from CenterY)
        private void NbThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double currentTop = Canvas.GetTop(NbThumb);
            double newTop = Math.Max(5, Math.Min(CenterY, currentTop + e.VerticalChange));
            Canvas.SetTop(NbThumb, newTop);

            // Map vertically to a standard 0 to 100 integer range
            NbValue = (int)((1 - (newTop / CenterY)) * 100);

            UpdateWaveform();
        }

        // Center Handle: Width / Notch Valley (Drags strictly DOWN from CenterY)
        private void WidthThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double currentTop = Canvas.GetTop(WidthThumb);
            double newTop = Math.Max(CenterY, Math.Min(CanvasHeight - 12, currentTop + e.VerticalChange));
            Canvas.SetTop(WidthThumb, newTop);

            // Map vertically to a standard 0 to 100 integer range
            WidthValue = (int)(((newTop - CenterY) / (CanvasHeight - 12 - CenterY)) * 100);

            UpdateWaveform();
        }

        // Right Handle: DNR Hill (FIXED CASING METHOD: Matches DnrThumb_DragDelta in XAML)
        private void DnrThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double currentTop = Canvas.GetTop(DnrThumb);
            double newTop = Math.Max(5, Math.Min(CenterY, currentTop + e.VerticalChange));
            Canvas.SetTop(DnrThumb, newTop);

            // Map vertically to match the FT-891's 15 distinct internal noise algorithms
            double pct = 1 - (newTop / CenterY);
            DnrValue = (int)Math.Round(pct * 15);

            UpdateWaveform();
        }

        // =========================================================================
        // CURVE GEOMETRY DRAWING ENGINE
        // =========================================================================
        private void UpdateWaveform()
        {
            // Null check to prevent WPF designer crashes during initialization
            if (NbThumb == null || WidthThumb == null || DnrThumb == null) return;

            // Target the absolute center pixels of our 12px circular handles (+6px offset)
            double nbX = Canvas.GetLeft(NbThumb) + 6;
            double nbY = Canvas.GetTop(NbThumb) + 6;

            double wX = Canvas.GetLeft(WidthThumb) + 6;
            double wY = Canvas.GetTop(WidthThumb) + 6;

            double dnrX = Canvas.GetLeft(DnrThumb) + 6;
            double dnrY = Canvas.GetTop(DnrThumb) + 6;

            // Bezier Segment 1: Fixed edge anchor line connecting up to the NB Peak
            Segment1.Point1 = new Point(nbX * 0.4, CenterY);
            Segment1.Point2 = new Point(nbX * 0.7, nbY);
            Segment1.Point3 = new Point(nbX, nbY);

            // Bezier Segment 2: Transition line coming off the NB Peak dropping into the Width Valley
            double midPointX1 = nbX + ((wX - nbX) * 0.5);
            Segment2.Point1 = new Point(nbX + 40, nbY);
            Segment2.Point2 = new Point(midPointX1 - 20, wY);
            Segment2.Point3 = new Point(wX, wY);

            // Bezier Segment 3: Transition line rising out of the Width Valley onto the DNR Hill
            double midPointX2 = wX + ((dnrX - wX) * 0.5);
            Segment3.Point1 = new Point(wX + 20, wY);
            Segment3.Point2 = new Point(midPointX2 - 40, dnrY);
            Segment3.Point3 = new Point(dnrX, dnrY);

            // Final tail line bleeding smoothly off the right side of the canvas
            EndSegment.Point = new Point(643, CenterY);
        }
    }
}