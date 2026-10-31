using FT891S_CatControl;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using YAESU_FT_891_Front_End.Models;
using static System.Windows.Forms.AxHost;
using static YAESU_FT_891_Front_End.MyStructs;

namespace YAESU_FT_891_Front_End
{
    public struct FilterTypes
    {
        public const int NB = 0;
        public const int Notch = 1;
        public const int Contour = 2;
        public const int Shift = 3;
        public const int Width = 4;
        public const int DNR = 5;
        public const int Narrow = 6;
        public const int AllOff = 7;
    }
    public struct States
    {
        public const bool Off = false;
        public const bool On = true;
    }
    public class FilterClass
    {
        public Int32 ID { get; set; }
        public String Name { get; set; }
        public int Value { get; set; }
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public int DefaultValue { get; set; }
        public bool Enabled { get; set; }
    }
    public class WidthExtractedValues
    {
        public int BandWidth { get; set; }
        public RadioMode RadioMode { get; set; }
        public WidthValues WidthValue { get; set; }
    }
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

    //public enum Ft891WidthCode
    //{
    //    Code00 = 0, Code01 = 1, Code02 = 2, Code03 = 3, Code04 = 4, Code05 = 5,
    //    Code06 = 6, Code07 = 7, Code08 = 8, Code09 = 9, Code10 = 10, Code11 = 11,
    //    Code12 = 12, Code13 = 13, Code14 = 14, Code15 = 15, Code16 = 16, Code17 = 17,
    //    Code18 = 18, Code19 = 19, Code20 = 20, Code21 = 21
    //}

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
        public const int WidthStepHz = 22;
    }

    public partial class FilterWaveControl : UserControl, INotifyPropertyChanged
    {
        MainWindow mainWindow;
        public List<FilterClass> filters = new List<FilterClass>();

        public readonly List<int> WidthFrequencies_SSB_Narrow = new List<int> { 1500, 200, 400, 600, 850, 1100, 1350, 1500, 1650, 1800, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
        public readonly List<int> WidthFrequencies_SSB_Wide = new List<int> { 2400, -1, -1, -1, -1, -1, -1, -1, -1, 1800, 1950, 2100, 2200, 2300, 2400, 2500, 2600, 2700, 2800, 2900, 3000, 3200 };
        public readonly List<int> WidthFrequencies_CW_Narrow = new List<int> { 500, 50, 100, 150, 200, 250, 300, 350, 400, 450, 500, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
        public readonly List<int> WidthFrequencies_CW_Wide = new List<int> { 2400, -1, -1, -1, -1, -1, -1, -1, -1, -1, 500, 800, 1200, 1400, 1700, 2000, 2400, 3000, -1, -1, -1, -1 };
        public readonly List<int> WidthFrequencies_RTTY_PSK_Narrow = new List<int> { 300, 50, 100, 150, 200, 250, 300, 350, 400, 450, 500, 300, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
        public readonly List<int> WidthFrequencies_RTTY_PSK_Wide = new List<int> { 500, -1, -1, -1, -1, -1, -1, -1, -1, -1, 500, 800, 1200, 1400, 1700, 2000, 2400, 3000, -1, -1, -1, -1 };
        
        public readonly int WidthFrequencies_SSB_Narrow_MinimumValue = 200;
        public readonly int WidthFrequencies_SSB_Wide_MinimumValue = 1800;
        public readonly int WidthFrequencies_CW_Narrow_MinimumValue = 50;
        public readonly int WidthFrequencies_CW_Wide_MinimumValue = 500;
        public readonly int WidthFrequencies_RTTY_PSK_Narrow_MinimumValue = 50;
        public readonly int WidthFrequencies_RTTY_PSK_Wide_MinimumValue = 500;

        private const double CanvasHeight = 102;
        private const double CenterY = 51;
        private const double PeakY = 25.0;
        private const double ContourPeakY = 51.0;
        private bool _isUpdatingProgrammatically = false;

        private const double BaseCenter = 320.0;
        private const double border1X = 84.0;
        private const double border2X = 556.0;
        private const double FixedSlopeWidth = 8.0;

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<string, object> UIValueChanged;

        public int NbValue { get; private set; }
        //public Ft891WidthCode WidthCode { get; private set; } = Ft891WidthCode.Code00;
        //public int WidthValue { get { return (int)WidthCode; } }
        public bool WidthEnabled { get; private set; } = true;

        public int NotchFrequency { get; private set; } = Ft891FilterRanges.NotchFrequencyMinHz;
        public Ft891OnOff NotchState { get; private set; } = Ft891OnOff.Off;
        public bool NotchEnabled { get { return NotchState == Ft891OnOff.On; } }

        public int NotchFreq { get { return NotchFrequency; } }

        public int ContourFrequency { get; private set; } = 1600;
        public int ContourValue { get { return ContourFrequency; } }
        public Ft891OnOff ContourState { get; private set; } = Ft891OnOff.Off;
        public bool ContourEnabled { get; private set; }

        //public int ShiftValue { get; private set; }
        public Ft891IfShiftState ShiftState { get; private set; } = Ft891IfShiftState.On;
        public Ft891IfShiftDirection ShiftDirection
        {
            get
            {
                if (FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftHz < 0) return Ft891IfShiftDirection.Minus;
                return Ft891IfShiftDirection.Plus;
            }
        }
        public int ShiftMagnitude { get { return Math.Abs(FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftHz); } }

        //public int DnrValue { get; private set; }
        
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
            value = SnapToStep(value, Ft891FilterRanges.IfShiftMinHz, Ft891FilterRanges.IfShiftMaxHz, Ft891FilterRanges.IfShiftStepHz);

            // Calculate current signed value to check for changes accurately
            var state = FT891S_CatManager.currentRadioState.IfShiftHz;
            int currentSignedValue = state.IfShiftHz * state.IfShiftDirection;
            if (currentSignedValue == value) return;

            // Update direction, on/off, and store the absolute magnitude in IfShiftHz
            if (value < 0)
            {
                state.IfShiftDirection = -1;
                state.IfShiftHz = -value; // Store as positive magnitude
            }
            else if (value > 0)
            {
                state.IfShiftDirection = 1;
                state.IfShiftHz = value;
            }
            else
            {
                state.IfShiftDirection = 1;
                state.IfShiftHz = 0;
            }
        }
        private void SetWidthCodeInternal(int value)
        {
            value = Clamp(value, Ft891FilterRanges.WidthCodeMin, Ft891FilterRanges.WidthCodeMax);
            FT891S_CatManager.currentRadioState.WidthValue = (WidthValues)value;
        }
        private void SetNotchEnabledInternal(bool enabled)
        {
            Ft891OnOff newState = enabled ? Ft891OnOff.On : Ft891OnOff.Off;
            if (NotchState == newState) return;
            NotchState = newState;
            OnPropertyChanged(nameof(NotchState));
            OnPropertyChanged(nameof(NotchEnabled));
        }

      

        public FilterWaveControl(MainWindow mainWindow)
        {
            InitializeComponent();

            this.mainWindow = mainWindow;

            Init_Filters();

            Loaded += (s, e) =>
            {
                UpdateThumbPositions();
                UpdateWaveform();
            }; 
        }

        private void Init_Filters()
        {
            filters.Add(new FilterClass { ID = FilterTypes.NB, Name = "NB", Value = 0, MinValue = 0, MaxValue = 10, DefaultValue = 0, Enabled = false });
            filters.Add(new FilterClass { ID = FilterTypes.Notch, Name = "NOTCH", Value = 0, MinValue = 0, MaxValue = 15, DefaultValue = 0, Enabled = false });
            filters.Add(new FilterClass { ID = FilterTypes.Contour, Name = "CONTOUR", Value = 0, MinValue = 0, MaxValue = 15, DefaultValue = 0, Enabled = false });
            filters.Add(new FilterClass { ID = FilterTypes.Shift, Name = "SHIFT", Value = 0, MinValue = 0, MaxValue = 15, DefaultValue = 0, Enabled = false });
            filters.Add(new FilterClass { ID = FilterTypes.Width, Name = "WIDTH", Value = 0, MinValue = 0, MaxValue = 15, DefaultValue = 0, Enabled = false });
            filters.Add(new FilterClass { ID = FilterTypes.DNR, Name = "DNR", Value = 0, MinValue = 0, MaxValue = 15, DefaultValue = 0, Enabled = false });
            filters.Add(new FilterClass { ID = FilterTypes.Narrow, Name = "NARROW", Value = 0, MinValue = 0, MaxValue = 1, DefaultValue = 0, Enabled = false });

            Update_Shift_TrapezoidPostion(FT891S_CatManager.currentRadioState.IfShiftHz);
            Update_Width_TrapezoidPostion(FT891S_CatManager.currentRadioState.WidthValue);
        }

        public void SetNbValue(int value)
        {
            NbValue = Clamp(value, 0, 10);
            OnPropertyChanged(nameof(NbValue));

            if (NBValueTextBlock != null)
            {
                if (NbValue == 0)
                {
                    NBValueTextBlock.Text = "0";
                    ButtonState(FilterTypes.NB, States.Off);
                }
                else
                {
                    NBValueTextBlock.Text = NbValue.ToString("D2");
                    ButtonState(FilterTypes.NB, States.On);
                }
            }

            UpdateThumbPositions();
            UpdateWaveform();
        }

        private void NotchThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            const double notchHalfWidth = 12.0;

            // 1. Enforce strict boundary limits so the notch shape never overflows the borders (similar to Contour)
            double minNotchX = border1X + notchHalfWidth;
            double maxNotchX = border2X - notchHalfWidth;

            double currentRatio = (NotchFrequency - Ft891FilterRanges.NotchFrequencyMinHz)
                / (double)(Ft891FilterRanges.NotchFrequencyMaxHz - Ft891FilterRanges.NotchFrequencyMinHz);
            double currentPixelPos = minNotchX + (currentRatio * (maxNotchX - minNotchX));

            // 2. Add drag delta and clamp
            double newPixelPos = currentPixelPos + e.HorizontalChange;
            newPixelPos = Math.Max(minNotchX, Math.Min(maxNotchX, newPixelPos));

            // 3. Map pixel back to frequency range and snap to step
            double newRatio = (newPixelPos - minNotchX) / (maxNotchX - minNotchX);
            int targetFreq = (int)Math.Round(
                Ft891FilterRanges.NotchFrequencyMinHz +
                (newRatio * (Ft891FilterRanges.NotchFrequencyMaxHz - Ft891FilterRanges.NotchFrequencyMinHz)));

            int frequency = SnapToStep(targetFreq, Ft891FilterRanges.NotchFrequencyMinHz,
                Ft891FilterRanges.NotchFrequencyMaxHz, Ft891FilterRanges.NotchFrequencyStepHz);

            // 4. Update internal frequency state
            SetNotchFrequencyInternal(frequency);
            SetNotchEnabledInternal(true);

            if (NotchValueTextBlock != null)
                NotchValueTextBlock.Text = "NCH: " + NotchFrequency + " Hz";

            // 5. Update geometry shape and thumb positions
            Update_Notch_PointPostion();

            // 6. Fire UI change events
            UIValueChanged?.Invoke("NCH_FREQ", NotchFrequency / 10);
            UIValueChanged?.Invoke("NCH_ENABLED", NotchEnabled);
        }

        private void Update_Notch_ThumbPostion()
        {
            if (NotchThumb == null) return;

            // Position the thumb centered at the upward tip of the notch V-shape
            Canvas.SetLeft(NotchThumb, NotchVBottomPoint.Point.X - 8);
            Canvas.SetTop(NotchThumb, NotchVBottomPoint.Point.Y - 8);
        }

        public readonly Double NotchYPos = 7;

        public void Update_Notch_PointPostion()
        {
            const double notchHalfWidth = 12.0;

            double minNotchX = border1X + notchHalfWidth;
            double maxNotchX = border2X - notchHalfWidth;

            double notchRatio = (NotchFrequency - Ft891FilterRanges.NotchFrequencyMinHz)
                / (double)(Ft891FilterRanges.NotchFrequencyMaxHz - Ft891FilterRanges.NotchFrequencyMinHz);

            double notchPixelX = Math.Max(minNotchX, Math.Min(maxNotchX, minNotchX + (notchRatio * (maxNotchX - minNotchX))));

            // Build upward-facing V-shape notch geometry anchored to baseline (NotchYPos)
            NotchStartStartPoint.StartPoint = new Point(84, NotchYPos);
            NotchVLeftPoint.Point = new Point(notchPixelX - notchHalfWidth, NotchYPos);
            NotchVBottomPoint.Point = new Point(notchPixelX, NotchYPos + 20);
            NotchVRightPoint.Point = new Point(notchPixelX + notchHalfWidth, NotchYPos);
            NotchEndPoint.Point = new Point(556, NotchYPos);

            Update_Notch_ThumbPostion();

            if (!NotchEnabled || NotchFrequency == 0)
            {
                ButtonState(FilterTypes.Notch, false);
                if (mainWindow?.NotchLabelBorder != null)
                    mainWindow.NotchLabelBorder.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                ButtonState(FilterTypes.Notch, true);
                if (mainWindow?.NotchLabelBorder != null)
                    mainWindow.NotchLabelBorder.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void ContourThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double targetX = Canvas.GetLeft(ContourThumb) + 8 + e.HorizontalChange;
            double targetY = Canvas.GetTop(ContourThumb) + 8 + e.VerticalChange;

            double halfArcWidth = TotalArcWidth / 2.0;
            double minCX = border1X + halfArcWidth;
            double maxCX = border2X - halfArcWidth;

            // Clamp X so the fixed-width arc never extends past border1X or border2X
            double clampedX = Math.Max(minCX, Math.Min(maxCX, targetX));

            // Clamp Y strictly to 24 pixels above and below ContourYPos
            double clampedY = Math.Max(ContourYPos - 24.0, Math.Min(ContourYPos + 24.0, targetY));

            // Map frequency ratio across the valid travel range (minCX to maxCX)
            double ratio = (clampedX - minCX) / (maxCX - minCX);
            int frequency = (int)Math.Round(
                Ft891FilterRanges.ContourFrequencyMinHz +
                ratio * (Ft891FilterRanges.ContourFrequencyMaxHz - Ft891FilterRanges.ContourFrequencyMinHz));

            SetContourFrequencyInternal(frequency);

            _contourVisualOffset = clampedY - ContourYPos;
            _contourVisualOffset = Math.Max(-24.0, Math.Min(24.0, _contourVisualOffset));

            ContourEnabled = true;
            OnPropertyChanged(nameof(ContourEnabled));

            UpdateContourAndWaveform();

            UIValueChanged?.Invoke("CONTOUR_FREQ", ContourFrequency);
        }
        public double ContourYPos = 51;
        private const double TotalArcWidth = 30.0;
        public void UpdateContourAndWaveform()
        {
            if (ContourThumb == null || ContourArcSegment == null || ContourFigureStart == null) return;

            double halfArcWidth = TotalArcWidth / 2.0;
            double minCX = border1X + halfArcWidth;
            double maxCX = border2X - halfArcWidth;

            double contourRatio = (ContourFrequency - Ft891FilterRanges.ContourFrequencyMinHz) /
                                  (double)(Ft891FilterRanges.ContourFrequencyMaxHz - Ft891FilterRanges.ContourFrequencyMinHz);

            // cX stays strictly within bounds so the arc never overflows the borders
            double cX = Math.Max(minCX, Math.Min(maxCX, minCX + contourRatio * (maxCX - minCX)));

            // Enforce strict ±24 pixel limit from ContourYPos for the center Y
            double clampedOffset = Math.Max(-24.0, Math.Min(24.0, _contourVisualOffset));
            double cY = ContourYPos + clampedOffset;
            if (!ContourEnabled) cY = ContourYPos;

            // --- THUMB IS ALWAYS DEAD-CENTER ON THE ARC ---
            Canvas.SetLeft(ContourThumb, cX - 8);
            Canvas.SetTop(ContourThumb, cY - 8);

            if (ContourValueTextBlock != null)
                ContourValueTextBlock.Text = !ContourEnabled ? "CNT: OFF" : "CNT: " + ContourFrequency + " Hz";

            // --- FIXED WIDTH ARC (Always centered on cX) ---
            double startX = cX - halfArcWidth;
            double endX = cX + halfArcWidth;

            ContourStartPoint.StartPoint = new Point(84, ContourYPos);
            ContourFigureStart.Point = new Point(startX, ContourYPos);
            ContourArcSegment.Point = new Point(endX, ContourYPos);
            ContourEndPointLineSegment.Point = new Point(556, ContourYPos);

            double arcRadiusX = halfArcWidth;
            double offset = cY - ContourYPos;

            // Adjust arc curve direction and height based on offset from ContourYPos
            if (Math.Abs(offset) < 0.5)
            {
                ContourArcSegment.Size = new Size(arcRadiusX, 0);
                ContourArcSegment.SweepDirection = SweepDirection.Clockwise;
            }
            else if (offset < 0) // Dragged Up (Peak is above baseline)
            {
                ContourArcSegment.SweepDirection = SweepDirection.Clockwise;
                ContourArcSegment.Size = new Size(arcRadiusX, Math.Min(Math.Abs(offset), 24.0));
            }
            else // Dragged Down (Peak is below baseline)
            {
                ContourArcSegment.SweepDirection = SweepDirection.Counterclockwise;
                ContourArcSegment.Size = new Size(arcRadiusX, Math.Min(offset, 24.0));
            }
        }
        private void WidthThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double currentCenter = BaseCenter + ShiftToPixels((int)FT891S_CatManager.currentRadioState.WidthValue);
            double currentLeft = Canvas.GetLeft(WidthThumb);
            double newLeft = currentLeft + e.HorizontalChange;

            double minX = currentCenter;
            double maxHalfWidth = (border2X - border1X - (2 * FixedSlopeWidth)) / 2.0;
            double maxX = currentCenter + maxHalfWidth - 8.0;

            if (newLeft < minX) newLeft = minX;
            if (newLeft > maxX) newLeft = maxX;

            double percentage = ((newLeft - minX) / (maxX - minX));
            int code = (int)Math.Round(percentage * Ft891FilterRanges.WidthCodeMax);

            FT891S_CatManager.currentRadioState.WidthMode = WidthModes.SH_ON;

            SetWidthCodeInternal(code);
            // Pass the updated state or target width depending on your implementation
            Update_Width_TrapezoidPostion(FT891S_CatManager.currentRadioState.WidthValue);
        }
        private void Update_Width_ThumbPostion()
        {
            if (WidthThumb == null) return;

            Canvas.SetLeft(WidthThumb, WidthTrapezoidRightBottomLineSegment.Point.X - 16);
            Canvas.SetTop(WidthThumb, WidthYPos - 28);
        }
        public readonly Double WidthYPos = 80;
        public void Update_Width_TrapezoidPostion(WidthValues WidthValue)
        {
            int freqInHz = WidthFrequencies_SSB_Wide[(int)FT891S_CatManager.currentRadioState.WidthValue];

            switch (FT891S_CatManager.currentRadioState.OperatingMode)
            {
                case RadioMode.LSB:
                case RadioMode.USB:
                    if (FT891S_CatManager.currentRadioState.NarrowMode == NAMode.NA_OFF)
                    {
                        freqInHz = WidthFrequencies_SSB_Wide[(int)FT891S_CatManager.currentRadioState.WidthValue];
                        if (freqInHz == -1) freqInHz = WidthFrequencies_SSB_Wide_MinimumValue;
                    }
                    else
                    {
                        freqInHz = WidthFrequencies_SSB_Narrow[(int)FT891S_CatManager.currentRadioState.WidthValue];
                        if (freqInHz == -1) freqInHz = WidthFrequencies_SSB_Narrow_MinimumValue;
                    }
                    break;
                case RadioMode.CW_L:
                case RadioMode.CW_U:
                    if (FT891S_CatManager.currentRadioState.NarrowMode == NAMode.NA_OFF)
                    {
                        freqInHz = WidthFrequencies_CW_Wide[(int)FT891S_CatManager.currentRadioState.WidthValue];
                        if (freqInHz == -1) freqInHz = WidthFrequencies_CW_Wide_MinimumValue;
                    }
                    else
                    {
                        freqInHz = WidthFrequencies_CW_Narrow[(int)FT891S_CatManager.currentRadioState.WidthValue];
                        if (freqInHz == -1) freqInHz = WidthFrequencies_CW_Narrow_MinimumValue;
                    }
                    break;
                case RadioMode.AM:
                    break;
                case RadioMode.AM_N:
                    break;
                case RadioMode.FM:
                    break;
                case RadioMode.FM_N:
                    break;
                case RadioMode.DATA_L:
                case RadioMode.DATA_U:
                    if (FT891S_CatManager.currentRadioState.NarrowMode == NAMode.NA_OFF)
                    {
                        freqInHz = WidthFrequencies_RTTY_PSK_Wide[(int)FT891S_CatManager.currentRadioState.WidthValue];
                        if (freqInHz == -1) freqInHz = WidthFrequencies_RTTY_PSK_Wide_MinimumValue;
                    }
                    else
                    {
                        freqInHz = WidthFrequencies_RTTY_PSK_Narrow[(int)FT891S_CatManager.currentRadioState.WidthValue];
                        if (freqInHz == -1) freqInHz = WidthFrequencies_RTTY_PSK_Narrow_MinimumValue;
                    }
                    break;
                case RadioMode.DATA_FM:
                    if (FT891S_CatManager.currentRadioState.NarrowMode == NAMode.NA_OFF)
                    {
                        freqInHz = WidthFrequencies_RTTY_PSK_Wide[(int)FT891S_CatManager.currentRadioState.WidthValue];
                        if (freqInHz == -1) freqInHz = WidthFrequencies_RTTY_PSK_Wide_MinimumValue;
                    }
                    else
                    {
                        freqInHz = WidthFrequencies_RTTY_PSK_Narrow[(int)FT891S_CatManager.currentRadioState.WidthValue];
                        if (freqInHz == -1) freqInHz = WidthFrequencies_RTTY_PSK_Narrow_MinimumValue;
                    }
                    break;
                case RadioMode.DATA_FM_N:
                    //we have both logics set to wide, because DATA_FM_N ?
                    if (FT891S_CatManager.currentRadioState.NarrowMode == NAMode.NA_OFF)
                    {
                        freqInHz = WidthFrequencies_RTTY_PSK_Narrow[(int)FT891S_CatManager.currentRadioState.WidthValue];
                        if (freqInHz == -1) freqInHz = WidthFrequencies_RTTY_PSK_Narrow_MinimumValue;
                    }
                    else
                    {
                        freqInHz = WidthFrequencies_RTTY_PSK_Narrow[(int)FT891S_CatManager.currentRadioState.WidthValue];
                        if (freqInHz == -1) freqInHz = WidthFrequencies_RTTY_PSK_Narrow_MinimumValue;
                    }
                    break;
                case RadioMode.RTTY_L:
                case RadioMode.RTTY_U:
                    if (FT891S_CatManager.currentRadioState.NarrowMode == NAMode.NA_OFF)
                    {
                        freqInHz = WidthFrequencies_RTTY_PSK_Wide[(int)FT891S_CatManager.currentRadioState.WidthValue];
                        if (freqInHz == -1) freqInHz = WidthFrequencies_RTTY_PSK_Wide_MinimumValue;
                    }
                    else
                    {
                        freqInHz = WidthFrequencies_RTTY_PSK_Narrow[(int)FT891S_CatManager.currentRadioState.WidthValue];
                        if (freqInHz == -1) freqInHz = WidthFrequencies_RTTY_PSK_Narrow_MinimumValue;
                    }
                    break;
                case RadioMode.PSK:
                    if (FT891S_CatManager.currentRadioState.NarrowMode == NAMode.NA_OFF)
                    {
                        freqInHz = WidthFrequencies_RTTY_PSK_Wide[(int)FT891S_CatManager.currentRadioState.WidthValue];
                        if (freqInHz == -1) freqInHz = WidthFrequencies_RTTY_PSK_Wide_MinimumValue;
                    }
                    else
                    {
                        freqInHz = WidthFrequencies_RTTY_PSK_Narrow[(int)FT891S_CatManager.currentRadioState.WidthValue];
                        if (freqInHz == -1) freqInHz = WidthFrequencies_RTTY_PSK_Narrow_MinimumValue;
                    }
                    break;
                case RadioMode.PRESET:
                    break;
            }

            Double pixelPosition;

            float value = (int)freqInHz / 11.5f;

            pixelPosition = value/2;

            const int CenterOfFilterArea = 320;
            const int TrapezoidSideWidth = 6;

            WidthTrapezoidStartPoint.StartPoint = new Point(84, WidthYPos);
            WidthTrapezoidLeftBottomLineSegment.Point = new Point((CenterOfFilterArea - pixelPosition) - (TrapezoidSideWidth * 2), WidthYPos);
            WidthTrapezoidLeftTopLineSegment.Point = new Point((CenterOfFilterArea - pixelPosition) - TrapezoidSideWidth, WidthYPos -20);
            WidthTrapezoidRightTopLineSegment.Point = new Point((CenterOfFilterArea + pixelPosition) + TrapezoidSideWidth, WidthYPos -20);
            WidthTrapezoidRightBottomLineSegment.Point = new Point((CenterOfFilterArea + pixelPosition) + (TrapezoidSideWidth * 2), WidthYPos);
            WidthTrapezoidEndPointLineSegment.Point = new Point(556, WidthYPos);

            if (WidthValueTextBlock != null) WidthValueTextBlock.Text = freqInHz + "Hz";

            Update_Width_ThumbPostion();

            if (FT891S_CatManager.currentRadioState.WidthMode == WidthModes.SH_OFF)
                ButtonState(FilterTypes.Width, false);
            else
                ButtonState(FilterTypes.Width, true);
        }
        private void ShiftThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            // Directly calculate the pixel change from the drag delta and map it to Hz
            const double maxPixelShift = ((border2X - border1X) / 2.0) - FixedSlopeWidth;

            // Get current shift state and apply direction to get the true signed Hz value
            var shiftState = FT891S_CatManager.currentRadioState.IfShiftHz;
            double currentShiftHz = shiftState.IfShiftHz * shiftState.IfShiftDirection;

            double currentPixelOffset = (currentShiftHz / (double)Ft891FilterRanges.IfShiftMaxHz) * maxPixelShift;

            // Add the horizontal drag change
            double newPixelOffset = currentPixelOffset + e.HorizontalChange;

            // Convert back to Hz and clamp within allowed ranges (min/max handles negatives)
            double newShiftHz = (newPixelOffset / maxPixelShift) * Ft891FilterRanges.IfShiftMaxHz;
            int targetShift = SnapToStep((int)Math.Round(newShiftHz), Ft891FilterRanges.IfShiftMinHz, Ft891FilterRanges.IfShiftMaxHz, Ft891FilterRanges.IfShiftStepHz);

            FT891S_CatManager.currentRadioState.IfShiftHz.OnOff = IFMode.IF_ON;

            SetShiftValueInternal(targetShift);

            // Pass the updated state or target shift depending on your implementation
            Update_Shift_TrapezoidPostion(FT891S_CatManager.currentRadioState.IfShiftHz);
        }
        private void Update_Shift_ThumbPostion()
        {
            if (ShiftThumb == null) return;

            Canvas.SetLeft(ShiftThumb, ShiftTrapezoidRightBottomLineSegment.Point.X - 8);
            Canvas.SetTop(ShiftThumb, ShiftYPos-10);
        }
        public readonly Double ShiftYPos = 100;
        public void Update_Shift_TrapezoidPostion(IfShiftResult IFShiftHz)
        {
            Double pixelPosition;

            float value = IFShiftHz.IfShiftHz / 5.5f;
            pixelPosition = (IFShiftHz.IfShiftDirection == -1) ? -value : value;

            const int CenterOfFilterArea = 320;
            const int TrapezoidSideWidth = 6;

            ShiftTrapezoidStartPoint.StartPoint = new Point(84, ShiftYPos);
            ShiftTrapezoidLeftBottomLineSegment.Point = new Point((CenterOfFilterArea + pixelPosition) - (TrapezoidSideWidth*2), ShiftYPos);
            
            ShiftTrapezoidLeftTopLineSegment.Point = new Point((CenterOfFilterArea + pixelPosition) - TrapezoidSideWidth, ShiftYPos-20);
            ShiftTrapezoidRightTopLineSegment.Point = new Point((CenterOfFilterArea + pixelPosition) + TrapezoidSideWidth, ShiftYPos-20);
            
            ShiftTrapezoidRightBottomLineSegment.Point = new Point((CenterOfFilterArea + pixelPosition) + (TrapezoidSideWidth*2), ShiftYPos);
            ShiftTrapezoidEndPointLineSegment.Point = new Point(556, ShiftYPos);

            if (ShiftValueTextBlock != null)
            {
                var shiftInfo = FT891S_CatManager.currentRadioState.IfShiftHz;
                bool isNegative = (shiftInfo.IfShiftDirection == -1) || (shiftInfo.IfShiftHz < 0);
                double absHz = Math.Abs(shiftInfo.IfShiftHz);
                ShiftValueTextBlock.Text = (isNegative && absHz > 0 ? "-" : "") + absHz + "Hz";
            }

            Update_Shift_ThumbPostion();

            if (FT891S_CatManager.currentRadioState.IfShiftHz.OnOff == IFMode.IF_OFF)
                ButtonState(FilterTypes.Shift, false);
            else
                ButtonState(FilterTypes.Shift, true);
        }
        public void UpdateThumbPositions()
        {
            if (NbThumb == null || WidthThumb == null || NotchThumb == null ||
                DnrThumb == null || ContourThumb == null || ShiftThumb == null) return;

            _isUpdatingProgrammatically = true;

            // 1. Noise Blanker Position
            double travelSpaceNb = CenterY - 5;
            double nbTop = CenterY - ((NbValue / 10.0) * travelSpaceNb);
            Canvas.SetTop(NbThumb, Math.Max(5, Math.Min(CenterY, nbTop)));

            // 2. WIDTH & IF SHIFT Position
            double currentCenter = BaseCenter;

            double maxHalfWidth = (border2X - border1X - (2 * FixedSlopeWidth)) / 2.0;
            const double thumbHalfWidth = 8.0;
            double minX = currentCenter;
            double maxX = currentCenter + maxHalfWidth - thumbHalfWidth;

            double widthRatio = (int)FT891S_CatManager.currentRadioState.WidthValue / (double)Ft891FilterRanges.WidthCodeMax;
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
            cX = Math.Max(border1X, Math.Min(border2X, cX));
            Canvas.SetLeft(ContourThumb, cX - 8);

            // Use ContourPeakY (51.0) instead of PeakY (25.0) and allow full range up to 105
            double contourCenterY = ContourPeakY + _contourVisualOffset;
            if (!ContourEnabled) contourCenterY = ContourPeakY;
            Canvas.SetTop(ContourThumb, Math.Max(5, Math.Min(105, contourCenterY)) - 8);

            // 5. DNR Position
            double travelSpaceDnr = CenterY - 5;
            double dnrTop = CenterY - (((int)FT891S_CatManager.currentRadioState.DNRValue / 15.0) * travelSpaceDnr);
            Canvas.SetTop(DnrThumb, Math.Max(5, Math.Min(CenterY, dnrTop)));

            // 6. IF SHIFT Position
            Canvas.SetLeft(ShiftThumb, currentBaseRightX);
            Canvas.SetTop(ShiftThumb, 105 - 8);

            if (WidthValueTextBlock != null)
                WidthValueTextBlock.Text = "WD: " + ((int)FT891S_CatManager.currentRadioState.WidthValue).ToString("D2");

            if (NotchValueTextBlock != null)
                NotchValueTextBlock.Text = !NotchEnabled ? "NCH: OFF" : "NCH: " + NotchFrequency + " Hz";

            if (ContourValueTextBlock != null)
                ContourValueTextBlock.Text = !ContourEnabled ? "CNT: OFF" : "CNT: " + ContourFrequency + " Hz";

            _isUpdatingProgrammatically = false;
        }

        private void NbThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            // Clamp newTop between the top limit (5) and bottom limit (CenterY)
            double newTop = Math.Max(5, Math.Min(CenterY, Canvas.GetTop(NbThumb) + e.VerticalChange));
            Canvas.SetTop(NbThumb, newTop);

            // Calculate normalized travel percentage (0.0 at bottom/CenterY to 1.0 at top/5)
            double travelSpace = CenterY - 5;
            double pct = travelSpace > 0 ? (CenterY - newTop) / travelSpace : 0;

            // Map percentage to range [-1, 10] (11 intervals / 12 steps)
            // pct = 1.0 (top) -> 10
            // pct = 0.0 (bottom) -> -1
            int newProposedValue = (int)Math.Round(pct * 11) - 1;

            // Strict boundary enforcement
            if (newTop <= 5) newProposedValue = 10;
            else if (newTop >= CenterY) newProposedValue = -1;

            NbValue = newProposedValue;

            bool buttonState = States.Off;

            if (newProposedValue < 0)
            {
                // -1 state: Noise Blanker OFF
                FT891S_CatManager.currentRadioState.NoiseBlankerMode = NBMode.NB_OFF;
                FT891S_CatManager.currentRadioState.NoiseBlankerLevel = 0;
                buttonState = States.Off;
            }
            else
            {
                // 0 to 10 state: Noise Blanker ON
                FT891S_CatManager.currentRadioState.NoiseBlankerMode = NBMode.NB_ON;
                FT891S_CatManager.currentRadioState.NoiseBlankerLevel = newProposedValue;
                buttonState = States.On;
            }

            ButtonState(FilterTypes.NB, buttonState);

            UpdateWaveform();
        }

        private void DnrThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newTop = Math.Max(5, Math.Min(CenterY, Canvas.GetTop(DnrThumb) + e.VerticalChange));
            Canvas.SetTop(DnrThumb, newTop);

            double travelSpace = CenterY - 5;
            double pct = (CenterY - newTop) / travelSpace;

            int newProposedValue = (int)Math.Round(pct * 15);

            bool buttonState = States.Off;

            if (newTop <= 5) FT891S_CatManager.currentRadioState.DNRValue = (DNRValues)15;

            if (newTop >= CenterY)
            {
                if (newProposedValue == 0)
                {
                    FT891S_CatManager.currentRadioState.DNRMode = DNRModes.NR_OFF;
                    FT891S_CatManager.currentRadioState.DNRValue = DNRValues.DNR_01;
                    buttonState = States.Off;
                }
            }

            if (newProposedValue >= 1 && newProposedValue <= 15)
            {
                FT891S_CatManager.currentRadioState.DNRMode = DNRModes.NR_ON;
                FT891S_CatManager.currentRadioState.DNRValue = (DNRValues)newProposedValue;
                buttonState = States.On;
            }

            ButtonState(FilterTypes.DNR, buttonState);

            UpdateWaveform();
        }

        public void UpdateWaveform()
        {
            if (NbThumb == null || WidthThumb == null || NotchThumb == null || DnrThumb == null || ContourThumb == null || ShiftThumb == null ||
                Segment1 == null || Segment2 == null || 
                DnrRightFigure == null || DnrRightSegment == null || DnrRightSegment2 == null ||
                ContourFigureStart == null || ContourArcSegment == null)
                return;

            const double border0X = 0.0;
            const double border3X = 639.0;

            double nbX = 42.0;
            double nbY = Canvas.GetTop(NbThumb) + 8;

            
            double dnrX = 598.0;
            double dnrY = Canvas.GetTop(DnrThumb) + 8;

            // NB Section
            Segment1.Point1 = new Point(border0X + (nbX - border0X) * 0.5, CenterY);
            Segment1.Point2 = new Point(nbX - (nbX - border0X) * 0.1, nbY);
            Segment1.Point3 = new Point(nbX, nbY);

            Segment2.Point1 = new Point(nbX + (border1X - nbX) * 0.1, nbY);
            Segment2.Point2 = new Point(border1X - (border1X - nbX) * 0.5, CenterY);
            Segment2.Point3 = new Point(border1X, CenterY);

            
            // DNR Section
            DnrRightFigure.StartPoint = new Point(border2X, CenterY);
            DnrRightSegment.Point1 = new Point(border2X + (dnrX - border2X) * 0.5, CenterY);
            DnrRightSegment.Point2 = new Point(dnrX - (dnrX - border2X) * 0.1, dnrY);
            DnrRightSegment.Point3 = new Point(dnrX, dnrY);

            DnrRightSegment2.Point1 = new Point(dnrX + (border3X - dnrX) * 0.1, dnrY);
            DnrRightSegment2.Point2 = new Point(border3X - (border3X - dnrX) * 0.5, CenterY);
            DnrRightSegment2.Point3 = new Point(border3X, CenterY);
        }

        private void FilterWaveButtons_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var code = border.Tag.ToString();
            uint switchValue = uint.Parse(code);

            ButtonSelection(Convert.ToByte(switchValue));
        }

        IfShiftResult Last_IfShiftResult = new IfShiftResult { IfShiftDirection = 1, IfShiftHz = 0 };

        public async void ButtonState(byte button, bool state)
        {
            switch (button)
            {
                case FilterTypes.NB:
                    if (state)
                    {
                        filters[FilterTypes.NB].Enabled = true;
                        NBTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                    }
                    else
                    {
                        filters[FilterTypes.NB].Enabled = false;
                        NBTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    }

                    NBValueTextBlock.Text = ((int)FT891S_CatManager.currentRadioState.NoiseBlankerLevel).ToString();

                    if (FT891S_CatManager.currentRadioState.NoiseBlankerMode == NBMode.NB_OFF)
                    {
                        mainWindow.NBLabelBorder.Visibility = System.Windows.Visibility.Hidden;
                        await mainWindow._catManager.SendCatCommandAsync("NB", new object[] { 0, 0 }, mainWindow._catManager.OutGoingDataLoopDelay);
                    }
                    else
                    {
                        mainWindow.NBLabelBorder.Visibility = System.Windows.Visibility.Visible;
                        await mainWindow._catManager.SendCatCommandAsync("NB", new object[] { 0, 1 }, mainWindow._catManager.OutGoingDataLoopDelay);
                        await mainWindow._catManager.SendCatCommandAsync("NL", new object[] { 0, FT891S_CatManager.currentRadioState.NoiseBlankerLevel }, mainWindow._catManager.OutGoingDataLoopDelay);
                    }
                    break;
                case FilterTypes.Notch:
                    if (state)
                    {
                        filters[FilterTypes.Notch].Enabled = true;
                        NotchTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                    }
                    else
                    {
                        filters[FilterTypes.Notch].Enabled = false;
                        NotchTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    }
                    break;
                case FilterTypes.Contour:
                    if (state)
                    {
                        filters[FilterTypes.Contour].Enabled = true;
                        ContourTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                    }
                    else
                    {
                        filters[FilterTypes.Contour].Enabled = false;
                        ContourTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    }
                    break;
                case FilterTypes.Shift:
                    String BuiltIsCommand;

                    if (state)
                    {
                        filters[FilterTypes.Shift].Enabled = true;
                        ShiftTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                        mainWindow.ShiftLabelBorder.Visibility = System.Windows.Visibility.Visible;

                        FT891S_CatManager.currentRadioState.IfShiftHz.OnOff = IFMode.IF_ON;

                        if (FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftDirection == -1)
                            BuiltIsCommand = FT891S_CatCommandTypes.BuildIsCommand(0, 1, '-', FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftHz);
                        else
                            BuiltIsCommand = FT891S_CatCommandTypes.BuildIsCommand(0, 1, '+', FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftHz);

                        await mainWindow._catManager.SendCatCommandAsync("IS", BuiltIsCommand, mainWindow._catManager.OutGoingDataLoopDelay);
                    }
                    else
                    {
                        filters[FilterTypes.Shift].Enabled = false;
                        ShiftTextBlock.Foreground = new SolidColorBrush(Colors.White);
                        mainWindow.ShiftLabelBorder.Visibility = System.Windows.Visibility.Collapsed;

                        FT891S_CatManager.currentRadioState.IfShiftHz.OnOff = IFMode.IF_OFF;

                        BuiltIsCommand = FT891S_CatCommandTypes.BuildIsCommand(0, 0, '+', FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftHz);
                        await mainWindow._catManager.SendCatCommandAsync("IS", BuiltIsCommand, mainWindow._catManager.OutGoingDataLoopDelay);
                    }
                    break;
                case FilterTypes.Width:
                    if (state)
                    {
                        filters[FilterTypes.Width].Enabled = true;
                        WidthTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                        mainWindow.WidthLabelBorder.Visibility = System.Windows.Visibility.Visible;

                        FT891S_CatManager.currentRadioState.WidthMode = WidthModes.SH_ON;

                        await mainWindow._catManager.SendCatCommandAsync("SH", new object[] { 0, 1, (int)FT891S_CatManager.currentRadioState.WidthValue }, mainWindow._catManager.OutGoingDataLoopDelay);
                    }
                    else
                    {
                        filters[FilterTypes.Width].Enabled = false;
                        WidthTextBlock.Foreground = new SolidColorBrush(Colors.White);
                        mainWindow.WidthLabelBorder.Visibility = System.Windows.Visibility.Collapsed;

                        FT891S_CatManager.currentRadioState.WidthMode = WidthModes.SH_OFF;

                        await mainWindow._catManager.SendCatCommandAsync("SH", new object[] { 0, 0, 0 }, mainWindow._catManager.OutGoingDataLoopDelay);
                    }
                    break;
                case FilterTypes.DNR:
                    if (state)
                    {
                        filters[FilterTypes.DNR].Enabled = true;
                        DNRTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                        mainWindow.DNRLabelBorder.Visibility = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        filters[FilterTypes.DNR].Enabled = false;
                        DNRTextBlock.Foreground = new SolidColorBrush(Colors.White);
                        mainWindow.DNRLabelBorder.Visibility = System.Windows.Visibility.Hidden;
                    }
                    
                    DNRValueTextBlock.Text = ((int)FT891S_CatManager.currentRadioState.DNRValue).ToString();

                    if (FT891S_CatManager.currentRadioState.DNRMode == DNRModes.NR_OFF)
                    {
                        await mainWindow._catManager.SendCatCommandAsync("NR", new object[] { 0, 0 }, mainWindow._catManager.OutGoingDataLoopDelay);
                    }
                    else
                    {
                        await mainWindow._catManager.SendCatCommandAsync("NR", new object[] { 0, 1 }, mainWindow._catManager.OutGoingDataLoopDelay);
                        await mainWindow._catManager.SendCatCommandAsync("RL", new object[] { 0, (int)FT891S_CatManager.currentRadioState.DNRValue }, mainWindow._catManager.OutGoingDataLoopDelay);
                    }
                    break;
                case FilterTypes.Narrow:
                    if (state)
                    {
                        filters[FilterTypes.Narrow].Enabled = true;
                        NarrowTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                        NarrowValueTextBlock.Text = "ON";
                        mainWindow.NarrowLabelBorder.Visibility = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        filters[FilterTypes.Narrow].Enabled = false;
                        NarrowTextBlock.Foreground = new SolidColorBrush(Colors.White);
                        NarrowValueTextBlock.Text = "OFF";
                        mainWindow.NarrowLabelBorder.Visibility = System.Windows.Visibility.Hidden;
                    }
                    break;
            }
        }
        public async void ButtonSelection(byte button)
        {
            switch (button)
            {
                case FilterTypes.NB:
                    if (!(filters[FilterTypes.NB].Enabled))
                    {
                        filters[FilterTypes.NB].Enabled = true;
                        FT891S_CatManager.currentRadioState.NoiseBlankerMode = NBMode.NB_ON;
                        ButtonState(FilterTypes.NB, States.On);
                    }
                    else
                    {
                        filters[FilterTypes.NB].Enabled = false;
                        FT891S_CatManager.currentRadioState.NoiseBlankerMode = NBMode.NB_OFF;
                        ButtonState(FilterTypes.NB, States.Off);
                    }
                    break;
                case FilterTypes.Notch:
                    if (!(filters[FilterTypes.Notch].Enabled))
                    {
                        filters[FilterTypes.Notch].Enabled = true;
                        NotchTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                    }
                    else
                    {
                        filters[FilterTypes.Notch].Enabled = false;
                        NotchTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    }
                    break;
                case FilterTypes.Contour:
                    if (!(filters[FilterTypes.Contour].Enabled))
                    {
                        filters[FilterTypes.Contour].Enabled = true;
                        ContourTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                    }
                    else
                    {
                        filters[FilterTypes.Contour].Enabled = false;
                        ContourTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    }
                    break;
                case FilterTypes.Shift:
                    if (!(filters[FilterTypes.Shift].Enabled))
                    {
                        filters[FilterTypes.Shift].Enabled = true;

                        FT891S_CatManager.currentRadioState.IfShiftHz.OnOff = IFMode.IF_ON;
                        
                        FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftDirection = Last_IfShiftResult.IfShiftDirection;
                        FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftHz = Last_IfShiftResult.IfShiftHz;

                        ButtonState(FilterTypes.Shift, States.On);
                    }
                    else
                    {
                        filters[FilterTypes.Shift].Enabled = false;

                        FT891S_CatManager.currentRadioState.IfShiftHz.OnOff = IFMode.IF_OFF;

                        Last_IfShiftResult.IfShiftDirection = FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftDirection;
                        Last_IfShiftResult.IfShiftHz = FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftHz;

                        FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftHz = 0;

                        ButtonState(FilterTypes.Shift, States.Off);
                    }
                    break;
                case FilterTypes.Width:
                    if (!(filters[FilterTypes.Width].Enabled))
                    {
                        filters[FilterTypes.Width].Enabled = true;

                        FT891S_CatManager.currentRadioState.WidthValue = (WidthValues)filters[FilterTypes.Width].Value;

                        ButtonState(FilterTypes.Width, States.On);
                    }
                    else
                    {
                        filters[FilterTypes.Width].Enabled = false;
                        
                        filters[FilterTypes.Width].Value = (int)FT891S_CatManager.currentRadioState.WidthValue;
 
                        ButtonState(FilterTypes.Width, States.Off);
                    }
                    break;
                case FilterTypes.DNR:
                    if (!(filters[FilterTypes.DNR].Enabled))
                    {
                        filters[FilterTypes.DNR].Enabled = true;
                        FT891S_CatManager.currentRadioState.DNRMode = DNRModes.NR_ON;
                        ButtonState(FilterTypes.DNR, States.On);
                    }
                    else
                    {
                        filters[FilterTypes.DNR].Enabled = false;
                        FT891S_CatManager.currentRadioState.DNRMode = DNRModes.NR_OFF;
                        ButtonState(FilterTypes.DNR, States.Off);
                    }
                    break;
                case FilterTypes.Narrow:
                    if (!(filters[FilterTypes.Narrow].Enabled))
                    {
                        filters[FilterTypes.Narrow].Enabled = true;
                        NarrowTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                        NarrowValueTextBlock.Text = "ON";
                        mainWindow.NarrowLabelBorder.Visibility = System.Windows.Visibility.Visible;
                        await mainWindow._catManager.SendCatCommandAsync("NA", new object[] { 0, 1 }, mainWindow._catManager.OutGoingDataLoopDelay);
                    }
                    else
                    {
                        filters[FilterTypes.Narrow].Enabled = false;
                        NarrowTextBlock.Foreground = new SolidColorBrush(Colors.White);
                        NarrowValueTextBlock.Text = "OFF";
                        mainWindow.NarrowLabelBorder.Visibility = System.Windows.Visibility.Hidden;
                        await mainWindow._catManager.SendCatCommandAsync("NA", new object[] { 0, 0 }, mainWindow._catManager.OutGoingDataLoopDelay);
                    }
                    break;
                case FilterTypes.AllOff:
                    filters[FilterTypes.Notch].Enabled = false;
                    NotchTextBlock.Foreground = new SolidColorBrush(Colors.White);
                 
                    filters[FilterTypes.Contour].Enabled = false;
                    ContourTextBlock.Foreground = new SolidColorBrush(Colors.White);
                
                    filters[FilterTypes.Width].Enabled = false;
                    WidthTextBlock.Foreground = new SolidColorBrush(Colors.White);
                
                    filters[FilterTypes.Shift].Enabled = false;
                    ShiftTextBlock.Foreground = new SolidColorBrush(Colors.White);
              
                    filters[FilterTypes.NB].Enabled = false;
                    NBTextBlock.Foreground = new SolidColorBrush(Colors.White);
                  
                    filters[FilterTypes.DNR].Enabled = false;
                    DNRTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    break;
            }
        }

        
    }
}