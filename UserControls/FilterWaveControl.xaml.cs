using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

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

            double nbTop = CenterY - ((NbValue / 10.0) * CenterY);
            Canvas.SetTop(NbThumb, Math.Max(5, Math.Min(CenterY, nbTop)));

            double widthTop = CenterY + ((WidthValue / 100.0) * (CanvasHeight - 12 - CenterY));
            Canvas.SetTop(WidthThumb, Math.Max(CenterY, Math.Min(CanvasHeight - 12, widthTop)));

            // Inside UpdateThumbPositions() - Replace the old dnrTop calculation with this:
            double travelSpace = CenterY - 5;
            double dnrTop = CenterY - ((DnrValue / 15.0) * travelSpace);
            Canvas.SetTop(DnrThumb, Math.Max(5, Math.Min(CenterY, dnrTop)));

            _isUpdatingProgrammatically = false;
        }

        private void NbThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newTop = Math.Max(5, Math.Min(CenterY, Canvas.GetTop(NbThumb) + e.VerticalChange));
            Canvas.SetTop(NbThumb, newTop);

            double pct = 1 - (newTop / CenterY);
            NbValue = (int)Math.Round(pct * 10);

            NbTextBlock.Text = $"NB: {NbValue}"; // <-- Add this line

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
            if (NbThumb == null || WidthThumb == null || DnrThumb == null) return;

            double nbX = Canvas.GetLeft(NbThumb) + 6;
            double nbY = Canvas.GetTop(NbThumb) + 6;
            double wX = Canvas.GetLeft(WidthThumb) + 6;
            double wY = Canvas.GetTop(WidthThumb) + 6;
            double dnrX = Canvas.GetLeft(DnrThumb) + 6;
            double dnrY = Canvas.GetTop(DnrThumb) + 6;

            Segment1.Point1 = new Point(nbX * 0.4, CenterY);
            Segment1.Point2 = new Point(nbX * 0.7, nbY);
            Segment1.Point3 = new Point(nbX, nbY);

            double midPointX1 = nbX + ((wX - nbX) * 0.5);
            Segment2.Point1 = new Point(nbX + 40, nbY);
            Segment2.Point2 = new Point(midPointX1 - 20, wY);
            Segment2.Point3 = new Point(wX, wY);

            double midPointX2 = wX + ((dnrX - wX) * 0.5);
            Segment3.Point1 = new Point(wX + 20, wY);
            Segment3.Point2 = new Point(midPointX2 - 40, dnrY);
            Segment3.Point3 = new Point(dnrX, dnrY);

            EndSegment.Point = new Point(643, CenterY);
        }
    }
}