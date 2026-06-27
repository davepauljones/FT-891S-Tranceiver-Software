using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using YAESU_FT_891_Front_End.Models;

namespace YAESU_FT_891_Front_End.Radio
{
    public class FT710ModeMapper : IModeMapper
    {
        public byte ToCAT(RadioMode mode) => (byte)mode;

        public RadioMode FromCAT(byte value)
            => Enum.IsDefined(typeof(RadioMode), value)
                ? (RadioMode)value
                : RadioMode.USB;
    }
}