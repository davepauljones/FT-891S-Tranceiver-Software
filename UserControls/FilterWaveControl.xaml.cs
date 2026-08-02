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

    // FT-891 CAT filter parameter enums/ranges.
    // These describe CAT protocol values; they are not UI-specific values.
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
        Zero = 0,
        Plus = 1
    }

    public enum Ft891WidthCode
    {
        Code00 = 0,
        Code01 = 1,
        Code02 = 2,
        Code03 = 3,
        Code04 = 4,
        Code05 = 5,
        Code06 = 6,
        Code07 = 7,
        Code08 = 8,
        Code09 = 9,
        Code10 = 10,
        Code11 = 11,
        Code12 = 12,
        Code13 = 13,
        Code14 = 14,
        Code15 = 15,
        Code16 = 16,
        Code17 = 17,
        Code18 = 18,
        Code19 = 19,
        Code20 = 20,
        Code21 = 21
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
        public const int IfShiftMagnitudeMinHz = 0;
        public const int IfShiftMagnitudeMaxHz = 1200;

        public const int WidthCodeMin = 0;
        public const int WidthCodeMax = 21;
    }

    public partial class FilterWaveControl : UserControl, INotifyPropertyChanged
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

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<string, object> UIValueChanged;

        public int NbValue { get; private set; }
        public Ft891WidthCode WidthCode { get; private set; } = Ft891WidthCode.Code00;
        public int WidthValue { get { return (int)WidthCode; } }

        public bool WidthEnabled { get; private set; } = true;

        public int NotchFrequency { get; private set; } = Ft891FilterRanges.NotchFrequencyMinHz;
        public Ft891OnOff NotchState { get; private set; } = Ft891OnOff.Off;
        public bool NotchEnabled { get { return NotchState == Ft891OnOff.On; } }

        // Compatibility aliases for existing consumers.
        public int NotchFreq { get { return NotchFrequency; } }
        [Obsolete("FT-891 CAT has no notch-depth parameter. Use NotchEnabled/NotchState instead.")]
        public int NotchDepth { get { return NotchEnabled ? 100 : 0; } }

        public int ContourFrequency { get; private set; } = 1600;
        public int ContourValue { get { return ContourFrequency; } }
        public bool ContourEnabled { get; private set; }

        // IF SHIFT is a signed value. CAT sends magnitude plus a separate +/- direction.
        public int ShiftValue { get; private set; }
        public Ft891IfShiftState ShiftState { get; private set; } = Ft891IfShiftState.On;
        public Ft891IfShiftDirection ShiftDirection
        {
            get
            {
                if (ShiftValue < 0) return Ft891IfShiftDirection.Minus;
                if (ShiftValue > 0) return Ft891IfShiftDirection.Plus;
                return Ft891IfShiftDirection.Zero;
            }
        }
        public int ShiftMagnitude { get { return Math.Abs(ShiftValue); } }

        public int DnrValue { get; private set; }

        private double _notchVisualDepth = 0.0;
        private double _contourVisualOffset = 0.0;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        // The CAT IF-SHIFT value is in Hz; the drawing uses pixels.
        // This mapping is deliberately UI-only. The CAT value remains -1200..+1200 Hz.
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
            OnPropertyChanged(nameof(NotchDepth));
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
            _notchVisualDepth = value ? 1.0 : 0.0;
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
                UpdateThumbPositions(); // 1. Calculate and place the thumbs physically
                UpdateWaveform();       // 2. Draw the visual trapezoid matching those positions
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

        // CAT SH P3 is a 00-21 code, not a percentage.
        public void SetWidthValue(int value)
        {
            SetWidthCodeInternal(value);
            if (WidthBadge != null) WidthBadge.Text = "WD: " + ((int)WidthCode).ToString("D2");
            UpdateThumbPositions();
            UpdateWaveform();
        }

        // Compatibility method: the old "depth" argument is now treated as ON/OFF
        // because the FT-891 BP command has no notch-depth parameter.
        public void SetNotchValues(int freq, int depth)
        {
            SetNotchFrequencyInternal(freq);
            SetNotchEnabledInternal(depth != 0);
            _notchVisualDepth = depth != 0 ? 1.0 : 0.0;

            if (NotchBadge != null)
                NotchBadge.Text = !NotchEnabled ? "NCH: OFF" : "NCH: " + NotchFrequency + " Hz";

            UpdateThumbPositions();
            UpdateWaveform();
        }

        // value is the FT-891 CO contour frequency in Hz.
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

            // 2. WIDTH: SH P3 is a discrete 00-21 CAT code.
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

            // 3. MANUAL NOTCH: BP frequency is 10-3200 Hz in 10 Hz steps.
            double notchRatio =
                (NotchFrequency - Ft891FilterRanges.NotchFrequencyMinHz) /
                (double)(Ft891FilterRanges.NotchFrequencyMaxHz - Ft891FilterRanges.NotchFrequencyMinHz);

            double nX = border1X + (notchRatio * (border2X - border1X));
            double clampedNotchX = Math.Max(leftShoulderX, Math.Min(rightShoulderX, nX));
            // The CAT frequency is not changed merely because the visual passband
            // is narrower. The thumb is visually clamped, but the CAT value remains
            // the value supplied by the radio/user.
            nX = clampedNotchX;

            double nY = NotchEnabled
                ? PeakY + (_notchVisualDepth * (CenterY - PeakY))
                : PeakY;

            Canvas.SetLeft(NotchThumb, nX - 9);
            Canvas.SetTop(NotchThumb, nY - 9);

            // 4. CONTOUR: CO P3 is a 10-3200 Hz frequency.
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

            // 6. IF SHIFT:
            // The Shift thumb is the handle for the RH side of the active
            // passband.  It must sit on the bottom-right trapezoid edge,
            // NOT at an absolute position representing the signed CAT value.
            //
            // ShiftValue remains the CAT value (-1200..+1200 Hz); only the
            // visual position is tied to the current passband geometry.
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

            // The thumb is attached to the RH trapezoid side.  Its drag is
            // therefore translated into movement of the passband centre.
            double currentCenter = BaseCenter + ShiftToPixels(ShiftValue);

            // Keep the existing passband width while moving its centre.
            double maxHalfWidth = (border2X - border1X - (2 * FixedSlopeWidth)) / 2.0;
            double halfWidth = Math.Max(0.0, Canvas.GetLeft(WidthThumb) + 8.0 - currentCenter);

            // The RH trapezoid side is centre + half-width + slope.
            // A dragged RH edge therefore moves the centre by the same amount.
            double currentBaseRightX = currentCenter + halfWidth + FixedSlopeWidth;
            double targetBaseRightX = currentBaseRightX + e.HorizontalChange;

            // Keep the complete trapezoid inside the filter section.
            double minBaseRightX = border1X + (2.0 * FixedSlopeWidth);
            double maxBaseRightX = border2X - 1.0;

            // Also retain the current width when determining the centre limits.
            double minCenter = border1X + FixedSlopeWidth + halfWidth;
            double maxCenter = border2X - FixedSlopeWidth - halfWidth;

            double targetCenter = targetBaseRightX - halfWidth - FixedSlopeWidth;
            targetCenter = Math.Max(minCenter, Math.Min(maxCenter, targetCenter));

            double ratio =
                (targetCenter - (BaseCenter + ShiftToPixels(Ft891FilterRanges.IfShiftMinHz))) /
                (ShiftToPixels(Ft891FilterRanges.IfShiftMaxHz) -
                 ShiftToPixels(Ft891FilterRanges.IfShiftMinHz));

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

            if (WidthBadge != null)
                WidthBadge.Text = "WD: " + ((int)WidthCode).ToString("D2");

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

        private void NotchThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double currentCenter = BaseCenter + ShiftToPixels(ShiftValue);
            double currentLeft = Canvas.GetLeft(NotchThumb);
            double currentTop = Canvas.GetTop(NotchThumb);

            double targetCenterX = currentLeft + 9 + e.HorizontalChange;
            double targetCenterY = currentTop + 9 + e.VerticalChange;

            double wX = Canvas.GetLeft(WidthThumb) + 8;
            double halfWidth = Math.Max(0, wX - currentCenter);

            double leftShoulderX = currentCenter - halfWidth;
            double rightShoulderX = currentCenter + halfWidth;

            if ((leftShoulderX - FixedSlopeWidth) < border1X)
                leftShoulderX = border1X + FixedSlopeWidth;
            if ((rightShoulderX + FixedSlopeWidth) > border2X)
                rightShoulderX = border2X - FixedSlopeWidth;

            double clampedCenterX = Math.Max(leftShoulderX, Math.Min(rightShoulderX, targetCenterX));
            double clampedTop = Math.Max(PeakY, Math.Min(CenterY, targetCenterY));

            Canvas.SetLeft(NotchThumb, clampedCenterX - 9);
            Canvas.SetTop(NotchThumb, clampedTop - 9);

            int frequency = (int)Math.Round(
                Ft891FilterRanges.NotchFrequencyMinHz +
                ((clampedCenterX - border1X) / (border2X - border1X)) *
                (Ft891FilterRanges.NotchFrequencyMaxHz - Ft891FilterRanges.NotchFrequencyMinHz));

            SetNotchFrequencyInternal(frequency);

            // The FT-891 BP command has ON/OFF, but no depth parameter.
            // Vertical movement is retained only as the visual ON/OFF gesture.
            bool enabled = clampedTop > PeakY + 2;
            SetNotchEnabledInternal(enabled);
            _notchVisualDepth = enabled
                ? Math.Max(0.0, Math.Min(1.0, (clampedTop - PeakY) / (CenterY - PeakY)))
                : 0.0;

            if (NotchBadge != null)
                NotchBadge.Text = !NotchEnabled ? "NCH: OFF" : "NCH: " + NotchFrequency + " Hz";

            UpdateWaveform();

            UIValueChanged?.Invoke("NCH_FREQ", NotchFrequency);
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

            // CO has no contour "depth" parameter. Keep vertical movement as
            // a visual curve control only; CAT value remains the frequency.
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
                    UpdateWaveform();
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
                    UpdateWaveform();
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
                NotchFigureStart == null || NotchLeftShoulderSegment == null || NotchTipSegment == null || NotchRightShoulderSegment == null ||
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
            double nY = Canvas.GetTop(NotchThumb) + 9;
            nX = Math.Max(leftShoulderX, Math.Min(rightShoulderX, nX));

            double cX = Canvas.GetLeft(ContourThumb) + 8;
            double cY = Canvas.GetTop(ContourThumb) + 8;
            cX = Math.Max(leftShoulderX, Math.Min(rightShoulderX, cX));

            if (!NotchEnabled) nY = PeakY;
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
            if (NotchEnabled && nY > PeakY)
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
            if (NotchEnabled && nY > PeakY)
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