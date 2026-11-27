using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static YAESU_FT_891_Front_End.Animations;

namespace YAESU_FT_891_Front_End
{
    public partial class WattUserControl : UserControl
    {
        public static readonly RoutedEvent WattChangedEvent =
            EventManager.RegisterRoutedEvent(
                "WattChanged",
                RoutingStrategy.Bubble,
                typeof(WattChangedEventHandler),
                typeof(WattUserControl));

        public event WattChangedEventHandler WattChanged
        {
            add => AddHandler(WattChangedEvent, value);
            remove => RemoveHandler(WattChangedEvent, value);
        }

        #region Dependency Properties

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(WattUserControl),
                new PropertyMetadata(0.0, OnScaleRangeChanged));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(WattUserControl),
                new PropertyMetadata(255.0, OnScaleRangeChanged));

        public static readonly DependencyProperty DefaultWattProperty =
            DependencyProperty.Register(nameof(DefaultWatt), typeof(int), typeof(WattUserControl),
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

        public int DefaultWatt
        {
            get => (int)GetValue(DefaultWattProperty);
            set => SetValue(DefaultWattProperty, value);
        }

        private static void OnScaleRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WattUserControl control)
            {
                control.UpdateSliderScale();
            }
        }

        #endregion

        private int _currentWatt = 0;
        private bool _isUpdatingSlider = false;
        private readonly Dictionary<int, (Border border, TextBlock text)> _ui;

        public WattUserControl()
        {
            InitializeComponent();

            _ui = new Dictionary<int, (Border border, TextBlock text)>
            {
                { 10, (Watt10Border, Watt10TextBlock) },
                { 20, (Watt20Border, Watt20TextBlock) },
                { 30, (Watt30Border, Watt30TextBlock) },
                { 40, (Watt40Border, Watt40TextBlock) },
                { 50, (Watt50Border, Watt50TextBlock) },
                { 60, (Watt60Border, Watt60TextBlock) },
                { 70, (Watt70Border, Watt70TextBlock) },
                { 80, (Watt80Border, Watt80TextBlock) },
                { 90, (Watt90Border, Watt90TextBlock) },
                { 100, (Watt100Border, Watt100TextBlock) },
                { 254, (WattDefaultBorder, WattDefaultTextBlock) },
                { 0, (WattMuteBorder, WattMuteTextBlock) }
            };

            Loaded += WattUserControl_Loaded;
        }

        private void WattUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateSliderScale();
        }

        private void UpdateSliderScale()
        {
            if (WattSlider == null) return;

            WattSlider.Minimum = Minimum;
            WattSlider.Maximum = Maximum;

            DoubleCollection ticks = new DoubleCollection();
            double range = Maximum - Minimum;
            double step = range / 8.0;

            for (int i = 0; i <= 8; i++)
            {
                ticks.Add(Math.Round(Minimum + (step * i)));
            }

            WattSlider.Ticks = ticks;
        }

        public void SetSupportedWatts(IEnumerable<int> supportedWatts)
        {
            var supported = new HashSet<int>(supportedWatts);

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

        private void WattWindowCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border == null || !border.IsEnabled)
                return;

            if (int.TryParse(border.Tag?.ToString(), out int tagValue))
            {
                int targetWatt;

                if (tagValue == 0) // MIN 5W
                {
                    targetWatt = (int)Minimum;
                }
                else if (tagValue == 254) // DEFAULT
                {
                    targetWatt = DefaultWatt;
                }
                else // Exact watt buttons: 10, 20, 30 ... 100
                {
                    targetWatt = tagValue;
                }

                ChangeWatt(targetWatt);
                FadoutUserControl(this);
            }
        }

        public void ChangeWatt(int watt)
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
            double currentPercent = range > 0 ? ((watt - Minimum) / range) * 100.0 : 0;

            List<int> presetsToHighlight = new List<int>();

            // 1. MUTE exact match
            if (watt == (int)Minimum && _ui.ContainsKey(0) && _ui[0].border.IsEnabled)
            {
                presetsToHighlight.Add(0);
            }
            // 2. DEFAULT match (Highlight DEFAULT + find nearest percentage preset)
            else if (watt == DefaultWatt)
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
            WattSlider.Value = watt;
            UpdateWattLabel(watt);
            _isUpdatingSlider = false;

            if (watt != _currentWatt)
            {
                RaiseEvent(new WattChangedEventArgs(WattChangedEvent, watt));
                _currentWatt = watt;
            }
        }

        private void WattSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (WattLabel == null || _isUpdatingSlider) return;

            int wattValue = (int)e.NewValue;
            ChangeWatt(wattValue);
        }

        private void UpdateWattLabel(int wattValue)
        {
            if (WattLabel == null) return;

            if (wattValue == (int)Minimum)
            {
                WattLabel.Content = "MUTE";
            }
            else if (wattValue == DefaultWatt)
            {
                WattLabel.Content = "DEFAULT";
            }
            else
            {
                WattLabel.Content = $"{wattValue}";
            }
        }
    }
}