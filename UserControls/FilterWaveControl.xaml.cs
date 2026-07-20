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
        private const double PeakY = 20.0;
        private bool _isUpdatingProgrammatically = false;

        // Core Layout boundaries
        private const double BaseCenter = 320.0;
        private const double border1X = 84.0;
        private const double border2X = 556.0;
        private const double FixedSlopeWidth = 20.0;

        public int NbValue { get; private set; }
        public int WidthValue { get; private set; }
        public int NotchFreq { get; private set; }
        public int NotchDepth { get; private set; }
        public int ContourValue { get; private set; }
        public bool ContourEnabled { get; private set; }
        public int DnrValue { get; private set; }
        public double ShiftValue { get; private set; }

        public event Action<string, object> UIValueChanged;

        public FilterWaveControl()
        {
            InitializeComponent();
            Loaded += (s, e) => {
                UpdateThumbPositions(); // 1. Calculate and place the thumbs physically
                UpdateWaveform();       // 2. Draw the visual trapezoid matching those positions
            };
        }

        public void SetNbValue(int value)
        {
            NbValue = value;
            if (NbTextBlock != null) NbTextBlock.Text = value == 0 ? "NB: OFF" : "NB: " + NbValue;
            if (NbBadge != null) NbBadge.Text = value == 0 ? "NB: OFF" : "NB: " + NbValue;
            UpdateThumbPositions();
            UpdateWaveform();
        }

        public void SetWidthValue(int value)
        {
            WidthValue = value;
            if (WidthBadge != null) WidthBadge.Text = "WD: " + WidthValue + "%";
            UpdateThumbPositions();
            UpdateWaveform();
        }

        public void SetNotchValues(int freq, int depth)
        {
            NotchFreq = freq;
            NotchDepth = depth;
            if (NotchBadge != null) NotchBadge.Text = NotchDepth == 0 ? "NCH: OFF" : "NCH: " + NotchFreq + "% F / " + NotchDepth + "% D";
            UpdateThumbPositions();
            UpdateWaveform();
        }

        public void SetContourValues(int value, bool enabled)
        {
            ContourValue = value;
            ContourEnabled = enabled;
            if (ContourBadge != null) ContourBadge.Text = !ContourEnabled ? "CNT: OFF" : "CNT: " + ContourValue + "%";
            UpdateThumbPositions();
            UpdateWaveform();
        }

        public void SetDnrValue(int value)
        {
            DnrValue = value;
            if (DNRTextBlock != null) DNRTextBlock.Text = value == 0 ? "NR: OFF" : "NR: " + DnrValue.ToString("D2");
            if (DnrBadge != null) DnrBadge.Text = value == 0 ? "NR: OFF" : "NR: " + DnrValue.ToString("D2");
            UpdateThumbPositions();
            UpdateWaveform();
        }

        public void SetDnfEnabled(bool value) { _isUpdatingProgrammatically = true; if (AutoDnfBtn != null) AutoDnfBtn.IsChecked = value; _isUpdatingProgrammatically = false; }
        public void SetApfEnabled(bool value) { _isUpdatingProgrammatically = true; if (CwApfBtn != null) CwApfBtn.IsChecked = value; _isUpdatingProgrammatically = false; }
        public void SetContourEnabled(bool value) { _isUpdatingProgrammatically = true; if (ContourBtn != null) ContourBtn.IsChecked = value; ContourEnabled = value; UpdateWaveform(); _isUpdatingProgrammatically = false; }

        private void UpdateThumbPositions()
        {
            if (NbThumb == null || WidthThumb == null || NotchThumb == null || DnrThumb == null || ContourThumb == null || ShiftThumb == null) return;

            _isUpdatingProgrammatically = true;

            // 1. Noise Blanker Position
            double travelSpaceNb = CenterY - 5;
            double nbTop = CenterY - ((NbValue / 10.0) * travelSpaceNb);
            Canvas.SetTop(NbThumb, Math.Max(5, Math.Min(CenterY, nbTop)));

            // 2. Width Position
            double currentCenter = BaseCenter + ShiftValue;
            double maxHalfWidth = (border2X - border1X - (2 * FixedSlopeWidth)) / 2.0;
            const double thumbHalfWidth = 8.0;
            double minX = currentCenter;
            double maxX = currentCenter + maxHalfWidth - thumbHalfWidth;

            double wX = minX + ((WidthValue / 100.0) * (maxX - minX));
            Canvas.SetLeft(WidthThumb, wX);

            double halfWidth = Math.Max(0, wX - currentCenter);
            double leftShoulderX = currentCenter - halfWidth;
            double rightShoulderX = currentCenter + halfWidth;

            double currentBaseLeftX = leftShoulderX - FixedSlopeWidth;
            double currentBaseRightX = rightShoulderX + FixedSlopeWidth;

            if (currentBaseLeftX < border1X)
            {
                currentBaseLeftX = border1X;
                leftShoulderX = currentBaseLeftX + FixedSlopeWidth;
            }
            if (currentBaseRightX > border2X - 1.0)
            {
                currentBaseRightX = border2X - 1.0;
                rightShoulderX = currentBaseRightX - FixedSlopeWidth;
            }

            // 3. Notch Position
            double nX = border1X + ((NotchFreq / 100.0) * (border2X - border1X));
            nX = Math.Max(leftShoulderX, Math.Min(rightShoulderX, nX));
            double nY = PeakY + ((NotchDepth / 100.0) * (CenterY - PeakY));
            Canvas.SetLeft(NotchThumb, nX - 9);
            Canvas.SetTop(NotchThumb, nY - 9);

            // 4. Contour Position
            double cX = Canvas.GetLeft(ContourThumb) + 8;
            cX = Math.Max(leftShoulderX, Math.Min(rightShoulderX, cX));
            double maxTravel = CenterY - 5;
            double cY = PeakY - ((ContourValue / 100.0) * maxTravel);
            Canvas.SetLeft(ContourThumb, cX - 8);
            Canvas.SetTop(ContourThumb, !ContourEnabled ? PeakY - 8 : cY - 8);

            // 5. DNR Position
            double travelSpaceDnr = CenterY - 5;
            double dnrTop = CenterY - ((DnrValue / 15.0) * travelSpaceDnr);
            Canvas.SetTop(DnrThumb, Math.Max(5, Math.Min(CenterY, dnrTop)));

            // 6. Shift Position - Set cleanly from the synchronized baseline edge data point
            Canvas.SetLeft(ShiftThumb, currentBaseRightX );
            Canvas.SetTop(ShiftThumb, CenterY - 8);

            _isUpdatingProgrammatically = false;
        }

        private void ShiftThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_isUpdatingProgrammatically) return;

            double wX = Canvas.GetLeft(WidthThumb) + 8;
            double currentCenter = BaseCenter + ShiftValue;
            double halfWidth = Math.Max(0, wX - currentCenter);

            double minAllowedCenter = border1X + FixedSlopeWidth + halfWidth;
            double maxAllowedCenter = border2X - FixedSlopeWidth - halfWidth;

            double targetCenter = currentCenter + e.HorizontalChange;
            targetCenter = Math.Max(minAllowedCenter, Math.Min(maxAllowedCenter, targetCenter));

            ShiftValue = targetCenter - BaseCenter;
            if (ShiftBadge != null) ShiftBadge.Text = "SF: " + (int)ShiftValue;

            double newLeftShoulder = targetCenter - halfWidth;
            double newRightShoulder = targetCenter + halfWidth;

            double currentNotchX = Canvas.GetLeft(NotchThumb) + 9;
            if (currentNotchX < newLeftShoulder) currentNotchX = newLeftShoulder;
            if (currentNotchX > newRightShoulder) currentNotchX = newRightShoulder;
            Canvas.SetLeft(NotchThumb, currentNotchX - 9);
            NotchFreq = (int)(((currentNotchX - border1X) / (border2X - border1X)) * 100);

            double currentContourX = Canvas.GetLeft(ContourThumb) + 8;
            if (currentContourX < newLeftShoulder) currentContourX = newLeftShoulder;
            if (currentContourX > newRightShoulder) currentContourX = newRightShoulder;
            Canvas.SetLeft(ContourThumb, currentContourX - 8);

            UpdateThumbPositions();
            UpdateWaveform();
            UIValueChanged?.Invoke("SHIFT", (int)ShiftValue);
        }

        private void WidthThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_isUpdatingProgrammatically) return;

            double currentCenter = BaseCenter + ShiftValue;
            double currentLeft = Canvas.GetLeft(WidthThumb);
            double newLeft = currentLeft + e.HorizontalChange;

            double minX = currentCenter;
            double maxHalfWidth = (border2X - border1X - (2 * FixedSlopeWidth)) / 2.0;
            double maxX = currentCenter + maxHalfWidth - 8.0;

            if (newLeft < minX) newLeft = minX;
            if (newLeft > maxX) newLeft = maxX;

            double percentage = ((newLeft - minX) / (maxX - minX)) * 100;
            WidthValue = (int)Math.Round(percentage);
            if (WidthBadge != null) WidthBadge.Text = "WD: " + WidthValue + "%";

            // Run updates in absolute sequence via data properties
            UpdateThumbPositions();
            UpdateWaveform();

            UIValueChanged?.Invoke("WIDTH", WidthValue);
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

            if (NbTextBlock != null) NbTextBlock.Text = NbValue == 0 ? "NB: OFF" : "NB: " + NbValue;
            if (NbBadge != null) NbBadge.Text = NbValue == 0 ? "NB: OFF" : "NB: " + NbValue;

            UpdateWaveform();
            UIValueChanged?.Invoke("NB", NbValue);
        }

        private void NotchThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double currentCenter = BaseCenter + ShiftValue;
            double currentLeft = Canvas.GetLeft(NotchThumb);
            double currentTop = Canvas.GetTop(NotchThumb);

            double targetCenterX = currentLeft + 9 + e.HorizontalChange;
            double targetCenterY = currentTop + 9 + e.VerticalChange;

            double wX = Canvas.GetLeft(WidthThumb) + 8;
            double halfWidth = Math.Max(0, wX - currentCenter);

            double leftShoulderX = currentCenter - halfWidth;
            double rightShoulderX = currentCenter + halfWidth;

            if ((leftShoulderX - FixedSlopeWidth) < border1X) leftShoulderX = border1X + FixedSlopeWidth;
            if ((rightShoulderX + FixedSlopeWidth) > border2X) rightShoulderX = border2X - FixedSlopeWidth;

            double clampedCenterX = Math.Max(leftShoulderX, Math.Min(rightShoulderX, targetCenterX));
            double clampedTop = Math.Max(PeakY - 9, Math.Min(CanvasHeight - 18, targetCenterY - 9));

            Canvas.SetLeft(NotchThumb, clampedCenterX - 9);
            Canvas.SetTop(NotchThumb, clampedTop);

            NotchFreq = (int)(((clampedCenterX - border1X) / (border2X - border1X)) * 100);
            NotchDepth = (int)(((clampedTop + 9 - PeakY) / (CanvasHeight - 18 - PeakY)) * 100);

            if (NotchDepth < 5) NotchDepth = 0;
            if (NotchBadge != null) NotchBadge.Text = NotchDepth == 0 ? "NCH: OFF" : "NCH: " + NotchFreq + "% F / " + NotchDepth + "% D";

            UpdateWaveform();
            UIValueChanged?.Invoke("NCH_FREQ", NotchFreq);
            UIValueChanged?.Invoke("NCH_DEPTH", NotchDepth);
        }

        private void ContourThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double currentCenter = BaseCenter + ShiftValue;
            double currentLeft = Canvas.GetLeft(ContourThumb);
            double currentTop = Canvas.GetTop(ContourThumb);

            double targetCenterX = currentLeft + 8 + e.HorizontalChange;
            double targetCenterY = currentTop + 8 + e.VerticalChange;

            double wX = Canvas.GetLeft(WidthThumb) + 8;
            double halfWidth = Math.Max(0, wX - currentCenter);

            double leftShoulderX = currentCenter - halfWidth;
            double rightShoulderX = currentCenter + halfWidth;

            if ((leftShoulderX - FixedSlopeWidth) < border1X) leftShoulderX = border1X + FixedSlopeWidth;
            if ((rightShoulderX + FixedSlopeWidth) > border2X) rightShoulderX = border2X - FixedSlopeWidth;

            double clampedCenterX = Math.Max(leftShoulderX, Math.Min(rightShoulderX, targetCenterX));
            double clampedTop = Math.Max(5, Math.Min(CanvasHeight - 16, targetCenterY - 8));

            Canvas.SetLeft(ContourThumb, clampedCenterX - 8);
            Canvas.SetTop(ContourThumb, clampedTop);

            double centerThumbY = clampedTop + 8;
            if (Math.Abs(centerThumbY - PeakY) < 3)
            {
                ContourValue = 0;
                ContourEnabled = false;
            }
            else
            {
                ContourEnabled = true;
                double maxTravel = CenterY - 5;
                ContourValue = (int)(((PeakY - centerThumbY) / maxTravel) * 100);
            }

            if (ContourBadge != null) ContourBadge.Text = !ContourEnabled ? "CNT: OFF" : "CNT: " + ContourValue + "%";
            UpdateWaveform();
            UIValueChanged?.Invoke("CONTOUR_VAL", ContourValue);
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

            if (DNRTextBlock != null) DNRTextBlock.Text = DnrValue == 0 ? "NR: OFF" : "NR: " + DnrValue.ToString("D2");
            if (DnrBadge != null) DnrBadge.Text = DnrValue == 0 ? "NR: OFF" : "NR: " + DnrValue.ToString("D2");

            UpdateWaveform();
            UIValueChanged?.Invoke("NR", DnrValue);
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingProgrammatically) return;
            ToggleButton btn = sender as ToggleButton;
            if (btn != null)
            {
                if (btn == ContourBtn) { ContourEnabled = true; UpdateWaveform(); }
                UIValueChanged?.Invoke(btn.Content.ToString(), true);
            }
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingProgrammatically) return;
            ToggleButton btn = sender as ToggleButton;
            if (btn != null)
            {
                if (btn == ContourBtn) { ContourEnabled = false; UpdateWaveform(); }
                UIValueChanged?.Invoke(btn.Content.ToString(), false);
            }
        }

        private void UpdateWaveform()
        {
            if (NbThumb == null || WidthThumb == null || NotchThumb == null || DnrThumb == null || ContourThumb == null || ShiftThumb == null ||
                Segment1 == null || Segment2 == null || EndSegment == null ||
                DnrRightFigure == null || DnrRightSegment == null || DnrRightSegment2 == null ||
                FillDnrRightFigure == null || FillDnrRightSegment == null || FillDnrRightSegment2 == null ||
                Segment3PointC == null || FillSegment3PointC == null ||
                Segment3PointD == null || Segment3PointE == null || Segment3PointF == null ||
                FillSegment3PointD == null || FillSegment3PointE == null || FillSegment3PointF == null)
                return;

            const double border0X = 0.0;
            const double border3X = 639.0;

            double nbX = 42.0;
            double nbY = Canvas.GetTop(NbThumb) + 8;

            // Mathematical Sync Path - Computes wX purely from data values instead of querying Canvas UI layouts
            double currentCenter = BaseCenter + ShiftValue;
            double maxHalfWidth = (border2X - border1X - (2 * FixedSlopeWidth)) / 2.0;
            double minX = currentCenter;
            double maxX = currentCenter + maxHalfWidth - 8.0;

            double wX = minX + ((WidthValue / 100.0) * (maxX - minX)) + 8.0;
            Canvas.SetTop(WidthThumb, PeakY - 8);

            double dnrX = 598.0;
            double dnrY = Canvas.GetTop(DnrThumb) + 8;

            double halfWidth = Math.Max(0, wX - currentCenter);
            double leftShoulderX = currentCenter - halfWidth;
            double rightShoulderX = currentCenter + halfWidth;

            double currentBaseLeftX = leftShoulderX - FixedSlopeWidth;
            double currentBaseRightX = rightShoulderX + FixedSlopeWidth;

            if (currentBaseLeftX < border1X)
            {
                currentBaseLeftX = border1X;
                leftShoulderX = currentBaseLeftX + FixedSlopeWidth;
            }
            if (currentBaseRightX > border2X - 1.0)
            {
                currentBaseRightX = border2X - 1.0;
                rightShoulderX = currentBaseRightX - FixedSlopeWidth;
            }

            if (leftShoulderX > currentCenter) leftShoulderX = currentCenter;
            if (rightShoulderX < currentCenter) rightShoulderX = currentCenter;

            double nX = Canvas.GetLeft(NotchThumb) + 9;
            double nY = Canvas.GetTop(NotchThumb) + 9;
            nX = Math.Max(leftShoulderX, Math.Min(rightShoulderX, nX));

            double cX = Canvas.GetLeft(ContourThumb) + 8;
            double cY = Canvas.GetTop(ContourThumb) + 8;
            cX = Math.Max(leftShoulderX, Math.Min(rightShoulderX, cX));

            if (NotchDepth == 0) nY = PeakY;
            if (!ContourEnabled) cY = PeakY;

            if (NbBadge != null) { Canvas.SetLeft(NbBadge, nbX - 22); Canvas.SetTop(NbBadge, nbY - 22); }
            if (WidthBadge != null) { Canvas.SetLeft(WidthBadge, border1X + 15); Canvas.SetTop(WidthBadge, 75); }
            if (NotchBadge != null) { Canvas.SetLeft(NotchBadge, border2X - 110); Canvas.SetTop(NotchBadge, 75); }
            if (DnrBadge != null) { Canvas.SetLeft(DnrBadge, dnrX - 22); Canvas.SetTop(DnrBadge, dnrY - 22); }
            if (ShiftBadge != null) { Canvas.SetLeft(ShiftBadge, currentBaseRightX - 15); }

            Segment1.Point1 = new Point(border0X + (nbX - border0X) * 0.5, CenterY);
            Segment1.Point2 = new Point(nbX - (nbX - border0X) * 0.1, nbY);
            Segment1.Point3 = new Point(nbX, nbY);

            Segment2.Point1 = new Point(nbX + (border1X - nbX) * 0.1, nbY);
            Segment2.Point2 = new Point(border1X - (border1X - nbX) * 0.5, CenterY);
            Segment2.Point3 = new Point(border1X, CenterY);

            Point pA = new Point(currentBaseLeftX, CenterY);
            Point pB = new Point(currentBaseRightX, CenterY);
            Segment3PointA.Point = pA;
            WidthRightSegment.Point = pB;

            List<Point> midPoints = new List<Point>
            {
                new Point(leftShoulderX, PeakY),
                new Point(rightShoulderX, PeakY)
            };

            if (NotchDepth > 0)
            {
                midPoints.Add(new Point(Math.Max(leftShoulderX, nX - 12), PeakY));
                midPoints.Add(new Point(nX, nY));
                midPoints.Add(new Point(Math.Min(rightShoulderX, nX + 12), PeakY));
            }
            else
            {
                midPoints.Add(new Point(leftShoulderX, PeakY));
                midPoints.Add(new Point(leftShoulderX, PeakY));
                midPoints.Add(new Point(leftShoulderX, PeakY));
            }

            if (ContourEnabled)
            {
                midPoints.Add(new Point(Math.Max(leftShoulderX, cX - 30), PeakY));
                midPoints.Add(new Point(cX, cY));
                midPoints.Add(new Point(Math.Min(rightShoulderX, cX + 30), PeakY));
            }
            else
            {
                midPoints.Add(new Point(rightShoulderX, PeakY));
                midPoints.Add(new Point(rightShoulderX, PeakY));
                midPoints.Add(new Point(rightShoulderX, PeakY));
            }

            midPoints.Sort((pt1, pt2) => pt1.X.CompareTo(pt2.X));

            Segment3PointNotchLeft.Point = midPoints[0];
            Segment3PointNotchTip.Point = midPoints[1];
            Segment3PointNotchRight.Point = midPoints[2];
            Segment3PointB.Point = midPoints[3];
            Segment3PointC.Point = midPoints[4];
            Segment3PointD.Point = midPoints[5];
            Segment3PointE.Point = midPoints[6];
            Segment3PointF.Point = midPoints[7];

            EndSegment.Point1 = new Point(currentBaseRightX, CenterY);
            EndSegment.Point2 = new Point(border2X, CenterY);
            EndSegment.Point3 = new Point(border2X, CenterY);

            DnrRightFigure.StartPoint = new Point(border2X, CenterY);
            DnrRightSegment.Point1 = new Point(border2X + (dnrX - border2X) * 0.5, CenterY);
            DnrRightSegment.Point2 = new Point(dnrX - (dnrX - border2X) * 0.1, dnrY);
            DnrRightSegment.Point3 = new Point(dnrX, dnrY);

            DnrRightSegment2.Point1 = new Point(dnrX + (border3X - dnrX) * 0.1, dnrY);
            DnrRightSegment2.Point2 = new Point(border3X - (border3X - dnrX) * 0.5, CenterY);
            DnrRightSegment2.Point3 = new Point(border3X, CenterY);

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
            FillSegment3PointD.Point = Segment3PointD.Point;
            FillSegment3PointE.Point = Segment3PointE.Point;
            FillSegment3PointF.Point = Segment3PointF.Point;
            FillWidthRightSegment.Point = WidthRightSegment.Point;

            FillEndSegment.Point1 = EndSegment.Point1;
            FillEndSegment.Point2 = EndSegment.Point2;
            FillEndSegment.Point3 = EndSegment.Point3;

            FillDnrRightFigure.StartPoint = DnrRightFigure.StartPoint;
            FillDnrRightSegment.Point1 = DnrRightSegment.Point1;
            FillDnrRightSegment.Point2 = DnrRightSegment.Point2;
            FillDnrRightSegment.Point3 = DnrRightSegment.Point3;
            FillDnrRightSegment2.Point1 = DnrRightSegment2.Point1;
            FillDnrRightSegment2.Point2 = DnrRightSegment2.Point2;
            FillDnrRightSegment2.Point3 = DnrRightSegment2.Point3;

            ((LineSegment)FillDnrRightFigure.Segments[2]).Point = new Point(639.0, 102);
            ((LineSegment)FillFigure.Segments[13]).Point = new Point(639.0, 102);
        }
    }
}