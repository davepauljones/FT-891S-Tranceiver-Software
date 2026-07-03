using HamRadioControls;
using System;
using System.Collections.Generic;
using System.Drawing;
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
using static System.Resources.ResXFileRef;

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

        // New property to notify the parent display when this specific digit is being edited
        public static readonly DependencyProperty IsEditingProperty =
            DependencyProperty.Register(nameof(IsEditing), typeof(bool), typeof(SevenSegmentDisplay),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public bool IsEditing
        {
            get => (bool)GetValue(IsEditingProperty);
            set => SetValue(IsEditingProperty, value);
        }

        public static readonly DependencyProperty HexDigitProperty =
         DependencyProperty.Register(
             nameof(HexDigit),
             typeof(HexDigits),
             typeof(SevenSegmentDisplay),
             new FrameworkPropertyMetadata(
                 HexDigits.Eight,
                 FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, // <-- Add this flags option
                 OnHexDigitChanged
             )
         );

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


        System.Windows.Media.Brush OnColor = new SolidColorBrush(Colors.White);
        System.Windows.Media.Brush OffColor = new SolidColorBrush(MakeSemiTransparent(System.Windows.Media.Colors.White, 16));
        
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


        public static System.Windows.Media.Color MakeSemiTransparent(System.Windows.Media.Color baseColor, byte alpha)
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

        bool ScrollWheelEnabled = false;
        private void SevenSegmentUserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("Mouse left button was clicked");

            if (!(ScrollWheelEnabled))
            {
                this.Background = new SolidColorBrush(Colors.Orange);
                ScrollWheelEnabled = true;
                IsEditing = true; // <-- Added: We are editing now!
            }
        }

        private void SevenSegmentUserControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Console.WriteLine("Mouse wheel is scrolling over the senven segment display");

            if (ScrollWheelEnabled)
            {
                double delta = (e.Delta > 0 ? 1 : -1) * 6.0;

                if (delta > 0)
                {
                    if ((int)HexDigit < 9)
                        HexDigit++;
                    else
                        HexDigit = 0;
                }
                else if (delta < 0)
                {
                    if (HexDigit > 0)
                        HexDigit--;
                    else
                        HexDigit = (HexDigits)9;
                }

                // --- CRITICAL STEP: Push the update directly up to the parent container binding ---
                var bindingExpression = this.GetBindingExpression(HexDigitProperty);
                bindingExpression?.UpdateSource();
            }
        }

        private void SevenSegmentUserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            System.Windows.Media.Color color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#AA444444");
            this.Background = new SolidColorBrush(color);
            ScrollWheelEnabled = false;
            IsEditing = false; // <-- Added: Done editing!
        }
    }
}
