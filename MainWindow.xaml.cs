using CWDecoder;
using FT891S_CatControl;
using HamRadioControls;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO.Ports;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using static YAESU_FT_891_Front_End.FT891S_CatCommand;
using static YAESU_FT_891_Front_End.MyStructs;
using static YAESU_FT_891_Front_End.RigState;
using static YAESU_FT_891_Front_End.RigStateChanges;
using static YAESU_FT_891_Front_End.SimulatedWaterfall;
using static YAESU_FT_891_Front_End.TranceiverDisplayModes;
using static YAESU_FT_891_Front_End.YAESU_FT_891_CAT_Dictionary;

namespace YAESU_FT_891_Front_End
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        double angle = 0;          // current angle
        double radius = 90;        // distance from center

        double centerX;
        double centerY;

        public static bool isDragging = false;
        Point lastMousePos;

        double tuningStep = 10; // sensitivity (Hz per pixel)

        public FT891S_SerialPort fT891S_SerialPort;
        public FT891S_CatCommands fT891S_CatCommands;

        public YAESU_FT_891_CAT_Dictionary yAESU_FT_891_CAT_Dictionary;
        public FrequencyManagement frequencyManagement;

        public int TranceiverTXRXState = TranceiverStates.RadioTXOff;

        public bool EnableDrag = false;

        public byte ConsoleDebugLevel = ConsoleDebugLevels.CurrentDebug;
        public byte DevelopmentStatus = Development.InProgress;

        public static RigState currentRigState;
        public MemorySlot memorySlot;

        public QMBRigStates qMBRigStates;

        public StationSeek stationSeek;

        private CWDecoderEngine decoder;
        public SimulatedWaterfall simulatedWaterfall;

        public Sprite sprite;
        public WaterFallSweep waterFallSweep;

        public FT891S_CatManager _catManager;

        public MainWindow()
        {
            InitializeComponent();

            EnableDrag = true;
            this.AllowsTransparency = true; 
        }


        void Init_Startup()
        {
            fT891S_SerialPort = new FT891S_SerialPort(this, "COM8");

            fT891S_CatCommands = new FT891S_CatCommands(this);


            //fT891S_CatCommands.FT891S_DoCatCommand(FT891S_CatCommandTypes.FA, YaesuCatCommandReadWriteStatus.ReadOnly, CatCommandCallback);

            //fT891S_CatCommands.FT891S_DoCatCommand(FT891S_CatCommandTypes.FA, YaesuCatCommandReadWriteStatus.WriteOnly, CatCommandCallback);

            
            yAESU_FT_891_CAT_Dictionary = new YAESU_FT_891_CAT_Dictionary(this);

            memorySlot = new MemorySlot(this);

            qMBRigStates = new QMBRigStates(this);

            stationSeek = new StationSeek(this);

            frequencyManagement = new FrequencyManagement(this);

            

            centerX = Canvas.GetLeft(FingerIndentImage);
            centerY = Canvas.GetTop(FingerIndentImage);

            //frequencyManagement.GetFrequency(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, MainFrequencyTextBlock);
            //frequencyManagement.GetFrequency(MemorySlot.MemorySlots.VFO_B, FrequencyLocations.RXFrequencyHz, SubFrequencyTextBlock);

            FastNormalGrid.Visibility = Visibility.Hidden;

            //yAESU_FT_891_CAT_Dictionary.FreqA(fT891S_SerialPort._port, 0);

            //SetRfGain(_port, 10);

            _blurTimer = new DispatcherTimer();
            _blurTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            _blurTimer.Tick += BlurTimer_Tick;
            _blurTimer.Start();

            _blurTimer2 = new DispatcherTimer();
            _blurTimer2.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            _blurTimer2.Tick += BlurTimer2_Tick;
            _blurTimer2.Start();

            if (ConsoleDebugLevel == ConsoleDebugLevels.All)
            {

            }

            FunctionMenuClass functionMenu = new FunctionMenuClass(FunctionMenuGrid, FunctionModeLabel);

            

            currentRigState = new RigState();
            currentRigState.TXPowerWatts = 5; //default rf power 5 watts for safety
            RfPowerFunctionTextBlock.Text = currentRigState.TXPowerWatts.ToString() + "W";


            decoder = new CWDecoderEngine();

            decoder.TextDecoded += Decoder_TextDecoded;
            decoder.SignalPowerUpdated += Decoder_SignalPowerUpdated;

            simulatedWaterfall = new SimulatedWaterfall(this, frequencyManagement);

            fT891S_SerialPort.OpenPort("COM8");

            //fT891S_SerialPort.StartSerialLoop();

            sprite = new Sprite(this, WaterfallCanvas);

            //sprite.GenerateSprite(128, 295, 0, 46, 6);

            waterFallSweep = new WaterFallSweep(this, SweepYellowCursorCanvas);

            // Instantiate the manager and pass your WPF window UI context
            _catManager = new FT891S_CatManager(this, this.Dispatcher);

            _catManager.StartOutgoingDataLoop();
        }

        private void CatCommandCallback()
        {
            if (ConsoleDebugLevel == ConsoleDebugLevels.All)
            {
                Console.WriteLine("CatCommandCallback OK");
            }
        }

        private void Decoder_TextDecoded(object sender, string text)
        {
            // If you don't do this, WPF will intermittently lock up or crash
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                CWDecoderTextBlock.Text += text;
            }));
        }

        private void Decoder_SignalPowerUpdated(object sender, double power)
        {
            // If you don't do this, WPF will intermittently lock up or crash
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                CWDecoderPowerTextBlock.Text = $"Power: {power:F5}";
            }));
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (decoder != null)
            {
                decoder.Stop();
                decoder.Dispose();
            }

            fT891S_SerialPort.StopSerialLoop();
            _catManager.StopOutgoingDataLoop();
        }
        
        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Init_Startup();

            this.Top = this.Top - 190;
            this.Left = this.Left - 120;
        }

        public void SetRigLEDColor(byte ledRigColor)
        {
            switch (ledRigColor)
            {
                case RigLEDColors.LightGray:
                    //ClearWindowRectangle.Fill = new SolidColorBrush(Colors.LightGray);
                    ClearWindowRectangleLinearGradientBrushGradientStop1.Color = Colors.LightGray;
                    ClearWindowRectangleLinearGradientBrushGradientStop2.Color = Colors.LightGray;
                    ClearWindowRectangleLinearGradientBrushGradientStop3.Color = Colors.LightGray;
                    ClearWindowDropShadowEffect.Color = Colors.LightGray;
                    break;
                case RigLEDColors.Green:
                    //ClearWindowRectangle.Fill = new SolidColorBrush(Colors.Green);
                    ClearWindowRectangleLinearGradientBrushGradientStop1.Color = Colors.LightGreen;
                    ClearWindowRectangleLinearGradientBrushGradientStop2.Color = Colors.Green;
                    ClearWindowRectangleLinearGradientBrushGradientStop3.Color = Colors.Green;
                    ClearWindowDropShadowEffect.Color = Colors.Green;
                    break;
                case RigLEDColors.Red:
                    //ClearWindowRectangle.Fill = new SolidColorBrush(Colors.Red);
                    ClearWindowRectangleLinearGradientBrushGradientStop1.Color = Colors.IndianRed;
                    ClearWindowRectangleLinearGradientBrushGradientStop2.Color = Colors.Red;
                    ClearWindowRectangleLinearGradientBrushGradientStop3.Color = Colors.Red;
                    ClearWindowDropShadowEffect.Color = Colors.Red;
                    break;
                case RigLEDColors.Blue:
                    //ClearWindowRectangle.Fill = new SolidColorBrush(Colors.Blue);
                    ClearWindowRectangleLinearGradientBrushGradientStop1.Color = Colors.LightBlue;
                    ClearWindowRectangleLinearGradientBrushGradientStop2.Color = Colors.Blue;
                    ClearWindowRectangleLinearGradientBrushGradientStop3.Color = Colors.LightBlue;
                    ClearWindowDropShadowEffect.Color = Colors.Blue;
                    break;
                default:
                    //ClearWindowRectangle.Fill = new SolidColorBrush(Colors.LightGray);
                    ClearWindowRectangleLinearGradientBrushGradientStop1.Color = Colors.LightGray;
                    ClearWindowRectangleLinearGradientBrushGradientStop2.Color = Colors.LightGray;
                    ClearWindowRectangleLinearGradientBrushGradientStop3.Color = Colors.LightGray;
                    ClearWindowDropShadowEffect.Color = Colors.LightGray;
                    break;
            }
        }

        public void UpdateMeter(Rectangle rectangle, int catValue)
        {
            catValue = Math.Max(0, Math.Min(195, catValue));
            rectangle.Width = (catValue / 195.0) * 140.0;

            if (ConsoleDebugLevel == ConsoleDebugLevels.All)
            {
                Console.Write("width = ");
                Console.WriteLine(rectangle.Width);
            }
        }

        public void SpinDial(byte direction)
        {
            switch (direction)
            {
                case MyStructs.DialDirection.None:
                    break;

                case MyStructs.DialDirection.Clockwise:
                    angle += 0.05; // adjust speed
                    break;

                case MyStructs.DialDirection.AntiClockwise:
                    angle -= 0.05;
                    break;
            }

            // convert angle → position
            double x = centerX + radius * Math.Cos(angle);
            double y = centerY + radius * Math.Sin(angle);

            if (ConsoleDebugLevel == ConsoleDebugLevels.All)
            {
                Console.WriteLine($"X={x} Y={y} angle={angle}");
            }

            Canvas.SetLeft(FingerIndentImage, x - FingerIndentImage.ActualWidth / 2);
            Canvas.SetTop(FingerIndentImage, y - FingerIndentImage.ActualHeight / 2);
        }
       
        public static int GetSMeterInteger(int rawValue)
        {
            // Range 0 to 105 maps to S0 through S9
            if (rawValue <= 105)
            {
                // Dividng by 11.6 to spread 105 units across 9 S-levels
                return (int)Math.Round(rawValue / 11.6);
            }

            // Range 106 to 195 maps to 10dB through 60dB over
            // We subtract 105 to get the "overage", then scale it to a max of 60
            else
            {
                int dbOver = (int)Math.Round((rawValue - 105) / 1.5);

                // Clamp to 60 to prevent overflow from stray CAT bytes
                return Math.Min(60, dbOver);
            }
        }

        public void UpdateTranceiverTXRXState(int state)
        {
            if (state == TranceiverStates.RadioTXOn)
            {
                DefaultMeterLabel.Content = "COMP";
                TranceiverTXRXState = TranceiverStates.RadioTXOn;
                
                TXMetersCanvas.Visibility = Visibility.Visible;

                BarGraphSignalMetersCanvas.Visibility = Visibility.Visible;
                AnalogueSignalMeterViewbox.Visibility = Visibility.Hidden;

                SetRigLEDColor(RigLEDColors.Red);
            }
            else if (state == TranceiverStates.RadioTXOff && RigMode != RigModes.FM)
            {
                DefaultMeterLabel.Content = "S";
                TranceiverTXRXState = TranceiverStates.RadioTXOff;
                
                TXMetersCanvas.Visibility = Visibility.Hidden;

                BarGraphSignalMetersCanvas.Visibility =  Visibility.Hidden;
                AnalogueSignalMeterViewbox.Visibility = Visibility.Visible;


                SetRigLEDColor(RigLEDColors.LightGray);
            }
            else if (state == TranceiverStates.RadioTXOff && RigMode == RigModes.FM)
            {
                DefaultMeterLabel.Content = "S";
                TranceiverTXRXState = TranceiverStates.RadioTXOff;
                
                TXMetersCanvas.Visibility = Visibility.Hidden;

                BarGraphSignalMetersCanvas.Visibility = Visibility.Hidden;
                AnalogueSignalMeterViewbox.Visibility = Visibility.Visible;
            }
        }

        public void AddFoundStationToListView(StationSeekClass station)
        {
            StationScopeListView.Items.Add(new StationScope(this, station, frequencyManagement));
        }


        // --- State Fields ---
        private Point _lastMousePos;
        private double _lastBlurSpeed = 0;
        private double _blurImpulse = 0;
        private DateTime _lastMoveTime = DateTime.Now;
        private DispatcherTimer _blurTimer;

        private Point _lastMousePos2;
        private double _lastBlurSpeed2 = 0;
        private double _blurImpulse2 = 0;
        private DateTime _lastMoveTime2 = DateTime.Now;
        private DispatcherTimer _blurTimer2;

        private Point _lastMousePos3;
        private double _lastBlurSpeed3 = 0;
        private double _blurImpulse3 = 0;
        private DateTime _lastMoveTime3 = DateTime.Now;
        private DispatcherTimer _blurTimer3;

        private Point _lastMousePos4;
        private double _lastBlurSpeed4 = 0;
        private double _blurImpulse4 = 0;
        private DateTime _lastMoveTime4 = DateTime.Now;
        private DispatcherTimer _blurTimer4;


        // --- Frequency Constraints ---
        private const long MinFrequency = 1000;
        private const long MaxFrequency = 60000000;

        // --- Initialization Note ---
        // Ensure this is initialized somewhere like your constructor:
        // _blurTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        // _blurTimer.Tick += BlurTimer_Tick;

        private void SignifyMovement()
        {
            _lastMoveTime = DateTime.Now;
            RigBlurVFOCanvas.Visibility = Visibility.Visible;

            if (_blurTimer != null && !_blurTimer.IsEnabled)
            {
                _blurTimer.Start();
            }
        }
        private void SignifyMovement2()
        {
            _lastMoveTime2 = DateTime.Now;
            SelectionKnobBrurCanvas.Visibility = Visibility.Visible;

            if (_blurTimer2 != null && !_blurTimer2.IsEnabled)
            {
                _blurTimer2.Start();
            }
        }

        private void ApplyKnobInput(double deltaY)
        {
            DateTime now = DateTime.Now;

            double dt = (now - _lastMoveTime).TotalMilliseconds;
            if (dt <= 0) dt = 1;

            double speed = Math.Abs(deltaY) / dt;
            _lastBlurSpeed = (_lastBlurSpeed * 0.8) + (speed * 0.2);

            // Apply tracking adjustments
            long _currentFrequency = frequencyManagement.GetFrequency(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, MainFrequencyTextBlock);
            
            _currentFrequency += (long)(deltaY * tuningStep);

            // FIXED: Traditional clamping math for backwards compatibility (.NET Framework)
            if (_currentFrequency < MinFrequency) _currentFrequency = MinFrequency;
            if (_currentFrequency > MaxFrequency) _currentFrequency = MaxFrequency;

            frequencyManagement.SetFrequency(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, _currentFrequency, MainFrequencyTextBlock);

            _lastMoveTime = now;
        }

        Int32 QMBListViewLastSelectedItem = 0;
        private void ApplyKnobInput2(double deltaY)
        {
            DateTime now = DateTime.Now;

            double dt = (now - _lastMoveTime2).TotalMilliseconds;
            if (dt <= 0) dt = 1;

            double speed = Math.Abs(deltaY) / dt;
            _lastBlurSpeed2 = (_lastBlurSpeed2 * 0.8) + (speed * 0.2);

            if (deltaY > 0 && QMBListView.SelectedIndex > 0)
            {
                QMBListView.SelectedIndex--;
            }
            else if (deltaY < 0 && QMBListView.SelectedIndex < QMBListView.Items.Count - 1)
            {
                QMBListView.SelectedIndex++;
            }

            /*StationScope item = (StationScope)QMBListView.Items[QMBListView.SelectedIndex];

            UpdateFrequency(item.station.Frequency);

            SetFrequency(item.station.Frequency);

            SetRfGain(_port, 20);*/

            _lastMoveTime2 = now;
        }
        private void AnimateBlur(BlurEffect blurEffect, double target)
        {
            DoubleAnimation anim = new DoubleAnimation
            {
                To = target,
                Duration = TimeSpan.FromMilliseconds(80),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            blurEffect.BeginAnimation(
                BlurEffect.RadiusProperty,
                anim,
                HandoffBehavior.SnapshotAndReplace);
        }

        // --- Mouse Interaction Handlers ---

        private void VFOKnobAreaCanvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            isDragging = true;

            _lastMousePos = e.GetPosition(VFOKnobAreaCanvas);
            VFOKnobAreaCanvas.CaptureMouse();

            RigBlurVFOCanvas.Visibility = Visibility.Visible;
            _lastBlurSpeed = 0;
            _lastMoveTime = DateTime.Now;

            RigBlurVFOCanvasBlurEffect.BeginAnimation(BlurEffect.RadiusProperty, null);
            RigBlurVFOCanvasBlurEffect.Radius = 0;
        }

        private void VFOKnobAreaCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging) return;

            e.Handled = true;

            Point pos = e.GetPosition(VFOKnobAreaCanvas);
            double deltaY = _lastMousePos.Y - pos.Y;

            ApplyKnobInput(deltaY);
            _lastMousePos = pos;

            SignifyMovement();
        }

        private void VFOKnobAreaCanvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            isDragging = false;
            VFOKnobAreaCanvas.ReleaseMouseCapture();

            // Kill tracking properties instantly so decay steps drop cleanly straight to 0
            _lastBlurSpeed = 0;
            _blurImpulse = 0;

            if (_blurTimer != null && !_blurTimer.IsEnabled)
            {
                _blurTimer.Start();
            }
        }

        private void VFOKnobAreaCanvas_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;

            if (!isDragging) _lastBlurSpeed = 0;

            double delta = (e.Delta > 0 ? 1 : -1) * 6.0;
            ApplyKnobInput(delta);

            // Accumulate structural wheel impulse capped tightly at 6
            _blurImpulse += Math.Abs(delta) * 0.8;
            if (_blurImpulse > 6.0) _blurImpulse = 6.0;

            SignifyMovement();
        }

        // --- Core Decay Loop Engine ---

        private void BlurTimer_Tick(object sender, EventArgs e)
        {
            if (!isDragging)
            {
                _lastBlurSpeed = 0;
            }

            bool isIdle = (DateTime.Now - _lastMoveTime).TotalMilliseconds > 40; // Reduced from 60ms to 40ms for faster idle detection
            if (isIdle && isDragging)
            {
                _lastBlurSpeed *= 0.3; // Dropped from 0.5 to 0.3 to kill drag blur faster
            }

            // SPEED UP: Changed from 0.85 to 0.65 so wheel impulse drops like a rock
            _blurImpulse *= 0.65;

            double targetBlur = (_lastBlurSpeed * 80) + _blurImpulse;

            // SPEED UP: Raised threshold from 0.15 to 0.40 so it snaps to absolute 0 instantly
            if (targetBlur < 0.40)
            {
                targetBlur = 0;
                _blurImpulse = 0;
                _lastBlurSpeed = 0;
            }
            else if (targetBlur > 6)
            {
                targetBlur = 6;
            }

            // Apply UI state updates
            if (targetBlur == 0 && !isDragging)
            {
                RigBlurVFOCanvasBlurEffect.BeginAnimation(BlurEffect.RadiusProperty, null);
                RigBlurVFOCanvasBlurEffect.Radius = 0;
                RigBlurVFOCanvas.Visibility = Visibility.Collapsed;

                _blurTimer.Stop();
            }
            else
            {
                AnimateBlur(RigBlurVFOCanvasBlurEffect, targetBlur);
            }
        }

        private void BlurTimer2_Tick(object sender, EventArgs e)
        {
            if (!isDragging)
            {
                _lastBlurSpeed2 = 0;
            }

            bool isIdle = (DateTime.Now - _lastMoveTime2).TotalMilliseconds > 40; // Reduced from 60ms to 40ms for faster idle detection
            if (isIdle && isDragging)
            {
                _lastBlurSpeed2 *= 0.3; // Dropped from 0.5 to 0.3 to kill drag blur faster
            }

            // SPEED UP: Changed from 0.85 to 0.65 so wheel impulse drops like a rock
            _blurImpulse2 *= 0.65;

            double targetBlur = (_lastBlurSpeed2 * 80) + _blurImpulse2;

            // SPEED UP: Raised threshold from 0.15 to 0.40 so it snaps to absolute 0 instantly
            if (targetBlur < 0.40)
            {
                targetBlur = 0;
                _blurImpulse2 = 0;
                _lastBlurSpeed2 = 0;
            }
            else if (targetBlur > 6)
            {
                targetBlur = 6;
            }

            // Apply UI state updates
            if (targetBlur == 0 && !isDragging)
            {
                SelectionKnobCanvasBlurEffect.BeginAnimation(BlurEffect.RadiusProperty, null);
                SelectionKnobCanvasBlurEffect.Radius = 0;
                SelectionKnobBrurCanvas.Visibility = Visibility.Collapsed;

                _blurTimer2.Stop();
            }
            else
            {
                AnimateBlur(SelectionKnobCanvasBlurEffect, targetBlur);
            }
        }

        

        
        private void RigCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (EnableDrag && Mouse.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void RigCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //EnableDrag = false;
        }

        private void StartStationSeekButton_Click(object sender, RoutedEventArgs e)
        {
            FoundStationCountGrid.Visibility = System.Windows.Visibility.Visible;
            FoundStationCountLabel.Content = 0;

            stationSeek.SeekActiveStations(this, fT891S_SerialPort._port, 14100000, 14380000, 500, 3, FoundStationCountLabel);
        }

        private void AnimateButtonClick(Canvas c, Action onComplete)
        {
            // 1. Setup the Brush
            SolidColorBrush animatedBrush = new SolidColorBrush(Colors.Black);
            animatedBrush.Opacity = 0;
            c.Background = animatedBrush;

            // 2. Define the Fade In (First)
            DoubleAnimation fadeIn = new DoubleAnimation
            {
                To = 0.35,
                Duration = TimeSpan.FromMilliseconds(200), // 1 second as requested
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            // 3. Define the Fade Out (Second)
            DoubleAnimation fadeOut = new DoubleAnimation
            {
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(5),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            // 4. Chain them: When Fade In finishes, start Fade Out
            fadeIn.Completed += (s, ev) =>
            {
                animatedBrush.BeginAnimation(SolidColorBrush.OpacityProperty, fadeOut);
            };

            // Chain: Fade Out -> Execute the Callback (Shutdown)
            fadeOut.Completed += (s, e) => onComplete?.Invoke();

            // Start the sequence
            animatedBrush.BeginAnimation(SolidColorBrush.OpacityProperty, fadeIn);
        }

        private void FButtonCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Canvas c = (Canvas)sender;
            AnimateButtonClick(c, () =>
            {
                ChangeDisplayMode(TabControlTabControl, TabControlDescriptionLabel, 0);
            });
        }

        private void AButtonCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Canvas c = (Canvas)sender;
            AnimateButtonClick(c, () =>
            {
                if (stationSeek.IsScanning == true) stationSeek.RequestToStopScanning = true;

                FoundStationCountGrid.Visibility = System.Windows.Visibility.Visible;
                FoundStationCountLabel.Content = 0;

                stationSeek.SeekActiveStations(this, fT891S_SerialPort._port, 3500000, 3700000, 500, Convert.ToInt16(StationScopeSignalStrengthThresholdNumericUpDown.Value), FoundStationCountLabel);
            }); 
        }

        private void BButtonCanvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Canvas c = (Canvas)sender;
            AnimateButtonClick(c, () =>
            {
                if (stationSeek.IsScanning == true) stationSeek.RequestToStopScanning = true;

                FoundStationCountGrid.Visibility = System.Windows.Visibility.Visible;
                FoundStationCountLabel.Content = 0;

                stationSeek.SeekActiveStations(this, fT891S_SerialPort._port, 7100000, 7200000, 500, Convert.ToInt16(StationScopeSignalStrengthThresholdNumericUpDown.Value), FoundStationCountLabel);
            });
        }

        private void CButtonCanvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Canvas c = (Canvas)sender;
            AnimateButtonClick(c, () =>
            {
                if (stationSeek.IsScanning == true) stationSeek.RequestToStopScanning = true;

                FoundStationCountGrid.Visibility = System.Windows.Visibility.Visible;
                FoundStationCountLabel.Content = 0;

                stationSeek.SeekActiveStations(this, fT891S_SerialPort._port, 14100000, 14380000, 500, Convert.ToInt16(StationScopeSignalStrengthThresholdNumericUpDown.Value), FoundStationCountLabel);
            });
        }

        private void FastButtonCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Canvas c = (Canvas)sender;
            AnimateButtonClick(c, () =>
            {
                if (tuningStep == 10)
                {
                    tuningStep = 100;
                    FastNormalGrid.Visibility = Visibility.Visible;
                }
                else
                {
                    tuningStep = 10;
                    FastNormalGrid.Visibility = Visibility.Hidden;
                }
            });
        }

        private void PowerOffButtonCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Canvas c = (Canvas)sender;
            AnimateButtonClick(c, () =>
            {
                fT891S_SerialPort.StopSerialLoop();
                Application.Current.Shutdown();
            });
        }

        public int stationScopeListViewSelectedItem;

        private void StationScopeListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;

            while ((dep != null) && !(dep is StationScope))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep == null)
                return;

            StationScope item = (StationScope)dep;

            StationScopeListView.SelectedItem = item;

            stationScopeListViewSelectedItem = StationScopeListView.SelectedIndex;

            stationSeek.UpdateFoundStationCountLabel(FoundStationCountLabel, stationSeek.StationSeekActiveList[StationScopeListView.SelectedIndex].ID.ToString() + " of " + stationSeek.StationSeekActiveList.Count);

            frequencyManagement.SetFrequency(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, item.station.Frequency, MainFrequencyTextBlock);

            yAESU_FT_891_CAT_Dictionary.SetRfGain(fT891S_SerialPort._port, 20);
        }
        private void QMBListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;

            while ((dep != null) && !(dep is StationScope))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep == null)
                return;

            StationScope item = (StationScope)dep;

            QMBListView.SelectedItem = item;

            stationScopeListViewSelectedItem = QMBListView.SelectedIndex;

            //add ability to send these freq's to rig QMB

            frequencyManagement.SetFrequency(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, item.station.Frequency, MainFrequencyTextBlock);

            yAESU_FT_891_CAT_Dictionary.SetRfGain(fT891S_SerialPort._port, 20);

            if (ConsoleDebugLevel == ConsoleDebugLevels.CurrentDebug)
            {
                Console.WriteLine("QMBListView_PreviewMouseLeftButtonDown");
                Console.Write("stationScopeListViewSelectedItem = ");
                Console.WriteLine(stationScopeListViewSelectedItem);
            }
        }

        private void CLARButtonCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Canvas c = (Canvas)sender;
            AnimateButtonClick(c, () =>
            {
                if (decoder.IsRunning)
                {
                    decoder.Stop();
                }
                else
                {
                    CWDecoderTextBlock.Text = string.Empty;
                    decoder.Start();
                }
            });
        }

        private void StationScopeSignalStrengthThresholdNumericUpDown_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            var value = StationScopeSignalStrengthThresholdNumericUpDown.Value;
            if (ConsoleDebugLevel == ConsoleDebugLevels.All)
            {
                Console.WriteLine(value);
            }
        }

        private void OMBButtonCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Canvas c = (Canvas)sender;
            AnimateButtonClick(c, () =>
            {
                if (StationScopeListView.SelectedIndex != -1)
                {
                    RigState rigState = new RigState { RXFrequencyHz = stationSeek.StationSeekActiveList[stationScopeListViewSelectedItem].Frequency, SMeter = stationSeek.StationSeekActiveList[stationScopeListViewSelectedItem].SignalStrength };

                    qMBRigStates.AddNewRigStateToList(QMBListView, rigState);
                }

                if (ConsoleDebugLevel == ConsoleDebugLevels.All)
                {
                    Console.WriteLine("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");
                    Console.Write("QMBRigStatesList.Count = ");
                    Console.WriteLine(qMBRigStates.QMBRigStatesList.Count);
                    Console.WriteLine("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");
                }

                qMBRigStates.ListRigStates();
            });
        }

        private void MVButtonCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Canvas c = (Canvas)sender;
            AnimateButtonClick(c, () =>
            {
                waterFallSweep.Sweep(14252500, 14380000, 1000, 6);
            });
        }

        private void VMButtonCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Canvas c = (Canvas)sender;
            AnimateButtonClick(c, () =>
            {

            });
        }

        private void VMToggleButtonCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Canvas c = (Canvas)sender;
            AnimateButtonClick(c, () =>
            {

            });
        }

        private void ABButtonCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Canvas c = (Canvas)sender;
            AnimateButtonClick(c, () =>
            {
                memorySlot.SwapVFOs(this);
            });
        }

        private void BandButtonCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Canvas c = (Canvas)sender;
            AnimateButtonClick(c, () =>
            {

            });
        }

        private void TabControlTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void SelectionKnobAreaCanvas_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;

            if (!isDragging)
                _lastBlurSpeed2 = 0;

            double delta = (e.Delta > 0 ? 1 : -1) * 6.0;
            ApplyKnobInput2(delta);

            if (TranceiverMode == TranceiverModes.FunctionMenu)
            {
                if (delta > 0)
                    FunctionMenuClass.ChangeFunctionMenu(MenuDirections.GoRight);
                else
                    FunctionMenuClass.ChangeFunctionMenu(MenuDirections.GoLeft);
            }
            else if (TranceiverMode == TranceiverModes.Main)
            {
                simulatedWaterfall.DoScrollBasedOnCursorMode(delta);
            }

            _blurImpulse2 += Math.Abs(delta) * 0.8;
            if (_blurImpulse2 > 6.0)
                _blurImpulse2 = 6.0;

            SignifyMovement2();
        }

        DateTime SelectionKnobAreaCanvas_PreviewMouseDown_DateTime = DateTime.MinValue;

        private void SelectionKnobAreaCanvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            SelectionKnobAreaCanvas_PreviewMouseDown_DateTime = DateTime.Now;

            e.Handled = true;
            isDragging = true;

            _lastMousePos2 = e.GetPosition(SelectionKnobAreaCanvas);
            SelectionKnobAreaCanvas.CaptureMouse();

            SelectionKnobBrurCanvas.Visibility = Visibility.Visible;
            _lastBlurSpeed2 = 0;
            _lastMoveTime2 = DateTime.Now;

            SelectionKnobBrurCanvas.BeginAnimation(BlurEffect.RadiusProperty, null);
            SelectionKnobCanvasBlurEffect.Radius = 0;
        }

        private void SelectionKnobAreaCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging) return;

            e.Handled = true;

            Point pos = e.GetPosition(SelectionKnobAreaCanvas);
            double deltaY = _lastMousePos2.Y - pos.Y;

            // FIX: Only apply the input if we've actually surpassed the click time threshold.
            // This stops "micro-movements" during a fast click from twitching your knob value.
            if (DateTime.Now > (SelectionKnobAreaCanvas_PreviewMouseDown_DateTime + TimeSpan.FromMilliseconds(250)))
            {
                ApplyKnobInput2(deltaY);
                SignifyMovement2();
            }

            _lastMousePos2 = pos;
        }

        private void SelectionKnobAreaCanvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            // CRITICAL FIX: These MUST happen every time the mouse is released, 
            // otherwise a fast click leaves the canvas permanently dragging!
            e.Handled = true;
            isDragging = false;
            SelectionKnobAreaCanvas.ReleaseMouseCapture();

            // Check if it was a genuine long press/drag or just a quick click
            if (DateTime.Now > (SelectionKnobAreaCanvas_PreviewMouseDown_DateTime + TimeSpan.FromMilliseconds(250)))
            {
                // --- WE WERE DRAGGING ---
                // Kill tracking properties instantly so decay steps drop cleanly straight to 0
                _lastBlurSpeed2 = 0;
                _blurImpulse2 = 0;

                if (_blurTimer2 != null && !_blurTimer2.IsEnabled)
                {
                    _blurTimer2.Start();
                }
            }
            else
            {
                if (TranceiverMode != TranceiverModes.FunctionMenu)
                {
                    TabControlCanvas.Visibility = Visibility.Hidden;
                    DefaultCanvas.Visibility = Visibility.Hidden;
                    FunctioMenuTabCanvas.Visibility = Visibility.Visible;

                    LastTranceiverMode = TranceiverMode;

                    TabControlTabControl.SelectedIndex = TranceiverModes.FunctionMenu;
                    TranceiverMode = TranceiverModes.FunctionMenu;
                }
                else if (TranceiverMode == TranceiverModes.FunctionMenu)
                {
                    TabControlCanvas.Visibility = Visibility.Visible;
                    DefaultCanvas.Visibility = Visibility.Visible;
                    FunctioMenuTabCanvas.Visibility = Visibility.Hidden;

                    TranceiverMode = LastTranceiverMode;
                    TabControlTabControl.SelectedIndex = TranceiverMode;

                    if (FunctionMenuClass.FunctionMenuSelectedItem > FunctionMenuClass.FunctionModeMaxFunction)
                    {
                        // if not on a valid adjustable parameter, just default to 1st element in the list which is LEVEL
                        FunctionMenuClass.GetBorderByTag(0);
                        FunctionModeLabel.Content = FunctionMenuClass.GetName(0);
                    }
                }

                // --- WE CLICKED ---
                if (ConsoleDebugLevel == ConsoleDebugLevels.All)
                {
                    Console.WriteLine("SelectionKnobAreaCanvas_MouseLeftButtonDown (Triggered via fast-click threshold)");
                }

                // Hide the blur canvas since we didn't actually spin the knob
                SelectionKnobBrurCanvas.Visibility = Visibility.Collapsed;
            }
        }

        private void FunctionMenu_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var code = border.Tag.ToString();
            uint switchValue = uint.Parse(code);

            switch (switchValue)
            {
                case FunctionMenu.Level:
                    break;
                case FunctionMenu.Peak:
                    break;
                case FunctionMenu.Marker:
                    break;
                default:

                    break;
            }

            if (FunctionMenuClass.FunctionMenuSelectedItem > FunctionMenuClass.FunctionModeMaxFunction)
            {
                // if not on a valid adjustable parameter, just default to 1st element in the list which is LEVEL
                FunctionMenuClass.GetBorderByTag(0);
                FunctionModeLabel.Content = FunctionMenuClass.GetName(0);
            }
            else
            {
                FunctionMenuClass.GetBorderByTag(Convert.ToInt16(switchValue));
                FunctionModeLabel.Content = FunctionMenuClass.GetName(Convert.ToByte(switchValue));
            }

            if (ConsoleDebugLevel == ConsoleDebugLevels.All)
            {
                Console.Write("FunctionMenu_MouseLeftButtonDown = ");
                Console.WriteLine(switchValue);
            }
        }

        private void FunctionMenu_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var border = sender as Border;
            var code = border.Tag.ToString();
            uint switchValue = uint.Parse(code);

            if (FunctionMenuClass.FunctionMenuSelectedItem != Convert.ToByte(switchValue)) return;

            double delta = (e.Delta > 0 ? 1 : -1) * 6.0;

            byte mouseWheelScrollDirection = MenuDirections.NoChange;

            if (delta > 0)
                mouseWheelScrollDirection = MenuDirections.GoUp;
            else if (delta < 0)
                mouseWheelScrollDirection = MenuDirections.GoDown;

            switch (switchValue)
            {
                case FunctionMenu.Level:
                    break;
                case FunctionMenu.Peak:
                    break;
                case FunctionMenu.Marker:
                    break;

                case FunctionMenu.RfPower:
                    if (RigMode == RigModes.AM || RigMode == RigModes.D_AM_N)
                    {
                        if (mouseWheelScrollDirection == MenuDirections.GoUp && currentRigState.TXPowerWatts <= (currentRigState.TXPowerWattsAMMaximum - currentRigState.TXPowerWattsStep))
                            currentRigState.TXPowerWatts += currentRigState.TXPowerWattsStep;
                        else if (mouseWheelScrollDirection == MenuDirections.GoDown && currentRigState.TXPowerWatts >= (currentRigState.TXPowerWattsMinimum + currentRigState.TXPowerWattsStep))
                            currentRigState.TXPowerWatts -= currentRigState.TXPowerWattsStep;

                        RfPowerFunctionTextBlock.Text = currentRigState.TXPowerWatts.ToString() + "W";

                        fT891S_SerialPort.SendCAT(fT891S_SerialPort._port, "PC" + currentRigState.TXPowerWatts.ToString("D3"));
                    }
                    else
                    { 
                        if (mouseWheelScrollDirection == MenuDirections.GoUp && currentRigState.TXPowerWatts <= (currentRigState.TXPowerWattsMaximum - currentRigState.TXPowerWattsStep))
                            currentRigState.TXPowerWatts += currentRigState.TXPowerWattsStep;
                        else if (mouseWheelScrollDirection == MenuDirections.GoDown && currentRigState.TXPowerWatts >= (currentRigState.TXPowerWattsMinimum + currentRigState.TXPowerWattsStep))
                            currentRigState.TXPowerWatts -= currentRigState.TXPowerWattsStep;

                        RfPowerFunctionTextBlock.Text = currentRigState.TXPowerWatts.ToString() + "W";

                        fT891S_SerialPort.SendCAT(fT891S_SerialPort._port, "PC" + currentRigState.TXPowerWatts.ToString("D3"));
                    }
 
                    if (ConsoleDebugLevel == ConsoleDebugLevels.All)
                    {
                        Console.WriteLine("FunctionMenu_MouseWheel = FunctionMenu.RfPower");
                    }
                    break;

                default:

                    break;
            }
        }

        private void SimulatedWaterfall_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var code = border.Tag.ToString();
            uint switchValue = uint.Parse(code);

            simulatedWaterfall.ButtonSelection(Convert.ToByte(switchValue));

            if (ConsoleDebugLevel == ConsoleDebugLevels.All)
            {
                Console.Write("SimulatedWaterfall_MouseLeftButtonDown = ");
                Console.WriteLine(switchValue);
            }
        }

        private void SpanPopupWindowCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var code = border.Tag.ToString();
            uint switchValue = uint.Parse(code);

            simulatedWaterfall.ChangeSpanFrequency(Convert.ToByte(switchValue));

            if (ConsoleDebugLevel == ConsoleDebugLevels.All)
            {
                Console.Write("SpanPopupWindowCanvas_MouseLeftButtonDown = ");
                Console.WriteLine(switchValue);
            }
        }

        private void SpeedPopupWindowCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var code = border.Tag.ToString();
            uint switchValue = uint.Parse(code);

            simulatedWaterfall.ChangeSpeed(Convert.ToByte(switchValue));

            if (ConsoleDebugLevel == ConsoleDebugLevels.All)
            {
                Console.Write("SpeedPopupWindowCanvas_MouseLeftButtonDown = ");
                Console.WriteLine(switchValue);
            }
        }

    }
}