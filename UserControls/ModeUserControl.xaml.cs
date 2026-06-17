using FT891S_CatControl;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static YAESU_FT_891_Front_End.Animations;

namespace YAESU_FT_891_Front_End
{
    // Custom arguments to carry our mode change data to the MainWindow
    public class ModeChangedEventArgs : RoutedEventArgs
    {
        public byte SelectedModeFT710 { get; }
        public int SelectedModeFT891 { get; }

        public ModeChangedEventArgs(RoutedEvent routedEvent, byte modeFT710, int modeFT891) : base(routedEvent)
        {
            SelectedModeFT710 = modeFT710;
            SelectedModeFT891 = modeFT891;
        }
    }

    public delegate void ModeChangedEventHandler(object sender, ModeChangedEventArgs e);

    public partial class ModeUserControl : UserControl
    {
        public struct ModesFT710
        {
            public const byte LSB = 0;
            public const byte USB = 1;
            public const byte CW_L = 2;
            public const byte CW_U = 3;
            public const byte AM = 4;
            public const byte AM_N = 5;
            public const byte FM = 6;
            public const byte FM_N = 7;
            public const byte DATA_L = 8;
            public const byte DATA_U = 9;
            public const byte DATA_FM = 10;
            public const byte D_FM_N = 11;
            public const byte RTTY_L = 12;
            public const byte RTTY_U = 13;
            public const byte PSK = 14;
            public const byte PRESET = 15;
        }

        // --- ROUTED EVENT REGISTRATION ---
        public static readonly RoutedEvent ModeChangedEvent = EventManager.RegisterRoutedEvent(
            "ModeChanged", RoutingStrategy.Bubble, typeof(ModeChangedEventHandler), typeof(ModeUserControl));

        public event ModeChangedEventHandler ModeChanged
        {
            add => AddHandler(ModeChangedEvent, value);
            remove => RemoveHandler(ModeChangedEvent, value);
        }
        // ----------------------------------

        public static byte currentModeFT710 = ModesFT710.USB;
        public static int currentModeFT891 = (int)RadioMode.USB;

        public ModeUserControl()
        {
            InitializeComponent();

            USBBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
            USBTextBlock.Foreground = new SolidColorBrush(Colors.White);
        }

        private void ModeWindowCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var code = border.Tag.ToString();
            uint switchValue = uint.Parse(code);

            ChangeMode(Convert.ToByte(switchValue));
        }

        public void ChangeMode(byte mode)
        {
            LSBBorder.Background = new SolidColorBrush(Colors.LightGray);
            LSBTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            USBBorder.Background = new SolidColorBrush(Colors.LightGray);
            USBTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            CW_LBorder.Background = new SolidColorBrush(Colors.LightGray);
            CW_LTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            CW_UBorder.Background = new SolidColorBrush(Colors.LightGray);
            CW_UTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            AMBorder.Background = new SolidColorBrush(Colors.LightGray);
            AMTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            AM_NBorder.Background = new SolidColorBrush(Colors.LightGray);
            AM_NTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            FMBorder.Background = new SolidColorBrush(Colors.LightGray);
            FMTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            FM_NBorder.Background = new SolidColorBrush(Colors.LightGray);
            FM_NTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            DATA_LBorder.Background = new SolidColorBrush(Colors.LightGray);
            DATA_LTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            DATA_UBorder.Background = new SolidColorBrush(Colors.LightGray);
            DATA_UTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            DATA_FMBorder.Background = new SolidColorBrush(Colors.LightGray);
            DATA_FMTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            D_FM_NBorder.Background = new SolidColorBrush(Colors.LightGray);
            D_FM_NTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            RTTY_LBorder.Background = new SolidColorBrush(Colors.LightGray);
            RTTY_LTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            RTTY_UBorder.Background = new SolidColorBrush(Colors.LightGray);
            RTTY_UTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            PSKBorder.Background = new SolidColorBrush(Colors.LightGray);
            PSKTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            PRESETBorder.Background = new SolidColorBrush(Colors.Gray);
            //PRESETTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            switch (mode)
            {
                case ModesFT710.LSB:
                    currentModeFT891 = (int)RadioMode.LSB;
                    LSBBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    LSBTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    break;
                case ModesFT710.USB:
                    currentModeFT891 = (int)RadioMode.USB;
                    USBBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    USBTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    break;
                case ModesFT710.CW_L:
                    currentModeFT891 = (int)RadioMode.CW;
                    CW_LBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    CW_LTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    break;
                case ModesFT710.CW_U:
                    currentModeFT891 = (int)RadioMode.CW_R;
                    CW_UBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    CW_UTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    break;
                case ModesFT710.AM:
                    currentModeFT891 = (int)RadioMode.AM;
                    AMBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    AMTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    break;
                case ModesFT710.AM_N:
                    currentModeFT891 = (int)RadioMode.AM_N;
                    AM_NBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    AM_NTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    break;
                case ModesFT710.FM:
                    currentModeFT891 = (int)RadioMode.FM;
                    FMBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    FMTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    break;
                case ModesFT710.FM_N:
                    currentModeFT891 = (int)RadioMode.FM_N;
                    FM_NBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    FM_NTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    break;
                case ModesFT710.DATA_L:
                    currentModeFT891 = (int)RadioMode.DATA_LSB;
                    DATA_LBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    DATA_LTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    break;
                case ModesFT710.DATA_U:
                    currentModeFT891 = (int)RadioMode.DATA_USB;
                    DATA_UBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    DATA_UTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    break;
                case ModesFT710.DATA_FM:
                    currentModeFT891 = (int)RadioMode.DATA_FM;
                    DATA_FMBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    DATA_FMTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    break;
                case ModesFT710.D_FM_N:
                    currentModeFT891 = (int)RadioMode.DATA_FM;//no equiv
                    D_FM_NBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    D_FM_NTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    break;
                case ModesFT710.RTTY_L:
                    currentModeFT891 = (int)RadioMode.RTTY_LSB;
                    RTTY_LBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    RTTY_LTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    break;
                case ModesFT710.RTTY_U:
                    currentModeFT891 = (int)RadioMode.RTTY_USB;
                    RTTY_UBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    RTTY_UTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    break;
                case ModesFT710.PSK:
                    currentModeFT891 = (int)RadioMode.DATA_FM;//no equiv
                    PSKBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    PSKTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    break;
                case ModesFT710.PRESET:
                    currentModeFT891 = (int)RadioMode.USB;//no equiv
                    PRESETBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    //PRESETTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    break;
                default:
                    currentModeFT891 = (int)RadioMode.USB;
                    USBBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    USBTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    break;
            }

            currentModeFT710 = mode;

            // --- RAISE EVENT UP TO MAINWINDOW ---
            RaiseEvent(new ModeChangedEventArgs(ModeChangedEvent, currentModeFT710, currentModeFT891));

            FadoutBorderWindow(ModeWindowBorder);
        }
    }
}