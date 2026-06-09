using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using static YAESU_FT_891_Front_End.MyStructs;

namespace YAESU_FT_891_Front_End
{
    public struct TranceiverModes
    {
        public const byte Main = 0;
        public const byte StationScope = 1;
        public const byte NoiseFilters = 2;
        public const byte CWDecoder = 3;
        public const byte Test = 4;
        public const byte FunctionMenu = 99;
    }
    public class TranceiverDisplayModes
    {
        public static int TranceiverMode = TranceiverModes.Main;
        public static int LastTranceiverMode = TranceiverModes.Main;

        public TranceiverDisplayModes()
        {
        }

        public static void ChangeDisplayMode(TabControl tabControl, Label tranceiverModeLabel, int displayMode)
        {
            if (TranceiverMode == TranceiverModes.Main)
            {
                TranceiverMode = TranceiverModes.StationScope;
            }
            else if (TranceiverMode == TranceiverModes.StationScope)
            {
                TranceiverMode = TranceiverModes.NoiseFilters;
            }
            else if (TranceiverMode == TranceiverModes.NoiseFilters)
            {
                TranceiverMode = TranceiverModes.CWDecoder;
            }
            else if (TranceiverMode == TranceiverModes.CWDecoder)
            {
                TranceiverMode = TranceiverModes.Main;
            }

            tabControl.SelectedIndex = TranceiverMode;

            if (tabControl.SelectedItem is TabItem currentTab)
            {
                string tagValue = currentTab.Tag?.ToString();

                tranceiverModeLabel.Content = tagValue;
            }
        }
    }
}
