using FT891S_CatControl;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static YAESU_FT_891_Front_End.MyStructs;

namespace YAESU_FT_891_Front_End
{
    public class YAESU_FT_891_CAT_Dictionary
    {
        MainWindow mainWindow;
        public YAESU_FT_891_CAT_Dictionary(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }
        public void TXRXState(SerialPort _port)
        {
            if (FT891CommandSet.TryGetValue("Tx", out CatCommand cmd))
            {
                string fullCommand = string.Empty;

                fullCommand = cmd.Format();

                mainWindow.fT891S_SerialPort.SendCAT(_port, fullCommand);

                if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
                {
                    Console.Write("TXRXState = ");
                    Console.WriteLine(fullCommand);
                }
            }
        }

        public void SMeter(SerialPort _port, int meterToRead)
        {
            if (FT891CommandSet.TryGetValue("SMeter", out CatCommand cmd))
            {
                string fullCommand = string.Empty;

                fullCommand = cmd.Format(meterToRead.ToString());

                mainWindow.fT891S_SerialPort.SendCAT(_port, fullCommand);

                if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
                {
                    Console.Write("SMeter = ");
                    Console.WriteLine(fullCommand);
                }
            }
        }

        public void FreqA(SerialPort _port, long value)
        {
            // Ensure value is within the radio's valid range
            if (value < 0) value = 0;
            if (value > 70000000) value = 70000000;

            if (FT891CommandSet.TryGetValue("FreqA", out CatCommand cmd))
            {
                string fullCommand = string.Empty;
                string parameter = string.Empty;

                if (value == 0)
                {
                    fullCommand = cmd.Format();
                }
                else
                {
                    // "D3" formats the integer as a 3-digit string with leading zeros
                    parameter = value.ToString("D9");
                    fullCommand = cmd.Format(parameter);
                }

                // Output: "FA;" for read
                // Output: "FA014500125;" for changing to 14.500.125
                mainWindow.fT891S_SerialPort.SendCAT(_port, fullCommand);

                if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
                {
                    Console.Write("FreqA = ");
                    Console.WriteLine(fullCommand);
                }
            }
        }

        public void SetMode(RadioMode radioMode)
        {
            byte modeValue = (byte)radioMode;

            if (modeValue > 0x0F)
                return;

            char hexChar = "0123456789ABCDEF"[modeValue];

            string cmd = $"MD0{hexChar};";

            mainWindow.fT891S_SerialPort.SendCAT(mainWindow.fT891S_SerialPort._port, cmd);

            if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
            {
                Console.WriteLine("SetMode = " + cmd);
            }
        }

        public void SwapSubWithMain()
        {
            string cmd = $"BA;";

            mainWindow.fT891S_SerialPort.SendCAT(mainWindow.fT891S_SerialPort._port, cmd);

            if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
            {
                Console.WriteLine("SwapSubWithMain");
            }
        }
        public void SwapMainWithSub()
        {
            string cmd = $"AB;";

            mainWindow.fT891S_SerialPort.SendCAT(mainWindow.fT891S_SerialPort._port, cmd);

            if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
            {
                Console.WriteLine("SwapSubWithMain");
            }
        }

        public void SetRfGain(SerialPort _port, int value)
        {
            // Ensure value is within the radio's valid range
            if (value < 0) value = 0;
            if (value > 255) value = 255;

            if (FT891CommandSet.TryGetValue("RfGain", out CatCommand cmd))
            {
                // "D3" formats the integer as a 3-digit string with leading zeros
                string parameter = value.ToString("D3");
                string fullCommand = cmd.Format(parameter);

                // Output: "RG0128;" for a value of 128
                mainWindow.fT891S_SerialPort.SendCAT(_port, fullCommand);

                if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
                {
                    Console.Write("RfGain = ");
                    Console.WriteLine(fullCommand);
                }
            }
        }
        public void SetFrequency(long hz)
        {
            if (hz <= 0)
                return;

            // FT-891 valid range (safe guard)
            if (hz < 1000) hz = 1000;
            if (hz > 60000000) hz = 60000000;

            string cmd = $"FA{hz:000000000};";

            mainWindow.fT891S_SerialPort.SendCAT(mainWindow.fT891S_SerialPort._port, cmd);

            if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
            {
                Console.WriteLine("SetFrequency = " + cmd);
            }
        }

