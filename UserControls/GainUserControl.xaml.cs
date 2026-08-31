using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static YAESU_FT_891_Front_End.Animations;
using YAESU_FT_891_Front_End.Models;

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

        private int _currentGain = 0;

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
                { 255, (GainMuteBorder, GainMuteTextBlock) }
            };
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
            if (border == null)
                return;

            if (!border.IsEnabled)
                return;

            int gain;
            if (int.TryParse(border.Tag.ToString(), out gain))
            {
                ChangeGain(gain);

                FadoutUserControl(this);
            }
        }

        public void ChangeGain(int gain)
        {
            foreach (var kvp in _ui)
            {
                if (kvp.Value.border.IsEnabled)
                {
                    kvp.Value.border.Background = Brushes.LightGray;
                    kvp.Value.text.Foreground = Brushes.Black;
                }
            }

            if (_ui.ContainsKey(gain) && _ui[gain].border.IsEnabled)
            {
                _ui[gain].border.Background = Brushes.DodgerBlue;
                _ui[gain].text.Foreground = Brushes.White;
            }

            // Keep GainSlider synced with preset buttons (this will automatically fire ValueChanged)
            GainSlider.Value = gain;

            if (!(gain == _currentGain))
            {
                RaiseEvent(new GainChangedEventArgs(GainChangedEvent, gain));
                _currentGain = gain;
            }
        }

        private void GainSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Ensure GainLabel is initialized during XAML loading
            if (GainLabel == null) return;

            int gainValue = (int)e.NewValue;

            // Display special preset labels or percentage format
            if (gainValue == 255)
            {
                GainLabel.Content = "MUTE";
            }
            else if (gainValue == 254)
            {
                GainLabel.Content = "DEFAULT";
            }
            else
            {
                // Example A: Display direct value (e.g., "50")
                GainLabel.Content = $"{gainValue}%";

                // Example B: Or convert 0-255 byte range to an actual percentage (0-100%)
                // int percent = (int)Math.Round((gainValue / 255.0) * 100);
                // GainLabel.Content = $"{percent}%";
            }
        }
    }
}