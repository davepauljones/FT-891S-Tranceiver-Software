using HamRadioControls;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
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
using YAESU_FT_891_Front_End.Models;
using static FT891S_CatControl.CatStructure;
using static YAESU_FT_891_Front_End.Animations;
using static YAESU_FT_891_Front_End.HelperFunctions;
using static YAESU_FT_891_Front_End.MainWindow;
using static YAESU_FT_891_Front_End.MyStructs;
using static YAESU_FT_891_Front_End.RigStateChanges;
using static YAESU_FT_891_Front_End.TranceiverDisplayModes;

namespace FT891S_CatControl
{
    // =========================================================================
    // 1. GLOBAL RADIO STATE VARIABLES (WPF / UI Reference Points)
    // =========================================================================
    public class IS_Shift
    {
        public int Fixed;
        public int OnOff;
        public int IfShiftDirection;
        public int IfShiftHz;
    }
    public class RadioState
    {
        public long VfoAFrequency { get; set; }
        public long VfoALastFrequency { get; set; }
        public long VfoBFrequency { get; set; }
        public int MainRX { get; set; }
        public int currentBand { get; set; }
        public RadioMode OperatingMode { get; set; }
        public int AgcBandSelection { get; set; }
        public AgcMode CurrentAgcMode { get; set; }
        public IS_Shift IfShiftHz { get; set; }
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
        public int SMeter { get; set; }
        public ScanMode ScanMode { get; set; }
        public NBMode NoiseBlankerMode { get; set; }
        public int NoiseBlankerLevel { get; set; } = 0;
        public NRMode NoiseReductionMode { get; set; }
        public DNRValues NoiseReductionLevel { get; set; } = 0;

        public WidthModes WidthMode { get; set; }
        public WidthValues WidthValue { get; set; } = 0;
        
        public NotchModes NotchMode { get; set; }
        public int NotchValue { get; set; } = 0;

        public FastStep FastStep { get; set; }

        public String ComPort { get; set; }
        public int DeveloperMode { get; set; } = (int)DeveloperModes.DeveloperMode_OFF;
        public String CallSign { get; set; }

        public PowerSwitchModes PowerSwitch { get; set; } = (int)PowerSwitchModes.PowerSwitchMode_OFF;
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

        public void ApplyOutgoingValues(object[] values)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            for (int i = 0; i < SetStructure.Parameters.Count; i++)
            {
                dict[SetStructure.Parameters[i].Name] =
                    values[i]?.ToString() ?? "";
            }

            T result = Parser(dict);

            _stateUpdater(result);
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
    public enum AgcMode { Off = 0, Fast = 1, Mid = 2, Slow = 3, Auto = 4 }

    public enum MeterTypes { DependsOnFrontPanelMETER = 0, S = 1, DependsOnFrontPanelMETER_PO_COMP_ALC_SWR_ID = 2, COMP = 3, ALC = 4, PO = 5, SWR = 6, ID = 7 }

    public enum ScanMode { Scan_OFF = 0, ScanUpward_ON = 1, ScanDownward_ON= 2 }

    public enum NBMode { NB_OFF = 0, NB_ON = 1 }
    public enum NRMode { NR_OFF = 0, NR_ON = 1 }
    public enum WidthModes { SH_OFF = 0, SH_ON = 1 }
    public enum NotchModes { ManualNotchOnOff = 0, ManualNotchFrequency = 1 }

    public enum DNRValues { DNR_01 = 1, DNR_02 = 2, DNR_03 = 3, DNR_04 = 4, DNR_05 = 5,
                            DNR_06 = 6, DNR_07 = 7, DNR_08 = 8, DNR_09 = 9, DNR_10 = 10,
                            DNR_11 = 11, DNR_12 = 12, DNR_13 = 13, DNR_14 = 14, DNR_15 = 15 }

