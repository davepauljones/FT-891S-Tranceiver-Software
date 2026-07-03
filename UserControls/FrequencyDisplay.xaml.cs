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

        // 1b. Define the Dependency Property for the editing state (visible to MainWindow)
        public static readonly DependencyProperty IsDigitEditingProperty =
            DependencyProperty.Register(
                nameof(IsDigitEditing),
                typeof(bool),
                typeof(FrequencyDisplay),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public bool IsDigitEditing
        {
            get => (bool)GetValue(IsDigitEditingProperty);
            set => SetValue(IsDigitEditingProperty, value);
        }
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

            // Also grab a descriptor for the new IsEditing property
            var editDpd = DependencyPropertyDescriptor.FromProperty(SevenSegmentDisplay.IsEditingProperty, typeof(SevenSegmentDisplay));

            SevenSegmentDisplay[] digits = { Digit_1, Digit_2, Digit_3, Digit_4, Digit_5, Digit_6, Digit_7, Digit_8 };

            foreach (var digit in digits)
            {
                if (digit != null)
                {
                    dpd.AddValueChanged(digit, OnChildDigitChanged);

                    // Added: Track when a digit enters or leaves its scroll-wheel edit state
                    editDpd.AddValueChanged(digit, OnChildEditStateChanged);
                }
            }

            // Push the initial frequency value down to the display
            UpdateDisplayFromFrequency();
        }

        // Automatically runs whenever any child digit turns its scroll wheel on or off
        private void OnChildEditStateChanged(object sender, EventArgs e)
        {
            IsDigitEditing = (Digit_1 != null && Digit_1.IsEditing) ||
                             (Digit_2 != null && Digit_2.IsEditing) ||
                             (Digit_3 != null && Digit_3.IsEditing) ||
                             (Digit_4 != null && Digit_4.IsEditing) ||
                             (Digit_5 != null && Digit_5.IsEditing) ||
                             (Digit_6 != null && Digit_6.IsEditing) ||
                             (Digit_7 != null && Digit_7.IsEditing) ||
                             (Digit_8 != null && Digit_8.IsEditing);
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
            // Changed: If _isUpdating OR a digit is being actively edited, return immediately!
            if (_isUpdating || IsDigitEditing) return;
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
