using FT891S_CatControl;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        private void SetWidthCodeInternal(int value)
        {
            value = Clamp(value, Ft891FilterRanges.WidthCodeMin, Ft891FilterRanges.WidthCodeMax);
            FT891S_CatManager.currentRadioState.WidthValue = (WidthValues)value;
            //OnPropertyChanged(nameof(FT891S_CatManager.currentRadioState.WidthValue));
            //OnPropertyChanged(nameof(WidthValue));
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
                state.OnOff = 1;
                state.IfShiftHz = -value; // Store as positive magnitude
            }
            else if (value > 0)
            {
                state.IfShiftDirection = 1;
                state.OnOff = 1;
                state.IfShiftHz = value;
            }
            else
            {
                state.IfShiftDirection = 1;
                state.OnOff = 0;
                state.IfShiftHz = 0;
            }
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

            Update_Width_TrapezoidPostion(WidthValues.WDH_10);
        }

        private void WidthThumb_DragDeltaa(object sender, DragDeltaEventArgs e)
        {
            if (_isUpdatingProgrammatically) return;

            double currentCenter = BaseCenter + ShiftToPixels(FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftHz);
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

            UIValueChanged?.Invoke("WIDTH", (int)FT891S_CatManager.currentRadioState.WidthValue);
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

        public void SetWidthValue(int value)
        {
            SetWidthCodeInternal(value);
            if (WidthValueTextBlock != null) WidthValueTextBlock.Text = "WD: " + ((int)FT891S_CatManager.currentRadioState.WidthValue).ToString("D2");

            if (WidthValueTextBlock != null)
            {
                //if (FT891S_CatManager.currentRadioState.OperatingMode == Models.RadioMode.)
            }

            UpdateThumbPositions();
            UpdateWaveform();
        }

        public void SetNotchValues(int freq, bool enabled)
        {
            SetNotchFrequencyInternal(freq);
            SetNotchEnabledInternal(enabled);

            if (NotchValueTextBlock != null)
                NotchValueTextBlock.Text = !NotchEnabled ? "NCH: OFF" : "NCH: " + NotchFrequency + " Hz";

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

            if (ContourValueTextBlock != null)
                ContourValueTextBlock.Text = !ContourEnabled ? "CNT: OFF" : "CNT: " + ContourFrequency + " Hz";

            UpdateThumbPositions();
            UpdateWaveform();
        }
        private void Update_Width_ThumbPostion()
        {
            if (WidthThumb == null) return;

            Canvas.SetLeft(WidthThumb, WidthTrapezoidRightBottomLineSegment.Point.X - 16);
        }
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

            Console.Write("freqInHz = ");
            Console.WriteLine(freqInHz);

            pixelPosition = value/2;

            const int CenterOfFilterArea = 320;
            const int TrapezoidSideWidth = 8;

            WidthTrapezoidLeftBottomLineSegment.Point = new Point((CenterOfFilterArea - pixelPosition) - (TrapezoidSideWidth * 2), 51);
            WidthTrapezoidLeftTopLineSegment.Point = new Point((CenterOfFilterArea - pixelPosition) - TrapezoidSideWidth, 21);
            WidthTrapezoidRightTopLineSegment.Point = new Point((CenterOfFilterArea + pixelPosition) + TrapezoidSideWidth, 21);
            WidthTrapezoidRightBottomLineSegment.Point = new Point((CenterOfFilterArea + pixelPosition) + (TrapezoidSideWidth * 2), 51);

            if (WidthValueTextBlock != null)
            {
                
                WidthValueTextBlock.Text = freqInHz + "Hz";
            }

            Update_Width_ThumbPostion();

            if (FT891S_CatManager.currentRadioState.WidthMode == WidthModes.SH_OFF)
            {
                ButtonState(FilterTypes.Width, false);
                mainWindow.WidthLabelBorder.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                ButtonState(FilterTypes.Width, true);
                mainWindow.WidthLabelBorder.Visibility = System.Windows.Visibility.Visible;
            }
        }
        private void Update_Shift_ThumbPostion()
        {
            if (ShiftThumb == null) return;

            Canvas.SetLeft(ShiftThumb, ShiftTrapezoidRightBottomLineSegment.Point.X - 8);
        }
        public void Update_Shift_TrapezoidPostion(IS_Shift IFShiftHz)
        {
            Double pixelPosition;

            float value = IFShiftHz.IfShiftHz / 5.5f;
            pixelPosition = (IFShiftHz.IfShiftDirection == -1) ? -value : value;

            const int CenterOfFilterArea = 320;
            const int TrapezoidSideWidth = 8;

            ShiftTrapezoidLeftBottomLineSegment.Point = new Point((CenterOfFilterArea + pixelPosition) - (TrapezoidSideWidth*2), 53);
            ShiftTrapezoidLeftTopLineSegment.Point = new Point((CenterOfFilterArea + pixelPosition) - TrapezoidSideWidth, 23);
            ShiftTrapezoidRightTopLineSegment.Point = new Point((CenterOfFilterArea + pixelPosition) + TrapezoidSideWidth, 23);
            ShiftTrapezoidRightBottomLineSegment.Point = new Point((CenterOfFilterArea + pixelPosition) + (TrapezoidSideWidth*2), 53);

            if (ShiftValueTextBlock != null)
            {
                var shiftInfo = FT891S_CatManager.currentRadioState.IfShiftHz;
                bool isNegative = (shiftInfo.IfShiftDirection == -1) || (shiftInfo.IfShiftHz < 0);
                double absHz = Math.Abs(shiftInfo.IfShiftHz);
                ShiftValueTextBlock.Text = (isNegative && absHz > 0 ? "-" : "") + absHz + "Hz";
            }

            Update_Shift_ThumbPostion();

            if (FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftHz == 0)
            {
                ButtonState(FilterTypes.Shift, false);
                mainWindow.ShiftLabelBorder.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                ButtonState(FilterTypes.Shift, true);
                mainWindow.ShiftLabelBorder.Visibility = System.Windows.Visibility.Visible;
            }
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

            double contourCenterY = PeakY + _contourVisualOffset;
            if (!ContourEnabled) contourCenterY = PeakY;
            Canvas.SetTop(ContourThumb, contourCenterY - 8);

            // 5. DNR Position
            double travelSpaceDnr = CenterY - 5;
            double dnrTop = CenterY - (((int)FT891S_CatManager.currentRadioState.DNRValue / 15.0) * travelSpaceDnr);
            Canvas.SetTop(DnrThumb, Math.Max(5, Math.Min(CenterY, dnrTop)));

            // 6. IF SHIFT Position
            Canvas.SetLeft(ShiftThumb, currentBaseRightX);
            Canvas.SetTop(ShiftThumb, CenterY - 8);

            if (WidthValueTextBlock != null)
                WidthValueTextBlock.Text = "WD: " + ((int)FT891S_CatManager.currentRadioState.WidthValue).ToString("D2");

            if (NotchValueTextBlock != null)
                NotchValueTextBlock.Text = !NotchEnabled ? "NCH: OFF" : "NCH: " + NotchFrequency + " Hz";

            if (ContourValueTextBlock != null)
                ContourValueTextBlock.Text = !ContourEnabled ? "CNT: OFF" : "CNT: " + ContourFrequency + " Hz";

            _isUpdatingProgrammatically = false;
        }
        private void WidthThumb_DragDeltaaa(object sender, DragDeltaEventArgs e)
        {
            double currentCenter = BaseCenter;
            double currentLeft = Canvas.GetLeft(WidthThumb);
            double newLeft = currentLeft + e.HorizontalChange;

            double minX = currentCenter;
            double maxHalfWidth = (border2X - border1X - (2 * FixedSlopeWidth)) / 2.0;
            double maxX = currentCenter + maxHalfWidth - 8.0;


            // Directly calculate the pixel change from the drag delta and map it to Hz
            const double maxPixelShift = ((border2X - border1X) / 2.0) - FixedSlopeWidth;

            double currentPixelOffset = ((int)FT891S_CatManager.currentRadioState.WidthValue / (double)Ft891FilterRanges.WidthCodeMax) * maxPixelShift;

            // Add the horizontal drag change
            double newPixelOffset = currentPixelOffset + e.HorizontalChange;

            // Convert back to Hz and clamp within allowed ranges (min/max handles negatives)
            double newWidthHz = (newPixelOffset / maxPixelShift) * Ft891FilterRanges.WidthCodeMax;
            int targetWidth = SnapToStep((int)Math.Round(newWidthHz), Ft891FilterRanges.WidthCodeMin, Ft891FilterRanges.WidthCodeMax, Ft891FilterRanges.WidthStepHz);

            SetWidthCodeInternal(targetWidth);

            // Pass the updated state or target width depending on your implementation
            Update_Width_TrapezoidPostion(FT891S_CatManager.currentRadioState.WidthValue);
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

            SetWidthCodeInternal(code);
            // Pass the updated state or target width depending on your implementation
            Update_Width_TrapezoidPostion(FT891S_CatManager.currentRadioState.WidthValue);
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

            SetShiftValueInternal(targetShift);

            // Pass the updated state or target shift depending on your implementation
            Update_Shift_TrapezoidPostion(FT891S_CatManager.currentRadioState.IfShiftHz);
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

            UpdateWaveform();
            UIValueChanged?.Invoke("NB", NbValue);
        }

        // Horizontal-only drag handler for Notch (No vertical movement permitted)
        private void NotchThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_isUpdatingProgrammatically) return;

            double currentCenter = BaseCenter + ShiftToPixels(FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftHz);
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

            if (NotchValueTextBlock != null)
                NotchValueTextBlock.Text = "NCH: " + NotchFrequency + " Hz";

            UpdateThumbPositions();
            UpdateWaveform();

            UIValueChanged?.Invoke("NCH_FREQ", NotchFrequency / 10);
            UIValueChanged?.Invoke("NCH_ENABLED", NotchEnabled);
        }

        private void ContourThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double currentLeft = Canvas.GetLeft(ContourThumb);
            double currentTop = Canvas.GetTop(ContourThumb);

            double targetCenterX = currentLeft + 8 + e.HorizontalChange;
            double targetCenterY = currentTop + 8 + e.VerticalChange;

            double clampedCenterX = Math.Max(border1X, Math.Min(border2X, targetCenterX));
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

            if (ContourValueTextBlock != null)
                ContourValueTextBlock.Text = "CNT: " + ContourFrequency + " Hz";

            UpdateWaveform();
            UIValueChanged?.Invoke("CONTOUR_FREQ", ContourFrequency);
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

            double currentCenter = BaseCenter;

            if (FT891S_CatManager.currentRadioState.IfShiftHz.OnOff == 1)
            {
                var shiftInfo = FT891S_CatManager.currentRadioState.IfShiftHz;
                double shiftHz = shiftInfo.IfShiftHz;

                bool isNegative = (shiftInfo.IfShiftDirection == -1) || (shiftHz < 0);
                double pixelOffset = ShiftToPixels((int)Math.Abs(shiftHz));

                currentCenter = isNegative ? (BaseCenter - pixelOffset) : (BaseCenter + pixelOffset);
            }
            else
            {
                currentCenter = BaseCenter;
            }

            double maxHalfWidth = (border2X - border1X - (2 * FixedSlopeWidth)) / 2.0;
            double minX = currentCenter;
            double maxX = currentCenter + maxHalfWidth - 8.0;

            double wX = minX + (((int)FT891S_CatManager.currentRadioState.WidthValue / (double)Ft891FilterRanges.WidthCodeMax) * (maxX - minX)) + 8.0;
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
            cX = Canvas.GetLeft(ContourThumb) + 8;
            cY = Canvas.GetTop(ContourThumb) + 8;
            cX = Math.Max(border1X, Math.Min(border2X, cX));

            if (!ContourEnabled) cY = PeakY;

            // ... (other sections)

            // Contour Section (DodgerBlue)
            if (ContourEnabled && ContourArcSegment != null)
            {
                cX = Math.Max(border1X, Math.Min(border2X, cX));

                double maxArcWidth = Math.Min(60.0, border2X - border1X);
                double halfArcWidth = maxArcWidth / 2.0;

                double startX = Math.Max(border1X, cX - halfArcWidth);
                double endX = Math.Min(border2X, cX + halfArcWidth);
                double actualSpan = endX - startX;

                ContourFigureStart.Point = new Point(startX, PeakY);
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
                ContourFigureStart.Point = new Point(border2X, PeakY);
                ContourArcSegment.Point = new Point(border2X, PeakY);
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

        private void FilterWaveButtons_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var code = border.Tag.ToString();
            uint switchValue = uint.Parse(code);

            ButtonSelection(Convert.ToByte(switchValue));
        }

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
                    if (state)
                    {
                        filters[FilterTypes.Shift].Enabled = true;
                        ShiftTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                    }
                    else
                    {
                        filters[FilterTypes.Shift].Enabled = false;
                        ShiftTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    }

                    String BuiltIsCommand;
                    if (FT891S_CatManager.currentRadioState.IfShiftHz.OnOff == 0)
                    {
                        BuiltIsCommand = FT891S_CatCommandTypes.BuildIsCommand(0, 0, '+', FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftHz);
                        await mainWindow._catManager.SendCatCommandAsync("IS", BuiltIsCommand, mainWindow._catManager.OutGoingDataLoopDelay);
                    }
                    else
                    {
                        if (FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftDirection == -1)
                            BuiltIsCommand = FT891S_CatCommandTypes.BuildIsCommand(0, 1, '-', FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftHz);
                        else
                            BuiltIsCommand = FT891S_CatCommandTypes.BuildIsCommand(0, 1, '+', FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftHz);

                        lastIS_Shift.IfShiftDirection = FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftDirection;
                        lastIS_Shift.IfShiftHz = FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftHz;

                        await mainWindow._catManager.SendCatCommandAsync("IS", BuiltIsCommand, mainWindow._catManager.OutGoingDataLoopDelay);
                    }
                    break;
                case FilterTypes.Width:
                    if (state)
                    {
                        filters[FilterTypes.Width].Enabled = true;
                        WidthTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                    }
                    else
                    {
                        filters[FilterTypes.Width].Enabled = false;
                        WidthTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    }

                    if (FT891S_CatManager.currentRadioState.WidthMode == WidthModes.SH_OFF)
                    {
                        mainWindow.WidthLabelBorder.Visibility = System.Windows.Visibility.Hidden;
                        await mainWindow._catManager.SendCatCommandAsync("SH", new object[] { 0, 0, 0 }, mainWindow._catManager.OutGoingDataLoopDelay);
                    }
                    else
                    {
                        mainWindow.WidthLabelBorder.Visibility = System.Windows.Visibility.Visible;
                        await mainWindow._catManager.SendCatCommandAsync("SH", new object[] { 0, 1, (int)FT891S_CatManager.currentRadioState.WidthValue }, mainWindow._catManager.OutGoingDataLoopDelay);
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
        private IS_Shift lastIS_Shift = new IS_Shift { IfShiftDirection = FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftDirection,
                                                       IfShiftHz = FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftHz};
        public async void ButtonSelection(byte button)
        {
            switch (button)
            {
                case FilterTypes.NB:
                    if (!(filters[FilterTypes.NB].Enabled))
                    {
                        filters[FilterTypes.NB].Enabled = true;
                        NbValue = filters[FilterTypes.NB].Value;
                        NBTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                    }
                    else
                    {
                        filters[FilterTypes.NB].Enabled = false;
                        filters[FilterTypes.NB].Value = NbValue;
                        NbValue = 0;
                        NBTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    }
                    NBValueTextBlock.Text = NbValue.ToString("D2");
                    UIValueChanged?.Invoke("NB", NbValue);
                    break;
                case FilterTypes.Notch:
                    if (!(filters[FilterTypes.Notch].Enabled))
                    {
                        filters[FilterTypes.Notch].Enabled = true;
                        NotchTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                        NotchPath.Visibility = Visibility.Visible;
                        NotchThumb.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        filters[FilterTypes.Notch].Enabled = false;
                        NotchTextBlock.Foreground = new SolidColorBrush(Colors.White);
                        NotchPath.Visibility = Visibility.Collapsed;
                        NotchThumb.Visibility = Visibility.Collapsed;
                    }
                    break;
                case FilterTypes.Contour:
                    if (!(filters[FilterTypes.Contour].Enabled))
                    {
                        filters[FilterTypes.Contour].Enabled = true;
                        ContourTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                        ContourPath.Visibility = Visibility.Visible;
                        ContourThumb.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        filters[FilterTypes.Contour].Enabled = false;
                        ContourTextBlock.Foreground = new SolidColorBrush(Colors.White);
                        ContourPath.Visibility = Visibility.Collapsed;
                        ContourThumb.Visibility = Visibility.Collapsed;
                    }
                    break;
                case FilterTypes.Shift:
                    if (!(filters[FilterTypes.Shift].Enabled))
                    {
                        filters[FilterTypes.Shift].Enabled = true;

                        FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftDirection = lastIS_Shift.IfShiftDirection;
                        FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftHz = lastIS_Shift.IfShiftHz;
                        
                        FT891S_CatManager.currentRadioState.IfShiftHz.OnOff = 1;

                        ShiftTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                        ShiftMainSectionPath.Visibility = Visibility.Visible;
                        ShiftThumb.Visibility = Visibility.Visible;

                        char _ShiftDirection = (FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftDirection == -1) ? '-' : '+';

                        // Sends the existing shift magnitude and direction along with the new OnOff state
                        String BuiltIsCommand = FT891S_CatCommandTypes.BuildIsCommand(0, FT891S_CatManager.currentRadioState.IfShiftHz.OnOff,
                                                                    _ShiftDirection, FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftHz);

                        await mainWindow._catManager.SendCatCommandAsync("IS", BuiltIsCommand, mainWindow._catManager.OutGoingDataLoopDelay);
                    }
                    else
                    {
                        filters[FilterTypes.Shift].Enabled = false;
                        
                        lastIS_Shift.IfShiftDirection = FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftDirection;
                        lastIS_Shift.IfShiftHz = FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftHz;

                        FT891S_CatManager.currentRadioState.IfShiftHz.OnOff = 0;

                        ShiftTextBlock.Foreground = new SolidColorBrush(Colors.White);
                        ShiftMainSectionPath.Visibility = Visibility.Collapsed;
                        ShiftThumb.Visibility = Visibility.Collapsed;

                        String BuiltIsCommandOff = FT891S_CatCommandTypes.BuildIsCommand(0, 0, '+', 0);

                        await mainWindow._catManager.SendCatCommandAsync("IS", BuiltIsCommandOff, mainWindow._catManager.OutGoingDataLoopDelay);
                    }
                    break;
                case FilterTypes.Width:
                    if (!(filters[FilterTypes.Width].Enabled))
                    {
                        filters[FilterTypes.Width].Enabled = true;
                        SetWidthValue(filters[FilterTypes.Width].Value);
                        WidthTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                        WidthMainSectionPath.Visibility = Visibility.Visible;
                        WidthThumb.Visibility = Visibility.Visible;

                        mainWindow.WidthLabelBorder.Visibility = System.Windows.Visibility.Visible;
                        await mainWindow._catManager.SendCatCommandAsync("SH", new object[] { 0, 1, (int)FT891S_CatManager.currentRadioState.WidthValue }, mainWindow._catManager.OutGoingDataLoopDelay);
                    }
                    else
                    {
                        filters[FilterTypes.Width].Enabled = false;
                        filters[FilterTypes.Width].Value = (int)FT891S_CatManager.currentRadioState.WidthValue;
                        SetWidthValue(0);
                        WidthTextBlock.Foreground = new SolidColorBrush(Colors.White);
                        WidthMainSectionPath.Visibility = Visibility.Collapsed;
                        WidthThumb.Visibility = Visibility.Collapsed;

                        mainWindow.WidthLabelBorder.Visibility = System.Windows.Visibility.Hidden;
                        await mainWindow._catManager.SendCatCommandAsync("SH", new object[] { 0, 0, 0 }, mainWindow._catManager.OutGoingDataLoopDelay);
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