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
    public partial class ModeUserControl : UserControl
    {
        public static readonly RoutedEvent ModeChangedEvent =
            EventManager.RegisterRoutedEvent(
                "ModeChanged",
                RoutingStrategy.Bubble,
                typeof(ModeChangedEventHandler),
                typeof(ModeUserControl));

        public event ModeChangedEventHandler ModeChanged
        {
            add => AddHandler(ModeChangedEvent, value);
            remove => RemoveHandler(ModeChangedEvent, value);
        }

        private RadioMode _currentMode = RadioMode.USB;

        private readonly Dictionary<RadioMode, (Border border, TextBlock text)> _ui;

        public ModeUserControl()
        {
            InitializeComponent();

            _ui = new Dictionary<RadioMode, (Border border, TextBlock text)>
            {
                { RadioMode.LSB, (LSBBorder, LSBTextBlock) },
                { RadioMode.USB, (USBBorder, USBTextBlock) },
                { RadioMode.CW_L, (CW_LBorder, CW_LTextBlock) },
                { RadioMode.CW_U, (CW_UBorder, CW_UTextBlock) },
                { RadioMode.AM, (AMBorder, AMTextBlock) },
                { RadioMode.AM_N, (AM_NBorder, AM_NTextBlock) },
                { RadioMode.FM, (FMBorder, FMTextBlock) },
                { RadioMode.FM_N, (FM_NBorder, FM_NTextBlock) },
                { RadioMode.DATA_L, (DATA_LBorder, DATA_LTextBlock) },
                { RadioMode.DATA_U, (DATA_UBorder, DATA_UTextBlock) },
                { RadioMode.DATA_FM, (DATA_FMBorder, DATA_FMTextBlock) },
                { RadioMode.DATA_FM_N, (D_FM_NBorder, D_FM_NTextBlock) },
                { RadioMode.RTTY_L, (RTTY_LBorder, RTTY_LTextBlock) },
                { RadioMode.RTTY_U, (RTTY_UBorder, RTTY_UTextBlock) },
                { RadioMode.PSK, (PSKBorder, PSKTextBlock) },
                { RadioMode.PRESET, (PRESETBorder, PRESETTextBlock) },
            };
        }

        public void SetSupportedModes(IEnumerable<RadioMode> supportedModes)
        {
            var supported = new HashSet<RadioMode>(supportedModes);

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

        private void ModeWindowCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border == null)
                return;

            if (!border.IsEnabled)
                return;

            RadioMode mode;
            if (Enum.TryParse(border.Tag.ToString(), out mode))
            {
                ChangeMode(mode);

                FadoutBorderWindow(ModeWindowBorder);
            }
        }

        public void ChangeMode(RadioMode mode)
        {
            foreach (var kvp in _ui)
            {
                if (kvp.Value.border.IsEnabled)
                {
                    kvp.Value.border.Background = Brushes.LightGray;
                    kvp.Value.text.Foreground = Brushes.Black;
                }
            }

            if (_ui.ContainsKey(mode) && _ui[mode].border.IsEnabled)
            {
                _ui[mode].border.Background = Brushes.DodgerBlue;
                _ui[mode].text.Foreground = Brushes.White;
            }

            if (!(mode == _currentMode))
            {
                RaiseEvent(new ModeChangedEventArgs(ModeChangedEvent, mode));

                _currentMode = mode;
            }
        }
    }
}