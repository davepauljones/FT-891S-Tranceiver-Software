using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static YAESU_FT_891_Front_End.Animations;

namespace YAESU_FT_891_Front_End
{
    public partial class GainUserControl : UserControl
    {
        public static readonly RoutedEvent GainChangedEvent =
            EventManager.RegisterRoutedEvent(
                "GainChanged",
                RoutingStrategy.Bubble,
                typeof(GainChangedEventHandler),
                typeof(GainUserControl));

        public event GainChangedEventHandler GainChanged
        {
            add => AddHandler(GainChangedEvent, value);
            remove => RemoveHandler(GainChangedEvent, value);
        }

        #region Dependency Properties

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(GainUserControl),
                new PropertyMetadata(0.0, OnScaleRangeChanged));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(GainUserControl),
                new PropertyMetadata(255.0, OnScaleRangeChanged));

        public static readonly DependencyProperty DefaultGainProperty =
            DependencyProperty.Register(nameof(DefaultGain), typeof(int), typeof(GainUserControl),
                new PropertyMetadata(254));

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public int DefaultGain
        {
            get => (int)GetValue(DefaultGainProperty);
            set => SetValue(DefaultGainProperty, value);
        }

        private static void OnScaleRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GainUserControl control)
            {
                control.UpdateSliderScale();
            }
        }

        #endregion

        private int _currentGain = 0;
        private bool _isUpdatingSlider = false;
        private readonly Dictionary<int, (Border border, TextBlock text)> _ui;

        public GainUserControl()
        {
            InitializeComponent();

            _ui = new Dictionary<int, (Border border, TextBlock text)>
            {
                { 10, (Gain10Border, Gain10TextBlock) },
                { 20, (Gain20Border, Gain20TextBlock) },
                { 30, (Gain30Border, Gain30TextBlock) },
                { 40, (Gain40Border, Gain40TextBlock) },
                { 50, (Gain50Border, Gain50TextBlock) },
                { 60, (Gain60Border, Gain60TextBlock) },
                { 70, (Gain70Border, Gain70TextBlock) },
                { 80, (Gain80Border, Gain80TextBlock) },
                { 90, (Gain90Border, Gain90TextBlock) },
                { 100, (Gain100Border, Gain100TextBlock) },
                { 254, (GainDefaultBorder, GainDefaultTextBlock) },
                { 0, (GainMuteBorder, GainMuteTextBlock) }
            };

            Loaded += GainUserControl_Loaded;
        }

        private void GainUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateSliderScale();
        }

        private void UpdateSliderScale()
        {
            if (GainSlider == null) return;

            GainSlider.Minimum = Minimum;
            GainSlider.Maximum = Maximum;

            DoubleCollection ticks = new DoubleCollection();
            double range = Maximum - Minimum;
            double step = range / 8.0;

            for (int i = 0; i <= 8; i++)
            {
                ticks.Add(Math.Round(Minimum + (step * i)));
            }

            GainSlider.Ticks = ticks;
        }

        public void SetSupportedGains(IEnumerable<int> supportedGains)
        {
            var supported = new HashSet<int>(supportedGains);

            foreach (var kvp in _ui)
            {
                bool isSupported = supported.Contains(kvp.Key);

                kvp.Value.border.IsEnabled = isSupported;
                kvp.Value.border.Opacity = isSupported ? 1.0 : 0.35;

                kvp.Value.border.Background = isSupported
                    ? Brushes.LightGray
                    : Brushes.DimGray;

                kvp.Value.text.Foreground = isSupported
                    ? Brushes.Black
                    : Brushes.Gray;
            }
        }

        private void GainWindowCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border == null || !border.IsEnabled)
                return;

            if (int.TryParse(border.Tag?.ToString(), out int tagValue))
            {
                int targetGain;

                if (tagValue == 0) // MUTE
                {
                    targetGain = (int)Minimum;
                }
                else if (tagValue == 254) // DEFAULT
                {
                    targetGain = DefaultGain;
                }
                else // Percentages 10% through 100%
                {
                    // Calculate gain relative to the dynamic Minimum/Maximum range
                    double range = Maximum - Minimum;
                    targetGain = (int)Math.Round(Minimum + ((tagValue / 100.0) * range));
                }

                ChangeGain(targetGain);
                FadoutUserControl(this);
            }
        }

        public void ChangeGain(int gain)
        {
            // Reset all supported borders to default LightGray
            foreach (var kvp in _ui)
            {
                if (kvp.Value.border.IsEnabled)
                {
                    kvp.Value.border.Background = Brushes.LightGray;
                    kvp.Value.text.Foreground = Brushes.Black;
                }
            }

            double range = Maximum - Minimum;
            double currentPercent = range > 0 ? ((gain - Minimum) / range) * 100.0 : 0;

            List<int> presetsToHighlight = new List<int>();

            // 1. MUTE exact match
            if (gain == (int)Minimum && _ui.ContainsKey(0) && _ui[0].border.IsEnabled)
            {
                presetsToHighlight.Add(0);
            }
            // 2. DEFAULT match (Highlight DEFAULT + find nearest percentage preset)
            else if (gain == DefaultGain)
            {
                if (_ui.ContainsKey(254) && _ui[254].border.IsEnabled)
                {
                    presetsToHighlight.Add(254);
                }

                int nearestPreset = -1;
                double smallestDiff = double.MaxValue;

                for (int targetPercent = 10; targetPercent <= 100; targetPercent += 10)
                {
                    if (_ui.ContainsKey(targetPercent) && _ui[targetPercent].border.IsEnabled)
                    {
                        double diff = Math.Abs(currentPercent - targetPercent);
                        if (diff < smallestDiff)
                        {
                            smallestDiff = diff;
                            nearestPreset = targetPercent;
                        }
                    }
                }

                if (nearestPreset != -1)
                {
                    presetsToHighlight.Add(nearestPreset);
                }
            }
            // 3. Regular Slider movement or Preset Button click (±5% tolerance)
            else
            {
                const double tolerance = 5.0;

                for (int targetPercent = 10; targetPercent <= 100; targetPercent += 10)
                {
                    if (Math.Abs(currentPercent - targetPercent) <= tolerance)
                    {
                        if (_ui.ContainsKey(targetPercent) && _ui[targetPercent].border.IsEnabled)
                        {
                            presetsToHighlight.Add(targetPercent);
                            break;
                        }
                    }
                }
            }

            // Highlight all selected presets
            foreach (int presetKey in presetsToHighlight)
            {
                if (_ui.ContainsKey(presetKey))
                {
                    _ui[presetKey].border.Background = Brushes.DodgerBlue;
                    _ui[presetKey].text.Foreground = Brushes.White;
                }
            }

            // Sync Slider value safely and update its content label
            _isUpdatingSlider = true;
            GainSlider.Value = gain;
            UpdateGainLabel(gain);
            _isUpdatingSlider = false;

            if (gain != _currentGain)
            {
                RaiseEvent(new GainChangedEventArgs(GainChangedEvent, gain));
                _currentGain = gain;
            }
        }

        private void GainSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (GainLabel == null || _isUpdatingSlider) return;

            int gainValue = (int)e.NewValue;
            ChangeGain(gainValue);
        }

        private void UpdateGainLabel(int gainValue)
        {
            if (GainLabel == null) return;

            if (gainValue == (int)Minimum)
            {
                GainLabel.Content = "MUTE";
            }
            else if (gainValue == DefaultGain)
            {
                GainLabel.Content = "DEFAULT";
            }
            else
            {
                GainLabel.Content = $"{gainValue}";
            }
        }
    }
}