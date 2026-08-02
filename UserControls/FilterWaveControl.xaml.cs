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

            // 3. Notch Position (Clamped between PeakY for fully up and CenterY for fully down)
            double nX = border1X + ((NotchFreq / 100.0) * (border2X - border1X));

            // Check if the notch is outside the active passband shoulders and force it to clamp
            double clampedNotchX = Math.Max(leftShoulderX, Math.Min(rightShoulderX, nX));
            if (nX != clampedNotchX)
            {
                // If the passband width reduction forced the notch to move, 
                // update its frequency percentage to match its new physical boundary!
                nX = clampedNotchX;
                NotchFreq = (int)(((nX - border1X) / (border2X - border1X)) * 100);
            }

            double nY = PeakY + ((NotchDepth / 100.0) * (CenterY - PeakY));
            Canvas.SetLeft(NotchThumb, nX - 9);
            Canvas.SetTop(NotchThumb, nY - 9);

            // 4. Contour Position
            double cX = Canvas.GetLeft(ContourThumb) + 8;
            double clampedContourX = Math.Max(leftShoulderX, Math.Min(rightShoulderX, cX));
            if (cX != clampedContourX)
            {
                cX = clampedContourX;
                // Optional: Recalculate or keep valid bounds if needed, but keeping X clamped prevents desync
            }
            Canvas.SetLeft(ContourThumb, cX - 8);

            if (!ContourEnabled)
            {
                Canvas.SetTop(ContourThumb, PeakY - 8);
            }

            // 5. DNR Position
            double travelSpaceDnr = CenterY - 5;
            double dnrTop = CenterY - ((DnrValue / 15.0) * travelSpaceDnr);
            Canvas.SetTop(DnrThumb, Math.Max(5, Math.Min(CenterY, dnrTop)));

            // 6. Shift Position
            Canvas.SetLeft(ShiftThumb, currentBaseRightX);
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

            // Restrict vertical movement strictly between PeakY (top flat line) and CenterY (bottom center line)
            double clampedTop = Math.Max(PeakY, Math.Min(CenterY, targetCenterY));

            Canvas.SetLeft(NotchThumb, clampedCenterX - 9);
            Canvas.SetTop(NotchThumb, clampedTop - 9);

            NotchFreq = (int)(((clampedCenterX - border1X) / (border2X - border1X)) * 100);

            // Calculate depth percentage (0% at PeakY/fully up, 100% at CenterY/fully down)
            double travelRange = CenterY - PeakY;
            NotchDepth = travelRange > 0 ? (int)(((clampedTop - PeakY) / travelRange) * 100) : 0;

            if (NotchDepth < 3) NotchDepth = 0;
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

            // Restrict vertical travel between top limit (5) and CenterY
            double clampedTop = Math.Max(5, Math.Min(CenterY, targetCenterY - 8));

            Canvas.SetLeft(ContourThumb, clampedCenterX - 8);
            Canvas.SetTop(ContourThumb, clampedTop);

            double centerThumbY = clampedTop + 8;

            if (Math.Abs(centerThumbY - PeakY) < 2)
            {
                ContourValue = 0;
            }
            else if (centerThumbY < PeakY)
            {
                // Dragged UP (Well state)
                ContourEnabled = true;
                double upwardTravel = PeakY - 5;
                ContourValue = upwardTravel > 0 ? (int)(((PeakY - centerThumbY) / upwardTravel) * 100) : 0;
            }
            else
            {
                // Dragged DOWN (Dome state)
                ContourEnabled = true;
                double downwardTravel = CenterY - PeakY;
                ContourValue = downwardTravel > 0 ? (int)(((centerThumbY - PeakY) / downwardTravel) * 100) : 0;
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
                Segment1 == null || Segment2 == null || PathFigureEndPoint == null ||
                DnrRightFigure == null || DnrRightSegment == null || DnrRightSegment2 == null ||
                LH_TrapezoidSide == null || LH_TrapezoidWallSegment == null || RH_TrapezoidSide == null || WidthPassbandEnd == null ||
                NotchFigureStart == null || NotchLeftShoulderSegment == null || NotchTipSegment == null || NotchRightShoulderSegment == null ||
                ContourFigureStart == null || ContourArcSegment == null)
                return;

            const double border0X = 0.0;
            const double border3X = 639.0;

            double nbX = 42.0;
            double nbY = Canvas.GetTop(NbThumb) + 8;

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

            // NB Section
            Segment1.Point1 = new Point(border0X + (nbX - border0X) * 0.5, CenterY);
            Segment1.Point2 = new Point(nbX - (nbX - border0X) * 0.1, nbY);
            Segment1.Point3 = new Point(nbX, nbY);

            Segment2.Point1 = new Point(nbX + (border1X - nbX) * 0.1, nbY);
            Segment2.Point2 = new Point(border1X - (border1X - nbX) * 0.5, CenterY);
            Segment2.Point3 = new Point(border1X, CenterY);

            // Passband Main Section
            Point pA = new Point(currentBaseLeftX, CenterY);
            Point pB = new Point(currentBaseRightX, CenterY);

            LH_TrapezoidSide.Point = pA;
            LH_TrapezoidWallSegment.Point = new Point(leftShoulderX, PeakY);
            WidthPassbandEnd.Point = new Point(rightShoulderX, PeakY);
            RH_TrapezoidSide.Point = pB;
            PathFigureEndPoint.Point = new Point(border2X, CenterY);

            // Notch Section (Firebrick) - Flat when fully up at PeakY, V-shape when lowered
            if (NotchDepth > 0 && nY > PeakY)
            {
                NotchFigureStart.StartPoint = new Point(Math.Max(leftShoulderX, nX - 12), PeakY);
                NotchLeftShoulderSegment.Point = new Point(Math.Max(leftShoulderX, nX - 12), PeakY);
                NotchTipSegment.Point = new Point(nX, nY);
                NotchRightShoulderSegment.Point = new Point(Math.Min(rightShoulderX, nX + 12), PeakY);
            }
            else
            {
                // Collapse into flat horizontal line along the top when fully up
                NotchFigureStart.StartPoint = new Point(leftShoulderX, PeakY);
                NotchLeftShoulderSegment.Point = new Point(leftShoulderX, PeakY);
                NotchTipSegment.Point = new Point(rightShoulderX, PeakY);
                NotchRightShoulderSegment.Point = new Point(rightShoulderX, PeakY);
            }

            // Notch Section (Firebrick) - Hidden when off/fully up, V-shape when lowered
            if (NotchDepth > 0 && nY > PeakY)
            {
                if (NotchPath != null) NotchPath.Visibility = Visibility.Visible;
                NotchFigureStart.StartPoint = new Point(Math.Max(leftShoulderX, nX - 12), PeakY);
                NotchLeftShoulderSegment.Point = new Point(Math.Max(leftShoulderX, nX - 12), PeakY);
                NotchTipSegment.Point = new Point(nX, nY);
                NotchRightShoulderSegment.Point = new Point(Math.Min(rightShoulderX, nX + 12), PeakY);
            }
            else
            {
                if (NotchPath != null) NotchPath.Visibility = Visibility.Collapsed;
            }

            // Contour Section (DodgerBlue) - Dynamic Arc Segment Shape
            if (ContourEnabled && ContourArcSegment != null)
            {
                // Ensure contour center stays locked inside the active passband shoulders
                cX = Math.Max(leftShoulderX, Math.Min(rightShoulderX, cX));

                // Dynamic max arc width (default 60px, but shrinks if passband width is narrow)
                double maxArcWidth = Math.Min(60.0, rightShoulderX - leftShoulderX);
                double halfArcWidth = maxArcWidth / 2.0;

                double startX = Math.Max(leftShoulderX, cX - halfArcWidth);
                double endX = Math.Min(rightShoulderX, cX + halfArcWidth);
                double actualSpan = endX - startX;

                ContourFigureStart.StartPoint = new Point(startX, PeakY);
                ContourArcSegment.Point = new Point(endX, PeakY);

                double offset = cY - PeakY;

                if (Math.Abs(offset) < 0.5)
                {
                    ContourArcSegment.Size = new Size(actualSpan / 2.0, 0);
                    ContourArcSegment.SweepDirection = SweepDirection.Clockwise;
                }
                else if (offset < 0)
                {
                    // Thumb is UP -> Well / U-shape
                    double maxUpTravel = PeakY - 5.0;
                    double currentHeight = Math.Min(Math.Abs(offset), maxUpTravel);

                    ContourArcSegment.SweepDirection = SweepDirection.Clockwise;
                    ContourArcSegment.Size = new Size(actualSpan / 2.0, currentHeight);
                }
                else
                {
                    // Thumb is DOWN -> Dome / Hill
                    double maxDownTravel = CenterY - PeakY;
                    double currentHeight = Math.Min(offset, maxDownTravel);

                    ContourArcSegment.SweepDirection = SweepDirection.Counterclockwise;
                    ContourArcSegment.Size = new Size(actualSpan / 2.0, currentHeight);
                }
            }
            else if (ContourArcSegment != null)
            {
                ContourFigureStart.StartPoint = new Point(rightShoulderX, PeakY);
                ContourArcSegment.Point = new Point(rightShoulderX, PeakY);
                ContourArcSegment.Size = new Size(0, 0);
            }

            // DNR Section
            DnrRightFigure.StartPoint = new Point(border2X, CenterY);
            DnrRightSegment.Point1 = new Point(border2X + (dnrX - border2X) * 0.5, CenterY);
            DnrRightSegment.Point2 = new Point(dnrX - (dnrX - border2X) * 0.1, dnrY);
            DnrRightSegment.Point3 = new Point(dnrX, dnrY);

            DnrRightSegment2.Point1 = new Point(dnrX + (border3X - dnrX) * 0.1, dnrY);
            DnrRightSegment2.Point2 = new Point(border3X - (border3X - dnrX) * 0.5, CenterY);
            DnrRightSegment2.Point3 = new Point(border3X, CenterY);
        }
    }
}