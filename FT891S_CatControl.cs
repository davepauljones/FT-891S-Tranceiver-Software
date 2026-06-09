using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using YAESU_FT_891_Front_End; // NOTE: Ensure "WindowsBase" is in your Project References!
using static YAESU_FT_891_Front_End.RigState;
using static YAESU_FT_891_Front_End.RigStateChanges;
using static YAESU_FT_891_Front_End.TranceiverDisplayModes;
using HamRadioControls;
using static YAESU_FT_891_Front_End.MyStructs;

namespace FT891S_CatControl
{
    // =========================================================================
    // 1. GLOBAL RADIO STATE VARIABLES (WPF / UI Reference Points)
    // =========================================================================
    public class RadioState
    {
        public long VfoAFrequency { get; set; }
        public long VfoBFrequency { get; set; }
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

    public class FT891S_CatCommand<T> : ICatCommand
    {
        public string OpCode { get; }
        public CatStructure SetStructure { get; }
        public CatStructure AnswerStructure { get; }
        public Func<Dictionary<string, string>, T> Parser { get; }
        private readonly Action<T> _stateUpdater;

        public FT891S_CatCommand(string opCode, CatStructure setStructure, CatStructure answerStructure, Func<Dictionary<string, string>, T> parser, Action<T> stateUpdater)
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
            {
                wpfDispatcher.Invoke(new Action(() => _stateUpdater(typedResult)));
            }
            else
            {
                _stateUpdater(typedResult);
            }
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

