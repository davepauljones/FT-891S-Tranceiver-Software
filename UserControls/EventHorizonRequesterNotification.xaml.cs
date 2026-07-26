using Microsoft.Win32;
using System;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YAESU_FT_891_Front_End;

namespace Event_Horizon
{
    /// <summary>
    /// Interaction logic for EventHorizonRequesterNotification.xaml
    /// </summary>

    public class OracleCustomMessage
    {
        public String MessageTitleTextBlock = string.Empty;
        public String InformationTextBlock = string.Empty;
        public String InputDefaultText = string.Empty;
    }
    public struct RequesterTypes
    {
        public const int NoYes = 0;
        public const int OK = 1;
        public const int Input = 2;
    }
    public partial class EventHorizonRequesterNotification : Window
    {
        MainWindow mw;
        OracleCustomMessage oracleCustomMessage;
        int requesterType;
        bool overrideNotificationSound;

        public string InputResult { get; private set; } = string.Empty;

        public EventHorizonRequesterNotification(MainWindow mw, OracleCustomMessage oracleCustomMessage, int requesterType, bool overrideNotificationSound = true)
        {
            InitializeComponent();
            this.Hide();

            this.mw = mw;
            this.oracleCustomMessage = oracleCustomMessage;
            this.requesterType = requesterType;
            this.overrideNotificationSound = overrideNotificationSound;

            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

            SetRequester();
        }

        private void SetRequester()
        {
            MessageTitleTextBlock.Text = oracleCustomMessage.MessageTitleTextBlock;
            InformationTextBlock.Text = oracleCustomMessage.InformationTextBlock;

            switch (requesterType)
            {
                case RequesterTypes.NoYes:
                    NoButton.Content = "No";
                    YesButton.Content = "Yes";
                    InputTextBox.Visibility = Visibility.Collapsed;
                    break;
                case RequesterTypes.OK:
                    NoButton.Content = "";
                    NoButton.Visibility = Visibility.Hidden;
                    YesButton.Content = "Ok";
                    InputTextBox.Visibility = Visibility.Collapsed;
                    break;
                case RequesterTypes.Input:
                    NoButton.Content = "Cancel";
                    NoButton.Visibility = Visibility.Visible;
                    YesButton.Content = "Enter";
                    InputTextBox.Text = oracleCustomMessage.InputDefaultText;
                    InputTextBox.Visibility = Visibility.Visible;
                    InputTextBox.Focus();
                    break;
            }

            if (!overrideNotificationSound) PlayNotificationSound();
        }

        private void PlayNotificationSound()
        {
            bool found = false;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"AppEvents\Schemes\Apps\.Default\Notification.Default\.Current"))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue(null); // pass null to get (Default)
                        if (o != null)
                        {
                            SoundPlayer theSound = new SoundPlayer((String)o);
                            theSound.Play();
                            found = true;
                        }
                    }
                }
            }
            catch
            { }
            if (!found)
                SystemSounds.Beep.Play(); // consolation prize
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void TreeView_ButtonClick(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            int buttonID = 255;

            bool success = Int32.TryParse(button.Tag.ToString(), out buttonID);

            if (button != null && success)
            {
                switch (buttonID)
                {
                    case 0:
                        DialogResult = false;
                        Close();
                        break;
                    case 1:
                        if (requesterType == RequesterTypes.Input)
                        {
                            InputResult = InputTextBox.Text;
                        }
                        DialogResult = true;
                        Close();
                        break;
                }
            }
        }
    }
}