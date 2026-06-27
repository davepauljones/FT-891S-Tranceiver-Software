using System;
using System.Collections.Generic;
using YAESU_FT_891_Front_End.Models;

namespace YAESU_FT_891_Front_End.Radio
{
    public class FT891ModeMapper : IModeMapper
    {
        public IEnumerable<RadioMode> SupportedModes => new[]
        {
            RadioMode.LSB,
            RadioMode.USB,
            RadioMode.CW_L,
            RadioMode.CW_U,
            RadioMode.AM,
            RadioMode.AM_N,
            RadioMode.FM,
            RadioMode.FM_N,
            RadioMode.DATA_L,
            RadioMode.DATA_U,
            RadioMode.RTTY_L,
            RadioMode.RTTY_U
        };
        public byte ToCAT(RadioMode mode)
        {
            switch (mode)
            {
                case RadioMode.LSB: return 1;
                case RadioMode.USB: return 2;
                case RadioMode.CW_L: return 3;
                case RadioMode.FM: return 4;
                case RadioMode.AM: return 5;
                case RadioMode.RTTY_L: return 6;
                case RadioMode.CW_U: return 7;
                case RadioMode.DATA_L: return 8;
                case RadioMode.RTTY_U: return 9;
                case RadioMode.FM_N: return 11;
                case RadioMode.DATA_U: return 12;
                case RadioMode.AM_N: return 13;

                default:
                    throw new NotSupportedException("Mode not supported on FT-891: " + mode);
            }
        }

        public RadioMode FromCAT(byte value)
        {
            switch (value)
            {
                case 1: return RadioMode.LSB;
                case 2: return RadioMode.USB;
                case 3: return RadioMode.CW_L;
                case 4: return RadioMode.FM;
                case 5: return RadioMode.AM;
                case 6: return RadioMode.RTTY_L;
                case 7: return RadioMode.CW_U;
                case 8: return RadioMode.DATA_L;
                case 9: return RadioMode.RTTY_U;
                case 11: return RadioMode.DATA_U;
                case 12: return RadioMode.FM_N;
                case 13: return RadioMode.AM_N;

                default:
                    return RadioMode.USB; // safe fallback
            }
        }
    }
}