using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using static YAESU_FT_891_Front_End.MyStructs;

namespace YAESU_FT_891_Front_End
{
    public class MyStructs
    {
        public struct Development
        {
            public const byte InProgress = 0;
            public const byte OnHold = 1;
            public const byte Complete = 99;
        }
        public struct ConsoleDebugLevels
        {
            public const byte All = 0;
            public const byte CATOnly = 1;
            public const byte NoCAT = 2;
            public const byte CurrentDebug = 99;
        }
        public struct DialDirection
        {
            public const byte None = 0;
            public const byte Clockwise = 1;
            public const byte AntiClockwise = 2;
        }

        public struct SMeters
        {
            public const byte DefaultPanelMeter = 0;
            public const byte S = 1;
            public const byte Depends = 2;
            public const byte COMP = 3;
            public const byte ALC = 4;
            public const byte PO = 5;
            public const byte SWR = 6;
            public const byte IDD = 7;
        }

        public struct TranceiverStates
        {
            public const byte RadioTXOff = 0;
            public const byte RadioTXOff2 = 1;
            public const byte RadioTXOn = 2;
        }

        public struct TranceiverModes
        {
            public const byte Main = 0;
            public const byte StationScope = 1;
            public const byte NoiseFilters = 2;
            public const byte CWDecoder = 3;
            public const byte Test = 4;
            public const byte FunctionMenu = 99;
        }

        public struct RigLEDColors
        {
            public const byte LightGray = 0;
            public const byte Green = 1;
            public const byte Red = 2;
            public const byte Blue = 3;
        }

        

    }
}
