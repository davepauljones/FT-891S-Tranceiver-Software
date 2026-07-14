using System;
using System.Collections.Generic;
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

        // New Notch Properties
        public int NotchFreq { get; private set; }  // 0% to 100% of middle segment
        public int NotchDepth { get; private set; } // 0% (off/top) to 100% (deepest/bottom)

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
            NbTextBlock.Text = value == 0 ? "NB: OFF" : $"NB: {NbValue}";
            NbBadge.Text = value == 0 ? "NB: OFF" : $"NB: {NbValue}";
            UpdateThumbPositions();
            UpdateWaveform();
        }

        public void SetWidthValue(int value)
        {
            WidthValue = value;
            WidthBadge.Text = $"WD: {WidthValue}%";
            UpdateThumbPositions();
            UpdateWaveform();
        }

        public void SetNotchValues(int freq, int depth)
        {
            NotchFreq = freq;
            NotchDepth = depth;
            NotchBadge.Text = NotchDepth == 0 ? "NCH: OFF" : $"NCH: {NotchFreq}% F / {NotchDepth}% D";
            UpdateThumbPositions();
            UpdateWaveform();
        }

        public void SetDnrValue(int value)
        {
            DnrValue = value;
            DNRTextBlock.Text = DnrValue == 0 ? "NR: OFF" : $"NR: {DnrValue:D2}";
            DnrBadge.Text = DnrValue == 0 ? "NR: OFF" : $"NR: {DnrValue:D2}";
            UpdateThumbPositions();
            UpdateWaveform();
        }

        public void SetDnfEnabled(bool value) { _isUpdatingProgrammatically = true; AutoDnfBtn.IsChecked = value; _isUpdatingProgrammatically = false; }
        public void SetApfEnabled(bool value) { _isUpdatingProgrammatically = true; CwApfBtn.IsChecked = value; _isUpdatingProgrammatically = false; }
        public void SetContourEnabled(bool value) { _isUpdatingProgrammatically = true; ContourBtn.IsChecked = value; _isUpdatingProgrammatically = false; }

        private void UpdateThumbPositions()
        {
            if (NbThumb == null || WidthThumb == null || NotchThumb == null || DnrThumb == null) return;

            _isUpdatingProgrammatically = true;

            // 1. Noise Blanker
            double travelSpaceNb = CenterY - 5;
            double nbTop = CenterY - ((NbValue / 10.0) * travelSpaceNb);
            Canvas.SetTop(NbThumb, Math.Max(5, Math.Min(CenterY, nbTop)));

            // 2. Width (Pulls DOWN from center line)
            double widthTop = CenterY + ((WidthValue / 100.0) * (CanvasHeight - 12 - CenterY));
            Canvas.SetTop(WidthThumb, Math.Max(CenterY, Math.Min(CanvasHeight - 12, widthTop)));

            // 3. Notch Handle (Drags in 2D X and Y inside middle segment 213.0 -> 427.0)
            double nX = 213.0 + ((NotchFreq / 100.0) * (427.0 - 213.0));
            double nY = CenterY + ((NotchDepth / 100.0) * (CanvasHeight - 18 - CenterY));
            Canvas.SetLeft(NotchThumb, Math.Max(213.0, Math.Min(427.0 - 18, nX - 9)));
            Canvas.SetTop(NotchThumb, Math.Max(CenterY, Math.Min(CanvasHeight - 18, nY - 9)));

            // 4. Digital Noise Reduction
            double travelSpaceDnr = CenterY - 5;
            double dnrTop = CenterY - ((DnrValue / 15.0) * travelSpaceDnr);
            Canvas.SetTop(DnrThumb, Math.Max(5, Math.Min(CenterY, dnrTop)));

            _isUpdatingProgrammatically = false;
        }

        private void NbThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newTop = Math.Max(5, Math.Min(CenterY, Canvas.GetTop(NbThumb) + e.VerticalChange));
            Canvas.SetTop(NbThumb, newTop);

            double travelSpace = CenterY - 5;
            double pct = (CenterY - newTop) / travelSpace;

            NbValue = (int)Math.Round(pct * 10);
            if (newTop <= 5) NbValue = 10;
            if (newTop >= CenterY) NbValue = 0;

            NbTextBlock.Text = NbValue == 0 ? "NB: OFF" : $"NB: {NbValue}";
            NbBadge.Text = NbValue == 0 ? "NB: OFF" : $"NB: {NbValue}";

            UpdateWaveform();
            UIValueChanged?.Invoke("NB", NbValue);
        }

        

        private void NotchThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            // 2D Dragging Calculations
            double currentLeft = Canvas.GetLeft(NotchThumb) + e.HorizontalChange;
            double currentTop = Canvas.GetTop(NotchThumb) + e.VerticalChange;

            // Constraints inside middle segment boundaries
            double clampedLeft = Math.Max(213.0, Math.Min(427.0 - 18, currentLeft));
            double clampedTop = Math.Max(CenterY, Math.Min(CanvasHeight - 18, currentTop));

            Canvas.SetLeft(NotchThumb, clampedLeft);
            Canvas.SetTop(NotchThumb, clampedTop);

            // Convert position offsets back to percentages
            NotchFreq = (int)(((clampedLeft + 9 - 213.0) / (427.0 - 213.0)) * 100);
            NotchDepth = (int)(((clampedTop + 9 - CenterY) / (CanvasHeight - 18 - CenterY)) * 100);

            // Snapping feature: close to the top center line disables the notch
            if (NotchDepth < 5) NotchDepth = 0;

            NotchBadge.Text = NotchDepth == 0 ? "NCH: OFF" : $"NCH: {NotchFreq}% F / {NotchDepth}% D";

            UpdateWaveform();
            UIValueChanged?.Invoke("NCH_FREQ", NotchFreq);
            UIValueChanged?.Invoke("NCH_DEPTH", NotchDepth);
        }

        private void DnrThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newTop = Math.Max(5, Math.Min(CenterY, Canvas.GetTop(DnrThumb) + e.VerticalChange));
            Canvas.SetTop(DnrThumb, newTop);

            double travelSpace = CenterY - 5;
            double pct = (CenterY - newTop) / travelSpace;

            DnrValue = (int)Math.Round(pct * 15);
            if (newTop <= 5) DnrValue = 15;
            if (newTop >= CenterY) DnrValue = 0;

            DNRTextBlock.Text = DnrValue == 0 ? "NR: OFF" : $"NR: {DnrValue:D2}";
            DnrBadge.Text = DnrValue == 0 ? "NR: OFF" : $"NR: {DnrValue:D2}";

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

        private void WidthThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            // The center of our passband is exactly halfway between 213 and 427 -> 320
            const double PassbandCenterX = 320.0;
            const double border2X = 427.0;
            const double thumbHalfWidth = 8.0;

            double currentLeft = Canvas.GetLeft(WidthThumb);
            double newLeft = currentLeft + e.HorizontalChange;

            // Clamp the drag so the right edge cannot go past the border or cross to the left of the center
            double minX = PassbandCenterX;
            double maxX = border2X - thumbHalfWidth;

            if (newLeft < minX) newLeft = minX;
            if (newLeft > maxX) newLeft = maxX;

            Canvas.SetLeft(WidthThumb, newLeft);

            // Calculate filter width percentage from 0% (narrowest dome) to 100% (flat open)
            double percentage = ((newLeft - minX) / (maxX - minX)) * 100;
            WidthBadge.Text = $"WD: {percentage:0}%";

            UpdateWaveform();
            UIValueChanged?.Invoke("SH", WidthValue);
        }

        // Symmetrical Trapezoid Calculator
        private double GetPassbandY(double x, double wX)
        {
            const double border1X = 213.0;
            const double border2X = 427.0;
            const double PassbandCenterX = 320.0;
            const double PeakY = 20.0;     // Top flat level of the passband
            const double CenterY = 51.0;   // Rest baseline level

            if (x <= border1X) return CenterY;
            if (x >= border2X) return CenterY;

            // Symmetrical half-width
            double halfWidth = Math.Max(0, wX - PassbandCenterX);
            double leftShoulderX = PassbandCenterX - halfWidth;
            double rightShoulderX = PassbandCenterX + halfWidth;

            if (x < leftShoulderX)
            {
                // Slope up from left boundary to the left shoulder
                double slope = (PeakY - CenterY) / (leftShoulderX - border1X);
                return CenterY + slope * (x - border1X);
            }
            else if (x > rightShoulderX)
            {
                // Slope down from the right shoulder to the right boundary
                double slope = (CenterY - PeakY) / (border2X - rightShoulderX);
                return PeakY + slope * (x - rightShoulderX);
            }
            else
            {
                // Flat top peak passband region
                return PeakY;
            }
        }

        private void UpdateWaveform()
        {
            if (NbThumb == null || WidthThumb == null || NotchThumb == null || DnrThumb == null ||
                Segment1 == null || Segment2 == null || EndSegment == null || DnrRightSegment == null ||
                Segment3PointC == null || FillSegment3PointC == null)
                return;

            const double CenterY = 51.0;
            const double PeakY = 20.0;
            const double PassbandCenterX = 320.0;

            double nbX = Canvas.GetLeft(NbThumb) + 8;
            double nbY = Canvas.GetTop(NbThumb) + 8;

            // Width handle calculations
            double wX = Canvas.GetLeft(WidthThumb) + 8;
            double wY = PeakY;
            Canvas.SetTop(WidthThumb, wY - 8);

            // Notch handle coordinates
            double nX = Canvas.GetLeft(NotchThumb) + 9;
            double nY = Canvas.GetTop(NotchThumb) + 9;

            double dnrX = Canvas.GetLeft(DnrThumb) + 8;
            double dnrY = Canvas.GetTop(DnrThumb) + 8;

            double border0X = 0.0;
            double border1X = 213.0;
            double border2X = 427.0;
            double border3X = 643.0;

            // Symmetrical shoulders based on current horizontal WidthThumb position
            double halfWidth = Math.Max(0, wX - PassbandCenterX);
            double leftShoulderX = PassbandCenterX - halfWidth;
            double rightShoulderX = PassbandCenterX + halfWidth;

            // ==========================================
            // PREVENT NOTCH INVERSION (Y-CLAMPING)
            // ==========================================
            double passbandBaselineY = GetPassbandY(nX, wX);
            if (nY < passbandBaselineY)
            {
                nY = passbandBaselineY;
                Canvas.SetTop(NotchThumb, passbandBaselineY - 9);
            }

            // Track floating labels
            Canvas.SetLeft(NbBadge, nbX - 22);
            Canvas.SetTop(NbBadge, nbY - 22);

            Canvas.SetLeft(WidthBadge, border1X + 15);
            Canvas.SetTop(WidthBadge, 75);

            Canvas.SetLeft(NotchBadge, border2X - 110);
            Canvas.SetTop(NotchBadge, 75);

            Canvas.SetLeft(DnrBadge, dnrX - 22);
            Canvas.SetTop(DnrBadge, dnrY - 22);

            // ==========================================
            // SECTION 1: NOISE BLANKER (NB)
            // ==========================================
            Segment1.Point1 = new Point(border0X + (nbX - border0X) * 0.5, CenterY);
            Segment1.Point2 = new Point(nbX - (nbX - border0X) * 0.1, nbY);
            Segment1.Point3 = new Point(nbX, nbY);

            Segment2.Point1 = new Point(nbX + (border1X - nbX) * 0.1, nbY);
            Segment2.Point2 = new Point(border1X - (border1X - nbX) * 0.5, CenterY);
            Segment2.Point3 = new Point(border1X, CenterY);

            // ==========================================
            // SECTION 2: SYMMETRICAL WIDTH WITH NOTCH
            // ==========================================
            Point pA = new Point(border1X, CenterY);
            Point pB = new Point(border2X, CenterY);

            Segment3PointA.Point = pA;
            WidthRightSegment.Point = pB; // Locked descent point

            Point pLeftShoulder = new Point(leftShoulderX, PeakY);
            Point pRightShoulder = new Point(rightShoulderX, PeakY);

            if (NotchDepth > 0)
            {
                double notchLeftX = Math.Max(border1X, nX - 12);
                double notchRightX = Math.Min(border2X, nX + 12);

                double notchLeftY = GetPassbandY(notchLeftX, wX);
                double notchRightY = GetPassbandY(notchRightX, wX);

                Point pNotchLeft = new Point(notchLeftX, notchLeftY);
                Point pNotchTip = new Point(nX, nY);
                Point pNotchRight = new Point(notchRightX, notchRightY);

                // Sort our five structural points left-to-right to maintain perfect geometry
                var midPoints = new List<Point> { pLeftShoulder, pNotchLeft, pNotchTip, pNotchRight, pRightShoulder };
                midPoints.Sort((pt1, pt2) => pt1.X.CompareTo(pt2.X));

                Segment3PointNotchLeft.Point = midPoints[0];
                Segment3PointNotchTip.Point = midPoints[1];
                Segment3PointNotchRight.Point = midPoints[2];
                Segment3PointB.Point = midPoints[3];
                Segment3PointC.Point = midPoints[4];
            }
            else
            {
                // Collapse the notch points cleanly against the shoulders of the trapezoid when disabled
                Segment3PointNotchLeft.Point = pLeftShoulder;
                Segment3PointNotchTip.Point = pLeftShoulder;
                Segment3PointNotchRight.Point = pRightShoulder;
                Segment3PointB.Point = pRightShoulder;
                Segment3PointC.Point = pRightShoulder;
            }

            // ==========================================
            // SECTION 3: DIGITAL NOISE REDUCTION (DNR)
            // ==========================================
            EndSegment.Point1 = new Point(border2X + (dnrX - border2X) * 0.5, CenterY);
            EndSegment.Point2 = new Point(dnrX - (dnrX - border2X) * 0.1, dnrY);
            EndSegment.Point3 = new Point(dnrX, dnrY);

            DnrRightSegment.Point1 = new Point(dnrX + (border3X - dnrX) * 0.1, dnrY);
            DnrRightSegment.Point2 = new Point(border3X - (border3X - dnrX) * 0.5, CenterY);
            DnrRightSegment.Point3 = new Point(border3X, CenterY);

            // ==========================================
            // SECTION 4: AREA GLOW COORDINATES SYNC
            // ==========================================
            FillSegment1.Point1 = Segment1.Point1;
            FillSegment1.Point2 = Segment1.Point2;
            FillSegment1.Point3 = Segment1.Point3;

            FillSegment2.Point1 = Segment2.Point1;
            FillSegment2.Point2 = Segment2.Point2;
            FillSegment2.Point3 = Segment2.Point3;

            FillSegment3PointA.Point = Segment3PointA.Point;
            FillSegment3PointNotchLeft.Point = Segment3PointNotchLeft.Point;
            FillSegment3PointNotchTip.Point = Segment3PointNotchTip.Point;
            FillSegment3PointNotchRight.Point = Segment3PointNotchRight.Point;
            FillSegment3PointB.Point = Segment3PointB.Point;
            FillSegment3PointC.Point = Segment3PointC.Point;
            FillWidthRightSegment.Point = WidthRightSegment.Point;

            FillEndSegment.Point1 = EndSegment.Point1;
            FillEndSegment.Point2 = EndSegment.Point2;
            FillEndSegment.Point3 = EndSegment.Point3;

            FillDnrRightSegment.Point1 = DnrRightSegment.Point1;
            FillDnrRightSegment.Point2 = DnrRightSegment.Point2;
            FillDnrRightSegment.Point3 = DnrRightSegment.Point3;
        }
    }
}