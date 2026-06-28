using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static YAESU_FT_891_Front_End.Animations;

namespace YAESU_FT_891_Front_End
{
    // Custom arguments to carry our band change data to the MainWindow
    public class BandChangedEventArgs : RoutedEventArgs
    {
        public byte SelectedBand { get; }
        public long SelectedFrequency { get; }

        public BandChangedEventArgs(RoutedEvent routedEvent, byte band, long frequency) : base(routedEvent)
        {
            SelectedBand = band;
            SelectedFrequency = frequency;
        }
    }

    public delegate void BandChangedEventHandler(object sender, BandChangedEventArgs e);

    public partial class BandUserControl : UserControl
    {
        public struct Bands
        {
            public const byte _1P8 = 0;
            public const byte _3P5 = 1;
            public const byte _5P0 = 2;
            public const byte _7P0 = 3;
            public const byte _10 = 4;
            public const byte _14 = 5;
            public const byte _18 = 6;
            public const byte _21 = 7;
            public const byte _24P5 = 8;
            public const byte _2829 = 9;
            public const byte _50 = 10;
            public const byte _70GEN = 11;
            public const byte _MW = 12;
        }

        // --- ROUTED EVENT REGISTRATION ---
        public static readonly RoutedEvent BandChangedEvent = EventManager.RegisterRoutedEvent(
            "BandChanged", RoutingStrategy.Bubble, typeof(BandChangedEventHandler), typeof(BandUserControl));

        public event BandChangedEventHandler BandChanged
        {
            add => AddHandler(BandChangedEvent, value);
            remove => RemoveHandler(BandChangedEvent, value);
        }
        // ----------------------------------

        public static byte currentBand = Bands._14;
        public static long currentBandFrequency = 14000000;

        public BandUserControl()
        {
            InitializeComponent();

            _14Border.Background = new SolidColorBrush(Colors.DodgerBlue);
            _14TextBlock.Foreground = new SolidColorBrush(Colors.White);
        }

        private void BandWindowCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var code = border.Tag.ToString();
            uint switchValue = uint.Parse(code);

            ChangeBand(Convert.ToByte(switchValue));
        }

        public void ChangeBand(byte band)
        {
            _1P8Border.Background = new SolidColorBrush(Colors.LightGray);
            _1P8TextBlock.Foreground = new SolidColorBrush(Colors.Black);

            _3P5Border.Background = new SolidColorBrush(Colors.LightGray);
            _3P5TextBlock.Foreground = new SolidColorBrush(Colors.Black);

            _5P0Border.Background = new SolidColorBrush(Colors.LightGray);
            _5P0TextBlock.Foreground = new SolidColorBrush(Colors.Black);

            _7P0Border.Background = new SolidColorBrush(Colors.LightGray);
            _7P0TextBlock.Foreground = new SolidColorBrush(Colors.Black);

            _10Border.Background = new SolidColorBrush(Colors.LightGray);
            _10TextBlock.Foreground = new SolidColorBrush(Colors.Black);

            _14Border.Background = new SolidColorBrush(Colors.LightGray);
            _14TextBlock.Foreground = new SolidColorBrush(Colors.Black);

            _18Border.Background = new SolidColorBrush(Colors.LightGray);
            _18TextBlock.Foreground = new SolidColorBrush(Colors.Black);

            _21Border.Background = new SolidColorBrush(Colors.LightGray);
            _21TextBlock.Foreground = new SolidColorBrush(Colors.Black);

            _24P5Border.Background = new SolidColorBrush(Colors.LightGray);
            _24P5TextBlock.Foreground = new SolidColorBrush(Colors.Black);

            _2829Border.Background = new SolidColorBrush(Colors.LightGray);
            _2829TextBlock.Foreground = new SolidColorBrush(Colors.Black);

            _50Border.Background = new SolidColorBrush(Colors.LightGray);
            _50TextBlock.Foreground = new SolidColorBrush(Colors.Black);

            _70GENBorder.Background = new SolidColorBrush(Colors.LightGray);
            _70GENTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            switch (band)
            {
                case Bands._1P8:
                    currentBandFrequency = 1800000;
                    _1P8Border.Background = new SolidColorBrush(Colors.DodgerBlue);
                    _1P8TextBlock.Foreground = new SolidColorBrush(Colors.White);
                    break;
                case Bands._3P5:
                    currentBandFrequency = 3500000;
                    _3P5Border.Background = new SolidColorBrush(Colors.DodgerBlue);
                    _3P5TextBlock.Foreground = new SolidColorBrush(Colors.White);
                    break;
                case Bands._5P0:
                    currentBandFrequency = 5000000;
                    _5P0Border.Background = new SolidColorBrush(Colors.DodgerBlue);
                    _5P0TextBlock.Foreground = new SolidColorBrush(Colors.White);
                    break;
                case Bands._7P0:
                    currentBandFrequency = 7000000;
                    _7P0Border.Background = new SolidColorBrush(Colors.DodgerBlue);
                    _7P0TextBlock.Foreground = new SolidColorBrush(Colors.White);
                    break;
                case Bands._10:
                    currentBandFrequency = 10000000;
                    _10Border.Background = new SolidColorBrush(Colors.DodgerBlue);
                    _10TextBlock.Foreground = new SolidColorBrush(Colors.White);
                    break;
                case Bands._14:
                    currentBandFrequency = 14000000;
                    _14Border.Background = new SolidColorBrush(Colors.DodgerBlue);
                    _14TextBlock.Foreground = new SolidColorBrush(Colors.White);
                    break;
                case Bands._18:
                    currentBandFrequency = 18000000;
                    _18Border.Background = new SolidColorBrush(Colors.DodgerBlue);
                    _18TextBlock.Foreground = new SolidColorBrush(Colors.White);
                    break;
                case Bands._21:
                    currentBandFrequency = 21000000;
                    _21Border.Background = new SolidColorBrush(Colors.DodgerBlue);
                    _21TextBlock.Foreground = new SolidColorBrush(Colors.White);
                    break;
                case Bands._24P5:
                    currentBandFrequency = 24500000;
                    _24P5Border.Background = new SolidColorBrush(Colors.DodgerBlue);
                    _24P5TextBlock.Foreground = new SolidColorBrush(Colors.White);
                    break;
                case Bands._2829:
                    currentBandFrequency = 28000000;
                    _2829Border.Background = new SolidColorBrush(Colors.DodgerBlue);
                    _2829TextBlock.Foreground = new SolidColorBrush(Colors.White);
                    break;
                case Bands._50:
                    currentBandFrequency = 50000000;
                    // Note: Fixed a minor bug here from original code where _3P5 was highlighted on band 50 selection
                    _50Border.Background = new SolidColorBrush(Colors.DodgerBlue);
                    _50TextBlock.Foreground = new SolidColorBrush(Colors.White);
                    break;
                case Bands._70GEN:
                    currentBandFrequency = 70000000;
                    _70GENBorder.Background = new SolidColorBrush(Colors.DodgerBlue);
                    _70GENTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    break;
                case Bands._MW:
                    break;
                default:
                    currentBandFrequency = 14000000;
                    _14Border.Background = new SolidColorBrush(Colors.DodgerBlue);
                    _14TextBlock.Foreground = new SolidColorBrush(Colors.White);
                    break;
            }

            currentBand = band;

            // --- RAISE EVENT UP TO MAINWINDOW ---
            RaiseEvent(new BandChangedEventArgs(BandChangedEvent, currentBand, currentBandFrequency));

            FadoutUserControl(this);
        }
    }
}