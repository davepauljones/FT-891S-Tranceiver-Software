using System;
using System.Collections.Generic;
using static YAESU_FT_891_Front_End.RigStateChanges;
using YAESU_FT_891_Front_End.Models;

namespace YAESU_FT_891_Front_End
{
    /// <summary>
    /// Represents the complete operating state of a Yaesu FT-891.
    /// Designed for CAT control applications.
    /// </summary>
    public class RigState
    {
        public struct FrequencyLocations
        {
            public const byte RXFrequencyHz = 0;
            public const byte TXFrequencyHz = 1;
        }
        // =========================================================
        // ENUMS
        // =========================================================

        public enum VFOSelection
        {
            A,
            B
        }

        public enum AGCMode
        {
            OFF,
            FAST,
            MID,
            SLOW,
            AUTO
        }

        public enum ToneMode
        {
            OFF,
            CTCSS_ENCODE,
            CTCSS_ENCODE_DECODE,
            DCS
        }

        public enum DuplexMode
        {
            SIMPLEX,
            PLUS_SHIFT,
            MINUS_SHIFT,
            SPLIT
        }

        // =========================================================
        // FREQUENCY
        // =========================================================

        /// <summary>
        /// Current receive frequency in Hz
        /// </summary>
        public long RXFrequencyHz { get; set; }

        /// <summary>
        /// Current transmit frequency in Hz
        /// </summary>
        public long TXFrequencyHz { get; set; }

        public long VFOAFrequencyHz { get; set; }//ignor these

        public long VFOBFrequencyHz { get; set; }//ignor these

        // =========================================================
        // BAND / MODE
        // =========================================================

        public string Band { get; set; } = "20m";

        public RadioMode Mode { get; set; } = RadioMode.USB;

        public VFOSelection ActiveVFO { get; set; } = VFOSelection.A;

        // =========================================================
        // POWER
        // =========================================================

        /// <summary>
        /// RF output power in watts
        /// </summary>
        public int TXPowerWatts { get; set; } = 5;
        public int TXPowerWattsMinimum { get; } = 5;
        public int TXPowerWattsMaximum { get; } = 100;

        public int TXPowerWattsAMMaximum { get; } = 40;
        public int TXPowerWattsStep { get; } = 5;

        // =========================================================
        // REPEATER / SPLIT
        // =========================================================

        public DuplexMode Duplex { get; set; } = DuplexMode.SIMPLEX;

        /// <summary>
        /// Repeater shift in Hz
        /// </summary>
        public int RepeaterOffsetHz { get; set; }

        public bool SplitEnabled { get; set; }

        // =========================================================
        // TONE SETTINGS
        // =========================================================

        public ToneMode Tone { get; set; } = ToneMode.OFF;

        /// <summary>
        /// CTCSS tone frequency
        /// Example: 88.5
        /// </summary>
        public double CTCSSFrequencyHz { get; set; }

        /// <summary>
        /// DCS code
        /// Example: 023
        /// </summary>
        public int DCSCode { get; set; }

        // =========================================================
        // FILTER / DSP
        // =========================================================

        public AGCMode AGC { get; set; } = AGCMode.AUTO;

        public bool NoiseBlankerEnabled { get; set; }

        public bool NoiseReductionEnabled { get; set; }

        public bool NotchFilterEnabled { get; set; }

        public int FilterWidthHz { get; set; } = 2400;

        // =========================================================
        // CLARIFIER / RIT / XIT
        // =========================================================

        public bool RITEnabled { get; set; }

        public bool XITEnabled { get; set; }

        public int RITOffsetHz { get; set; }

        public int XITOffsetHz { get; set; }

        // =========================================================
        // AUDIO
        // =========================================================

        public int RFGain { get; set; } = 100;

        public int VolumeLevel { get; set; } = 50;

        public int SquelchLevel { get; set; }

        public int MicGain { get; set; } = 50;

        public int MonitorLevel { get; set; } = 50;

        // =========================================================
        // MISC LEVELS
        // =========================================================

        public int IFShift { get; set; }

        public int ContourLevel { get; set; }

        public int NoiseReductionLevel { get; set; }

        public int NoiseBlankerLevel { get; set; }

        public int SpeechProcessorLevel { get; set; }

        public bool IPOEnabled { get; set; }

        public bool ATTEnabled { get; set; }

        public bool NarrowFilterEnabled { get; set; }

        public int CWPitchHz { get; set; }

        public int CWSpeedWPM { get; set; }

        public bool BreakInEnabled { get; set; }

        public int VoxGain { get; set; }

        public int VoxDelay { get; set; }

        // =========================================================
        // STATUS FLAGS
        // =========================================================

        public bool PTTActive { get; set; }

        public bool TXActive { get; set; }

        public bool TunerEnabled { get; set; }

        public bool VOXEnabled { get; set; }

        // =========================================================
        // METERS
        // =========================================================

        public double SWR { get; set; } = 1.0;

        public int SMeter { get; set; }

        public int ALCLevel { get; set; }

        public int CompressionLevel { get; set; }

        // =========================================================
        // MEMORY / CHANNEL
        // =========================================================

        public int MemoryChannel { get; set; } = -1;

        public string MemoryTag { get; set; }

        // =========================================================
        // EXTENSIBLE CUSTOM PROPERTIES
        // =========================================================

        /// <summary>
        /// Allows adding future CAT properties without changing class structure
        /// </summary>
        public Dictionary<string, object> CustomProperties { get; set; }
            = new Dictionary<string, object>();

        // =========================================================
        // HELPERS
        // =========================================================

        public void SetCustomProperty(string key, object value)
        {
            if (CustomProperties.ContainsKey(key))
            {
                CustomProperties[key] = value;
            }
            else
            {
                CustomProperties.Add(key, value);
            }
        }

        public T GetCustomProperty<T>(string key)
        {
            if (CustomProperties.ContainsKey(key))
            {
                return (T)CustomProperties[key];
            }

            return default(T);
        }

        public bool HasCustomProperty(string key)
        {
            return CustomProperties.ContainsKey(key);
        }
    }
}