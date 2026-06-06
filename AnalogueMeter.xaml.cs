// ====================================================================
//  Custom Analog S-Meter Control (Yaesu FT-710 Style Layout)
//  
//  Co-Created & Developed By: 
//  - G7UIV (Amateur Radio Operator)
//  - Gemini (AI Engineering Collaborator)
//
//  Created: May 2026
//  Description: High-fidelity analog transceiver meter for WPF with 
//               dynamic hardware-calibrated scale layouts.
// ====================================================================

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace HamRadioControls
{
    public partial class AnalogMeter : UserControl
    {
        #region Mode

        public enum MeterMode
        {
            SignalS,
            Power,
            SWR
        }

        #endregion

        #region Scale Layout Tuning Parameters

        // Use these variables to dial in your layout perfectly without touching the math loop
        private const double METER_CENTER_X = 200;
        private const double METER_CENTER_Y = 259;   // Increase to drop middle, decrease to lift middle
        private const double METER_ARC_RADIUS = 133; // Global sizing (Elevator up/down)

        private const double LABEL_PADDING = 18;     // Distance from the top of the ticks to the labels

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(AnalogMeter),
                new PropertyMetadata(0.0, OnValueChanged));

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register(nameof(Mode), typeof(MeterMode), typeof(AnalogMeter),
                new PropertyMetadata(MeterMode.SignalS, OnModeChanged));

        public static readonly DependencyProperty MeterTitleProperty =
            DependencyProperty.Register(nameof(MeterTitle), typeof(string), typeof(AnalogMeter),
                new PropertyMetadata("SIGNAL", OnTitleChanged));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public MeterMode Mode
        {
            get => (MeterMode)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        public string MeterTitle
        {
            get => (string)GetValue(MeterTitleProperty);
            set => SetValue(MeterTitleProperty, value);
        }

        #endregion

        #region Livery Model

        private class MeterLivery
        {
            public double MinAngle;
            public double MaxAngle;
            public int TickCount;

            public Func<double, double> MapValueToAngle;
            public Func<int, string> Label;
            public Func<int, Brush> TickBrush;
        }

        private MeterLivery _livery;

        #endregion

        public AnalogMeter()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                Dispatcher.BeginInvoke(new Action(ApplyMode),
                    System.Windows.Threading.DispatcherPriority.Loaded);
            };
        }

        #region Mode + Title

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var meter = (AnalogMeter)d;
            meter.ApplyMode();
        }

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var meter = (AnalogMeter)d;
            meter.MeterTitleText.Text = (string)e.NewValue;
        }

        private void ApplyMode()
        {
            _livery = GetLivery(Mode);
            DrawScale();
        }

        #endregion

        #region Livery Definitions

        private MeterLivery GetLivery(MeterMode mode)
        {
            switch (mode)
            {
                case MeterMode.SignalS:
                    return new MeterLivery
                    {
                        MinAngle = -60,
                        MaxAngle = 60,
                        // 14 steps perfectly maps the FT-710 scale increments
                        TickCount = 14,
                        MapValueToAngle = v =>
                        {
                            // FIX: Changed multiplier from 1.2 to 1.285 to accurately line up 
                            // incoming value calibration with physical S9 tick placement
                            return (Math.Max(0, Math.Min(100, v)) * 1.285) - 60;
                        },
                        Label = i =>
                        {
                            // Ticks 0 to 8 map cleanly to S1 through S9
                            if (i == 0) return "1";
                            if (i == 2) return "3";
                            if (i == 4) return "5";
                            if (i == 6) return "7";
                            if (i == 8) return "9";
                            // Ticks 10, 12, 14 run the high dB over-signal layout
                            if (i == 10) return "+20";
                            if (i == 12) return "+40";
                            if (i == 14) return "+60";
                            return "";
                        },
                        // S1-S9 are white, +20dB to +60dB illuminate DodgerBlue
                        TickBrush = i => i > 8 ? Brushes.DodgerBlue : Brushes.White
                    };

                case MeterMode.Power:
                    return new MeterLivery
                    {
                        MinAngle = -60,
                        MaxAngle = 60,
                        TickCount = 10,
                        MapValueToAngle = v =>
                        {
                            double pct = Math.Log10(v + 1) / Math.Log10(1000);
                            return (pct * 120) - 60;
                        },
                        Label = i => $"{i * 100}W",
                        TickBrush = i => Brushes.White
                    };

                case MeterMode.SWR:
                    return new MeterLivery
                    {
                        MinAngle = -60,
                        MaxAngle = 60,
                        TickCount = 10,
                        MapValueToAngle = v =>
                        {
                            double swr = Math.Max(1, Math.Min(10, v));
                            return ((swr - 1) / 9.0 * 120) - 60;
                        },
                        Label = i => (1 + i).ToString("0.0"),
                        TickBrush = i => i > 5 ? Brushes.DodgerBlue : Brushes.White
                    };

                default:
                    return null;
            }
        }

        #endregion

        #region Scale Drawing

        private void DrawScale()
        {
            ScaleCanvas.Children.Clear();

            if (_livery == null)
                _livery = GetLivery(Mode);

            for (int i = 0; i <= _livery.TickCount; i++)
            {
                double angle =
                    _livery.MinAngle +
                    ((double)i / _livery.TickCount) *
                    (_livery.MaxAngle - _livery.MinAngle);

                // Convert to radians (0 degrees points straight up/North)
                double rad = (angle - 90) * Math.PI / 180;

                //------------------------------------------
                // Tick configurations
                //------------------------------------------
                bool majorTick = (i % 2 == 0); // FT-710 leverages alternate major tick patterns
                double tickLength = majorTick ? 10 : 6;

                // Ticks START exactly on the arc line
                double x1 = METER_CENTER_X + METER_ARC_RADIUS * Math.Cos(rad);
                double y1 = METER_CENTER_Y + METER_ARC_RADIUS * Math.Sin(rad);

                // Ticks EMANATE OUTWARD from the arc
                double tickOuterRadius = METER_ARC_RADIUS + tickLength;
                double x2 = METER_CENTER_X + tickOuterRadius * Math.Cos(rad);
                double y2 = METER_CENTER_Y + tickOuterRadius * Math.Sin(rad);

                ScaleCanvas.Children.Add(new Line
                {
                    X1 = x1,
                    Y1 = y1,
                    X2 = x2,
                    Y2 = y2,
                    Stroke = _livery.TickBrush(i),
                    StrokeThickness = majorTick ? 2.5 : 1.2,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                });

                //------------------------------------------
                // Labels (Positioned ABOVE the ticks and arc)
                //------------------------------------------
                string label = _livery.Label(i);

                if (!string.IsNullOrWhiteSpace(label))
                {
                    double labelRadius = tickOuterRadius + LABEL_PADDING;

                    double lx = METER_CENTER_X + labelRadius * Math.Cos(rad);
                    double ly = METER_CENTER_Y + labelRadius * Math.Sin(rad);

                    var text = new TextBlock
                    {
                        Text = label,
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Width = 44,
                        TextAlignment = TextAlignment.Center,
                        Foreground = _livery.TickBrush(i) // Dynamic label matching color scheme
                    };

                    Canvas.SetLeft(text, lx - 22);
                    Canvas.SetTop(text, ly - 8);

                    ScaleCanvas.Children.Add(text);
                }
            }

            //------------------------------------------
            // Twin Static Unit Labels (S & dB)
            //------------------------------------------
            if (Mode == MeterMode.SignalS)
            {
                // 1. "S" Label (Very Top, Very Left)
                var sText = new TextBlock
                {
                    Text = "S",
                    FontSize = 22,
                    FontWeight = FontWeights.Bold,
                    Width = 30,
                    TextAlignment = TextAlignment.Center,
                    Foreground = Brushes.White
                };
                Canvas.SetLeft(sText, 75);
                Canvas.SetTop(sText, 95);
                ScaleCanvas.Children.Add(sText);

                // 2. "dB" Label (Very Top, Very Right - Symmetrical to S)
                var dbText = new TextBlock
                {
                    Text = "dB",
                    FontSize = 22,
                    FontWeight = FontWeights.Bold,
                    Width = 40,
                    TextAlignment = TextAlignment.Center,
                    Foreground = Brushes.DodgerBlue // Sourced to match over-signal colors
                };
                // Symmetric coordinate mirror: Canvas Width (400) - Left Offset (75) - Width (40) = 285
                Canvas.SetLeft(dbText, 285);
                Canvas.SetTop(dbText, 95);
                ScaleCanvas.Children.Add(dbText);
            }
        }

        #endregion

        #region Value

        public static double ConvertDoubleToPercentage(double byteValue)
        {
            if (byteValue <= 0) return 0.0;
            if (byteValue >= 255) return 100.0;

            return (byteValue / 255.0) * 100.0;
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var meter = (AnalogMeter)d;

            if (meter.NeedleRotation == null)
                return;

            double targetValue = Convert.ToDouble(e.NewValue);

            if (targetValue == 0.0 && e.OldValue != null && Convert.ToDouble(e.OldValue) > 0.0)
            {
                return;
            }

            if (meter._livery == null)
                meter._livery = meter.GetLivery(meter.Mode);

            double targetAngle = meter._livery.MapValueToAngle(targetValue);
            double currentAngleMidFlight = meter.NeedleRotation.Angle;

            meter.NeedleRotation.BeginAnimation(RotateTransform.AngleProperty, null);
            meter.NeedleRotation.Angle = currentAngleMidFlight;

            DoubleAnimation smoothAnim = new DoubleAnimation
            {
                From = currentAngleMidFlight,
                To = targetAngle,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new QuadraticEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            meter.NeedleRotation.BeginAnimation(
                RotateTransform.AngleProperty,
                smoothAnim,
                HandoffBehavior.SnapshotAndReplace
            );
        }

        #endregion
    }
}