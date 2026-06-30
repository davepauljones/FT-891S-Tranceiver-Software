using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace YAESU_FT_891_Front_End
{
    public partial class FrequencyDisplay : UserControl
    {
        // 1. Define the Dependency Property for the total multi-digit frequency (in Hz)
        public static readonly DependencyProperty FrequencyProperty =
            DependencyProperty.Register(
                nameof(Frequency),
                typeof(long),
                typeof(FrequencyDisplay),
                new FrameworkPropertyMetadata(14242500L, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnFrequencyChanged));

        public long Frequency
        {
            get => (long)GetValue(FrequencyProperty);
            set => SetValue(FrequencyProperty, value);
        }

        private bool _isUpdating = false;

        public FrequencyDisplay()
        {
            InitializeComponent();

            // Wait until components are initialized to wire up the events
            this.Loaded += FrequencyDisplay_Loaded;
        }

        private void FrequencyDisplay_Loaded(object sender, RoutedEventArgs e)
        {
            // 2. Hook into property change notifications for each individual digit
            var dpd = DependencyPropertyDescriptor.FromProperty(SevenSegmentDisplay.HexDigitProperty, typeof(SevenSegmentDisplay));

            SevenSegmentDisplay[] digits = { Digit_1, Digit_2, Digit_3, Digit_4, Digit_5, Digit_6, Digit_7, Digit_8 };

            foreach (var digit in digits)
            {
                if (digit != null)
                {
                    dpd.AddValueChanged(digit, OnChildDigitChanged);
                }
            }

            // Push the initial frequency value down to the display
            UpdateDisplayFromFrequency();
        }

        // Triggered when someone changes the overarching "Frequency" property externally
        private static void OnFrequencyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FrequencyDisplay)d;
            control.UpdateDisplayFromFrequency();
        }

        // Pushes the 'Frequency' long value out into the individual UI segments
        private void UpdateDisplayFromFrequency()
        {
            if (_isUpdating) return;
            _isUpdating = true;

            try
            {
                // Pad left to 8 digits (e.g., 14242500 -> "14242500")
                string freqStr = Frequency.ToString().PadLeft(8, '0');

                if (freqStr.Length > 8)
                    freqStr = freqStr.Substring(freqStr.Length - 8); // clamp to max 8 digits

                Digit_1.HexDigit = (SevenSegmentDisplay.HexDigits)char.GetNumericValue(freqStr[0]);
                Digit_2.HexDigit = (SevenSegmentDisplay.HexDigits)char.GetNumericValue(freqStr[1]);
                Digit_3.HexDigit = (SevenSegmentDisplay.HexDigits)char.GetNumericValue(freqStr[2]);
                Digit_4.HexDigit = (SevenSegmentDisplay.HexDigits)char.GetNumericValue(freqStr[3]);
                Digit_5.HexDigit = (SevenSegmentDisplay.HexDigits)char.GetNumericValue(freqStr[4]);
                Digit_6.HexDigit = (SevenSegmentDisplay.HexDigits)char.GetNumericValue(freqStr[5]);
                Digit_7.HexDigit = (SevenSegmentDisplay.HexDigits)char.GetNumericValue(freqStr[6]);
                Digit_8.HexDigit = (SevenSegmentDisplay.HexDigits)char.GetNumericValue(freqStr[7]);
            }
            catch
            {
                // Fallback catch if parsing fails
            }
            finally
            {
                _isUpdating = false;
            }
        }

        // 3. Triggered when the user clicks/scrolls a child digit UI component manually
        private void OnChildDigitChanged(object sender, EventArgs e)
        {
            if (_isUpdating) return;
            _isUpdating = true;

            try
            {
                // Reconstruct the frequency string based on the current states of the UI elements
                string freqStr =
                    ((int)Digit_1.HexDigit).ToString() +
                    ((int)Digit_2.HexDigit).ToString() +
                    ((int)Digit_3.HexDigit).ToString() +
                    ((int)Digit_4.HexDigit).ToString() +
                    ((int)Digit_5.HexDigit).ToString() +
                    ((int)Digit_6.HexDigit).ToString() +
                    ((int)Digit_7.HexDigit).ToString() +
                    ((int)Digit_8.HexDigit).ToString();

                if (long.TryParse(freqStr, out long calculatedFrequency))
                {
                    // Update the master dependency property, which bubbles out to your ViewModel/Main Application
                    Frequency = calculatedFrequency;
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }
    }
}
