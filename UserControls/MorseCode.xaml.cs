using CWDecoder;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using static YAESU_FT_891_Front_End.MyStructs;

namespace YAESU_FT_891_Front_End
{
    /// <summary>
    /// Interaction logic for MorseCode.xaml
    /// </summary>
    public partial class MorseCode : UserControl
    {
        public CWDecoderEngine decoder;

        private readonly DispatcherTimer uiTelemetryTimer;
        private double latestPower;
        private bool hasPendingPower;
        private CWDecoderDiagnostic latestDiagnostic;
        private bool hasPendingDiagnostic;

        public MorseCode()
        {
            InitializeComponent();

            uiTelemetryTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(60)
            };
            uiTelemetryTimer.Tick += UiTelemetryTimer_Tick;
            uiTelemetryTimer.Start();

            Init_MorseCode();
        }

        private void Init_MorseCode()
        {
            decoder = new CWDecoderEngine();

            decoder.TextDecoded += Decoder_TextDecoded;
            decoder.SignalPowerUpdated += Decoder_SignalPowerUpdated;
            decoder.DiagnosticUpdated += Decoder_DiagnosticUpdated;
        }

        private void Decoder_TextDecoded(object sender, string text)
        {
            // If you don't do this, WPF will intermittently lock up or crash
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                CWDecoderTextBlock.Text += text;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    CWDecoderScrollViewer.ScrollToEnd();
                }), DispatcherPriority.Render);
            }));
        }

        private void Decoder_SignalPowerUpdated(object sender, double power)
        {
            latestPower = power;
            Volatile.Write(ref hasPendingPower, true);
        }

        private void Decoder_DiagnosticUpdated(object sender, CWDecoderDiagnostic diag)
        {
            latestDiagnostic = diag;
            Volatile.Write(ref hasPendingDiagnostic, true);
        }

        private void UiTelemetryTimer_Tick(object sender, EventArgs e)
        {
            if (Volatile.Read(ref hasPendingPower))
            {
                Volatile.Write(ref hasPendingPower, false);
                CWDecoderPowerTextBlock.Text = $"{latestPower:F5}";
            }

            if (Volatile.Read(ref hasPendingDiagnostic))
            {
                Volatile.Write(ref hasPendingDiagnostic, false);
                CWDecoderDiagnostic diag = latestDiagnostic;
                ToneStateTextBlock.Text = diag.ToneState ? "1" : "0";
                DotLengthTextBlock.Text = $"{diag.DotLength:F0}";
                DurationTextBlock.Text = $"{diag.DurationMs}ms";
                SymbolTextBlock.Text = string.IsNullOrEmpty(diag.Symbol) ? "-" : diag.Symbol;
                LastDecodedTextBlock.Text = string.IsNullOrEmpty(diag.LastDecoded)
                    ? "-"
                    : (diag.LastDecoded == " " ? "[SP]" : diag.LastDecoded);
                StageTextBlock.Text = diag.Stage;
            }
        }

        private void MorseCodeButtons_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var code = border.Tag.ToString();
            uint switchValue = uint.Parse(code);

            ButtonSelection(Convert.ToByte(switchValue));
        }

        public void ButtonSelection(byte buttonClicked)
        {
            //if (buttonClicked != ScanBands._START && buttonClicked != ScanBands._STOP)
            //{
            //    if (IsScanning) return;
            //    mainWindow._160MATextBlock.Foreground = new SolidColorBrush(Colors.White);
            //    mainWindow._80MATextBlock.Foreground = new SolidColorBrush(Colors.White);
            //    mainWindow._40MATextBlock.Foreground = new SolidColorBrush(Colors.White);
            //    mainWindow._30MATextBlock.Foreground = new SolidColorBrush(Colors.White);
            //    mainWindow._20MATextBlock.Foreground = new SolidColorBrush(Colors.White);
            //    mainWindow._15MATextBlock.Foreground = new SolidColorBrush(Colors.White);
            //    mainWindow._12MATextBlock.Foreground = new SolidColorBrush(Colors.White);
            //    mainWindow._11MATextBlock.Foreground = new SolidColorBrush(Colors.White);
            //    mainWindow._10MATextBlock.Foreground = new SolidColorBrush(Colors.White);
            //    mainWindow._6MATextBlock.Foreground = new SolidColorBrush(Colors.White);
            //}

            //switch (buttonClicked)
            //{
            //    case ScanBands._160M:
            //}

            if (decoder.IsRunning)
            {
                decoder.Stop();
            }
            else
            {
                CWDecoderTextBlock.Text = string.Empty;
                decoder.Start();
            }
        }
    }
}