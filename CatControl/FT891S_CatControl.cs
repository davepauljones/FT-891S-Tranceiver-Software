using HamRadioControls;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Threading;
using YAESU_FT_891_Front_End; // NOTE: Ensure "WindowsBase" is in your Project References!
using static FT891S_CatControl.CatStructure;
using static YAESU_FT_891_Front_End.MainWindow;
using static YAESU_FT_891_Front_End.MyStructs;
using static YAESU_FT_891_Front_End.RigState;
using static YAESU_FT_891_Front_End.RigStateChanges;
using static YAESU_FT_891_Front_End.TranceiverDisplayModes;

namespace FT891S_CatControl
{
    // =========================================================================
    // 1. GLOBAL RADIO STATE VARIABLES (WPF / UI Reference Points)
    // =========================================================================
    public class RadioState
    {
        public long VfoAFrequency { get; set; }
        public long VfoALastFrequency { get; set; }
        public long VfoBFrequency { get; set; }
        public int MainRX { get; set; }
        public RadioMode OperatingMode { get; set; }
        public int AgcBandSelection { get; set; }
        public AgcMode CurrentAgcMode { get; set; }
        public int IfShiftHz { get; set; }
        public MeterTypes ActiveMeterType { get; set; }
        public int CurrentMeterReading { get; set; }
        public int BusyMode { get; set; }
        public int TXPowerWatts { get; set; } = 5;
        public int TXPowerWattsMinimum { get; } = 5;
        public int TXPowerWattsMaximum { get; } = 100;
        public int TXPowerWattsAMMaximum { get; } = 40;
        public int TXPowerWattsStep { get; } = 5;
        public int RFGain { get; set; } = 0;
        public int AFGain { get; set; } = 0;
        public long RadioID { get; set; }
    }

    // =========================================================================
    // 2. ENGINE, METADATA CORE & PARSER DICTIONARIES
    // =========================================================================
    public class CatParameter
    {
        public string Name { get; set; }
        public int Length { get; set; }

        public CatParameter(string name, int length)
        {
            Name = name;
            Length = length;
        }
    }

    public class CatStructure
    {
        public List<CatParameter> Parameters { get; set; }

        public CatStructure()
        {
            Parameters = new List<CatParameter>();
        }

        public CatStructure Expect(string name, int length)
        {
            Parameters.Add(new CatParameter(name, length));
            return this;
        }

        public interface ICatCommandDefinition
        {
            string OpCode { get; }
            CatStructure SetStructure { get; }
        }

        public Dictionary<string, string> Parse(string rawRadioData, string opCode)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            string payload = rawRadioData.Replace(";", "");

            if (payload.StartsWith(opCode))
            {
                payload = payload.Substring(opCode.Length);
            }

