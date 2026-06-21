using HamRadioControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static HamRadioControls.AnalogMeter;

namespace YAESU_FT_891_Front_End
{
    /// <summary>
    /// Interaction logic for SevenSegmentDisplay.xaml
    /// </summary>
    public partial class SevenSegmentDisplay : UserControl
    {
        public enum HexDigits
        {
            Zero,
            One,
            Two,
            Three,
            Four,
            Five,
            Six,
            Seven,
            Eight,
            Nine,
            A,
            B,
            C,
            D,
            E,
            F,
            Off
        }
        public enum DotOffOn
        {
            Off,
            On
        }

        public static readonly DependencyProperty HexDigitProperty =
            DependencyProperty.Register(nameof(HexDigit), typeof(HexDigits), typeof(SevenSegmentDisplay),
                new PropertyMetadata(HexDigits.Eight, OnHexDigitChanged));

        public HexDigits HexDigit
        {
            get => (HexDigits)GetValue(HexDigitProperty);
            set => SetValue(HexDigitProperty, value);
        }


        public static readonly DependencyProperty DoOffOnProperty =
            DependencyProperty.Register(nameof(Dot), typeof(DotOffOn), typeof(SevenSegmentDisplay),
                new PropertyMetadata(DotOffOn.Off, OnDotOffOnChanged));

        public DotOffOn Dot
        {
            get => (DotOffOn)GetValue(DoOffOnProperty);
            set => SetValue(DoOffOnProperty, value);
        }


        Brush OnColor = new SolidColorBrush(Colors.White);
        Brush OffColor = new SolidColorBrush(MakeSemiTransparent(Colors.White, 16));
        
        public SevenSegmentDisplay()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                Dispatcher.BeginInvoke(new Action(ApplyHexDigit),
                    System.Windows.Threading.DispatcherPriority.Loaded);

