using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace HamRadioControls
{
    public partial class SignalPowerMeter : UserControl
    {
        // Needle X-offset travel range matching your meter scale (-170 left to +54 right)
        private const double MinNeedleX = -170.0;
        private const double MaxNeedleX = 54.0;
        private static readonly TimeSpan NeedleAnimationDuration = TimeSpan.FromMilliseconds(250);

        public SignalPowerMeter()
        {
            InitializeComponent();
            SetNeedleImmediate(0.0);
            UpdateBacklightVisual(IsBacklightOn);
        }

        #region Dependency Properties

        // 1. Signal Strength
        public static readonly DependencyProperty SignalStrengthProperty =
            DependencyProperty.Register(
                nameof(SignalStrength),
                typeof(double),
                typeof(SignalPowerMeter),
                new FrameworkPropertyMetadata(
                    0.0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSignalValueChanged));

        public double SignalStrength
        {
            get => (double)GetValue(SignalStrengthProperty);
            set => SetValue(SignalStrengthProperty, value);
        }

        // 2. Signal Max Scale
        public static readonly DependencyProperty SignalMaxProperty =
            DependencyProperty.Register(
                nameof(SignalMax),
                typeof(double),
                typeof(SignalPowerMeter),
                new FrameworkPropertyMetadata(
                    100.0,
                    OnSignalValueChanged,
                    CoerceMaxValue));

        public double SignalMax
        {
            get => (double)GetValue(SignalMaxProperty);
            set => SetValue(SignalMaxProperty, value);
        }

        // 3. Calibration Multiplier (1.0 = default; increase e.g. 1.2 to lift readings to match AnalogMeter while keeping 0 at zero)
        public static readonly DependencyProperty CalibrationFactorProperty =
            DependencyProperty.Register(
                nameof(CalibrationFactor),
                typeof(double),
                typeof(SignalPowerMeter),
                new FrameworkPropertyMetadata(
                    1.0,
                    OnSignalValueChanged,
                    CoerceCalibrationFactor));

        public double CalibrationFactor
        {
            get => (double)GetValue(CalibrationFactorProperty);
            set => SetValue(CalibrationFactorProperty, value);
        }

        // 4. Power Value
        public static readonly DependencyProperty PowerValueProperty =
            DependencyProperty.Register(
                nameof(PowerValue),
                typeof(double),
                typeof(SignalPowerMeter),
                new FrameworkPropertyMetadata(
                    0.0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnPowerValueChanged));

        public double PowerValue
        {
            get => (double)GetValue(PowerValueProperty);
            set => SetValue(PowerValueProperty, value);
        }

        // 5. Power Max Scale
        public static readonly DependencyProperty PowerMaxProperty =
            DependencyProperty.Register(
                nameof(PowerMax),
                typeof(double),
                typeof(SignalPowerMeter),
                new FrameworkPropertyMetadata(
                    100.0,
                    OnPowerValueChanged,
                    CoerceMaxValue));

        public double PowerMax
        {
            get => (double)GetValue(PowerMaxProperty);
            set => SetValue(PowerMaxProperty, value);
        }

        // 6. Backlight On/Off
        public static readonly DependencyProperty IsBacklightOnProperty =
            DependencyProperty.Register(
                nameof(IsBacklightOn),
                typeof(bool),
                typeof(SignalPowerMeter),
                new FrameworkPropertyMetadata(
                    true,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnIsBacklightOnChanged));

        public bool IsBacklightOn
        {
            get => (bool)GetValue(IsBacklightOnProperty);
            set => SetValue(IsBacklightOnProperty, value);
        }

        #endregion

        #region Property Callbacks & Movement Logic

        private static object CoerceMaxValue(DependencyObject d, object baseValue)
        {
            double max = Convert.ToDouble(baseValue);
            return max <= 0.0 ? 1.0 : max;
        }

        private static object CoerceCalibrationFactor(DependencyObject d, object baseValue)
        {
            double factor = Convert.ToDouble(baseValue);
            return factor <= 0.0 ? 1.0 : factor;
        }

        private double NormalizeSignal(double rawValue)
        {
            double max = SignalMax <= 0.0 ? 1.0 : SignalMax;
            double calibrated = Math.Max(0.0, rawValue) * CalibrationFactor;
            return Math.Max(0.0, Math.Min(1.0, calibrated / max));
        }

        private double NormalizePower(double rawValue)
        {
            double max = PowerMax <= 0.0 ? 1.0 : PowerMax;
            return Math.Max(0.0, Math.Min(1.0, rawValue / max));
        }

        private static void OnSignalValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SignalPowerMeter meter && meter.NeedleTranslation != null)
            {
                double targetValue = Convert.ToDouble(e.NewValue);
                if (targetValue == 0.0 && e.OldValue != null && Convert.ToDouble(e.OldValue) > 0.0)
                {
                    return;
                }

                double normalized = meter.NormalizeSignal(targetValue);
                meter.AnimateNeedleToNormalized(normalized);
            }
        }

        private static void OnPowerValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SignalPowerMeter meter && meter.NeedleTranslation != null)
            {
                double targetValue = Convert.ToDouble(e.NewValue);
                if (targetValue == 0.0 && e.OldValue != null && Convert.ToDouble(e.OldValue) > 0.0)
                {
                    return;
                }

                double normalized = meter.NormalizePower(targetValue);
                meter.AnimateNeedleToNormalized(normalized);
            }
        }

        private static void OnIsBacklightOnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SignalPowerMeter meter && meter.SignalMeterSunburstBackground != null)
            {
                meter.UpdateBacklightVisual((bool)e.NewValue);
            }
        }

        private void AnimateNeedleToNormalized(double normalized)
        {
            double targetX = MinNeedleX + (normalized * (MaxNeedleX - MinNeedleX));
            double currentXMidFlight = NeedleTranslation.X;

            NeedleTranslation.BeginAnimation(TranslateTransform.XProperty, null);
            NeedleTranslation.X = currentXMidFlight;

            DoubleAnimation smoothAnim = new DoubleAnimation
            {
                From = currentXMidFlight,
                To = targetX,
                Duration = NeedleAnimationDuration,
                EasingFunction = new QuadraticEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            NeedleTranslation.BeginAnimation(
                TranslateTransform.XProperty,
                smoothAnim,
                HandoffBehavior.SnapshotAndReplace
            );
        }

        private void SetNeedleImmediate(double normalized)
        {
            if (NeedleTranslation == null) return;
            double targetX = MinNeedleX + (normalized * (MaxNeedleX - MinNeedleX));
            NeedleTranslation.BeginAnimation(TranslateTransform.XProperty, null);
            NeedleTranslation.X = targetX;
        }

        private void UpdateBacklightVisual(bool isOn)
        {
            if (SignalMeterSunburstBackground != null)
            {
                SignalMeterSunburstBackground.Visibility = isOn ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        #endregion

        #region Helper Methods

        public void SetNeedlePosition(double xOffset)
        {
            if (NeedleTranslation == null) return;
            NeedleTranslation.BeginAnimation(TranslateTransform.XProperty, null);
            NeedleTranslation.X = xOffset;
        }

        #endregion
    }
}