    public enum WidthValues
    {
        WDH_00_Default = 0,
        WDH_01 = 1, WDH_02 = 2, WDH_03 = 3, WDH_04 = 4, WDH_05 = 5,
        WDH_06 = 6, WDH_07 = 7, WDH_08 = 8, WDH_09 = 9, WDH_10 = 10,
        WDH_11 = 11, WDH_12 = 12, WDH_13 = 13, WDH_14 = 14, WDH_15 = 15,
        WDH_16 = 16, WDH_17 = 17, WDH_18 = 18, WDH_19 = 19, WDH_20 = 20,
        WDH_21 = 21
    }

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
    public class DnrResult
    {
        public int Fixed { get; set; }
        public DNRValues Values { get; set; }
    }
    public class WidthResult
    {
        public int Fixed { get; set; }
        public WidthModes Switch { get; set; }
        public WidthValues Values { get; set; }
    }
    public class NotchResult
    {
        public int Fixed { get; set; }
        public NotchModes Switch { get; set; }
        public int Values { get; set; }
    }
    public class IfShiftResult
    {
        public int Fixed { get; set; }
        public int OnOff { get; set; }// on the 710 it is just Fixed at 0
        public int IfShiftDirection { get; set; }// +/- it needs to see a character "+" or "-"
        public int IfShiftHz { get; set; }// SHift from 0-1200Hz in 20Hz steps
    }
    public enum FastStep { FastStep_OFF = 0, FastStep_ON = 1 }

    public enum DeveloperModes { DeveloperMode_OFF = 0, DeveloperMode_ON = 1 }
    public enum PowerSwitchModes { PowerSwitchMode_OFF = 0, PowerSwitchMode_ON = 1 }

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
                // FIX: Parse P2 as a Hexadecimal string to support A, B, C, etc.
                //Mode = (RadioMode)int.Parse(dict["P2"], System.Globalization.NumberStyles.HexNumber)
                Mode = _modeMapper.FromCAT(byte.Parse(dict["P2"], System.Globalization.NumberStyles.HexNumber))
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

        //public static readonly FT891S_CatCommand<int> SH = new FT891S_CatCommand<int>(
        //    "SH",
        //    new CatStructure().Expect("P1", 1).Expect("P2", 4),
        //    new CatStructure().Expect("P1", 1).Expect("P2", 4),
        //    dict => int.Parse(dict["P1"] + dict["P2"]),
        //    result => FT891S_CatManager.currentRadioState.IfShiftHz = result
        //);

