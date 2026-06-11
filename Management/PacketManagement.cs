using System;
using System.Windows;

namespace YAESU_FT_891_Front_End
{
    public class PacketManagement
    {
        private readonly MainWindow mainWindow;

        private DateTime lastSendFlashTime = DateTime.MinValue;
        private DateTime lastReceiveFlashTime = DateTime.MinValue;

        private long currentFPS;

        private double currentSendFrameCount;
        private double currentReceiveFrameCount;

        public string currentSendCATCommand = string.Empty;

        public PacketManagement(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        }

        public void UpdateSendFPS()
        {
            currentFPS++;
            currentSendFrameCount++;

            // Explicitly cast to (Action) to satisfy the .NET 4.8 compiler
            mainWindow.Dispatcher.Invoke((Action)(() =>
            {
                DateTime now = DateTime.Now;

                if (now > lastSendFlashTime + TimeSpan.FromSeconds(1))
                {
                    mainWindow.FPSTextBlock.Text = currentFPS.ToString();
                    currentFPS = 0;

                    mainWindow.SendRedLEDRectangle.Opacity = 0.5;
                    lastSendFlashTime = now;
                }
                else if (now > lastSendFlashTime + TimeSpan.FromMilliseconds(200))
                {
                    mainWindow.SendRedLEDRectangle.Opacity = 0.2;
                }

                UpdatePDR();
            }));
        }

        public void UpdateReceiveFPS(string incomingMessage)
        {
            if (CompareSentReceivedCATCommands(currentSendCATCommand, incomingMessage))
            {
                currentReceiveFrameCount++;
            }

            // Explicitly cast to (Action) to satisfy the .NET 4.8 compiler
            mainWindow.Dispatcher.Invoke((Action)(() =>
            {
                DateTime now = DateTime.Now;

                if (now > lastReceiveFlashTime + TimeSpan.FromSeconds(1))
                {
                    mainWindow.ReceiveGreenLEDRectangle.Opacity = 0.5;
                    lastReceiveFlashTime = now;
                }
                else if (now > lastReceiveFlashTime + TimeSpan.FromMilliseconds(200))
                {
                    mainWindow.ReceiveGreenLEDRectangle.Opacity = 0.2;
                }

                UpdatePDR();
            }));
        }

        public void UpdatePDR()
        {
            double finalPdr = CalculatePDR(currentSendFrameCount, currentReceiveFrameCount);
            mainWindow.PDRTextBlock.Text = finalPdr.ToString("F0") + "%";
        }

        private bool CompareSentReceivedCATCommands(string sent, string received)
        {
            if (string.IsNullOrEmpty(sent) || sent.Length < 3 ||
                string.IsNullOrEmpty(received) || received.Length < 3)
            {
                return false;
            }

            bool bothEndWithSemicolon = sent.EndsWith(";") && received.EndsWith(";");

            string sentPrefix = sent.Substring(0, 2);
            string receivedPrefix = received.Substring(0, 2);

            bool prefixesMatch = sentPrefix.Equals(receivedPrefix, StringComparison.OrdinalIgnoreCase);

            return bothEndWithSemicolon && prefixesMatch;
        }

        private double CalculatePDR(double sent, double received)
        {
            if (sent <= 0.0)
            {
                return 100.0;
            }

            double result = (received / sent) * 100.0;

            if (result > 100.0) return 100.0;
            if (result < 0.0) return 0.0;

            return result;
        }
    }
}