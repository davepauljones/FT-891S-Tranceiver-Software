using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;

namespace YAESU_FT_891_Front_End
{
    public class ModeChangedEventArgs : RoutedEventArgs
    {
        public Models.RadioMode Mode { get; }

        public ModeChangedEventArgs(RoutedEvent routedEvent, Models.RadioMode mode)
            : base(routedEvent)
        {
            Mode = mode;
        }
    }

    public delegate void ModeChangedEventHandler(object sender, ModeChangedEventArgs e);
}