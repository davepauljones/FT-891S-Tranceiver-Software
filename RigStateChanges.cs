using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Controls;
using FT891S_CatControl;

namespace YAESU_FT_891_Front_End
{
    public static class RigStateChanges
    {
        public static int RigMode = RigModes.SSB1;

        public class RigModeClass
        {
            public String Name { get; set; } = String.Empty;
            public Color BackgroundColor { get; set; } = Colors.Transparent;
            public Color ForegroundColor { get; set; } = Colors.Black;   
        }
        public struct RigModes
        {
            public const int NONE = 0;
            public const int SSB1 = 1;
            public const int SSB2 = 2;
            public const int CW = 3;
            public const int FM = 4;
            public const int AM = 5;
            public const int Rtty = 6;
            public const int CW2 = 7;
            public const int Data = 8;
            public const int Rtty2 = 9;
            public const int A_ = 10;
            public const int B_FM_N = 11;
            public const int C_DATA = 12;
            public const int D_AM_N = 13;
        }

        public static void UpdateUIRigMode(Border rigModeBorder, Label rigModeLabel, RadioMode rigMode)
        {
            RigModeClass rmc = ChangeMode(rigMode);

            rigModeLabel.Content = rmc.Name;
            rigModeBorder.Background = new SolidColorBrush(rmc.BackgroundColor);
            rigModeLabel.Foreground = new SolidColorBrush(rmc.ForegroundColor);
        }

        public static RigModeClass ChangeMode(RadioMode mode)
        {
            RigModeClass rigMode = new RigModeClass();

            switch (mode)
            {
                case RadioMode.LSB:
                    rigMode.Name = "LSB";
                    rigMode.BackgroundColor = Colors.DodgerBlue;
                    rigMode.ForegroundColor = Colors.White;
                    break;
                case RadioMode.USB:
                    rigMode.Name = "USB";
                    rigMode.BackgroundColor = Colors.DodgerBlue;
                    rigMode.ForegroundColor = Colors.White;
                    break;
                case RadioMode.CW:
                    rigMode.Name = "CWL";
                    rigMode.BackgroundColor = Colors.Brown;
                    rigMode.ForegroundColor = Colors.White;
                    break;
                case RadioMode.FM:
                    rigMode.Name = "FM";
                    rigMode.BackgroundColor = Colors.White;
                    rigMode.ForegroundColor = Colors.Black;
                    break;
                case RadioMode.AM:
                    rigMode.Name = "AM";
                    rigMode.BackgroundColor = Colors.Green;
                    rigMode.ForegroundColor = Colors.White;
                    break;
                case RadioMode.RTTY_LSB:
                    rigMode.Name = "R-L";
                    rigMode.BackgroundColor = Colors.SaddleBrown;
                    rigMode.ForegroundColor = Colors.White;
                    break;
                case RadioMode.CW_R:
                    rigMode.Name = "CWL";
                    rigMode.BackgroundColor = Colors.Brown;
                    rigMode.ForegroundColor = Colors.White;
                    break;
                case RadioMode.DATA_LSB:
                    rigMode.Name = "D-L";
                    rigMode.BackgroundColor = Colors.SaddleBrown;
                    rigMode.ForegroundColor = Colors.White;
                    break;
                case RadioMode.RTTY_USB:
                    rigMode.Name = "R-U";
                    rigMode.BackgroundColor = Colors.SaddleBrown;
                    rigMode.ForegroundColor = Colors.White;
                    break;
                case RadioMode.DATA_USB:
                    rigMode.Name = "A-";
                    rigMode.BackgroundColor = Colors.Green;
                    rigMode.ForegroundColor = Colors.White;
                    break;
                case RadioMode.FM_N:
                    rigMode.Name = "FM";
                    rigMode.BackgroundColor = Colors.SaddleBrown;
                    rigMode.ForegroundColor = Colors.White;
                    break;
                case RadioMode.DATA_FM:
                    rigMode.Name = "D-L";
                    rigMode.BackgroundColor = Colors.SaddleBrown;
                    rigMode.ForegroundColor = Colors.White;
                    break;
            }

            return rigMode;
        }
    }
}
