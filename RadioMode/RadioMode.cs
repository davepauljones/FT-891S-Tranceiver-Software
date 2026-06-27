using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YAESU_FT_891_Front_End.Models
{
    public enum RadioMode : byte
    {
        LSB,
        USB,
        CW_L,
        CW_U,
        AM,
        AM_N,
        FM,
        FM_N,
        DATA_L,
        DATA_U,
        DATA_FM,
        DATA_FM_N,
        RTTY_L,
        RTTY_U,
        PSK,
        PRESET
    }
}