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
using static YAESU_FT_891_Front_End.SevenSegmentDisplay;

namespace YAESU_FT_891_Front_End
{
    /// <summary>
    /// Interaction logic for FrequencyDisplay.xaml
    /// </summary>
    public partial class FrequencyDisplay : UserControl
    {
        public static readonly DependencyProperty FrequencyProperty =
            DependencyProperty.Register(nameof(Frequency), typeof(long), typeof(FrequencyDisplay),
                new PropertyMetadata(14242500L, OnFrequencyChanged));

        public long Frequency
        {
            get => (long)GetValue(FrequencyProperty);
            set => SetValue(FrequencyProperty, value);
        }

        public FrequencyDisplay()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                Dispatcher.BeginInvoke(new Action(ApplyFrequency),
                    System.Windows.Threading.DispatcherPriority.Loaded);
            };
        }

        private static void OnFrequencyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var frequency = (FrequencyDisplay)d;
            frequency.ApplyFrequency();
        }
        private void ApplyFrequency()
        {
            CreateDigits(Frequency);
        }

        private void CreateDigits(long frequency)
        {
            Digit_8.HexDigit = (HexDigits)(frequency % 10); frequency /= 10;
            Digit_7.HexDigit = (HexDigits)(frequency % 10); frequency /= 10;
            Digit_6.HexDigit = (HexDigits)(frequency % 10); frequency /= 10;
            Digit_5.HexDigit = (HexDigits)(frequency % 10); frequency /= 10;
            Digit_4.HexDigit = (HexDigits)(frequency % 10); frequency /= 10;
            Digit_3.HexDigit = (HexDigits)(frequency % 10); frequency /= 10;
            Digit_2.HexDigit = (HexDigits)(frequency % 10); frequency /= 10;
            Digit_1.HexDigit = (HexDigits)(frequency % 10);
        }
    }
}