            int pos = 0;
            foreach (CatParameter param in Parameters)
            {
                if (pos + param.Length <= payload.Length)
                {
                    result[param.Name] = payload.Substring(pos, param.Length);
                    pos += param.Length;
                }
            }
            return result;
        }
    }

    public interface ICatCommand
    {
        void ParseAndApplyToGlobalState(string rawRadioData, Dispatcher wpfDispatcher);
    }

    public class FT891S_CatCommand<T> : ICatCommand, ICatCommandDefinition
    {
        public string OpCode { get; }
        public CatStructure SetStructure { get; }
        public CatStructure AnswerStructure { get; }
        public Func<Dictionary<string, string>, T> Parser { get; }
        private readonly Action<T> _stateUpdater;

        public FT891S_CatCommand(
            string opCode,
            CatStructure setStructure,
            CatStructure answerStructure,
            Func<Dictionary<string, string>, T> parser,
            Action<T> stateUpdater)
        {
            OpCode = opCode;
            SetStructure = setStructure;
            AnswerStructure = answerStructure;
            Parser = parser;
            _stateUpdater = stateUpdater;
        }

        public void ParseAndApplyToGlobalState(string rawRadioData, Dispatcher wpfDispatcher = null)
        {
            Dictionary<string, string> rawDictionary = AnswerStructure.Parse(rawRadioData, OpCode);
            T typedResult = Parser(rawDictionary);

            if (wpfDispatcher != null)
                wpfDispatcher.Invoke(() => _stateUpdater(typedResult));
            else
                _stateUpdater(typedResult);
        }
    }

    // Fluent Chaining Extension Engine (.NET 4.8 Compatibility Layout)
    public static class CatStructureExtensions
    {
        public class CatStringBuilder
        {
            private readonly string _opCode;
            private readonly CatStructure _structure;
            private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

            public CatStringBuilder(string opCode, CatStructure structure)
            {
                _opCode = opCode;
                _structure = structure;
            }

            public CatStringBuilder With(string paramName, object value)
            {
                _values[paramName] = value != null ? value.ToString() : "";
                return this;
            }

            public string Build()
            {
                StringBuilder sb = new StringBuilder(_opCode);
                foreach (CatParameter param in _structure.Parameters)
                {
                    if (!_values.TryGetValue(param.Name, out string val))
                        throw new ArgumentException("Missing required field: " + param.Name);

                    if (val.Length > param.Length)
                        val = val.Substring(0, param.Length);
                    else if (val.Length < param.Length && !val.StartsWith("+") && !val.StartsWith("-"))
                        val = val.PadLeft(param.Length, '0');

                    sb.Append(val);
                }
                return sb.Append(";").ToString();
            }
        }

        public static CatStringBuilder Prepare(this CatStructure structure, string opCode)
        {
            return new CatStringBuilder(opCode, structure);
        }
    }

    // =========================================================================
    // 3. TYPES, ENUMS & BACKING TARGET STRUCTURES
    // =========================================================================
    public enum RadioMode { LSB = 1, USB = 2, CW = 3, FM = 4, AM = 5, RTTY_LSB = 6, CW_R = 7, DATA_LSB = 8, RTTY_USB = 9, DATA_USB = 10, FM_N = 11, DATA_FM = 12 }
    public enum AgcMode { Off = 0, Fast = 1, Mid = 2, Slow = 3, Auto = 4 }

    public enum MeterTypes { DependsOnFrontPanelMETER = 0, S = 1, DependsOnFrontPanelMETER_PO_COMP_ALC_SWR_ID = 2, COMP = 3, ALC = 4, PO = 5, SWR = 6, ID = 7 }

    public class AgcResult
    {
        public int MainSubSelection { get; set; }
        public AgcMode Mode { get; set; }
    }

    public class MeterResult
    {
        public MeterTypes MeterType { get; set; }
        public int ReadingValue { get; set; }
    }

    public class ModeResult
    {
        public int MainRX { get; set; }
        public RadioMode Mode { get; set; }
    }

    // =========================================================================
    // 4. THE YAESU CONFIGURATION REGISTRY WITH GLOBAL ROUTER
    // =========================================================================
    public static class FT891S_CatCommandTypes
    {
        public static readonly FT891S_CatCommand<int> BY = new FT891S_CatCommand<int>(
            "BY",
            new CatStructure(), // SetStructure is empty ("BY;")
            new CatStructure().Expect("P1", 1).Expect("P2", 1), // Absorbs P1 (busy) and P2 (fixed 0)
            dict => int.Parse(dict["P1"]), // We only extract and care about P1!
            result => FT891S_CatManager.currentRadioState.BusyMode = result
        );

        public static readonly FT891S_CatCommand<long> FA = new FT891S_CatCommand<long>(
            "FA",
            new CatStructure().Expect("P1", 9),
            new CatStructure().Expect("P1", 9),
            dict => long.Parse(dict["P1"]),
            result => FT891S_CatManager.currentRadioState.VfoAFrequency = result
        );

        public static readonly FT891S_CatCommand<ModeResult> MD = new FT891S_CatCommand<ModeResult>(
            "MD",
            new CatStructure().Expect("P1", 1).Expect("P2", 1),
            new CatStructure().Expect("P1", 1).Expect("P2", 1),
            dict => new ModeResult
            {
                MainRX = int.Parse(dict["P1"]),
                Mode = (RadioMode)int.Parse(dict["P2"])
            },
            result =>
            {
                FT891S_CatManager.currentRadioState.MainRX = result.MainRX;
                FT891S_CatManager.currentRadioState.OperatingMode = result.Mode;
            }
        );

        public static readonly FT891S_CatCommand<AgcResult> GT = new FT891S_CatCommand<AgcResult>(
            "GT",
            new CatStructure().Expect("P1", 1).Expect("P2", 1),
            new CatStructure().Expect("P1", 1).Expect("P2", 1),
            dict => new AgcResult { MainSubSelection = int.Parse(dict["P1"]), Mode = (AgcMode)int.Parse(dict["P2"]) },
            result => {
                FT891S_CatManager.currentRadioState.AgcBandSelection = result.MainSubSelection;
                FT891S_CatManager.currentRadioState.CurrentAgcMode = result.Mode;
            }
        );

        public static readonly FT891S_CatCommand<int> SH = new FT891S_CatCommand<int>(
            "SH",
            new CatStructure().Expect("P1", 1).Expect("P2", 4),
            new CatStructure().Expect("P1", 1).Expect("P2", 4),
            dict => int.Parse(dict["P1"] + dict["P2"]),
            result => FT891S_CatManager.currentRadioState.IfShiftHz = result
        );

        public static readonly FT891S_CatCommand<MeterResult> RM = new FT891S_CatCommand<MeterResult>(
            "RM",
            // 1. Tell it to expect a 1-digit parameter when building the outbound command string
            new CatStructure().Expect("P1", 1),
        
            // 2. This remains the same for parsing the incoming response
            new CatStructure().Expect("P1", 1).Expect("P2", 3),
        
            dict => new MeterResult
            {
                MeterType = (MeterTypes)int.Parse(dict["P1"]),
                ReadingValue = int.Parse(dict["P2"])
            },
            result => {
                FT891S_CatManager.currentRadioState.ActiveMeterType = result.MeterType;
                FT891S_CatManager.currentRadioState.CurrentMeterReading = result.ReadingValue;
            }
        );

        public static readonly FT891S_CatCommand<int> PC = new FT891S_CatCommand<int>(
            "PC",
            new CatStructure().Expect("P1", 3),
            new CatStructure().Expect("P1", 3),
            dict => int.Parse(dict["P1"]),
            result => FT891S_CatManager.currentRadioState.TXPowerWatts = result
        );

        public static readonly FT891S_CatCommand<long> ID = new FT891S_CatCommand<long>(
            "ID",
            new CatStructure(),
            new CatStructure().Expect("P1", 4),
            dict => long.Parse(dict["P1"]),
            result => FT891S_CatManager.currentRadioState.RadioID = result
        );

        public static readonly FT891S_CatCommand<int> RG = new FT891S_CatCommand<int>(
            "RG",
            new CatStructure().Expect("P1", 1).Expect("P2", 3),
            new CatStructure().Expect("P1", 1).Expect("P2", 3),
            dict => int.Parse(dict["P1"] + dict["P2"]),
            result => FT891S_CatManager.currentRadioState.RFGain = result
        );
        public static readonly FT891S_CatCommand<int> AG = new FT891S_CatCommand<int>(
            "AG",
            new CatStructure().Expect("P1", 1).Expect("P2", 3),
            new CatStructure().Expect("P1", 1).Expect("P2", 3),
            dict => int.Parse(dict["P1"] + dict["P2"]),
            result => FT891S_CatManager.currentRadioState.AFGain = result
        );

        public static readonly Dictionary<string, ICatCommand> ParsersByOpCode = new Dictionary<string, ICatCommand>()
        {
            { "BY", BY },
            { "FA", FA },
            { "MD", MD },
            { "GT", GT },
            { "SH", SH },
            { "RM", RM },
            { "PC", PC },
            { "ID", ID },
            { "RG", RG },
            { "AG", AG }
        };

        public static void ProcessIncomingRadioData(string rawRadioData, Dispatcher wpfDispatcher = null)
        {
            if (string.IsNullOrEmpty(rawRadioData) || rawRadioData.Length < 2) return;

            string opCode = rawRadioData.Substring(0, 2);

            if (ParsersByOpCode.TryGetValue(opCode, out ICatCommand processingCommand))
            {
                processingCommand.ParseAndApplyToGlobalState(rawRadioData, wpfDispatcher);
            }
        }
    }

    // =========================================================================
    // 5. PRODUCTION MANAGER CONSTRUCTOR PATTERN
    // =========================================================================
    public class FT891S_CatManager
    {
        MainWindow mainWindow;
        private Dispatcher _uiDispatcher;

        public CancellationTokenSource _serialCts;
        public Task _serialTask;

        public static RadioState currentRadioState = new RadioState();

        /// <summary>
        /// Instantiates the CAT manager engine.
        /// </summary>
        /// <param name="currentDispatcher">Pass 'this.Dispatcher' from your WPF Window context here.</param>
        public FT891S_CatManager(MainWindow mainWindow, Dispatcher currentDispatcher)
        {
            // Cache the WPF UI synchronization context
            this.mainWindow = mainWindow;
            _uiDispatcher = currentDispatcher;
        }

        public async Task SendCatCommandAsync(string opCode, string manualParameters, int delayMs = 0)
        {
            // 1. Validation: Ensure the OpCode is actually registered in your dictionary
            if (!FT891S_CatCommandTypes.ParsersByOpCode.TryGetValue(opCode, out ICatCommand cmd))
                throw new ArgumentException($"Unknown CAT command: {opCode}");

            // 2. Formatting: Construct the string without manual string concatenation in your logic
            // We trim any existing semicolons from the parameters to ensure we only have one at the end.
            string paramsClean = (manualParameters ?? "").Replace(";", "").Trim();
            string catString = $"{opCode.ToUpper()}{paramsClean};";

            // 3. Serial Port Communication
            if (mainWindow.fT891S_SerialPort._port != null &&
                mainWindow.fT891S_SerialPort._port.IsOpen)
            {
                mainWindow.fT891S_SerialPort._port.Write(catString);
            }

            // 4. Maintenance & UI Counters (Matching your pattern)
            if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.CurrentDebug)
            {
                Console.Write(">");
                Console.WriteLine(catString);
            }

            mainWindow.packetManagement.currentSendCATCommand = catString;
            mainWindow.packetManagement.UpdateSendFPS();

            if (delayMs > 0)
                await Task.Delay(delayMs);
        }

        public async Task SendCatCommandAsync(string opCode, int delayMs = 0)
        {
            if (!FT891S_CatCommandTypes.ParsersByOpCode.TryGetValue(opCode, out ICatCommand cmd))
                throw new ArgumentException($"Unknown CAT command: {opCode}");

            var definition = cmd as ICatCommandDefinition;

            if (definition == null)
                throw new InvalidOperationException($"Command {opCode} has no definition.");

            CatStructure structure = definition.SetStructure;

            string catString = opCode.Trim().ToUpper() + ";";

            if (mainWindow.fT891S_SerialPort._port != null &&
                mainWindow.fT891S_SerialPort._port.IsOpen)
            {
                mainWindow.fT891S_SerialPort._port.Write(catString);
            }

            if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.CurrentDebug)
            {
                Console.Write(">");
                Console.WriteLine(catString);
            }

            mainWindow.packetManagement.currentSendCATCommand = catString;
            mainWindow.packetManagement.UpdateSendFPS();

            if (delayMs > 0)
                await Task.Delay(delayMs);
        }
        public async Task SendCatCommandAsync(string opCode, object[] values, int delayMs = 0)
        {
            if (!FT891S_CatCommandTypes.ParsersByOpCode.TryGetValue(opCode, out ICatCommand cmd))
                throw new ArgumentException($"Unknown CAT command: {opCode}");

            var definition = (ICatCommandDefinition)cmd;

            CatStructure structure = definition.SetStructure;

            var builder = structure.Prepare(opCode);

            if (structure.Parameters.Count != values.Length)
                throw new ArgumentException(
                    $"CAT {opCode} expects {structure.Parameters.Count} values, got {values.Length}");

            for (int i = 0; i < values.Length; i++)
            {
                builder.With(structure.Parameters[i].Name, values[i]);
            }

            string catString = builder.Build();

            if (mainWindow.fT891S_SerialPort._port != null &&
                mainWindow.fT891S_SerialPort._port.IsOpen)
            {
                mainWindow.fT891S_SerialPort._port.Write(catString);
            }

            if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.CurrentDebug)
            {
                Console.Write(">");
                Console.WriteLine(catString);
            }

            mainWindow.packetManagement.currentSendCATCommand = catString;
            mainWindow.packetManagement.UpdateSendFPS();

            if (delayMs > 0)
                await Task.Delay(delayMs);
        }

        private void DoTranceiverMode_Main()
        {
            mainWindow.frequencyManagement.SetFrequencyUI(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, currentRadioState.VfoAFrequency, mainWindow.MainFrequencyTextBlock);

            UpdateUIRigMode(mainWindow.MainRigModeLabelBorder, mainWindow.MainRigModeLabel, currentRadioState.OperatingMode);

            //RM READ METER
            switch (currentRadioState.ActiveMeterType)
            {
                case MeterTypes.DependsOnFrontPanelMETER:
                    mainWindow.UpdateMeter(mainWindow.BarGraphRectangle, currentRadioState.CurrentMeterReading);
                    mainWindow.stationSeek.LastSMeterRawReading = currentRadioState.CurrentMeterReading;
                    mainWindow.stationSeek.LastSMeterReading = MainWindow.GetSMeterInteger(currentRadioState.CurrentMeterReading);

                    mainWindow.SignalMeter.Value = AnalogMeter.ConvertDoubleToPercentage(Convert.ToDouble(currentRadioState.CurrentMeterReading));

                    byte signalStrength = Convert.ToByte(AnalogMeter.ConvertDoubleToPercentage(Convert.ToDouble(currentRadioState.CurrentMeterReading)));
                    mainWindow.sprite.GenerateBandScopeSprite(Convert.ToByte(currentRadioState.CurrentMeterReading), 299, 1);
                    mainWindow.sprite.GenerateHistorySprite(signalStrength, 299, 2);

                    if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
                    {
                        Console.WriteLine("meterReading = " + currentRadioState.CurrentMeterReading);
                    }

                    break;
                case MeterTypes.S:
                    mainWindow.UpdateMeter(mainWindow.BarGraphRectangle, currentRadioState.CurrentMeterReading);
                    mainWindow.stationSeek.LastSMeterRawReading = currentRadioState.CurrentMeterReading;
                    mainWindow.stationSeek.LastSMeterReading = MainWindow.GetSMeterInteger(currentRadioState.CurrentMeterReading);

                    mainWindow.SignalMeter.Value = AnalogMeter.ConvertDoubleToPercentage(Convert.ToDouble(currentRadioState.CurrentMeterReading));

                    signalStrength = Convert.ToByte(AnalogMeter.ConvertDoubleToPercentage(Convert.ToDouble(currentRadioState.CurrentMeterReading)));
                    mainWindow.sprite.GenerateBandScopeSprite(signalStrength, 295, 2);
                    mainWindow.sprite.GenerateHistorySprite(signalStrength, 295, 2);

                    if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
                    {
                        Console.WriteLine("meterReading = " + currentRadioState.CurrentMeterReading);
                    }

                    break;
                case MeterTypes.COMP:
                    mainWindow.UpdateMeter(mainWindow.BarGraphRectangle, currentRadioState.CurrentMeterReading);
                    break;
                case MeterTypes.ALC:
                    mainWindow.UpdateMeter(mainWindow.ALCBarGraphRectangle, currentRadioState.CurrentMeterReading);
                    break;
                case MeterTypes.PO:
                    mainWindow.UpdateMeter(mainWindow.POBarGraphRectangle, currentRadioState.CurrentMeterReading);
                    if (currentRadioState.CurrentMeterReading > 0)
                        mainWindow.UpdateTranceiverTXRXState(TranceiverStates.RadioTXOn);
                    else
                        mainWindow.UpdateTranceiverTXRXState(TranceiverStates.RadioTXOff);

                    mainWindow.SignalMeter.Value = currentRadioState.CurrentMeterReading;
                    break;
                case MeterTypes.SWR:
                    mainWindow.UpdateMeter(mainWindow.SWRBarGraphRectangle, currentRadioState.CurrentMeterReading);
                    mainWindow.SignalMeter.Value = MainWindow.GetSMeterInteger(currentRadioState.CurrentMeterReading);
                    break;
                case MeterTypes.ID:
                    mainWindow.UpdateMeter(mainWindow.IDDBarGraphRectangle, currentRadioState.CurrentMeterReading);
                    break;
            }
            //RM READ METER

            //BY BUSY
            if (currentRadioState.OperatingMode == RadioMode.FM && mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOff)
            {
                if (currentRadioState.BusyMode == 0)
                    mainWindow.SetRigLEDColor(RigLEDColors.LightGray);
                else if (currentRadioState.BusyMode == 1)
                    mainWindow.SetRigLEDColor(RigLEDColors.Green);
            }
            //BY BUSY

            //PC POWER CONTROL
            mainWindow.PowerControlLabel.Content = currentRadioState.TXPowerWatts.ToString() + "W";
            mainWindow.RfPowerFunctionTextBlock.Text = currentRadioState.TXPowerWatts.ToString() + "W";
            //PC POWER CONTROL
        }

        /// <summary>
        /// Public endpoint to feed streaming responses directly from your serial port pipeline.
        /// </summary>
        public void HandleIncomingData(string serialMessageLine)
        {
            if (string.IsNullOrEmpty(serialMessageLine)) return;

            if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.CurrentDebug)
            {
                Console.Write("<");
                Console.WriteLine(serialMessageLine);
            }

            // Routes through layouts and updates global properties safely on your UI Thread
            FT891S_CatCommandTypes.ProcessIncomingRadioData(serialMessageLine, _uiDispatcher);

            switch (TranceiverMode)
            {
                case TranceiverModes.RadioIDCheck:
                    if (currentRadioState.RadioID == 650)
                    {
                        mainWindow.RadioIDTextBlock.Text = "FT-891";
                        mainWindow.RadioIDAmberLED.Opacity = 0.5;
                    }
                    else if (currentRadioState.RadioID > 0 && currentRadioState.RadioID < 9999)
                    {
                        mainWindow.RadioIDTextBlock.Text = "??????";
                        mainWindow.RadioIDAmberLED.Opacity = 0.2;
                    }
                    break;
                case TranceiverModes.Main:
                    DoTranceiverMode_Main();
                    break;
                case TranceiverModes.StationScope:
                    if (!(mainWindow.stationSeek.IsScanning))
                    {
                        mainWindow.frequencyManagement.SetFrequencyUI(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, currentRadioState.VfoAFrequency, mainWindow.MainFrequencyTextBlock);
                    }
                    break;
                case TranceiverModes.NoiseFilters:
                    mainWindow.frequencyManagement.SetFrequency(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, currentRadioState.VfoAFrequency, mainWindow.MainFrequencyTextBlock);
                    break;
                case TranceiverModes.CWDecoder:
                    mainWindow.frequencyManagement.SetFrequency(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, currentRadioState.VfoAFrequency, mainWindow.MainFrequencyTextBlock);
                    break;
            }

            if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
            {
                Console.Write("currentRadioState.VfoAFrequency = ");
                Console.WriteLine(currentRadioState.VfoAFrequency);
                Console.Write("currentRadioState.OperatingMode = ");
                Console.WriteLine(currentRadioState.OperatingMode);
                Console.Write("currentRadioState.ActiveMeterType = ");
                Console.WriteLine(currentRadioState.ActiveMeterType);
                Console.Write("currentRadioState.CurrentMeterReading = ");
                Console.WriteLine(currentRadioState.CurrentMeterReading);
                Console.Write("currentRadioState.BusyMode = ");
                Console.WriteLine(currentRadioState.BusyMode);
                Console.Write("currentRadioState.TXPowerWatts = ");
                Console.WriteLine(currentRadioState.TXPowerWatts);
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                // FIX: Pass the raw string line parameter down to the FPS calculation directly
                mainWindow.packetManagement.UpdateReceiveFPS(serialMessageLine);
            });
        }

        public bool OutGoingDataLoop_IsRunning = false;
        public void StartOutgoingDataLoop()
        {
            if (OutGoingDataLoop_IsRunning) return;

            mainWindow.SendOnOffTextBlock.Text = "ON";

            OutGoingDataLoop_IsRunning = true;
            
            _serialCts = new CancellationTokenSource();
            _serialTask = Task.Run(() => OutgoingDataLoop(_serialCts.Token));
        }
        public void StopOutgoingDataLoop()
        {
            if (!(OutGoingDataLoop_IsRunning)) return;

            mainWindow.SendOnOffTextBlock.Text = "OFF";

            OutGoingDataLoop_IsRunning = false;

            _serialCts?.Cancel();   
        }

        DateTime lowPriorityDateTime = DateTime.Now;
        TimeSpan lowPriorityCATCommandsTimeSpan = TimeSpan.FromSeconds(15);

        public int OutGoingDataLoopDelay = 5;
        public async Task OutgoingDataLoop(CancellationToken token)
        {
            if (!(OutGoingDataLoop_IsRunning)) return;

            int packet = 0;

            while (!token.IsCancellationRequested)
            {
                if (packet == 8)
                    packet = 0;

                switch (TranceiverMode)
                {
                    case TranceiverModes.RadioIDCheck:
                        //SendReadQuery("ID");
                        await SendCatCommandAsync("ID", OutGoingDataLoopDelay);
                        break;
                    case TranceiverModes.Main:
                        if (!(mainWindow.waterFallSweep.SweepActive))
                        {
                            await SendCatCommandAsync("BY", OutGoingDataLoopDelay);

                            await SendCatCommandAsync("FA", OutGoingDataLoopDelay);

                            await SendCatCommandAsync("MD", "0", OutGoingDataLoopDelay);

                            await SendCatCommandAsync("PC", OutGoingDataLoopDelay);

                            await SendCatCommandAsync("RM", new object[] { (int)MeterTypes.PO }, 5);


                            if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOff)
                            {
                                await SendCatCommandAsync("RM", new object[] { (int)MeterTypes.DependsOnFrontPanelMETER }, 5);
                            }

                            switch (packet)
                            {
                                case 0:
                                    if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOn)
                                    {
                                        await SendCatCommandAsync("RM", new object[] { (int)MeterTypes.DependsOnFrontPanelMETER_PO_COMP_ALC_SWR_ID }, 5);
                                    }
                                    break;
                                case 1:
                                    if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOn)
                                    {
                                        await SendCatCommandAsync("RM", new object[] { (int)MeterTypes.COMP }, 5);
                                    }
                                    break;
                                case 2:
                                    if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOn)
                                    {
                                        await SendCatCommandAsync("RM", new object[] { (int)MeterTypes.ALC }, 5);
                                    }
                                    break;
                                case 3:
                                    if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOn)
                                    {
                                        await SendCatCommandAsync("RM", new object[] { (int)MeterTypes.PO }, 5);
                                    }
                                    break;
                                case 4:
                                    if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOn)
                                    {
                                        await SendCatCommandAsync("RM", new object[] { (int)MeterTypes.SWR }, 5);
                                    }
                                    break;
                                case 5:
                                    if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOn)
                                    {
                                        await SendCatCommandAsync("RM", new object[] { (int)MeterTypes.ID }, 5);
                                    }
                                    break;
                            }

                            if (mainWindow.waterFallSweep.ScopeOnOff == true)
                            {
                                if (DateTime.Now > lowPriorityDateTime + lowPriorityCATCommandsTimeSpan)
                                {
                                    if (mainWindow.Dispatcher.CheckAccess())
                                    {
                                        // We are already on the UI thread! Run it directly.
                                        mainWindow.waterFallSweep.Sweep(14252500, 14380000, 500, 6);
                                    }
                                    else
                                    {
                                        // We are on a background thread. Marshal it over.
                                        await mainWindow.Dispatcher.BeginInvoke(new Action(() => mainWindow.waterFallSweep.Sweep(14252500, 14380000, 500, 6)));
                                    }

                                    lowPriorityDateTime = DateTime.Now;
                                }
                            }
                        }
                        break;
                    case TranceiverModes.StationScope:
                        if (!(mainWindow.stationSeek.IsScanning))
                        {
                            await SendCatCommandAsync("FA", OutGoingDataLoopDelay);
                        }

                        break;
                    case TranceiverModes.NoiseFilters:
                        await SendCatCommandAsync("FA", OutGoingDataLoopDelay);

                        break;
                    case TranceiverModes.CWDecoder:
                        await SendCatCommandAsync("FA", OutGoingDataLoopDelay);

                        break;
                }
                packet++;
                //Console.WriteLine("OutgoingDataLoop");
            }
        }

    }
}