                Dispatcher.BeginInvoke(new Action(ApplyDot),
                    System.Windows.Threading.DispatcherPriority.Loaded);
            };
        }


        private static void OnHexDigitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var hexDigit = (SevenSegmentDisplay)d;
            hexDigit.ApplyHexDigit();
        }
        private void ApplyHexDigit()
        {
            CreateDigit(HexDigit);
        }


        private static void OnDotOffOnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dot = (SevenSegmentDisplay)d;
            dot.ApplyDot();
        }
        private void ApplyDot()
        {
            CreateDot(Dot);
        }


        public static Color MakeSemiTransparent(Color baseColor, byte alpha)
        {
            baseColor.A = alpha;
            return baseColor;
        }

        private void CreateDigit(HexDigits hexDigit)
        {
            switch (hexDigit)
            {
                case HexDigits.Zero:
                    SegmentA.Fill = OnColor;
                    SegmentF.Fill = OnColor;
                    SegmentB.Fill = OnColor;
                    SegmentG.Fill = OffColor;
                    SegmentE.Fill = OnColor;
                    SegmentC.Fill = OnColor;
                    SegmentD.Fill = OnColor;
                    break;
                case HexDigits.One:
                    SegmentA.Fill = OffColor;
                    SegmentF.Fill = OffColor;
                    SegmentB.Fill = OnColor;
                    SegmentG.Fill = OffColor;
                    SegmentE.Fill = OffColor;
                    SegmentC.Fill = OnColor;
                    SegmentD.Fill = OffColor;
                    break;
                case HexDigits.Two:
                    SegmentA.Fill = OnColor;
                    SegmentF.Fill = OffColor;
                    SegmentB.Fill = OnColor;
                    SegmentG.Fill = OnColor;
                    SegmentE.Fill = OnColor;
                    SegmentC.Fill = OffColor;
                    SegmentD.Fill = OnColor;
                    break;
                case HexDigits.Three:
                    SegmentA.Fill = OnColor;
                    SegmentF.Fill = OffColor;
                    SegmentB.Fill = OnColor;
                    SegmentG.Fill = OnColor;
                    SegmentE.Fill = OffColor;
                    SegmentC.Fill = OnColor;
                    SegmentD.Fill = OnColor;
                    break;
                case HexDigits.Four:
                    SegmentA.Fill = OffColor;
                    SegmentF.Fill = OnColor;
                    SegmentB.Fill = OnColor;
                    SegmentG.Fill = OnColor;
                    SegmentE.Fill = OffColor;
                    SegmentC.Fill = OnColor;
                    SegmentD.Fill = OffColor;
                    break;
                case HexDigits.Five:
                    SegmentA.Fill = OnColor;
                    SegmentF.Fill = OnColor;
                    SegmentB.Fill = OffColor;
                    SegmentG.Fill = OnColor;
                    SegmentE.Fill = OffColor;
                    SegmentC.Fill = OnColor;
                    SegmentD.Fill = OnColor;
                    break;
                case HexDigits.Six:
                    SegmentA.Fill = OnColor;
                    SegmentF.Fill = OnColor;
                    SegmentB.Fill = OffColor;
                    SegmentG.Fill = OnColor;
                    SegmentE.Fill = OnColor;
                    SegmentC.Fill = OnColor;
                    SegmentD.Fill = OnColor;
                    break;
                case HexDigits.Seven:
                    SegmentA.Fill = OnColor;
                    SegmentF.Fill = OffColor;
                    SegmentB.Fill = OnColor;
                    SegmentG.Fill = OffColor;
                    SegmentE.Fill = OffColor;
                    SegmentC.Fill = OnColor;
                    SegmentD.Fill = OffColor;
                    break;
                case HexDigits.Eight:
                    SegmentA.Fill = OnColor;
                    SegmentF.Fill = OnColor;
                    SegmentB.Fill = OnColor;
                    SegmentG.Fill = OnColor;
                    SegmentE.Fill = OnColor;
                    SegmentC.Fill = OnColor;
                    SegmentD.Fill = OnColor;
                    break;
                case HexDigits.Nine:
                    SegmentA.Fill = OnColor;
                    SegmentF.Fill = OnColor;
                    SegmentB.Fill = OnColor;
                    SegmentG.Fill = OnColor;
                    SegmentE.Fill = OffColor;
                    SegmentC.Fill = OnColor;
                    SegmentD.Fill = OnColor;
                    break;
                case HexDigits.A: // A (Uppercase A)
                    SegmentA.Fill = OnColor;
                    SegmentF.Fill = OnColor;
                    SegmentB.Fill = OnColor;
                    SegmentG.Fill = OnColor;
                    SegmentE.Fill = OnColor;
                    SegmentC.Fill = OnColor;
                    SegmentD.Fill = OffColor;
                    break;
                case HexDigits.B: // B (Lowercase b)
                    SegmentA.Fill = OffColor;
                    SegmentF.Fill = OnColor;
                    SegmentB.Fill = OffColor;
                    SegmentG.Fill = OnColor;
                    SegmentE.Fill = OnColor;
                    SegmentC.Fill = OnColor;
                    SegmentD.Fill = OnColor;
                    break;
                case HexDigits.C: // C (Uppercase C)
                    SegmentA.Fill = OnColor;
                    SegmentF.Fill = OnColor;
                    SegmentB.Fill = OffColor;
                    SegmentG.Fill = OffColor;
                    SegmentE.Fill = OnColor;
                    SegmentC.Fill = OffColor;
                    SegmentD.Fill = OnColor;
                    break;
                case HexDigits.D: // D (Lowercase d)
                    SegmentA.Fill = OffColor;
                    SegmentF.Fill = OffColor;
                    SegmentB.Fill = OnColor;
                    SegmentG.Fill = OnColor;
                    SegmentE.Fill = OnColor;
                    SegmentC.Fill = OnColor;
                    SegmentD.Fill = OnColor;
                    break;
                case HexDigits.E: // E (Uppercase E)
                    SegmentA.Fill = OnColor;
                    SegmentF.Fill = OnColor;
                    SegmentB.Fill = OffColor;
                    SegmentG.Fill = OnColor;
                    SegmentE.Fill = OnColor;
                    SegmentC.Fill = OffColor;
                    SegmentD.Fill = OnColor;
                    break;
                case HexDigits.F: // F (Uppercase F)
                    SegmentA.Fill = OnColor;
                    SegmentF.Fill = OnColor;
                    SegmentB.Fill = OffColor;
                    SegmentG.Fill = OnColor;
                    SegmentE.Fill = OnColor;
                    SegmentC.Fill = OffColor;
                    SegmentD.Fill = OffColor;
                    break;
                case HexDigits.Off:
                default:
                    SegmentA.Fill = OffColor;
                    SegmentF.Fill = OffColor;
                    SegmentB.Fill = OffColor;
                    SegmentG.Fill = OffColor;
                    SegmentE.Fill = OffColor;
                    SegmentC.Fill = OffColor;
                    SegmentD.Fill = OffColor;
                    break;
            }
        }

        private void CreateDot(DotOffOn dotState)
        {
            switch (dotState)
            {
                case DotOffOn.Off:
                    SegmentDot.Fill = OffColor;
                    break;
                case DotOffOn.On:
                    SegmentDot.Fill = OnColor;
                    break;
                default:
                    SegmentDot.Fill = OffColor;
                    break;
            }
        }

    }
}