        public void SetMode(SerialPort _port, int value)
        {
            // Ensure value is within the radio's valid range
            if (value < 0) value = 0;
            if (value > 13) value = 13;

            if (FT891CommandSet.TryGetValue("Mode", out CatCommand cmd))
            {
                // "D3" formats the integer as a 3-digit string with leading zeros
                string parameter = value.ToString("D3");
                string fullCommand = cmd.Format(parameter);

                // Output: "MD0128;" for a value of 128
                //SendCAT(_port, fullCommand);
                mainWindow.fT891S_SerialPort.SendCAT(_port, "MD0");

                if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
                {
                    Console.Write("Mode = ");
                    Console.WriteLine(fullCommand);
                }
            }
        }

        public class CatCommand
        {
            public string Code { get; set; }        // e.g., "FA"
            public string Description { get; set; } // e.g., "Frequency Set/Read"
            public bool HasResponse { get; set; }   // Does it return data?
            public int ExpectedLength { get; set; } // Expected byte length of response

            public CatCommand(string code, string desc, bool hasResponse = true, int length = 0)
            {
                Code = code;
                Description = desc;
                HasResponse = hasResponse;
                ExpectedLength = length;
            }

            // Helper to format the string for the radio (ending in ';')
            public string Format(string parameter = "")
                => $"{Code}{parameter};";
        }
        

        public static Dictionary<string, CatCommand> FT891CommandSet = new Dictionary<string, CatCommand>
        {
            // --- VFO & Frequency ---
            { "FreqA",      new CatCommand("FA", "VFO-A Frequency", true, 11) },
            { "FreqB",      new CatCommand("FB", "VFO-B Frequency", true, 11) },
            { "VfoSwap",    new CatCommand("AB", "VFO A/B Swap", false) },
            { "VfoCopy",    new CatCommand("BB", "VFO A to B Copy", false) },
        
            // --- Audio & Gain ---
            { "AfGain",     new CatCommand("AG", "AF Gain (0-255)", true, 6) },
            { "RfGain",     new CatCommand("RG0", "RF Gain (0-255)", true, 7) }, // 7
            { "Squelch",    new CatCommand("SQ", "Squelch Level", true, 6) },
            { "Mute",       new CatCommand("MU", "Mute Toggle", true, 4) },
        
            // --- Mode & Filters ---
            { "Mode",       new CatCommand("MD0", "Operating Mode", true, 7) },
            { "Narrow",     new CatCommand("NA", "Narrow Filter", true, 4) },
            { "Width",      new CatCommand("SH", "IF Width", true, 5) },
            { "Shift",      new CatCommand("IS", "IF Shift", true, 8) },
        
            // --- Transmission & Power ---
            { "Tx",         new CatCommand("TX", "Transmit ON (PTT)", false) },
            { "Rx",         new CatCommand("RX", "Transmit OFF (PTT)", false) },
            { "Power",      new CatCommand("PC", "RF Power Output", true, 6) },
            { "Mox",        new CatCommand("MX", "MOX Toggle", true, 4) },
        
            // --- Meters & Status ---
            { "SMeter",     new CatCommand("RM", "Read S-Meter", true, 4) },
            { "Busy",       new CatCommand("BY", "Busy Status", true, 4) },
            { "AiStatus",   new CatCommand("AI", "Auto Info Toggle", true, 4) },
            
            // --- DSP Features ---
            { "NoiseRed",   new CatCommand("NR", "Noise Reduction", true, 4) },
            { "NrLevel",    new CatCommand("NL", "NR Level", true, 5) },
            { "AutoNotch",  new CatCommand("BC", "Auto Notch", true, 4) },
            { "Contour",    new CatCommand("CO", "Contour", true, 4) },
            
            // --- Memory & Tuning ---
            { "BandUp",     new CatCommand("BU", "Band Up", false) },
            { "BandDown",   new CatCommand("BD", "Band Down", false) },
            { "Lock",       new CatCommand("LK", "Dial Lock", true, 4) }
        };
    }

}