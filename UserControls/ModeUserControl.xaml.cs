using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

        private void ModeWindowCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border b && byte.TryParse(b.Tag.ToString(), out var val))
            {
                ChangeMode((RadioMode)val);
            }
        }

        public void ChangeMode(RadioMode mode)
        {
            foreach (var item in _ui.Values)
            {
                item.border.Background = Brushes.LightGray;
                item.text.Foreground = Brushes.Black;
            }

            // preset styling
            PRESETBorder.Background = Brushes.Gray;
            PRESETTextBlock.Foreground = Brushes.White;

            if (_ui.TryGetValue(mode, out var selected))
            {
                selected.border.Background = Brushes.DodgerBlue;
                selected.text.Foreground = Brushes.White;
            }

            _currentMode = mode;

            RaiseEvent(new ModeChangedEventArgs(ModeChangedEvent, mode));
        }
    }
}