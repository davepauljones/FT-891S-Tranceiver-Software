using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using YAESU_FT_891_Front_End.Models;

namespace YAESU_FT_891_Front_End
{
    public static class RigStateChanges
    {
        public static RadioMode RigMode = RadioMode.USB;

        private static readonly Dictionary<RadioMode, RigModeDisplay> _map =
            new Dictionary<RadioMode, RigModeDisplay>
        {
            { RadioMode.LSB, new RigModeDisplay { Name="LSB", BackgroundColor=Colors.DodgerBlue, ForegroundColor=Colors.White }},
            { RadioMode.USB, new RigModeDisplay { Name="USB", BackgroundColor=Colors.DodgerBlue, ForegroundColor=Colors.White }},
            { RadioMode.CW_L, new RigModeDisplay { Name="CWL", BackgroundColor=Colors.Brown, ForegroundColor=Colors.White }},
            { RadioMode.CW_U, new RigModeDisplay { Name="CWU", BackgroundColor=Colors.Brown, ForegroundColor=Colors.White }},
            { RadioMode.FM, new RigModeDisplay { Name="FM", BackgroundColor=Colors.White, ForegroundColor=Colors.Black }},
            { RadioMode.AM, new RigModeDisplay { Name="AM", BackgroundColor=Colors.Green, ForegroundColor=Colors.White }},
            { RadioMode.RTTY_L, new RigModeDisplay { Name="R-L", BackgroundColor=Colors.SaddleBrown, ForegroundColor=Colors.White }},
            { RadioMode.RTTY_U, new RigModeDisplay { Name="R-U", BackgroundColor=Colors.SaddleBrown, ForegroundColor=Colors.White }},
            { RadioMode.DATA_L, new RigModeDisplay { Name="D-L", BackgroundColor=Colors.CornflowerBlue, ForegroundColor=Colors.White }},
            { RadioMode.DATA_U, new RigModeDisplay { Name="D-U", BackgroundColor=Colors.CornflowerBlue, ForegroundColor=Colors.White }},
            { RadioMode.FM_N, new RigModeDisplay { Name="FM-N", BackgroundColor=Colors.White, ForegroundColor=Colors.Black }},
            { RadioMode.DATA_FM, new RigModeDisplay { Name="D-FM", BackgroundColor=Colors.Cyan, ForegroundColor=Colors.White }},
            { RadioMode.AM_N, new RigModeDisplay { Name="AM-N", BackgroundColor=Colors.Green, ForegroundColor=Colors.White }},
        };

        public static void UpdateUIRigMode(Border rigModeBorder, Label rigModeLabel, RadioMode rigMode)
        {
            RigModeDisplay rmc = ChangeMode(rigMode);

            rigModeLabel.Content = rmc.Name;
            rigModeBorder.Background = new SolidColorBrush(rmc.BackgroundColor);
            rigModeLabel.Foreground = new SolidColorBrush(rmc.ForegroundColor);
        }

        public static RigModeDisplay ChangeMode(RadioMode mode)
        {
            if (_map.TryGetValue(mode, out var display))
                return display;

            return new RigModeDisplay
            {
                Name = "USB",
                BackgroundColor = Colors.DodgerBlue,
                ForegroundColor = Colors.White
            };
        }
    }
}