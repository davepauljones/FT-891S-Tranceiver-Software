using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;

namespace YAESU_FT_891_Front_End
{
    public class GainChangedEventArgs : RoutedEventArgs
    {
        public int Gain { get; }

        public GainChangedEventArgs(RoutedEvent routedEvent, int gain)
            : base(routedEvent)
        {
            Gain = gain;
        }
    }

    public delegate void GainChangedEventHandler(object sender, GainChangedEventArgs e);
}