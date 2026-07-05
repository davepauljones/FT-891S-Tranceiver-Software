using FT891S_CatControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using static YAESU_FT_891_Front_End.MyStructs;

namespace YAESU_FT_891_Front_End
{
    public struct TranceiverModes //are nothing to do with Tab Item
    {
        public const int BootUp = 0;
        public const int MainWaterfall = 1;
        public const int StationScope = 2;
        public const int NoiseFilters = 3;
        public const int MorseCode = 4;
        public const int FunctionMenu = 10;
        public const int CatCommandLog = 11;
    }
    public class TranceiverMode
    {
        public int ID;
        public String ShortName;
        public String LongName;
        public bool HasTabItem;
    }
    public class TranceiverDisplayModes
    {
        public TranceiverMode CurrentTranceiverMode;
        public TranceiverMode LastTranceiverMode;

        public static Dictionary<int, TranceiverMode> TranceiverModesDictionary = new Dictionary<int, TranceiverMode>();

        MainWindow mainWindow;
        public TranceiverDisplayModes(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;

            SetupTranceiverModes();
        }

        private void SetupTranceiverModes()
        {
            TranceiverMode BootUp = new TranceiverMode { ID = TranceiverModes.BootUp, ShortName = "Boot Up", LongName = "Boot Up", HasTabItem = true };
            TranceiverModesDictionary.Add(TranceiverModes.BootUp, BootUp);

            TranceiverMode MainWaterfall = new TranceiverMode { ID = TranceiverModes.MainWaterfall, ShortName = "Waterfall", LongName = "Waterfall", HasTabItem = true };
            TranceiverModesDictionary.Add(TranceiverModes.MainWaterfall, MainWaterfall);

            TranceiverMode StationScope = new TranceiverMode { ID = TranceiverModes.StationScope, ShortName = "Stations", LongName = "Station Scope", HasTabItem = true };
            TranceiverModesDictionary.Add(TranceiverModes.StationScope, StationScope);

            TranceiverMode NoiseFilters = new TranceiverMode { ID = TranceiverModes.NoiseFilters, ShortName = "Filters", LongName = "Noise Filters", HasTabItem = true };
            TranceiverModesDictionary.Add(TranceiverModes.NoiseFilters, NoiseFilters);

            TranceiverMode MorseCode = new TranceiverMode { ID = TranceiverModes.MorseCode, ShortName = "Morse", LongName = "Morse Code", HasTabItem = true };
            TranceiverModesDictionary.Add(TranceiverModes.MorseCode, MorseCode);

            TranceiverMode FunctionMenu = new TranceiverMode { ID = TranceiverModes.FunctionMenu, ShortName = "Function", LongName = "Function Menu", HasTabItem = false };
            TranceiverModesDictionary.Add(TranceiverModes.FunctionMenu, FunctionMenu);

            TranceiverMode CatCommandLog = new TranceiverMode { ID = TranceiverModes.CatCommandLog, ShortName = "CAT Log", LongName = "CAT Log", HasTabItem = false };
            TranceiverModesDictionary.Add(TranceiverModes.CatCommandLog, CatCommandLog);
        }

        public void SwitchToTranceiverMode(int tranceiverMode)
        {
            if (TranceiverModesDictionary.TryGetValue(tranceiverMode, out TranceiverMode GotTranceiverMode))
            {
                if (GotTranceiverMode.HasTabItem)
                {
                    mainWindow.TabControlTabControl.SelectedIndex = tranceiverMode;
                }

                CurrentTranceiverMode = GotTranceiverMode;
                mainWindow.TranceiverModeLabel.Content = GotTranceiverMode.ShortName;
                mainWindow.TabControlDescriptionLabel.Content = GotTranceiverMode.LongName;

                Console.Write("SwitchToADisplayMode = ");
                Console.WriteLine(GotTranceiverMode.ID);
            }
            else
            {
                Console.Write("SwitchToADisplayMode = ");
                Console.Write(GotTranceiverMode.ID);
                Console.WriteLine(" Not found");
            }
        }
        public void ToggleTranceiverMode(int tranceiverMode)
        {

            if (tranceiverMode == TranceiverModes.BootUp)
            {
                tranceiverMode = TranceiverModes.MainWaterfall;
            }
            else if (tranceiverMode == TranceiverModes.MainWaterfall)
            {
                tranceiverMode = TranceiverModes.StationScope;
            }
            else if (tranceiverMode == TranceiverModes.StationScope)
            {
                tranceiverMode = TranceiverModes.NoiseFilters;
            }
            else if (tranceiverMode == TranceiverModes.NoiseFilters)
            {
                tranceiverMode = TranceiverModes.MorseCode;
            }
            else if (tranceiverMode == TranceiverModes.MorseCode)
            {
                tranceiverMode = TranceiverModes.MainWaterfall;
            }

            SwitchToTranceiverMode(tranceiverMode);
        }
    }
}