        public static readonly FT891S_CatCommand<MeterResult> RM = new FT891S_CatCommand<MeterResult>(
            "RM",

            // 1. Outbound layout
            new CatStructure().Expect("P1", 1),

            // 2. Inbound layout
            new CatStructure().Expect("P1", 1).Expect("P2", 3),

            // FIX: Check if P2 exists to avoid KeyNotFoundException during local state updates
            dict => new MeterResult
            {
                MeterType = (MeterTypes)int.Parse(dict["P1"]),
                ReadingValue = dict.ContainsKey("P2") ? int.Parse(dict["P2"]) : 0
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
        public static readonly FT891S_CatCommand<int> BS = new FT891S_CatCommand<int>(
            "BS",
            new CatStructure().Expect("P1", 2),
            new CatStructure(),
            dict => int.Parse(dict["P1"]),
            result => FT891S_CatManager.currentRadioState.currentBand = result
        );
        public static readonly FT891S_CatCommand<int> AB = new FT891S_CatCommand<int>(
            "AB",
            new CatStructure(),
            new CatStructure(),
            dict => 0,
            result => { }
        );
        public static readonly FT891S_CatCommand<int> BA = new FT891S_CatCommand<int>(
            "BA",
            new CatStructure(),
            new CatStructure(),
            dict => 0,
            result => { }
        );
        public static readonly FT891S_CatCommand<int> EX = new FT891S_CatCommand<int>(
            "EX",
            new CatStructure().Expect("P1", 4).Expect("P2", 1),
            new CatStructure().Expect("P1", 4).Expect("P2", 1),
            dict => int.Parse(dict["P1"] + dict["P2"]),
            result => { }
        );
        public static readonly FT891S_CatCommand<int> SC = new FT891S_CatCommand<int>(
            "SC",
            new CatStructure().Expect("P1", 1),
            new CatStructure().Expect("P1", 1),
            dict => int.Parse(dict["P1"]),
            result => FT891S_CatManager.currentRadioState.ScanMode = (ScanMode)result
        );
        public static readonly FT891S_CatCommand<int> NB = new FT891S_CatCommand<int>(
            "NB",
            new CatStructure().Expect("P1", 1).Expect("P2", 1),
            new CatStructure().Expect("P1", 1).Expect("P2", 1),
            dict => int.Parse(dict["P1"] + dict["P2"]),
            result => FT891S_CatManager.currentRadioState.NoiseBlankerMode = (NBMode)result
        );
        public static readonly FT891S_CatCommand<int> NL = new FT891S_CatCommand<int>(
            "NL",
            new CatStructure().Expect("P1", 1).Expect("P2", 3),
            new CatStructure().Expect("P1", 1).Expect("P2", 3),
            dict => int.Parse(dict["P1"] + dict["P2"]),
            result => FT891S_CatManager.currentRadioState.NoiseBlankerLevel = result
        );
        public static readonly FT891S_CatCommand<int> NR = new FT891S_CatCommand<int>(
            "NR",
            new CatStructure().Expect("P1", 1).Expect("P2", 1),
            new CatStructure().Expect("P1", 1).Expect("P2", 1),
            dict => int.Parse(dict["P1"] + dict["P2"]),
            result => FT891S_CatManager.currentRadioState.NoiseReductionMode = (NRMode)result
        );
        public static readonly FT891S_CatCommand<DnrResult> RL = new FT891S_CatCommand<DnrResult>(
            "RL",
            // 1. Outbound layout
            new CatStructure().Expect("P1", 1).Expect("P2", 2),
            // 2. Inbound layout
            new CatStructure().Expect("P1", 1).Expect("P2", 2),

            // FIX: Check if P2 exists to avoid KeyNotFoundException during local state updates
            dict => new DnrResult
            {
                Fixed = int.Parse(dict["P1"]),
                Values = dict.ContainsKey("P2") ? (DNRValues)int.Parse(dict["P2"]) : 0
            },
            result => {
                //Do nothing with Fixed
                FT891S_CatManager.currentRadioState.NoiseReductionLevel = result.Values;
            }
        );
        public static readonly FT891S_CatCommand<WidthResult> SH = new FT891S_CatCommand<WidthResult>(
            "SH",
            new CatStructure().Expect("P1", 1).Expect("P2", 1).Expect("P3", 2),
            new CatStructure().Expect("P1", 1).Expect("P2", 1).Expect("P3", 2),
            
            dict => new WidthResult
            {
                Fixed = int.Parse(dict["P1"]),
                Switch = dict.ContainsKey("P2") ? (WidthModes)int.Parse(dict["P2"]) : 0,
                Values = dict.ContainsKey("P3") ? (WidthValues)int.Parse(dict["P3"]) : 0
            },
            result => {
                //Do nothing with Fixed
                FT891S_CatManager.currentRadioState.WidthMode = result.Switch;
                FT891S_CatManager.currentRadioState.WidthValue = result.Values;
            }
        );
        public static readonly FT891S_CatCommand<int> FS = new FT891S_CatCommand<int>(
            "FS",
            new CatStructure().Expect("P1", 1),
            new CatStructure().Expect("P1", 1),
            dict => int.Parse(dict["P1"]),
            result => FT891S_CatManager.currentRadioState.FastStep = (FastStep)result
        );
        public static readonly FT891S_CatCommand<int> PS = new FT891S_CatCommand<int>(
            "PS",
            new CatStructure().Expect("P1", 1),
            new CatStructure().Expect("P1", 1),
            dict => int.Parse(dict["P1"]),
            result => FT891S_CatManager.currentRadioState.PowerSwitch = (PowerSwitchModes)result
        );
        public static readonly FT891S_CatCommand<IfShiftResult> IS = new FT891S_CatCommand<IfShiftResult>(
            "IS",
            new CatStructure().Expect("P1", 1).Expect("P2", 1).Expect("P3", 1).Expect("P4", 4),
            new CatStructure().Expect("P1", 1).Expect("P2", 1).Expect("P3", 1).Expect("P4", 4),
            dict => new IfShiftResult
            {
                Fixed = int.Parse(dict["P1"]),//0
                OnOff = int.Parse(dict["P2"]),//0 or 1
                IfShiftDirection = dict["P3"] == "+" ? 1 : -1, // Safely maps '+' to 1 and '-' to -1
                IfShiftHz = int.Parse(dict["P4"]) // 0-1200Hz Steps on 20Hz
            },
            result => {
                // Ensure the object instance exists before setting its properties
                if (FT891S_CatManager.currentRadioState.IfShiftHz == null)
                {
                    FT891S_CatManager.currentRadioState.IfShiftHz = new IS_Shift();
                }
                FT891S_CatManager.currentRadioState.IfShiftHz.Fixed = result.Fixed;
                FT891S_CatManager.currentRadioState.IfShiftHz.OnOff = result.OnOff;
                FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftDirection = result.IfShiftDirection;
                FT891S_CatManager.currentRadioState.IfShiftHz.IfShiftHz = result.IfShiftHz;
            }
        );
        //BP = Manual Notch, Values are 001-320 x10Hz
        public static readonly FT891S_CatCommand<NotchResult> BP = new FT891S_CatCommand<NotchResult>(
            "BP",
            new CatStructure().Expect("P1", 1).Expect("P2", 1).Expect("P3", 3),
            new CatStructure().Expect("P1", 1).Expect("P2", 1).Expect("P3", 3),

            dict => new NotchResult
            {
                Fixed = int.Parse(dict["P1"]),
                Switch = dict.ContainsKey("P2") ? (NotchModes)int.Parse(dict["P2"]) : 0,
                Values = dict.ContainsKey("P3") ? int.Parse(dict["P3"]) : 0
            },
            result => {
                //Do nothing with Fixed
                FT891S_CatManager.currentRadioState.NotchMode = result.Switch;
                FT891S_CatManager.currentRadioState.NotchValue = result.Values;
            }
        );

        public static readonly Dictionary<string, ICatCommand> ParsersByOpCode = new Dictionary<string, ICatCommand>()
        {
            { "BY", BY },
            { "FA", FA },
            { "MD", MD },
            { "GT", GT },
            { "RM", RM },
            { "PC", PC },
            { "ID", ID },
            { "RG", RG },
            { "AG", AG },
            { "BS", BS },
            { "AB", AB },
            { "BA", BA },
            { "EX", EX },
            { "SC", SC },
            { "NB", NB },
            { "NL", NL },
            { "NR", NR },
            { "RL", RL },
            { "SH", SH },
            { "FS", FS },
            { "PS", PS },
            { "IS", IS },
            { "BP", BP }
        };

        // Example method to build the transmission string for the radio
        public static string BuildIsCommand(int fixedVal, int onOff, char direction, int hz)
        {
            // Use Math.Abs to ensure P4 is always a positive 4-digit number (e.g., 100 -> "0100", not "-0100")
            string p4Formatted = Math.Abs(hz).ToString("D4");

            return $"{fixedVal}{onOff}{direction}{p4Formatted};";
        }
        // Resulting string: "IS0+0100;"

        public static void ProcessIncomingRadioData(string rawRadioData, Dispatcher wpfDispatcher = null)
        {
            if (string.IsNullOrEmpty(rawRadioData) || rawRadioData.Length < 2) return;

            string opCode = rawRadioData.Substring(0, 2);

            if (ParsersByOpCode.TryGetValue(opCode, out ICatCommand processingCommand))
            {
                processingCommand.ParseAndApplyToGlobalState(rawRadioData, wpfDispatcher);
            }
            else
            {
                try
                {
                    if (rawRadioData != null && !string.IsNullOrWhiteSpace(rawRadioData.ToString()))
                    {
                        string replyStr = rawRadioData.ToString();

                        // Filter out known error responses (e.g., Kenwood/Elecraft often return "??;")
                        if (!replyStr.Contains("?"))
                        {
                            Console.WriteLine($"[DISCOVERED] Response: {rawRadioData}");
                            // TODO: Log this to a file or a ObservableCollection bound to your WPF UI
                        }
                    }
                }
                catch (TimeoutException)
                {
                    // Expected for 95% of commands that the radio doesn't recognize
                    Console.WriteLine($"Command timed out.");
                }
                catch (Exception ex)
                {
                    // Handle port errors or unexpected issues
                    Console.WriteLine($"Error testing {ex.Message}");
                }
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

            // ======================================================
            // LOCAL STATE UPDATE (NO RADIO REQUIRED)
            // ======================================================
            if (cmd is FT891S_CatCommand<int> cmdInt)
            {
                cmdInt.ApplyOutgoingValues(values);
            }
            else if (cmd is FT891S_CatCommand<long> cmdLong)
            {
                cmdLong.ApplyOutgoingValues(values);
            }
            else if (cmd is FT891S_CatCommand<ModeResult> cmdMode)
            {
                cmdMode.ApplyOutgoingValues(values);
            }
            else if (cmd is FT891S_CatCommand<AgcResult> cmdAgc)
            {
                cmdAgc.ApplyOutgoingValues(values);
            }
            else if (cmd is FT891S_CatCommand<MeterResult> cmdMeter)
            {
                cmdMeter.ApplyOutgoingValues(values);
            }
            else if (cmd is FT891S_CatCommand<IfShiftResult> cmdIfShift)
            {
                cmdIfShift.ApplyOutgoingValues(values);
            }

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
            mainWindow.frequencyManagement.SetFrequencyUI(MemorySlot.MemorySlots.VFO_A, currentRadioState.VfoAFrequency, mainWindow.MainFrequencyTextBlock);

            if (mainWindow.waterFallSweep.UseTimeSlicing && mainWindow.waterFallSweep.ScopeOnOff)
                mainWindow.simulatedWaterfall.ChangeSpanCenterFrequency(mainWindow.waterFallSweep.currentQsoCenterFrequency);
            else
                mainWindow.simulatedWaterfall.ChangeSpanCenterFrequency(currentRadioState.VfoAFrequency);

            mainWindow.LargeFrequencyDisplay.Frequency = currentRadioState.VfoAFrequency;

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
                    //mainWindow.sprite.GenerateBandScopeSprite(Convert.ToByte(currentRadioState.CurrentMeterReading), 299, 2);
                    //mainWindow.sprite.GenerateHistorySprite(signalStrength, 299, 2);
                    //mainWindow.sprite.GenerateCombinedSignalSprite(Convert.ToByte(currentRadioState.CurrentMeterReading), 299, 2, 2);
                    // Make sure you are passing the actual live span variable here!
                    mainWindow.sprite.GenerateCombinedSignalSprite(Convert.ToByte(currentRadioState.CurrentMeterReading), 299, SimulatedWaterfall.currentBandScopeSpriteRectangleWidth, 1 );
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
                    //mainWindow.sprite.GenerateBandScopeSprite(signalStrength, 295, 2);
                    //mainWindow.sprite.GenerateHistorySprite(signalStrength, 295, 2);

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
                    SetRigLEDColor(mainWindow, RigLEDColors.LightGray);
                else if (currentRadioState.BusyMode == 1)
                    SetRigLEDColor(mainWindow, RigLEDColors.Green);
            }
            //BY BUSY

            //PC POWER CONTROL
            mainWindow.PowerControlLabel.Content = currentRadioState.TXPowerWatts.ToString() + "W";
            mainWindow.RfPowerFunctionTextBlock.Text = currentRadioState.TXPowerWatts.ToString() + "W";
            //PC POWER CONTROL

            //mainWindow.lastRFGain = FT891S_CatManager.currentRadioState.RFGain;
            //mainWindow.lastAFGain = FT891S_CatManager.currentRadioState.AFGain;
            mainWindow.UpdateKnobInput3();
            mainWindow.UpdateKnobInput4();

            if (currentRadioState.NoiseBlankerMode == NBMode.NB_OFF)
                mainWindow.NBLabelBorder.Visibility = Visibility.Hidden;
            else
                mainWindow.NBLabelBorder.Visibility = Visibility.Visible;

            if (currentRadioState.NoiseReductionMode == NRMode.NR_OFF)
                mainWindow.DNRLabelBorder.Visibility = Visibility.Hidden;
            else
                mainWindow.DNRLabelBorder.Visibility = Visibility.Visible;

            if (currentRadioState.FastStep == FastStep.FastStep_OFF)
                mainWindow.FastNormalBorder.Visibility = Visibility.Hidden;
            else
                mainWindow.FastNormalBorder.Visibility = Visibility.Visible;
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

            DoTranceiverMode_Main();

            switch (mainWindow.tranceiverDisplayModes.CurrentTranceiverMode.ID)
            {
                case TranceiverModes.BootUp:
                    if (currentRadioState.RadioID == 650)
                    {
                        mainWindow.RadioIDTextBlock.Text = "FT-891";
                        mainWindow.RadioIDAmberLED.Opacity = 0.5;
                    }
                    else if (currentRadioState.RadioID == 800)
                    {
                        mainWindow.RadioIDTextBlock.Text = "FT-710";
                        mainWindow.RadioIDAmberLED.Opacity = 0.5;
                    }
                    else if (currentRadioState.RadioID > 0 && currentRadioState.RadioID < 9999)
                    {
                        mainWindow.RadioIDTextBlock.Text = "??????";
                        mainWindow.RadioIDAmberLED.Opacity = 0.2;
                    }
                    break;
                case TranceiverModes.MainWaterfall:
                  
                    break;
                case TranceiverModes.StationScope:
                    if (!(mainWindow.stationSeek.IsScanning))
                    {
                        //mainWindow.frequencyManagement.SetFrequencyUI(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, currentRadioState.VfoAFrequency, mainWindow.MainFrequencyTextBlock);
                        //mainWindow.LargeFrequencyDisplay.Frequency = currentRadioState.VfoAFrequency;
                    }
                    break;
                case TranceiverModes.NoiseFilters:
                    //mainWindow.frequencyManagement.SetFrequency(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, currentRadioState.VfoAFrequency, mainWindow.MainFrequencyTextBlock);
                    //mainWindow.LargeFrequencyDisplay.Frequency = currentRadioState.VfoAFrequency;
                    break;
                case TranceiverModes.MorseCode:
                    //mainWindow.frequencyManagement.SetFrequency(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, currentRadioState.VfoAFrequency, mainWindow.MainFrequencyTextBlock);
                    //mainWindow.LargeFrequencyDisplay.Frequency = currentRadioState.VfoAFrequency;
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
            if (!OutGoingDataLoop_IsRunning) return;

            OutGoingDataLoop_IsRunning = false;
            _serialCts?.Cancel();

            // Safely update UI only if the dispatcher is still alive
            var dispatcher = mainWindow?.Dispatcher;
            if (dispatcher != null && !dispatcher.HasShutdownStarted)
            {
                try
                {
                    dispatcher.Invoke(() =>
                    {
                        mainWindow.SendOnOffTextBlock.Text = "OFF";
                    });
                }
                catch (TaskCanceledException)
                {
                    // App is closing, safe to ignore
                }
            }
        }

        DateTime lowPriorityDateTime = DateTime.Now;
        TimeSpan lowPriorityCATCommandsTimeSpan = TimeSpan.FromSeconds(7);

        private async Task MainWaterfallOutGoingData(int packet)
        {
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
                    case 6:
                        if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOff)
                        {
                            await SendCatCommandAsync("AG", "0", mainWindow._catManager.OutGoingDataLoopDelay);
                        }
                        break;
                    case 7:
                        if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOff)
                        {
                            await SendCatCommandAsync("RG", "0", mainWindow._catManager.OutGoingDataLoopDelay);
                        }
                        break;
                    case 8:
                        if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOff)
                        {
                            await SendCatCommandAsync("NB", "0", mainWindow._catManager.OutGoingDataLoopDelay);
                            await SendCatCommandAsync("NL", "0", mainWindow._catManager.OutGoingDataLoopDelay);
                        }
                        break;
                    case 9:
                        if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOff)
                        {
                            await SendCatCommandAsync("NR", "0", mainWindow._catManager.OutGoingDataLoopDelay);
                            await SendCatCommandAsync("RL", "0", mainWindow._catManager.OutGoingDataLoopDelay);
                        }
                        break;
                    case 10:
                        if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOff)
                        {
                            await SendCatCommandAsync("FS", mainWindow._catManager.OutGoingDataLoopDelay);
                        }
                        break;
                }

                if (mainWindow.waterFallSweep.ScopeOnOff == true)
                {
                    //if (DateTime.Now > lowPriorityDateTime + lowPriorityCATCommandsTimeSpan)
                    //{
                    //    if (mainWindow.Dispatcher.CheckAccess())
                    //    {
                    //        // We are already on the UI thread! Run it directly.
                    //        mainWindow.waterFallSweep.Sweep(14252500, 14380000, 500, 6);
                    //    }
                    //    else
                    //    {
                    //        // We are on a background thread. Marshal it over.
                    //        await mainWindow.Dispatcher.BeginInvoke(new Action(() => mainWindow.waterFallSweep.Sweep(14252500, 14380000, 500, 6)));
                    //    }

                    //    lowPriorityDateTime = DateTime.Now;
                    //}
                }
            }
        }

        public int OutGoingDataLoopDelay = 5;
        public async Task OutgoingDataLoop(CancellationToken token)
        {
            if (!(OutGoingDataLoop_IsRunning)) return;

            int packet = 0;

            while (!token.IsCancellationRequested)
            {
                if (packet == 11)
                    packet = 0;

                await MainWaterfallOutGoingData(packet);

                switch (mainWindow.tranceiverDisplayModes.CurrentTranceiverMode.ID)
                {
                    case TranceiverModes.BootUp:
                        await SendCatCommandAsync("ID", OutGoingDataLoopDelay);

                        String BuiltIsCommand = FT891S_CatCommandTypes.BuildIsCommand(0, 1, '-', 300);
                        await SendCatCommandAsync("IS", BuiltIsCommand, OutGoingDataLoopDelay);
                        await SendCatCommandAsync("IS", "0", OutGoingDataLoopDelay);
                        break;
                    case TranceiverModes.MainWaterfall:
                        break;
                    case TranceiverModes.StationScope:
                        break;
                    case TranceiverModes.NoiseFilters:
                        break;
                    case TranceiverModes.MorseCode:
                        break;
                }
                packet++;
            }
        }

    }
}