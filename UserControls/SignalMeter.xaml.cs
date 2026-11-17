using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace HamRadioControls
{
    public partial class SignalPowerMeter : UserControl
    {
        // Needle X-offset travel range matching your meter scale (-170 left to +54 right)[cite: 2]
        private const double MinNeedleX = -170.0;
        private const double MaxNeedleX = 54.0;

        private DispatcherTimer _signalSimTimer;
        private DateTime _simEndTime;
        private readonly Random _rng = new Random();
        private double _simulatedBaseSignal;

        public SignalPowerMeter()
        {
            InitializeComponent();
            MoveNeedleToNormalized(0.0);
            UpdateBacklightVisual(IsBacklightOn);
        }

        #region Dependency Properties

        // 1. Signal Strength (e.g., 0 to SignalMax)
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

        // 2. Signal Max Scale (Default 100.0; e.g., set to 255.0 for byte-range S-meter data)
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

        // 3. Power Value (e.g., 0 to PowerMax)
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

        // 4. Power Max Scale (Default 100.0; e.g., set to 30.0 or 10.0 for watt ranges)
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

        // 5. Backlight On/Off
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

        #region Property Callbacks & Scaling Logic

        private static object CoerceMaxValue(DependencyObject d, object baseValue)
        {
            double max = (double)baseValue;
            return max <= 0.0 ? 1.0 : max;
        }

        private static void OnSignalValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SignalPowerMeter meter)
            {
                double normalized = Math.Max(0.0, Math.Min(1.0, meter.SignalStrength / meter.SignalMax));
                meter.MoveNeedleToNormalized(normalized);
            }
        }

        private static void OnPowerValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SignalPowerMeter meter)
            {
                meter.StopSlidingAnimation();
                double normalized = Math.Max(0.0, Math.Min(1.0, meter.PowerValue / meter.PowerMax));
                meter.MoveNeedleToNormalized(normalized);
            }
        }

        private static void OnIsBacklightOnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SignalPowerMeter meter)
            {
                meter.UpdateBacklightVisual((bool)e.NewValue);
            }
        }

        private void MoveNeedleToNormalized(double normalized, double transitionMs = 0)
        {
            double targetX = MinNeedleX + (normalized * (MaxNeedleX - MinNeedleX));

            if (transitionMs <= 0)
            {
                NeedleTranslation.BeginAnimation(TranslateTransform.XProperty, null);
                NeedleTranslation.X = targetX;
            }
            else
            {
                // Smooth ballistic needle sweep between random signal fluctuations
                DoubleAnimation sweep = new DoubleAnimation
                {
                    To = targetX,
                    Duration = TimeSpan.FromMilliseconds(transitionMs),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                NeedleTranslation.BeginAnimation(TranslateTransform.XProperty, sweep);
            }
        }

        private void UpdateBacklightVisual(bool isOn)
        {
            SignalMeterSunburstBackground.Visibility = isOn ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region Animation & Helper Methods

        public void SetNeedlePosition(double xOffset)
        {
            StopSlidingAnimation();
            NeedleTranslation.BeginAnimation(TranslateTransform.XProperty, null);
            NeedleTranslation.X = xOffset;
        }

        //public void StartSlidingAnimation()
        //{
        //    StopSlidingAnimation();

        //    // Pick a random carrier base strength (e.g. S5 to S9+ area: 35% to 75% of SignalMax)
        //    _simulatedBaseSignal = SignalMax * (0.35 + (_rng.NextDouble() * 0.40));
        //    _simEndTime = DateTime.Now.AddSeconds(10);

        //    _signalSimTimer = new DispatcherTimer
        //    {
        //        Interval = TimeSpan.FromMilliseconds(120)
        //    };

        //    _signalSimTimer.Tick += (s, e) =>
        //    {
        //        if (DateTime.Now >= _simEndTime)
        //        {
        //            StopSlidingAnimation();
        //            // Drop needle smoothly back to noise floor when transmission ends
        //            SetValue(SignalStrengthProperty, 0.0);
        //            MoveNeedleToNormalized(0.0, 350);
        //            return;
        //        }

        //        // Occasionally drift the carrier strength up or down like fading propagation
        //        if (_rng.NextDouble() < 0.25)
        //        {
        //            _simulatedBaseSignal = SignalMax * (0.25 + (_rng.NextDouble() * 0.55));
        //        }

        //        // Add rapid voice/modulation jitter (+/- 18% of SignalMax)
        //        double jitter = (_rng.NextDouble() - 0.5) * 0.36 * SignalMax;
        //        double nextSignal = Math.Max(0.0, Math.Min(SignalMax, _simulatedBaseSignal + jitter));

        //        // Update property and smoothly animate needle to the new reading
        //        SetValue(SignalStrengthProperty, nextSignal);
        //        MoveNeedleToNormalized(nextSignal / SignalMax, 110);
        //    };

        //    _signalSimTimer.Start();
        //}

        public void StopSlidingAnimation()
        {
            if (_signalSimTimer != null)
            {
                _signalSimTimer.Stop();
                _signalSimTimer = null;
            }
        }

        #endregion
    }
}