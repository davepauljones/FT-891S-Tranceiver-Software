using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YAESU_FT_891_Front_End.Models
{
    public interface IModeMapper
    {
        byte ToCAT(RadioMode mode);
        RadioMode FromCAT(byte value);

        IEnumerable<RadioMode> SupportedModes { get; }
    }
}