        public static readonly FT891S_CatCommand<RadioMode> MD = new FT891S_CatCommand<RadioMode>(
            "MD",
            new CatStructure().Expect("P1", 2),
            new CatStructure().Expect("P1", 2),
            dict => (RadioMode)int.Parse(dict["P1"]),
            result => FT891S_CatManager.currentRadioState.OperatingMode = result
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
            new CatStructure(), // SetStructure is empty ("RM0;")
            new CatStructure().Expect("P1", 1).Expect("P2", 3), // P1 is meter type, P2 is 3-digit value
            dict => new MeterResult
            {
                // Parse P1 directly into your enum (e.g., "1" becomes MeterTypes.S)
                MeterType = (MeterTypes)int.Parse(dict["P1"]),
        
                // Parse P2 directly into an integer value (e.g., "045" becomes 45)
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

        public static readonly Dictionary<string, ICatCommand> ParsersByOpCode = new Dictionary<string, ICatCommand>()
        {
            { "BY", BY },
            { "FA", FA },
            { "MD", MD },
            { "GT", GT },
            { "SH", SH },
            { "RM", RM },
            { "PC", PC }
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

        // ---> ADD THE NEW METHOD HERE <---
        /// <summary>
        /// Sends a direct status read request to the radio (e.g., "FA;" or "MD;")
        /// </summary>
        public void SendReadQuery(string opCode)
        {
            if (mainWindow.fT891S_SerialPort._port != null && mainWindow.fT891S_SerialPort._port.IsOpen)
            {
                // Enforce the Yaesu standard format: OpCode + Semicolon
                string query = opCode.Trim().ToUpper() + ";";
                mainWindow.fT891S_SerialPort._port.Write(query);
            }
        }

        /// <summary>
        /// Public endpoint to feed streaming responses directly from your serial port pipeline.
        /// </summary>
        public void HandleIncomingData(string serialMessageLine)
        {
            if (string.IsNullOrEmpty(serialMessageLine)) return;

            // Routes through layouts and updates global properties safely on your UI Thread
            FT891S_CatCommandTypes.ProcessIncomingRadioData(serialMessageLine, _uiDispatcher);

            

            switch (TranceiverMode)
            {
                case TranceiverModes.Main:
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

                            byte spriteWidth = Convert.ToByte(AnalogMeter.ConvertDoubleToPercentage(Convert.ToDouble(currentRadioState.CurrentMeterReading)));
                            mainWindow.sprite.GenerateSprite(spriteWidth, 295, 2);

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

                            byte spriteWidth2 = Convert.ToByte(AnalogMeter.ConvertDoubleToPercentage(Convert.ToDouble(currentRadioState.CurrentMeterReading)));
                            mainWindow.sprite.GenerateSprite(spriteWidth2, 295, 2);

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

                            //change to correct meter some day
                            mainWindow.SignalMeter.Value = currentRadioState.CurrentMeterReading;

                            break;
                        case MeterTypes.SWR:
                            mainWindow.UpdateMeter(mainWindow.SWRBarGraphRectangle, currentRadioState.CurrentMeterReading);

                            //change to correct meter some day
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

                    break;
                case TranceiverModes.StationScope:
                    mainWindow.frequencyManagement.SetFrequency(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, currentRadioState.VfoAFrequency, mainWindow.MainFrequencyTextBlock);

                    break;
                case TranceiverModes.NoiseFilters:
                    mainWindow.frequencyManagement.SetFrequency(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, currentRadioState.VfoAFrequency, mainWindow.MainFrequencyTextBlock);

                    break;
                case TranceiverModes.CWDecoder:
                    mainWindow.frequencyManagement.SetFrequency(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, currentRadioState.VfoAFrequency, mainWindow.MainFrequencyTextBlock);

                    break;
            }

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

        public void StartOutgoingDataLoop()
        {
            _serialCts = new CancellationTokenSource();
            _serialTask = Task.Run(() => OutgoingDataLoop(_serialCts.Token));
        }
        public void StopOutgoingDataLoop()
        {
            _serialCts?.Cancel();
        }

        public int OutGoingDataLoopDelay = 5;
        public async Task OutgoingDataLoop(CancellationToken token)
        {
            int packet = 0;

            while (!token.IsCancellationRequested)
            {
                if (packet == 8)
                    packet = 0;

                switch (TranceiverMode)
                {
                    case TranceiverModes.Main:
                        SendReadQuery("BY");
                        await Task.Delay(OutGoingDataLoopDelay);

                        SendReadQuery("FA");
                        await Task.Delay(OutGoingDataLoopDelay);

                        SendReadQuery("MD0");
                        await Task.Delay(OutGoingDataLoopDelay);

                        SendReadQuery("PC");
                        await Task.Delay(OutGoingDataLoopDelay);

                        SendReadQuery("RM5");
                        await Task.Delay(OutGoingDataLoopDelay);

                        if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOff)
                        {
                            SendReadQuery("RM0");
                            await Task.Delay(OutGoingDataLoopDelay);
                        }

                        switch (packet)
                        {
                            case 0:
                                if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOn)
                                {
                                    SendReadQuery("RM2");
                                    await Task.Delay(OutGoingDataLoopDelay);
                                }
                                break;
                            case 1:
                                if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOn)
                                {
                                    SendReadQuery("RM3");
                                    await Task.Delay(OutGoingDataLoopDelay);
                                }
                                break;
                            case 2:
                                if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOn)
                                {
                                    SendReadQuery("RM4");
                                    await Task.Delay(OutGoingDataLoopDelay);
                                }
                                break;
                            case 3:
                                if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOn)
                                {
                                    SendReadQuery("RM5");
                                    await Task.Delay(OutGoingDataLoopDelay);
                                }
                                break;
                            case 4:
                                if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOn)
                                {
                                    SendReadQuery("RM6");
                                    await Task.Delay(OutGoingDataLoopDelay);
                                }
                                break;
                            case 5:
                                if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOn)
                                {
                                    SendReadQuery("RM7");
                                    await Task.Delay(OutGoingDataLoopDelay);
                                }
                                break;
                        }
                        break;
                    case TranceiverModes.StationScope:
                        SendReadQuery("FA");
                        await Task.Delay(OutGoingDataLoopDelay);

                        break;
                    case TranceiverModes.NoiseFilters:
                        SendReadQuery("FA");
                        await Task.Delay(OutGoingDataLoopDelay);

                        break;
                    case TranceiverModes.CWDecoder:
                        SendReadQuery("FA");
                        await Task.Delay(OutGoingDataLoopDelay);

                        break;
                }
                packet++;
                //Console.WriteLine("OutgoingDataLoop");
            }
        }

    }
}