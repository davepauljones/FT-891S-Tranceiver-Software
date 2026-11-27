using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;

namespace YAESU_FT_891_Front_End
{
    public class WattChangedEventArgs : RoutedEventArgs
    {
        public int Watt { get; }

        public WattChangedEventArgs(RoutedEvent routedEvent, int watt)
            : base(routedEvent)
        {
            Watt = watt;
        }
    }

    public delegate void WattChangedEventHandler(object sender, WattChangedEventArgs e);
}