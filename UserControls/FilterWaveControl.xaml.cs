using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace YAESU_FT_891_Front_End
{
    public enum Ft891OnOff
    {
        Off = 0,
        On = 1
    }

    public enum Ft891ManualNotchParameter
    {
        Enable = 0,
        Frequency = 1
    }

    public enum Ft891ContourParameter
    {
        ContourOnOff = 0,
        ContourFrequency = 1,
        ApfOnOff = 2,
        ApfFrequency = 3
    }

    public enum Ft891IfShiftState
    {
        Off = 0,
        On = 1
    }

    public enum Ft891IfShiftDirection
    {
        Minus = -1,
        Plus = 1
    }

    public enum Ft891WidthCode
    {
        Code00 = 0, Code01 = 1, Code02 = 2, Code03 = 3, Code04 = 4, Code05 = 5,
        Code06 = 6, Code07 = 7, Code08 = 8, Code09 = 9, Code10 = 10, Code11 = 11,
        Code12 = 12, Code13 = 13, Code14 = 14, Code15 = 15, Code16 = 16, Code17 = 17,
        Code18 = 18, Code19 = 19, Code20 = 20, Code21 = 21
    }

    public static class Ft891FilterRanges
    {
        public const int NotchFrequencyMinHz = 10;
        public const int NotchFrequencyMaxHz = 3200;
        public const int NotchFrequencyStepHz = 10;

        public const int ContourFrequencyMinHz = 10;
        public const int ContourFrequencyMaxHz = 3200;
        public const int ContourFrequencyStepHz = 10;

        public const int IfShiftMinHz = -1200;
        public const int IfShiftMaxHz = 1200;
        public const int IfShiftStepHz = 20;

        public const int WidthCodeMin = 0;
        public const int WidthCodeMax = 21;
    }

    public partial class FilterWaveControl : UserControl, INotifyPropertyChanged
    {
        private const double CanvasHeight = 102;
        private const double CenterY = 51;
        private const double PeakY = 20.0;
        private bool _isUpdatingProgrammatically = false;

        private const double BaseCenter = 320.0;
        private const double border1X = 84.0;
        private const double border2X = 556.0;
        private const double FixedSlopeWidth = 8.0;

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<string, object> UIValueChanged;

        public int NbValue { get; private set; }
        public Ft891WidthCode WidthCode { get; private set; } = Ft891WidthCode.Code00;
        public int WidthValue { get { return (int)WidthCode; } }
        public bool WidthEnabled { get; private set; } = true;

        public int NotchFrequency { get; private set; } = Ft891FilterRanges.NotchFrequencyMinHz;
        public Ft891OnOff NotchState { get; private set; } = Ft891OnOff.Off;
        public bool NotchEnabled { get { return NotchState == Ft891OnOff.On; } }

        public int NotchFreq { get { return NotchFrequency; } }

        public int ContourFrequency { get; private set; } = 1600;
        public int ContourValue { get { return ContourFrequency; } }
        public Ft891OnOff ContourState { get; private set; } = Ft891OnOff.Off;
        public bool ContourEnabled { get; private set; }

        public int ShiftValue { get; private set; }
        public Ft891IfShiftState ShiftState { get; private set; } = Ft891IfShiftState.On;
        public Ft891IfShiftDirection ShiftDirection
        {
            get
            {
                if (ShiftValue < 0) return Ft891IfShiftDirection.Minus;
                return Ft891IfShiftDirection.Plus;
            }
        }
        public int ShiftMagnitude { get { return Math.Abs(ShiftValue); } }

        public int DnrValue { get; private set; }
        private double _contourVisualOffset = 0.0;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private static double ShiftToPixels(int shiftHz)
        {
            const double maxPixelShift = ((border2X - border1X) / 2.0) - FixedSlopeWidth;
            return (shiftHz / (double)Ft891FilterRanges.IfShiftMaxHz) * maxPixelShift;
        }

        private static int SnapToStep(int value, int min, int max, int step)
        {
            value = Clamp(value, min, max);
            int steps = (int)Math.Round((value - min) / (double)step);
            return Clamp(min + (steps * step), min, max);
        }

        private void SetWidthCodeInternal(int value)
        {
            value = Clamp(value, Ft891FilterRanges.WidthCodeMin, Ft891FilterRanges.WidthCodeMax);
            WidthCode = (Ft891WidthCode)value;
            OnPropertyChanged(nameof(WidthCode));
            OnPropertyChanged(nameof(WidthValue));
        }

        private void SetNotchFrequencyInternal(int value)
        {
            value = SnapToStep(value, Ft891FilterRanges.NotchFrequencyMinHz,
                Ft891FilterRanges.NotchFrequencyMaxHz, Ft891FilterRanges.NotchFrequencyStepHz);

            if (NotchFrequency == value) return;
            NotchFrequency = value;
            OnPropertyChanged(nameof(NotchFrequency));
            OnPropertyChanged(nameof(NotchFreq));
        }

        private void SetContourFrequencyInternal(int value)
        {
            value = SnapToStep(value, Ft891FilterRanges.ContourFrequencyMinHz,
                Ft891FilterRanges.ContourFrequencyMaxHz, Ft891FilterRanges.ContourFrequencyStepHz);

            if (ContourFrequency == value) return;
            ContourFrequency = value;
            OnPropertyChanged(nameof(ContourFrequency));
            OnPropertyChanged(nameof(ContourValue));
        }

        private void SetShiftValueInternal(int value)
        {
            value = SnapToStep(value, Ft891FilterRanges.IfShiftMinHz,
                Ft891FilterRanges.IfShiftMaxHz, Ft891FilterRanges.IfShiftStepHz);

            if (ShiftValue == value) return;
            ShiftValue = value;
            OnPropertyChanged(nameof(ShiftValue));
            OnPropertyChanged(nameof(ShiftDirection));
            OnPropertyChanged(nameof(ShiftMagnitude));
        }

        private void SetNotchEnabledInternal(bool enabled)
        {
            Ft891OnOff newState = enabled ? Ft891OnOff.On : Ft891OnOff.Off;
            if (NotchState == newState) return;
            NotchState = newState;
            OnPropertyChanged(nameof(NotchState));
            OnPropertyChanged(nameof(NotchEnabled));
        }

        public void SetWidthEnabled(bool value)
        {
            if (WidthEnabled == value) return;
            WidthEnabled = value;
            OnPropertyChanged(nameof(WidthEnabled));
            UpdateThumbPositions();
            UpdateWaveform();
        }

        public void SetNotchFrequency(int value)
        {
            SetNotchFrequencyInternal(value);
            UpdateThumbPositions();
            UpdateWaveform();
        }

        public void SetNotchEnabled(bool value)
        {
            SetNotchEnabledInternal(value);
            UpdateThumbPositions();
            UpdateWaveform();
        }

        public void SetContourFrequency(int value)
        {
            SetContourFrequencyInternal(value);
            UpdateThumbPositions();
            UpdateWaveform();
        }

        public void SetShiftValue(int value)
        {
            SetShiftValueInternal(value);
            UpdateThumbPositions();
            UpdateWaveform();
        }

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
            NbValue = Clamp(value, 0, 10);
            OnPropertyChanged(nameof(NbValue));
            if (NbTextBlock != null) NbTextBlock.Text = NbValue == 0 ? "NB: OFF" : "NB: " + NbValue;
            if (NbBadge != null) NbBadge.Text = NbValue == 0 ? "NB: OFF" : "NB: " + NbValue;
            UpdateThumbPositions();
            UpdateWaveform();
        }

        public void SetWidthValue(int value)
        {
            SetWidthCodeInternal(value);
            if (WidthBadge != null) WidthBadge.Text = "WD: " + ((int)WidthCode).ToString("D2");
            UpdateThumbPositions();
            UpdateWaveform();
        }

        public void SetNotchValues(int freq, bool enabled)
        {
            SetNotchFrequencyInternal(freq);
            SetNotchEnabledInternal(enabled);

            if (NotchBadge != null)
                NotchBadge.Text = !NotchEnabled ? "NCH: OFF" : "NCH: " + NotchFrequency + " Hz";

            UpdateThumbPositions();
            UpdateWaveform();
        }

        // Compatibility overload for tuple/old signatures if needed
        public void SetNotchValues(int freq, int depth)
        {
            SetNotchValues(freq, depth != 0);
        }

        public void SetContourValues(int value, bool enabled)
        {
            SetContourFrequencyInternal(value);
            ContourEnabled = enabled;
            OnPropertyChanged(nameof(ContourEnabled));

            if (ContourBadge != null)
                ContourBadge.Text = !ContourEnabled ? "CNT: OFF" : "CNT: " + ContourFrequency + " Hz";

            UpdateThumbPositions();
            UpdateWaveform();
        }

        public void SetDnrValue(int value)
        {
            DnrValue = Clamp(value, 0, 15);
            OnPropertyChanged(nameof(DnrValue));
            if (DNRTextBlock != null) DNRTextBlock.Text = DnrValue == 0 ? "NR: OFF" : "NR: " + DnrValue.ToString("D2");
            if (DnrBadge != null) DnrBadge.Text = DnrValue == 0 ? "NR: OFF" : "NR: " + DnrValue.ToString("D2");
            UpdateThumbPositions();
            UpdateWaveform();
        }

        public void SetDnfEnabled(bool value) { _isUpdatingProgrammatically = true; if (AutoDnfBtn != null) AutoDnfBtn.IsChecked = value; _isUpdatingProgrammatically = false; }
        public void SetApfEnabled(bool value) { _isUpdatingProgrammatically = true; if (CwApfBtn != null) CwApfBtn.IsChecked = value; _isUpdatingProgrammatically = false; }
        public void SetContourEnabled(bool value)
        {
            _isUpdatingProgrammatically = true;
            if (ContourBtn != null) ContourBtn.IsChecked = value;
            if (ContourEnabled != value)
            {
                ContourEnabled = value;
                OnPropertyChanged(nameof(ContourEnabled));
            }
            UpdateThumbPositions();
            UpdateWaveform();
            _isUpdatingProgrammatically = false;
        }

        private void UpdateThumbPositions()
        {
            if (NbThumb == null || WidthThumb == null || NotchThumb == null ||
                DnrThumb == null || ContourThumb == null || ShiftThumb == null) return;

            _isUpdatingProgrammatically = true;

            // 1. Noise Blanker Position
            double travelSpaceNb = CenterY - 5;
            double nbTop = CenterY - ((NbValue / 10.0) * travelSpaceNb);
            Canvas.SetTop(NbThumb, Math.Max(5, Math.Min(CenterY, nbTop)));

            // 2. WIDTH Position
            double currentCenter = BaseCenter + ShiftToPixels(ShiftValue);
            double maxHalfWidth = (border2X - border1X - (2 * FixedSlopeWidth)) / 2.0;
            const double thumbHalfWidth = 8.0;
            double minX = currentCenter;
            double maxX = currentCenter + maxHalfWidth - thumbHalfWidth;

            double widthRatio = (int)WidthCode / (double)Ft891FilterRanges.WidthCodeMax;
            double wX = minX + (widthRatio * (maxX - minX));
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

            // 3. MANUAL NOTCH: Locked strictly to baseline (PeakY), horizontal move only
            double notchRatio =
                (NotchFrequency - Ft891FilterRanges.NotchFrequencyMinHz) /
                (double)(Ft891FilterRanges.NotchFrequencyMaxHz - Ft891FilterRanges.NotchFrequencyMinHz);

            double nX = border1X + (notchRatio * (border2X - border1X));
            double clampedNotchX = Math.Max(leftShoulderX, Math.Min(rightShoulderX, nX));

            Canvas.SetLeft(NotchThumb, clampedNotchX - 9);
            Canvas.SetTop(NotchThumb, PeakY - 19); // Locked to baseline Y

            // 4. CONTOUR Position
            double contourRatio =
                (ContourFrequency - Ft891FilterRanges.ContourFrequencyMinHz) /
                (double)(Ft891FilterRanges.ContourFrequencyMaxHz - Ft891FilterRanges.ContourFrequencyMinHz);

            double cX = border1X + (contourRatio * (border2X - border1X));
            cX = Math.Max(leftShoulderX, Math.Min(rightShoulderX, cX));
            Canvas.SetLeft(ContourThumb, cX - 8);

            double contourCenterY = PeakY + _contourVisualOffset;
            if (!ContourEnabled) contourCenterY = PeakY;
            Canvas.SetTop(ContourThumb, contourCenterY - 8);

            // 5. DNR Position
            double travelSpaceDnr = CenterY - 5;
            double dnrTop = CenterY - ((DnrValue / 15.0) * travelSpaceDnr);
            Canvas.SetTop(DnrThumb, Math.Max(5, Math.Min(CenterY, dnrTop)));

            // 6. IF SHIFT Position
            Canvas.SetLeft(ShiftThumb, currentBaseRightX);
            Canvas.SetTop(ShiftThumb, CenterY - 8);

            if (WidthBadge != null)
                WidthBadge.Text = "WD: " + ((int)WidthCode).ToString("D2");

            if (NotchBadge != null)
                NotchBadge.Text = !NotchEnabled ? "NCH: OFF" : "NCH: " + NotchFrequency + " Hz";

            if (ContourBadge != null)
                ContourBadge.Text = !ContourEnabled ? "CNT: OFF" : "CNT: " + ContourFrequency + " Hz";

            if (ShiftBadge != null)
                ShiftBadge.Text = "SF: " + (ShiftValue > 0 ? "+" : "") + ShiftValue;

            _isUpdatingProgrammatically = false;
        }

        private void ShiftThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_isUpdatingProgrammatically) return;

            double currentCenter = BaseCenter + ShiftToPixels(ShiftValue);
            double maxHalfWidth = (border2X - border1X - (2 * FixedSlopeWidth)) / 2.0;
            double halfWidth = Math.Max(0.0, Canvas.GetLeft(WidthThumb) + 8.0 - currentCenter);

            double currentBaseRightX = currentCenter + halfWidth + FixedSlopeWidth;
            double targetBaseRightX = currentBaseRightX + e.HorizontalChange;

            double minCenter = border1X + FixedSlopeWidth + halfWidth;
            double maxCenter = border2X - FixedSlopeWidth - halfWidth;

            double targetCenter = targetBaseRightX - halfWidth - FixedSlopeWidth;
            targetCenter = Math.Max(minCenter, Math.Min(maxCenter, targetCenter));

            double ratio =
                (targetCenter - (BaseCenter + ShiftToPixels(Ft891FilterRanges.IfShiftMinHz))) /
                (ShiftToPixels(Ft891FilterRanges.IfShiftMaxHz) - ShiftToPixels(Ft891FilterRanges.IfShiftMinHz));

            int newShift = (int)Math.Round(
                Ft891FilterRanges.IfShiftMinHz +
                ratio * (Ft891FilterRanges.IfShiftMaxHz - Ft891FilterRanges.IfShiftMinHz));

            SetShiftValueInternal(newShift);
            UpdateThumbPositions();
            UpdateWaveform();

            UIValueChanged?.Invoke("SHIFT", ShiftValue);
        }

        private void WidthThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_isUpdatingProgrammatically) return;

            double currentCenter = BaseCenter + ShiftToPixels(ShiftValue);
            double currentLeft = Canvas.GetLeft(WidthThumb);
            double newLeft = currentLeft + e.HorizontalChange;

            double minX = currentCenter;
            double maxHalfWidth = (border2X - border1X - (2 * FixedSlopeWidth)) / 2.0;
            double maxX = currentCenter + maxHalfWidth - 8.0;

            if (newLeft < minX) newLeft = minX;
            if (newLeft > maxX) newLeft = maxX;

            double percentage = ((newLeft - minX) / (maxX - minX));
            int code = (int)Math.Round(percentage * Ft891FilterRanges.WidthCodeMax);

            SetWidthCodeInternal(code);
            UpdateThumbPositions();
            UpdateWaveform();

            UIValueChanged?.Invoke("WIDTH", (int)WidthCode);
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
            OnPropertyChanged(nameof(NbValue));

            if (NbTextBlock != null) NbTextBlock.Text = NbValue == 0 ? "NB: OFF" : "NB: " + NbValue;
            if (NbBadge != null) NbBadge.Text = NbValue == 0 ? "NB: OFF" : "NB: " + NbValue;

            UpdateWaveform();
            UIValueChanged?.Invoke("NB", NbValue);
        }

        // Horizontal-only drag handler for Notch (No vertical movement permitted)
        private void NotchThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_isUpdatingProgrammatically) return;

            double currentCenter = BaseCenter + ShiftToPixels(ShiftValue);
            double wX = Canvas.GetLeft(WidthThumb) + 8;
            double halfWidth = Math.Max(0, wX - currentCenter);

            double leftShoulderX = currentCenter - halfWidth;
            double rightShoulderX = currentCenter + halfWidth;

            if ((leftShoulderX - FixedSlopeWidth) < border1X)
                leftShoulderX = border1X + FixedSlopeWidth;

            if ((rightShoulderX + FixedSlopeWidth) > border2X)
                rightShoulderX = border2X - FixedSlopeWidth;

            double logicalX =
                border1X +
                ((NotchFrequency - Ft891FilterRanges.NotchFrequencyMinHz) /
                (double)(Ft891FilterRanges.NotchFrequencyMaxHz - Ft891FilterRanges.NotchFrequencyMinHz)) *
                (border2X - border1X);

            logicalX += e.HorizontalChange;
            logicalX = Math.Max(border1X, Math.Min(border2X, logicalX));

            double frequencyRatio = (logicalX - border1X) / (border2X - border1X);
            int frequency = (int)Math.Round(
                Ft891FilterRanges.NotchFrequencyMinHz +
                frequencyRatio * (Ft891FilterRanges.NotchFrequencyMaxHz - Ft891FilterRanges.NotchFrequencyMinHz));

            frequency = SnapToStep(frequency, Ft891FilterRanges.NotchFrequencyMinHz,
                Ft891FilterRanges.NotchFrequencyMaxHz, Ft891FilterRanges.NotchFrequencyStepHz);

            SetNotchFrequencyInternal(frequency);

            double visualX =
                border1X +
                ((NotchFrequency - Ft891FilterRanges.NotchFrequencyMinHz) /
                (double)(Ft891FilterRanges.NotchFrequencyMaxHz - Ft891FilterRanges.NotchFrequencyMinHz)) *
                (border2X - border1X);

            double clampedCenterX = Math.Max(leftShoulderX, Math.Min(rightShoulderX, visualX));

            // Constrain X position, strictly pin Y to PeakY (baseline)
            Canvas.SetLeft(NotchThumb, clampedCenterX - 9);
            Canvas.SetTop(NotchThumb, PeakY - 9);

            // Automatically enable when dragged/active if not already enabled
            SetNotchEnabledInternal(true);

            if (NotchBadge != null)
                NotchBadge.Text = "NCH: " + NotchFrequency + " Hz";

            UpdateThumbPositions();
            UpdateWaveform();

            UIValueChanged?.Invoke("NCH_FREQ", NotchFrequency / 10);
            UIValueChanged?.Invoke("NCH_ENABLED", NotchEnabled);
        }

        private void ContourThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double currentCenter = BaseCenter + ShiftToPixels(ShiftValue);
            double currentLeft = Canvas.GetLeft(ContourThumb);
            double currentTop = Canvas.GetTop(ContourThumb);

            double targetCenterX = currentLeft + 8 + e.HorizontalChange;
            double targetCenterY = currentTop + 8 + e.VerticalChange;

            double wX = Canvas.GetLeft(WidthThumb) + 8;
            double halfWidth = Math.Max(0, wX - currentCenter);

            double leftShoulderX = currentCenter - halfWidth;
            double rightShoulderX = currentCenter + halfWidth;

            if ((leftShoulderX - FixedSlopeWidth) < border1X)
                leftShoulderX = border1X + FixedSlopeWidth;
            if ((rightShoulderX + FixedSlopeWidth) > border2X)
                rightShoulderX = border2X - FixedSlopeWidth;

            double clampedCenterX = Math.Max(leftShoulderX, Math.Min(rightShoulderX, targetCenterX));
            double clampedTop = Math.Max(5, Math.Min(CenterY, targetCenterY));

            Canvas.SetLeft(ContourThumb, clampedCenterX - 8);
            Canvas.SetTop(ContourThumb, clampedTop - 8);

            int frequency = (int)Math.Round(
                Ft891FilterRanges.ContourFrequencyMinHz +
                ((clampedCenterX - border1X) / (border2X - border1X)) *
                (Ft891FilterRanges.ContourFrequencyMaxHz - Ft891FilterRanges.ContourFrequencyMinHz));

            SetContourFrequencyInternal(frequency);

            _contourVisualOffset = clampedTop + 8 - PeakY;
            _contourVisualOffset = Math.Max(-(PeakY - 5), Math.Min(CenterY - PeakY, _contourVisualOffset));

            ContourEnabled = true;
            OnPropertyChanged(nameof(ContourEnabled));

            if (ContourBadge != null)
                ContourBadge.Text = "CNT: " + ContourFrequency + " Hz";

            UpdateWaveform();
            UIValueChanged?.Invoke("CONTOUR_FREQ", ContourFrequency);
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
            OnPropertyChanged(nameof(DnrValue));

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
                if (btn == ContourBtn)
                {
                    ContourEnabled = true;
                    OnPropertyChanged(nameof(ContourEnabled));
                    ContourState = Ft891OnOff.On;
                    UIValueChanged?.Invoke("CONTOUR_ENABLED", ContourEnabled);
                    ContourThumb.Visibility = Visibility.Visible;
                    ContourPath.Visibility = Visibility.Visible;
                    UpdateWaveform();
                }
                else if (btn == NotchBtn)
                {
                    OnPropertyChanged(nameof(NotchEnabled));
                    NotchState = Ft891OnOff.On;
                    UIValueChanged?.Invoke("NCH_ENABLED", NotchEnabled);
                    NotchThumb.Visibility = Visibility.Visible;
                    NotchPath.Visibility = Visibility.Visible;
                }
                UIValueChanged?.Invoke(btn.Content.ToString(), true);
            }
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingProgrammatically) return;
            ToggleButton btn = sender as ToggleButton;
            if (btn != null)
            {
                if (btn == ContourBtn)
                {
                    ContourEnabled = false;
                    OnPropertyChanged(nameof(ContourEnabled));
                    ContourState = Ft891OnOff.Off;
                    UIValueChanged?.Invoke("CONTOUR_ENABLED", ContourEnabled);
                    ContourThumb.Visibility = Visibility.Hidden;
                    ContourPath.Visibility = Visibility.Hidden;
                    UpdateWaveform();
                }
                else if (btn == NotchBtn)
                {
                    OnPropertyChanged(nameof(NotchEnabled));
                    NotchState = Ft891OnOff.Off;
                    UIValueChanged?.Invoke("NCH_ENABLED", NotchEnabled);
                    NotchThumb.Visibility = Visibility.Hidden;
                    NotchPath.Visibility = Visibility.Hidden;
                }
                UIValueChanged?.Invoke(btn.Content.ToString(), false);
            }
        }

        private void UpdateWaveform()
        {
            if (NbThumb == null || WidthThumb == null || NotchThumb == null || DnrThumb == null || ContourThumb == null || ShiftThumb == null ||
                Segment1 == null || Segment2 == null || PathFigureEndPoint == null ||
                DnrRightFigure == null || DnrRightSegment == null || DnrRightSegment2 == null ||
                LH_TrapezoidSide == null || LH_TrapezoidWallSegment == null || RH_TrapezoidSide == null || WidthPassbandEnd == null ||
                NotchFigureStart == null || NotchTipSegment == null || NotchRightShoulderSegment == null ||
                ContourFigureStart == null || ContourArcSegment == null)
                return;

            const double border0X = 0.0;
            const double border3X = 639.0;

            double nbX = 42.0;
            double nbY = Canvas.GetTop(NbThumb) + 8;

            double currentCenter = BaseCenter + ShiftToPixels(ShiftValue);
            double maxHalfWidth = (border2X - border1X - (2 * FixedSlopeWidth)) / 2.0;
            double minX = currentCenter;
            double maxX = currentCenter + maxHalfWidth - 8.0;

            double wX = minX + (((int)WidthCode / (double)Ft891FilterRanges.WidthCodeMax) * (maxX - minX)) + 8.0;
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
            nX = Math.Max(leftShoulderX, Math.Min(rightShoulderX, nX));

            double cX = Canvas.GetLeft(ContourThumb) + 8;
            double cY = Canvas.GetTop(ContourThumb) + 8;
            cX = Math.Max(leftShoulderX, Math.Min(rightShoulderX, cX));

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

            // Notch Section (Firebrick) - Fixed size V-shape pinned to baseline, visible only when enabled
            if (NotchEnabled)
            {
                if (NotchPath != null) NotchPath.Visibility = Visibility.Visible;

                const double vHalfWidth = 10.0;
                const double vDepth = 30.0; // Fixed depth downward

                NotchFigureStart.StartPoint = new Point(Math.Max(leftShoulderX, nX - vHalfWidth), PeakY);
                NotchTipSegment.Point = new Point(nX, PeakY + vDepth);
                NotchRightShoulderSegment.Point = new Point(Math.Min(rightShoulderX, nX + vHalfWidth), PeakY);
            }
            else
            {
                if (NotchPath != null) NotchPath.Visibility = Visibility.Collapsed;
            }

            // Contour Section (DodgerBlue)
            if (ContourEnabled && ContourArcSegment != null)
            {
                cX = Math.Max(leftShoulderX, Math.Min(rightShoulderX, cX));

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
                    double maxUpTravel = PeakY - 5.0;
                    double currentHeight = Math.Min(Math.Abs(offset), maxUpTravel);

                    ContourArcSegment.SweepDirection = SweepDirection.Clockwise;
                    ContourArcSegment.Size = new Size(actualSpan / 2.0, currentHeight);
                }
                else
                {
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