using System;
using System.Collections.Generic;
using YAESU_FT_891_Front_End.Models;

namespace YAESU_FT_891_Front_End.Radio
{
    public class FT710ModeMapper : IModeMapper
    {
        public IEnumerable<RadioMode> SupportedModes => new[]
        {
            RadioMode.LSB,
            RadioMode.USB,
            RadioMode.CW_L,
            RadioMode.CW_U,
            RadioMode.AM,
            RadioMode.FM,
            RadioMode.RTTY_L,
            RadioMode.RTTY_U,
            RadioMode.DATA_L,
            RadioMode.DATA_U,
            RadioMode.DATA_FM,
            RadioMode.PSK
        };
        public byte ToCAT(RadioMode mode)
        {
            switch (mode)
            {
                case RadioMode.LSB: return 0x00;
                case RadioMode.USB: return 0x01;

                case RadioMode.CW_L: return 0x02;
                case RadioMode.CW_U: return 0x03;

                case RadioMode.AM: return 0x04;
                case RadioMode.FM: return 0x05;

                case RadioMode.RTTY_L: return 0x06;
                case RadioMode.RTTY_U: return 0x07;

                case RadioMode.DATA_L: return 0x08;
                case RadioMode.DATA_U: return 0x09;

                case RadioMode.DATA_FM:
                    return 0x0A; // PSK/DATA mode on FT-710

                case RadioMode.FM_N:
                case RadioMode.AM_N:
                case RadioMode.DATA_FM_N:
                    throw new NotSupportedException(
                        $"FT-710 does not support narrow variants separately: {mode}");

                case RadioMode.PSK:
                    return 0x0A;

                default:
                    throw new NotSupportedException($"Unsupported mode: {mode}");
            }
        }

        public RadioMode FromCAT(byte value)
        {
            switch (value)
            {
                case 0x00: return RadioMode.LSB;
                case 0x01: return RadioMode.USB;

                case 0x02: return RadioMode.CW_L;
                case 0x03: return RadioMode.CW_U;

                case 0x04: return RadioMode.AM;
                case 0x05: return RadioMode.FM;

                case 0x06: return RadioMode.RTTY_L;
                case 0x07: return RadioMode.RTTY_U;

                case 0x08: return RadioMode.DATA_L;
                case 0x09: return RadioMode.DATA_U;

                case 0x0A: return RadioMode.DATA_FM;

                default:
                    return RadioMode.USB;
            }
        }

    }
}