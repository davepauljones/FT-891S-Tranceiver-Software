using CWDecoder;
using FT891S_CatControl;
using MahApps.Metro.Controls;
using System;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using YAESU_FT_891_Front_End.Models;
using YAESU_FT_891_Front_End.Radio;
using static YAESU_FT_891_Front_End.Animations;
using static YAESU_FT_891_Front_End.HelperFunctions;
using static YAESU_FT_891_Front_End.MyStructs;
using static YAESU_FT_891_Front_End.RigStateChanges;
using static YAESU_FT_891_Front_End.SevenSegmentDisplay;
using static YAESU_FT_891_Front_End.TranceiverDisplayModes;

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

        double tuningStep = 10; // sensitivity (Hz per pixel)

        public FT891S_SerialPort fT891S_SerialPort;

        public FrequencyManagement frequencyManagement;

        public int TranceiverTXRXState = TranceiverStates.RadioTXOff;

        public bool EnableDrag = false;

        public byte ConsoleDebugLevel = ConsoleDebugLevels.CurrentDebug;
        public byte DevelopmentStatus = Development.InProgress;

        public MemorySlot memorySlot;

        public QMBRigStates qMBRigStates;

        public StationSeek stationSeek;

        private CWDecoderEngine decoder;
        public SimulatedWaterfall simulatedWaterfall;

        public Sprite sprite;
        public WaterFallSweep waterFallSweep;

        public FT891S_CatManager _catManager;

        public PacketManagement packetManagement;

        public CATCommandLog cATCommandLog;

        public TranceiverDisplayModes tranceiverDisplayModes;

        public static IModeMapper _modeMapper;

        public Psudo3DWaterfall psudo3DWaterfall;

        public FilterWave filterWave;

        public MainWindow()
        {
            InitializeComponent();

            EnableDrag = true;
            this.AllowsTransparency = true;

            // 1. Listen for when the user changes the frequency via the UI digits
            var freqDescriptor = DependencyPropertyDescriptor.FromProperty(
                FrequencyDisplay.FrequencyProperty, typeof(FrequencyDisplay));

            freqDescriptor.AddValueChanged(LargeFrequencyDisplay, OnFrequencyUiChanged);

            // 2. Listen for when the user starts/stops spinning the wheel
            var editDescriptor = DependencyPropertyDescriptor.FromProperty(
                FrequencyDisplay.IsDigitEditingProperty, typeof(FrequencyDisplay));

            editDescriptor.AddValueChanged(LargeFrequencyDisplay, OnUiEditStateChanged);
        }

        private async void OnFrequencyUiChanged(object sender, EventArgs e)
        {
            // This runs instantly when the user finishes scrolling a digit!
            long newFrequency = LargeFrequencyDisplay.Frequency;

            FT891S_CatManager.currentRadioState.VfoAFrequency = newFrequency;
            await _catManager.SendCatCommandAsync("FA", new object[] { newFrequency }, 5);

            // TODO: Send 'newFrequency' to your physical Yaesu FT-891 radio here via CAT commands
            Console.WriteLine($"Sending new frequency to radio: {newFrequency} Hz");
        }

        private void OnUiEditStateChanged(object sender, EventArgs e)
        {
            if (LargeFrequencyDisplay.IsDigitEditing)
            {
                // User is spinning the wheel! Stop your continuous polling timer here
                //StopRadioPollingTimer();
                _catManager.StopOutgoingDataLoop();
            }
            else
            {
                // User walked away / closed edit. Resume fetching live data from the rig
                //StartRadioPollingTimer();
                _catManager.StartOutgoingDataLoop();
            }
        }
        void Init_Startup()
        {
            fT891S_SerialPort = new FT891S_SerialPort(this, "COM8");

            memorySlot = new MemorySlot(this);

            qMBRigStates = new QMBRigStates(this);

            stationSeek = new StationSeek(this);

            frequencyManagement = new FrequencyManagement(this);

            centerX = Canvas.GetLeft(FingerIndentImage);
            centerY = Canvas.GetTop(FingerIndentImage);

            FastNormalBorder.Visibility = Visibility.Hidden;

            _blurTimer = new DispatcherTimer();
            _blurTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            _blurTimer.Tick += BlurTimer_Tick;
            _blurTimer.Start();

            _blurTimer2 = new DispatcherTimer();
            _blurTimer2.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            _blurTimer2.Tick += BlurTimer2_Tick;
            _blurTimer2.Start();

            _blurTimer3 = new DispatcherTimer();
            _blurTimer3.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            _blurTimer3.Tick += BlurTimer3_Tick;
            _blurTimer3.Start();

            _blurTimer4 = new DispatcherTimer();
            _blurTimer4.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            _blurTimer4.Tick += BlurTimer4_Tick;
            _blurTimer4.Start();

            FunctionMenuClass functionMenu = new FunctionMenuClass(this, FunctionMenuGrid, FunctionModeLabel);

            FT891S_CatManager.currentRadioState.TXPowerWatts = 5; //default rf power 5 watts for safety
            RfPowerFunctionTextBlock.Text = FT891S_CatManager.currentRadioState.TXPowerWatts.ToString() + "W";

            decoder = new CWDecoderEngine();

            decoder.TextDecoded += Decoder_TextDecoded;
            decoder.SignalPowerUpdated += Decoder_SignalPowerUpdated;

            simulatedWaterfall = new SimulatedWaterfall(this, frequencyManagement);

            packetManagement = new PacketManagement(this);

            cATCommandLog = new CATCommandLog(this);

            fT891S_SerialPort.OpenPort("COM8");

            sprite = new Sprite(this, WaterfallCanvas, BandScopeCanvas);

            waterFallSweep = new WaterFallSweep(this, BandScopeCanvas, SweepYellowCursorCanvas);

            _catManager = new FT891S_CatManager(this, this.Dispatcher);

            bandUserControl.Visibility = Visibility.Hidden;
            modeUserControl.Visibility = Visibility.Hidden;

            tranceiverDisplayModes = new TranceiverDisplayModes(this);

            tranceiverDisplayModes.SwitchToTranceiverMode(TranceiverModes.BootUp);

            _modeMapper = new FT891ModeMapper();
            modeUserControl.SetSupportedModes(_modeMapper.SupportedModes);

            _catManager.StartOutgoingDataLoop();

            //ApplyKnobInput3(0);
            //ApplyKnobInput4(0);

            // 1. Initialize your new class (Targeting 800x600 canvas)
            psudo3DWaterfall = new Psudo3DWaterfall(this, WaterfallImage);

            // 2. Assign the generated bitmap to the XAML Image control
            WaterfallImage.Source = psudo3DWaterfall.Bitmap;

            filterWave = new FilterWave(this, MyFilterWave);

            //filterWave.UpdateFromRadio("NB", object value)
        }

        private void BlurTimer_Tick(object sender, EventArgs e)
        {
            HandleBlurTimerTick(ref _lastBlurSpeed, ref _blurImpulse, ref _lastMoveTime, RigBlurVFOCanvasBlurEffect, RigBlurVFOCanvas, _blurTimer);
        }
        private void BlurTimer2_Tick(object sender, EventArgs e)
        {
            HandleBlurTimerTick(ref _lastBlurSpeed2, ref _blurImpulse2, ref _lastMoveTime2, SelectionKnobCanvasBlurEffect, SelectionKnobBrurCanvas, _blurTimer2);
        }
        private void BlurTimer3_Tick(object sender, EventArgs e)
        {
            HandleBlurTimerTick(ref _lastBlurSpeed3, ref _blurImpulse3, ref _lastMoveTime3, RFGainKnobCanvasBlurEffect, RFGainKnobBrurCanvas, _blurTimer3);
        }
        private void BlurTimer4_Tick(object sender, EventArgs e)
        {
            HandleBlurTimerTick(ref _lastBlurSpeed4, ref _blurImpulse4, ref _lastMoveTime4, AFGainKnobCanvasBlurEffect, AFGainKnobBrurCanvas, _blurTimer4);
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

            _catManager.StopOutgoingDataLoop();
        }
        
        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Init_Startup();
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
        public static int GetSMeterIntegerForBandScope(int rawValue)
        {
            int baseValue;

            // 1. Range 0 to 105 maps to S0 through S9
            if (rawValue <= 105)
            {
                // Ensure we don't drop below 0 for unexpected negative raw values
                int clampedRaw = Math.Max(0, rawValue);
                baseValue = (int)Math.Round(clampedRaw / 11.6);
            }
            // 2. Range 106 to 195 maps to 10dB through 60dB over
            else
            {
                int dbOver = (int)Math.Round((rawValue - 105) / 1.5);
                // Clamp to 60 to prevent overflow from stray CAT bytes
                baseValue = Math.Min(60, dbOver);
            }

            // 3. Scale the final result across 50 (Original Max was 60)
            // Multiplying by 50.0 / 60.0 ensures accurate floating-point math before rounding
            return (int)Math.Round(baseValue * (50.0 / 60.0));
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

                SetRigLEDColor(this, RigLEDColors.Red);
            }
            else if (state == TranceiverStates.RadioTXOff && RigMode != RadioMode.FM)
            {
                DefaultMeterLabel.Content = "S";
                TranceiverTXRXState = TranceiverStates.RadioTXOff;
                
                TXMetersCanvas.Visibility = Visibility.Hidden;

                BarGraphSignalMetersCanvas.Visibility =  Visibility.Hidden;
                AnalogueSignalMeterViewbox.Visibility = Visibility.Visible;


                SetRigLEDColor(this, RigLEDColors.LightGray);
            }
            else if (state == TranceiverStates.RadioTXOff && RigMode == RadioMode.FM)
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

        private void SignifyMovement(DateTime lastMoveTime, Canvas blurCanvas, DispatcherTimer blurTimer)
        {
            lastMoveTime = DateTime.Now;
            blurCanvas.Visibility = Visibility.Visible;

            if (blurTimer != null && !blurTimer.IsEnabled)
            {
                blurTimer.Start();
            }
        }

        private void ApplyKnobInput(double deltaY)
        {
            DateTime now = DateTime.Now;

            double dt = (now - _lastMoveTime).TotalMilliseconds;
            if (dt <= 0) dt = 1;

            double speed = Math.Abs(deltaY) / dt;
            _lastBlurSpeed = (_lastBlurSpeed * 0.8) + (speed * 0.2);

            FT891S_CatManager.currentRadioState.VfoAFrequency += (long)(deltaY * tuningStep);

            // FIXED: Traditional clamping math for backwards compatibility (.NET Framework)
            if (FT891S_CatManager.currentRadioState.VfoAFrequency < MinFrequency) FT891S_CatManager.currentRadioState.VfoAFrequency = MinFrequency;
            if (FT891S_CatManager.currentRadioState.VfoAFrequency > MaxFrequency) FT891S_CatManager.currentRadioState.VfoAFrequency = MaxFrequency;

            frequencyManagement.SetFrequency(MemorySlot.MemorySlots.VFO_A, FT891S_CatManager.currentRadioState.VfoAFrequency, MainFrequencyTextBlock);

            _lastMoveTime = now;
        }

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

        private async void ApplyKnobInput3(double deltaY)
        {
            DateTime now = DateTime.Now;

            double dt = (now - _lastMoveTime3).TotalMilliseconds;
            if (dt <= 0) dt = 1;

            double speed = Math.Abs(deltaY) / dt;
            _lastBlurSpeed3 = (_lastBlurSpeed3 * 0.8) + (speed * 0.2);

            // 1. Determine direction (Scrolling up adds 10, scrolling down subtracts 10)
            int step = (deltaY > 0) ? 1 : -1;//do this to change the step int step = (deltaY > 0) ? 10 : -10;//

            if (deltaY == 0) step = 0;

            // 2. Add the step to the current value
            int proposedValue = FT891S_CatManager.currentRadioState.RFGain + step;

            // 3. Force it to stay strictly between 0 and 255
            FT891S_CatManager.currentRadioState.RFGain = Math.Min(Math.Max(proposedValue, 0), 30);

            if (FT891S_CatManager.currentRadioState.RFGain == 0)
                RFGainTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            else
                RFGainTextBlock.Foreground = new SolidColorBrush(Colors.White);

            RFGainTextBlock.Text = FT891S_CatManager.currentRadioState.RFGain.ToString() + " RF";

            await _catManager.SendCatCommandAsync("RG", new object[] { 0, FT891S_CatManager.currentRadioState.RFGain }, _catManager.OutGoingDataLoopDelay);

            _lastMoveTime3 = now;
        }

        public async void UpdateKnobInput3()
        {
            if (FT891S_CatManager.currentRadioState.RFGain == 0)
                RFGainTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            else
                RFGainTextBlock.Foreground = new SolidColorBrush(Colors.White);

            RFGainTextBlock.Text = FT891S_CatManager.currentRadioState.RFGain.ToString() + " RF";
        }

        private async void ApplyKnobInput4(double deltaY)
        {
            DateTime now = DateTime.Now;

            double dt = (now - _lastMoveTime4).TotalMilliseconds;
            if (dt <= 0) dt = 1;

            double speed = Math.Abs(deltaY) / dt;
            _lastBlurSpeed4 = (_lastBlurSpeed4 * 0.8) + (speed * 0.2);

            // 1. Determine direction (Scrolling up adds 10, scrolling down subtracts 10)
            int step = (deltaY > 0) ? 10 : -10;

            if (deltaY == 0) step = 0;

            // 2. Add the step to the current value
            int proposedValue = FT891S_CatManager.currentRadioState.AFGain + step;

            // 3. Force it to stay strictly between 0 and 255
            FT891S_CatManager.currentRadioState.AFGain = Math.Min(Math.Max(proposedValue, 0), 255);

            if (FT891S_CatManager.currentRadioState.AFGain == 0)
                AFGainTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            else
                AFGainTextBlock.Foreground = new SolidColorBrush(Colors.White);

            AFGainTextBlock.Text = FT891S_CatManager.currentRadioState.AFGain.ToString() + " AF";

            await _catManager.SendCatCommandAsync("AG", new object[] { 0, FT891S_CatManager.currentRadioState.AFGain }, _catManager.OutGoingDataLoopDelay);

            _lastMoveTime4 = now;
        }

        public async void UpdateKnobInput4()
        {
            if (FT891S_CatManager.currentRadioState.AFGain == 0)
                AFGainTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            else
                AFGainTextBlock.Foreground = new SolidColorBrush(Colors.White);

            AFGainTextBlock.Text = FT891S_CatManager.currentRadioState.AFGain.ToString() + " AF";
        }

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

        DateTime RigBlurVFOKnobAreaCanvas_PreviewMouseDown_DateTime = DateTime.MinValue;
        private void VFOKnobAreaCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging) return;
            e.Handled = true;

            Point pos = e.GetPosition(VFOKnobAreaCanvas);
            double deltaY = _lastMousePos.Y - pos.Y;

            if (DateTime.Now > (RigBlurVFOKnobAreaCanvas_PreviewMouseDown_DateTime + TimeSpan.FromMilliseconds(250)))
            {
                ApplyKnobInput(deltaY); // Your specific business logic
                ProcessKnobRotation(ref _lastBlurSpeed, ref _blurImpulse, ref _lastMoveTime, RigBlurVFOCanvas, deltaY);
                SignifyMovement(_lastMoveTime, RigBlurVFOCanvas, _blurTimer);
            }
            _lastMousePos = pos;
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

            SignifyMovement(_lastMoveTime, RigBlurVFOCanvas, _blurTimer);
        }

        private void RigCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (EnableDrag && Mouse.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void StartStationSeekButton_Click(object sender, RoutedEventArgs e)
        {
            FoundStationCountGrid.Visibility = System.Windows.Visibility.Visible;
            FoundStationCountLabel.Content = 0;

            stationSeek.SeekActiveStations(this, fT891S_SerialPort._port, 14100000, 14380000, 500, 3, FoundStationCountLabel);
        }

        private void FButtonCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Canvas c = (Canvas)sender;
            AnimateButtonClick(c, () =>
            {
                tranceiverDisplayModes.ToggleTranceiverMode(tranceiverDisplayModes.CurrentTranceiverMode.ID);
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

        private async void FastButtonCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Canvas c = (Canvas)sender;
            AnimateButtonClick(c, async () =>
            {
                if (tuningStep == 10)
                {
                    tuningStep = 100;
                    FastNormalBorder.Visibility = Visibility.Visible;
                    await _catManager.SendCatCommandAsync("FS", new object[] { (int)FastStep.FastStep_ON }, _catManager.OutGoingDataLoopDelay);
                }
                else
                {
                    tuningStep = 10;
                    FastNormalBorder.Visibility = Visibility.Hidden;
                    await _catManager.SendCatCommandAsync("FS", new object[] { (int)FastStep.FastStep_OFF }, _catManager.OutGoingDataLoopDelay);
                }
            });
        }

        private void PowerOffButtonCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Canvas c = (Canvas)sender;
            AnimateButtonClick(c, () =>
            {
                _catManager.StopOutgoingDataLoop();
                Application.Current.Shutdown();
            });
        }

        public int stationScopeListViewSelectedItem;

        private async void StationScopeListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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

            frequencyManagement.SetFrequency(MemorySlot.MemorySlots.VFO_A, item.station.Frequency, MainFrequencyTextBlock);

            //yAESU_FT_891_CAT_Dictionary.SetRfGain(fT891S_SerialPort._port, 20);
            await _catManager.SendCatCommandAsync("RG", new object[] { 0, 30 }, _catManager.OutGoingDataLoopDelay);
        }
        private async void QMBListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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

            frequencyManagement.SetFrequency(MemorySlot.MemorySlots.VFO_A, item.station.Frequency, MainFrequencyTextBlock);

            //yAESU_FT_891_CAT_Dictionary.SetRfGain(fT891S_SerialPort._port, 20);
            await _catManager.SendCatCommandAsync("RG", new object[] { 0, 30 }, _catManager.OutGoingDataLoopDelay);

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
                    RadioState radioState = new RadioState { VfoAFrequency = stationSeek.StationSeekActiveList[stationScopeListViewSelectedItem].Frequency, SMeter = stationSeek.StationSeekActiveList[stationScopeListViewSelectedItem].SignalStrength };

                    qMBRigStates.AddNewRigStateToList(QMBListView, radioState);
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
            AnimateButtonClick(c, async () =>
            {
                // Properly await the single sweep execution path
                await waterFallSweep.ToggleSweepOnOff(forceSingleSweep: true);
            });
        }

        int ScanningState = 0;
        private async void VMButtonCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Canvas c = (Canvas)sender;
            AnimateButtonClick(c, () =>
            {
                
            });

            if (ScanningState == 0)
            {
                waterFallSweep.SweepActive = true;
                ScanningState = 1;
                await _catManager.SendCatCommandAsync("SC", new object[] { ScanningState }, _catManager.OutGoingDataLoopDelay);
            }
            else if (ScanningState == 1)
            {
                waterFallSweep.SweepActive = true;
                ScanningState = 2;
                await _catManager.SendCatCommandAsync("SC", new object[] { ScanningState }, _catManager.OutGoingDataLoopDelay);
            }
            else if (ScanningState == 2)
            {
                waterFallSweep.SweepActive = false;
                ScanningState = 0;
                await _catManager.SendCatCommandAsync("SC", new object[] { ScanningState }, _catManager.OutGoingDataLoopDelay);
            }
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
                if (bandUserControl.Visibility != Visibility.Visible)
                {
                    // 1. Instant Show: Make it visible and reset opacity to full
                    bandUserControl.Visibility = Visibility.Visible;
                    bandUserControl.Opacity = 1.0;
                }
                else
                {
                    FadoutUserControl(bandUserControl, 0);
                }
            });
        }

        private void RFGainKnobAreaCanvas_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;

            if (!isDragging)
                _lastBlurSpeed3 = 0;

            double delta = (e.Delta > 0 ? 1 : -1) * 6.0;
            ApplyKnobInput3(delta);

            if (tranceiverDisplayModes.CurrentTranceiverMode.ID == TranceiverModes.FunctionMenu)
            {
                if (delta > 0)
                    FunctionMenuClass.ChangeFunctionMenu(MenuDirections.GoRight);
                else
                    FunctionMenuClass.ChangeFunctionMenu(MenuDirections.GoLeft);
            }
            else if (tranceiverDisplayModes.CurrentTranceiverMode.ID == TranceiverModes.MainWaterfall)
            {
                simulatedWaterfall.DoScrollBasedOnCursorMode(delta);
            }

            _blurImpulse3 += Math.Abs(delta) * 0.8;
            if (_blurImpulse3 > 6.0)
                _blurImpulse3 = 6.0;

            SignifyMovement(_lastMoveTime3, RFGainKnobBrurCanvas, _blurTimer3);
        }

        DateTime RFGainKnobAreaCanvas_PreviewMouseDown_DateTime = DateTime.MinValue;

        private void RFGainKnobAreaCanvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            RFGainKnobAreaCanvas_PreviewMouseDown_DateTime = DateTime.Now;

            e.Handled = true;
            isDragging = true;

            _lastMousePos3 = e.GetPosition(RFGainKnobAreaCanvas);
            RFGainKnobAreaCanvas.CaptureMouse();

            RFGainKnobBrurCanvas.Visibility = Visibility.Visible;
            _lastBlurSpeed3 = 0;
            _lastMoveTime3 = DateTime.Now;

            RFGainKnobBrurCanvas.BeginAnimation(BlurEffect.RadiusProperty, null);
            RFGainKnobCanvasBlurEffect.Radius = 0;
        }

        // 1. Add this field at the top of your class to track the last update time
        private DateTime _lastKnobUpdateTime3 = DateTime.MinValue;
        private void RFGainKnobAreaCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging) return;
            e.Handled = true;

            // --- TIME THROTTLE START ---
            // Only allow changes every 50 milliseconds (adjust this to change speed)
            if ((DateTime.Now - _lastKnobUpdateTime3).TotalMilliseconds < 50)
            {
                return; // Too soon! Ignore this mouse twitch.
            }
            // --- TIME THROTTLE END ---

            Point pos = e.GetPosition(RFGainKnobAreaCanvas);
            double deltaY = _lastMousePos3.Y - pos.Y;

            if (DateTime.Now > (RFGainKnobAreaCanvas_PreviewMouseDown_DateTime + TimeSpan.FromMilliseconds(250)))
            {
                // Now we can use a very simple modifier because the events are spaced out
                double controlledDelta = deltaY * 0.5;

                ApplyKnobInput3(controlledDelta);

                // Update the timestamp *only* when a valid movement happens
                _lastKnobUpdateTime3 = DateTime.Now;
            }

            ProcessKnobRotation(ref _lastBlurSpeed3, ref _blurImpulse3, ref _lastMoveTime3, RFGainKnobBrurCanvas, deltaY);
            SignifyMovement(_lastMoveTime3, RFGainKnobBrurCanvas, _blurTimer3);

            _lastMousePos3 = pos;
        }
        public int lastRFGain = 100;
        private async void RFGainKnobAreaCanvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            // CRITICAL FIX: These MUST happen every time the mouse is released, 
            // otherwise a fast click leaves the canvas permanently dragging!
            e.Handled = true;
            isDragging = false;
            RFGainKnobAreaCanvas.ReleaseMouseCapture();

            // Check if it was a genuine long press/drag or just a quick click
            if (DateTime.Now > (RFGainKnobAreaCanvas_PreviewMouseDown_DateTime + TimeSpan.FromMilliseconds(250)))
            {
                // --- WE WERE DRAGGING ---
                // Kill tracking properties instantly so decay steps drop cleanly straight to 0
                _lastBlurSpeed3 = 0;
                _blurImpulse3 = 0;

                if (_blurTimer3 != null && !_blurTimer3.IsEnabled)
                {
                    _blurTimer3.Start();
                }
            }
            else
            {
                if (FT891S_CatManager.currentRadioState.RFGain > 0 && FT891S_CatManager.currentRadioState.RFGain < 255)
                {
                    lastRFGain = FT891S_CatManager.currentRadioState.RFGain;

                    FT891S_CatManager.currentRadioState.RFGain = 0;
                    RFGainTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    await _catManager.SendCatCommandAsync("RG", new object[] { 0, 0 }, _catManager.OutGoingDataLoopDelay);
                }
                else
                {
                    FT891S_CatManager.currentRadioState.RFGain = lastRFGain;
                    RFGainTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    await _catManager.SendCatCommandAsync("RG", new object[] { 0, lastRFGain }, _catManager.OutGoingDataLoopDelay);
                }

                // --- WE CLICKED ---
                if (ConsoleDebugLevel == ConsoleDebugLevels.CurrentDebug)
                {
                    Console.WriteLine("RFGainKnobAreaCanvas_MouseLeftButtonDown (Triggered via fast-click threshold) = " + lastRFGain);
                    Console.WriteLine(FT891S_CatManager.currentRadioState.RFGain);
                }

                // Hide the blur canvas since we didn't actually spin the knob
                RFGainKnobBrurCanvas.Visibility = Visibility.Collapsed;
            }
        }

        private void AFGainKnobAreaCanvas_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;

            if (!isDragging)
                _lastBlurSpeed4 = 0;

            double delta = (e.Delta > 0 ? 1 : -1) * 6.0;
            ApplyKnobInput4(delta);

            if (tranceiverDisplayModes.CurrentTranceiverMode.ID == TranceiverModes.FunctionMenu)
            {
                if (delta > 0)
                    FunctionMenuClass.ChangeFunctionMenu(MenuDirections.GoRight);
                else
                    FunctionMenuClass.ChangeFunctionMenu(MenuDirections.GoLeft);
            }
            else if (tranceiverDisplayModes.CurrentTranceiverMode.ID == TranceiverModes.MainWaterfall)
            {
                simulatedWaterfall.DoScrollBasedOnCursorMode(delta);
            }

            _blurImpulse4 += Math.Abs(delta) * 0.8;
            if (_blurImpulse4 > 6.0)
                _blurImpulse4 = 6.0;

            SignifyMovement(_lastMoveTime4, AFGainKnobBrurCanvas, _blurTimer4);
        }

        DateTime AFGainKnobAreaCanvas_PreviewMouseDown_DateTime = DateTime.MinValue;

        private void AFGainKnobAreaCanvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            AFGainKnobAreaCanvas_PreviewMouseDown_DateTime = DateTime.Now;

            e.Handled = true;
            isDragging = true;

            _lastMousePos4 = e.GetPosition(AFGainKnobAreaCanvas);
            AFGainKnobAreaCanvas.CaptureMouse();

            AFGainKnobBrurCanvas.Visibility = Visibility.Visible;
            _lastBlurSpeed4 = 0;
            _lastMoveTime4 = DateTime.Now;

            AFGainKnobBrurCanvas.BeginAnimation(BlurEffect.RadiusProperty, null);
            AFGainKnobCanvasBlurEffect.Radius = 0;
        }

        // 1. Add this field at the top of your class to track the last update time
        private DateTime _lastKnobUpdateTime = DateTime.MinValue;
        private void AFGainKnobAreaCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging) return;
            e.Handled = true;

            // --- TIME THROTTLE START ---
            // Only allow changes every 50 milliseconds (adjust this to change speed)
            if ((DateTime.Now - _lastKnobUpdateTime).TotalMilliseconds < 50)
            {
                return; // Too soon! Ignore this mouse twitch.
            }
            // --- TIME THROTTLE END ---

            Point pos = e.GetPosition(AFGainKnobAreaCanvas);
            double deltaY = _lastMousePos4.Y - pos.Y;

            if (DateTime.Now > (AFGainKnobAreaCanvas_PreviewMouseDown_DateTime + TimeSpan.FromMilliseconds(250)))
            {
                // Now we can use a very simple modifier because the events are spaced out
                double controlledDelta = deltaY * 0.5;

                ApplyKnobInput4(controlledDelta);

                // Update the timestamp *only* when a valid movement happens
                _lastKnobUpdateTime = DateTime.Now;
            }

            ProcessKnobRotation(ref _lastBlurSpeed4, ref _blurImpulse4, ref _lastMoveTime4, AFGainKnobBrurCanvas, deltaY);
            SignifyMovement(_lastMoveTime4, AFGainKnobBrurCanvas, _blurTimer4);

            _lastMousePos4 = pos;
        }
        public int lastAFGain = 100;
        private async void AFGainKnobAreaCanvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            // CRITICAL FIX: These MUST happen every time the mouse is released, 
            // otherwise a fast click leaves the canvas permanently dragging!
            e.Handled = true;
            isDragging = false;
            AFGainKnobAreaCanvas.ReleaseMouseCapture();

            // Check if it was a genuine long press/drag or just a quick click
            if (DateTime.Now > (AFGainKnobAreaCanvas_PreviewMouseDown_DateTime + TimeSpan.FromMilliseconds(250)))
            {
                // --- WE WERE DRAGGING ---
                // Kill tracking properties instantly so decay steps drop cleanly straight to 0
                _lastBlurSpeed4 = 0;
                _blurImpulse4 = 0;

                if (_blurTimer4 != null && !_blurTimer4.IsEnabled)
                {
                    _blurTimer4.Start();
                }
            }
            else
            {
                if (FT891S_CatManager.currentRadioState.AFGain > 0 && FT891S_CatManager.currentRadioState.AFGain < 255)
                {
                    lastAFGain = FT891S_CatManager.currentRadioState.AFGain;

                    FT891S_CatManager.currentRadioState.AFGain = 0;
                    AFGainTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    await _catManager.SendCatCommandAsync("AG", new object[] { 0, 0 }, _catManager.OutGoingDataLoopDelay);
                }
                else
                {
                    FT891S_CatManager.currentRadioState.AFGain = lastAFGain;
                    AFGainTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    await _catManager.SendCatCommandAsync("AG", new object[] { 0, lastAFGain }, _catManager.OutGoingDataLoopDelay);
                }

                // --- WE CLICKED ---
                if (ConsoleDebugLevel == ConsoleDebugLevels.CurrentDebug)
                {
                    Console.WriteLine("AFGainKnobAreaCanvas_MouseLeftButtonDown (Triggered via fast-click threshold) = " + lastAFGain);
                    Console.WriteLine(FT891S_CatManager.currentRadioState.AFGain);
                }

                // Hide the blur canvas since we didn't actually spin the knob
                AFGainKnobBrurCanvas.Visibility = Visibility.Collapsed;
            }
        }

        private void SelectionKnobAreaCanvas_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;

            if (!isDragging)
                _lastBlurSpeed2 = 0;

            double delta = (e.Delta > 0 ? 1 : -1) * 6.0;
            ApplyKnobInput2(delta);

            if (tranceiverDisplayModes.CurrentTranceiverMode.ID == TranceiverModes.FunctionMenu)
            {
                if (delta > 0)
                    FunctionMenuClass.ChangeFunctionMenu(MenuDirections.GoRight);
                else
                    FunctionMenuClass.ChangeFunctionMenu(MenuDirections.GoLeft);
            }
            else if (tranceiverDisplayModes.CurrentTranceiverMode.ID == TranceiverModes.MainWaterfall)
            {
                FunctionMenuClass.SetFunctionMenuSelectedItemLevel(delta, FunctionValueTextBlock);
                //simulatedWaterfall.DoScrollBasedOnCursorMode(delta);
            }
            else if (tranceiverDisplayModes.CurrentTranceiverMode.ID == TranceiverModes.NoiseFilters)
            {
                FunctionMenuClass.SetFunctionMenuSelectedItemLevel(delta, FunctionValueTextBlock);
            }

            _blurImpulse2 += Math.Abs(delta) * 0.8;
            if (_blurImpulse2 > 6.0)
                _blurImpulse2 = 6.0;

            SignifyMovement(_lastMoveTime2, SelectionKnobBrurCanvas, _blurTimer2);
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

            if (DateTime.Now > (SelectionKnobAreaCanvas_PreviewMouseDown_DateTime + TimeSpan.FromMilliseconds(250)))
            {
                ApplyKnobInput2(deltaY); // Your specific business logic
                ProcessKnobRotation(ref _lastBlurSpeed2, ref _blurImpulse2, ref _lastMoveTime2, SelectionKnobBrurCanvas, deltaY);
                SignifyMovement(_lastMoveTime2, SelectionKnobBrurCanvas, _blurTimer2);
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
                if (tranceiverDisplayModes.CurrentTranceiverMode.ID != TranceiverModes.FunctionMenu)
                {
                    TabControlCanvas.Visibility = Visibility.Hidden;
                    DefaultCanvas.Visibility = Visibility.Hidden;
                    FunctioMenuTabCanvas.Visibility = Visibility.Visible;

                    tranceiverDisplayModes.LastTranceiverMode = tranceiverDisplayModes.CurrentTranceiverMode;

                    tranceiverDisplayModes.ToggleTranceiverMode(TranceiverModes.FunctionMenu);
                }
                else if (tranceiverDisplayModes.CurrentTranceiverMode.ID == TranceiverModes.FunctionMenu)
                {
                    TabControlCanvas.Visibility = Visibility.Visible;
                    DefaultCanvas.Visibility = Visibility.Visible;
                    FunctioMenuTabCanvas.Visibility = Visibility.Hidden;

                    tranceiverDisplayModes.SwitchToTranceiverMode(tranceiverDisplayModes.LastTranceiverMode.ID);

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

            FunctionMenuClass.SetFunctionMenuSelectedItemLevel(0, FunctionValueTextBlock, true);

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
                    if (RigMode == RadioMode.AM || RigMode == RadioMode.AM_N)
                    {
                        if (mouseWheelScrollDirection == MenuDirections.GoUp && FT891S_CatManager.currentRadioState.TXPowerWatts <= (FT891S_CatManager.currentRadioState.TXPowerWattsAMMaximum - FT891S_CatManager.currentRadioState.TXPowerWattsStep))
                            FT891S_CatManager.currentRadioState.TXPowerWatts += FT891S_CatManager.currentRadioState.TXPowerWattsStep;
                        else if (mouseWheelScrollDirection == MenuDirections.GoDown && FT891S_CatManager.currentRadioState.TXPowerWatts >= (FT891S_CatManager.currentRadioState.TXPowerWattsMinimum + FT891S_CatManager.currentRadioState.TXPowerWattsStep))
                            FT891S_CatManager.currentRadioState.TXPowerWatts -= FT891S_CatManager.currentRadioState.TXPowerWattsStep;

                        RfPowerFunctionTextBlock.Text = FT891S_CatManager.currentRadioState.TXPowerWatts.ToString() + "W";

                        fT891S_SerialPort.SendCAT(fT891S_SerialPort._port, "PC" + FT891S_CatManager.currentRadioState.TXPowerWatts.ToString("D3"));
                    }
                    else
                    { 
                        if (mouseWheelScrollDirection == MenuDirections.GoUp && FT891S_CatManager.currentRadioState.TXPowerWatts <= (FT891S_CatManager.currentRadioState.TXPowerWattsMaximum - FT891S_CatManager.currentRadioState.TXPowerWattsStep))
                            FT891S_CatManager.currentRadioState.TXPowerWatts += FT891S_CatManager.currentRadioState.TXPowerWattsStep;
                        else if (mouseWheelScrollDirection == MenuDirections.GoDown && FT891S_CatManager.currentRadioState.TXPowerWatts >= (FT891S_CatManager.currentRadioState.TXPowerWattsMinimum + FT891S_CatManager.currentRadioState.TXPowerWattsStep))
                            FT891S_CatManager.currentRadioState.TXPowerWatts -= FT891S_CatManager.currentRadioState.TXPowerWattsStep;

                        RfPowerFunctionTextBlock.Text = FT891S_CatManager.currentRadioState.TXPowerWatts.ToString() + "W";

                        fT891S_SerialPort.SendCAT(fT891S_SerialPort._port, "PC" + FT891S_CatManager.currentRadioState.TXPowerWatts.ToString("D3"));
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

            waterFallSweep.ClearBandScope();
            psudo3DWaterfall.ClearWaterfall();

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

        private void TimeSliceOnOffCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (waterFallSweep != null)
            {
                // Flip the current state to its opposite value
                waterFallSweep.UseTimeSlicing = !waterFallSweep.UseTimeSlicing;

                // Optional feedback: Update a text block or variable to show the current state
                if (waterFallSweep.UseTimeSlicing)
                {
                    TimeSliceOnOffTextBlock.Text = "ON";
                }
                else
                {
                    TimeSliceOnOffTextBlock.Text = "OFF";
                }
            }
        }

        private void SendOnOffCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_catManager.OutGoingDataLoop_IsRunning)
                _catManager.StopOutgoingDataLoop();
            else
                _catManager.StartOutgoingDataLoop();
        }

        public void SwitchSendRedLEDRectangle(bool state)
        {
            if (state)
                SendRedLEDRectangle.Opacity = 0.5;
            else
                SendRedLEDRectangle.Opacity = 0.2;
        }

        private void ScopeOnOffCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            waterFallSweep.ToggleSweepOnOff();
        }

        private async void BandUserControl_BandChanged(object sender, YAESU_FT_891_Front_End.BandChangedEventArgs e)
        {
            // You now have access to both fields right here!
            byte selectedBandCode = e.SelectedBand;
            long targetFrequency = e.SelectedFrequency;

            // Example Usage: Update a MainWindow status bar, radio interface frequency, etc.
            System.Diagnostics.Debug.WriteLine($"Band changed to: {selectedBandCode}, Freq: {targetFrequency} Hz");

            await _catManager.SendCatCommandAsync("BS", new object[] { Convert.ToInt16(selectedBandCode) }, _catManager.OutGoingDataLoopDelay);
        }

        private async void ModeUserControl_ModeChanged(object sender, ModeChangedEventArgs e)
        {
            byte catValue = _modeMapper.ToCAT(e.Mode);

            if (e.Mode == RadioMode.USB)
                await _catManager.SendCatCommandAsync("EX", new object[] { "1107", 0 }, _catManager.OutGoingDataLoopDelay);
            else if (e.Mode == RadioMode.LSB)
                await _catManager.SendCatCommandAsync("EX", new object[] { "1107", 1 }, _catManager.OutGoingDataLoopDelay);
            else if (e.Mode == RadioMode.CW_U)
                await _catManager.SendCatCommandAsync("EX", new object[] { "0707", 0 }, _catManager.OutGoingDataLoopDelay);
            else if (e.Mode == RadioMode.CW_L)
                await _catManager.SendCatCommandAsync("EX", new object[] { "0707", 1 }, _catManager.OutGoingDataLoopDelay);
            else if (e.Mode == RadioMode.DATA_U)
                await _catManager.SendCatCommandAsync("EX", new object[] { "0812", 0 }, _catManager.OutGoingDataLoopDelay);
            else if (e.Mode == RadioMode.DATA_L)
                await _catManager.SendCatCommandAsync("EX", new object[] { "0812", 1 }, _catManager.OutGoingDataLoopDelay);
            else if (e.Mode == RadioMode.RTTY_U)
                await _catManager.SendCatCommandAsync("EX", new object[] { "1011", 0 }, _catManager.OutGoingDataLoopDelay);
            else if (e.Mode == RadioMode.RTTY_L)
                await _catManager.SendCatCommandAsync("EX", new object[] { "1011", 1 }, _catManager.OutGoingDataLoopDelay);

            await _catManager.SendCatCommandAsync("MD", new object[] { 0, ((int)Convert.ToInt16(catValue)).ToString("X") }, _catManager.OutGoingDataLoopDelay);

            await _catManager.SendCatCommandAsync("MD", "0", _catManager.OutGoingDataLoopDelay);
        }

        private void MainRigModeLabelBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {          
            if (modeUserControl.Visibility != Visibility.Visible)
            {
                // 1. Instant Show: Make it visible and reset opacity to full
                modeUserControl.ChangeMode(FT891S_CatManager.currentRadioState.OperatingMode);
                modeUserControl.Visibility = Visibility.Visible;
                modeUserControl.Opacity = 1.0;
            }
            else
            {
                FadoutUserControl(modeUserControl, 0);
            }
        }

        private void QRZExpandRetractButtonAreaCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (QRZGrid.Visibility == Visibility.Collapsed)
                QRZGrid.Visibility = Visibility;
            else
                QRZGrid.Visibility = Visibility.Collapsed;
        }

        private void QRZSent59Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            QRZSentTextBox.Text = "59";
        }

        private void QRZReceived59Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            QRZReceivedTextBox.Text = "59";
        }

        private async void QRZLogButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var qrzService = new QrzLogbookService();

            string myApiKey = "YOUR-QRZ-LOGBOOK-API-KEY"; // Retrieve this from settings

            // Construct a minimal ADIF string. 
            // Format: <fieldname:length>value
            string adifRecord = "<call:5>W1AW<band:3>20m<mode:3>SSB<qso_date:8>20260703<time_on:6>194500<station_callsign:6>ZZ1ZZZ";

            // Disable button to prevent double-clicks
            QRZLogButton.IsEnabled = false;
            QRZLogStatus.Content = "Uploading to QRZ...";

            string result = await qrzService.PushLogEntryAsync(myApiKey, adifRecord);

            // Re-enable button and display the raw response
            QRZLogButton.IsEnabled = true;
            QRZLogStatus.Content = result;

            // Note: A successful response from QRZ looks something like:
            // RESULT=OK&logid=12345678 or an XML structure depending on the key version.
        }

        bool ToggleScreen;
        private void RigExpandRetractButtonAreaCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ToggleScreen == false)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Force it into full grid space
                    Grid.SetRow(MainViewBox, 0);
                    Grid.SetColumn(MainViewBox, 0);
                    Grid.SetRowSpan(MainViewBox, 1);
                    Grid.SetColumnSpan(MainViewBox, 1);

                    // Ensure it is stretchable
                    MainViewBox.HorizontalAlignment = HorizontalAlignment.Stretch;
                    MainViewBox.VerticalAlignment = VerticalAlignment.Stretch;
                    MainViewBox.Stretch = Stretch.Uniform;
                }), System.Windows.Threading.DispatcherPriority.Loaded);

                ToggleScreen = true;
            }
            else
            {
                // Normal
                MainViewBox.Width = 1349;
                MainViewBox.Height = 452;
                MainViewBox.Stretch = Stretch.None;

                // Full screen
                MainViewBox.Width = double.NaN;
                MainViewBox.Height = double.NaN;
                MainViewBox.Stretch = Stretch.Uniform;

                ToggleScreen = false;
            }

            Debug.WriteLine(MainViewBox.ActualWidth);
            Debug.WriteLine(MainViewBox.ActualHeight);
        }

        private void MetroWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (!stationSeek.IsScanning)
            {

                if (e.Key == Key.Down)
                {
                    LargeFrequencyDisplay.Frequency -= 1000;

                    e.Handled = true; // Prevent the event from bubbling/scrolling parent containers
                }
                else if (e.Key == Key.Up)
                {
                    LargeFrequencyDisplay.Frequency += 1000;

                    e.Handled = true;
                }

            }
        }
    }
}