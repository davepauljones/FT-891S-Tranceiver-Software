using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace YAESU_FT_891_Front_End
{
    public partial class FilterWaveControl : UserControl
    {
        private const double CanvasHeight = 102;
        private const double CenterY = 51;
        private bool _isUpdatingProgrammatically = false;

        public int NbValue { get; private set; }
        public int WidthValue { get; private set; }
        public int DnrValue { get; private set; }

        public event Action<string, object> UIValueChanged;

        public FilterWaveControl()
        {
            InitializeComponent();
            Loaded += (s, e) => {
                UpdateThumbPositions();
                UpdateWaveform();
            };
        }

        public void SetNbValue(int value)
        {
            NbValue = value;
            NbTextBlock.Text = $"NB: {NbValue}"; // <-- Add this line
            UpdateThumbPositions();
            UpdateWaveform();
        }
        //public void SetNbValue(int value) { NbValue = value; UpdateThumbPositions(); UpdateWaveform(); }
        public void SetWidthValue(int value) { WidthValue = value; UpdateThumbPositions(); UpdateWaveform(); }

        public void SetDnrValue(int value)
        {
            DnrValue = value;
            DNRTextBlock.Text = DnrValue == 0 ? "NR: OFF" : $"NR: {DnrValue:D2}";
            UpdateThumbPositions();
            UpdateWaveform();
        }
        //public void SetDnrValue(int value) { DnrValue = value; UpdateThumbPositions(); UpdateWaveform(); }

        public void SetDnfEnabled(bool value) { _isUpdatingProgrammatically = true; AutoDnfBtn.IsChecked = value; _isUpdatingProgrammatically = false; }
        public void SetApfEnabled(bool value) { _isUpdatingProgrammatically = true; CwApfBtn.IsChecked = value; _isUpdatingProgrammatically = false; }
        public void SetContourEnabled(bool value) { _isUpdatingProgrammatically = true; ContourBtn.IsChecked = value; _isUpdatingProgrammatically = false; }

        private void UpdateThumbPositions()
        {
            if (NbThumb == null || WidthThumb == null || DnrThumb == null) return;

            _isUpdatingProgrammatically = true;

            // Fix: Account for the 5-pixel top margin gap
            double travelSpaceNb = CenterY - 5;
            double nbTop = CenterY - ((NbValue / 10.0) * travelSpaceNb);
            Canvas.SetTop(NbThumb, Math.Max(5, Math.Min(CenterY, nbTop)));

            double widthTop = CenterY + ((WidthValue / 100.0) * (CanvasHeight - 12 - CenterY));
            Canvas.SetTop(WidthThumb, Math.Max(CenterY, Math.Min(CanvasHeight - 12, widthTop)));

            double travelSpaceDnr = CenterY - 5;
            double dnrTop = CenterY - ((DnrValue / 15.0) * travelSpaceDnr);
            Canvas.SetTop(DnrThumb, Math.Max(5, Math.Min(CenterY, dnrTop)));

            _isUpdatingProgrammatically = false;
        }

        private void NbThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newTop = Math.Max(5, Math.Min(CenterY, Canvas.GetTop(NbThumb) + e.VerticalChange));
            Canvas.SetTop(NbThumb, newTop);

            // Fix: Calculate percentage based on actual 5 to CenterY travel space
            double travelSpace = CenterY - 5;
            double pct = (CenterY - newTop) / travelSpace;

            NbValue = (int)Math.Round(pct * 10);

            // Fix: Hard clamps to prevent rounding anomalies at the edges
            if (newTop <= 5) NbValue = 10;
            if (newTop >= CenterY) NbValue = 0;

            NbTextBlock.Text = $"NB: {NbValue}";

            UpdateWaveform();
            UIValueChanged?.Invoke("NB", NbValue);
        }

        private void WidthThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newTop = Math.Max(CenterY, Math.Min(CanvasHeight - 12, Canvas.GetTop(WidthThumb) + e.VerticalChange));
            Canvas.SetTop(WidthThumb, newTop);
            WidthValue = (int)(((newTop - CenterY) / (CanvasHeight - 12 - CenterY)) * 100);
            UpdateWaveform();
            UIValueChanged?.Invoke("WD", WidthValue);
        }

        private void DnrThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newTop = Math.Max(5, Math.Min(CenterY, Canvas.GetTop(DnrThumb) + e.VerticalChange));
            Canvas.SetTop(DnrThumb, newTop);

            // Calculate percentage based on the actual allowed travel space (5 to CenterY)
            double travelSpace = CenterY - 5;
            double pct = (CenterY - newTop) / travelSpace;

            DnrValue = (int)Math.Round(pct * 15);

            // Hard clamps to fix edge cases caused by fractional pixel boundaries
            if (newTop <= 5) DnrValue = 15;
            if (newTop >= CenterY) DnrValue = 0;

            DNRTextBlock.Text = DnrValue == 0 ? "NR: OFF" : $"NR: {DnrValue:D2}";

            UpdateWaveform();
            UIValueChanged?.Invoke("NR", DnrValue);
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingProgrammatically) return;
            if (sender is ToggleButton btn)
                UIValueChanged?.Invoke(btn.Content.ToString(), true);
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingProgrammatically) return;
            if (sender is ToggleButton btn)
                UIValueChanged?.Invoke(btn.Content.ToString(), false);
        }

        private void UpdateWaveform()
        {
            // Safety check to ensure all UI elements are fully loaded
            if (NbThumb == null || WidthThumb == null || DnrThumb == null ||
                Segment1 == null || Segment2 == null || Segment3 == null ||
                WidthRightSegment == null || EndSegment == null || DnrRightSegment == null)
                return;

            // Center point of the canvas vertical travel reference
            const double CenterY = 51.0;

            // Fetch the exact layout center coordinates of each physical interactive handle (+6px offset for the 12x12 thumb size)
            double nbX = Canvas.GetLeft(NbThumb) + 6;
            double nbY = Canvas.GetTop(NbThumb) + 6;
            double wX = Canvas.GetLeft(WidthThumb) + 6;
            double wY = Canvas.GetTop(WidthThumb) + 6;
            double dnrX = Canvas.GetLeft(DnrThumb) + 6;
            double dnrY = Canvas.GetTop(DnrThumb) + 6;

            // Hardcoded panel boundary splits matching your background layout exactly
            double border0X = 0.0;
            double border1X = 213.0; // Updated
            double border2X = 427.0; // Updated
            double border3X = 643.0;

            // ================= SECTION 1: NB CURVE (0.0 to 213.3) =================
            // Left Slope: From baseline start up to the exact thumb center apex
            Segment1.Point1 = new Point(border0X + (nbX - border0X) * 0.5, CenterY);
            Segment1.Point2 = new Point(nbX - (nbX - border0X) * 0.1, nbY);
            Segment1.Point3 = new Point(nbX, nbY);

            // Right Slope: From the thumb apex back down to rest exactly on the border line (213.3, 51)
            Segment2.Point1 = new Point(nbX + (border1X - nbX) * 0.1, nbY);
            Segment2.Point2 = new Point(border1X - (border1X - nbX) * 0.5, CenterY);
            Segment2.Point3 = new Point(border1X, CenterY);


            // ================= SECTION 2: WIDTH CURVE (213.3 to 426.6) =================
            // Left Slope: From the first border line down to the inverted thumb center notch valley
            Segment3.Point1 = new Point(border1X + (wX - border1X) * 0.5, CenterY);
            Segment3.Point2 = new Point(wX - (wX - border1X) * 0.1, wY);
            Segment3.Point3 = new Point(wX, wY);

            // Right Slope: From the thumb valley back up to rest exactly on the second border line (426.6, 51)
            WidthRightSegment.Point1 = new Point(wX + (border2X - wX) * 0.1, wY);
            WidthRightSegment.Point2 = new Point(border2X - (border2X - wX) * 0.5, CenterY);
            WidthRightSegment.Point3 = new Point(border2X, CenterY);


            // ================= SECTION 3: DNR CURVE (426.6 to 643.0) =================
            // Left Slope: From the second border line up to the third thumb center noise apex
            EndSegment.Point1 = new Point(border2X + (dnrX - border2X) * 0.5, CenterY);
            EndSegment.Point2 = new Point(dnrX - (dnrX - border2X) * 0.1, dnrY);
            EndSegment.Point3 = new Point(dnrX, dnrY);

            // Right Slope: From the third thumb apex down to finish exactly flush on the right window border (643, 51)
            DnrRightSegment.Point1 = new Point(dnrX + (border3X - dnrX) * 0.1, dnrY);
            DnrRightSegment.Point2 = new Point(border3X - (border3X - dnrX) * 0.5, CenterY);
            DnrRightSegment.Point3 = new Point(border3X, CenterY);
        }
    }
}