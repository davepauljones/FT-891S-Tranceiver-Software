using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Controls;

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
            public const byte NONE = 0;
            public const byte SSB1 = 1;
            public const byte SSB2 = 2;
            public const byte CW = 3;
            public const byte FM = 4;
            public const byte AM = 5;
            public const byte Rtty = 6;
            public const byte CW2 = 7;
            public const byte Data = 8;
            public const byte Rtty2 = 9;
            public const byte A_ = 10;
            public const byte B_FM_N = 11;
            public const byte C_DATA = 12;
            public const byte D_AM_N = 13;
        }

        public static void UpdateUIRigMode(Border rigModeBorder, Label rigModeLabel, int rigMode)
        {
            RigMode = rigMode;

            RigModeClass rmc = ChangeMode(rigMode);

            rigModeLabel.Content = rmc.Name;
            rigModeBorder.Background = new SolidColorBrush(rmc.BackgroundColor);
            rigModeLabel.Foreground = new SolidColorBrush(rmc.ForegroundColor);
        }

        public static RigModeClass ChangeMode(int mode)
        {
            RigModeClass rigMode = new RigModeClass();

            switch (mode)
            {
                case RigModes.NONE:
                    rigMode.Name = "NONE";
                    rigMode.BackgroundColor = Colors.Transparent;
                    rigMode.ForegroundColor = Colors.Black;
                    break;
                case RigModes.SSB1:
                    rigMode.Name = "LSB";
                    rigMode.BackgroundColor = Colors.DodgerBlue;
                    rigMode.ForegroundColor = Colors.White;
                    break;
                case RigModes.SSB2:
                    rigMode.Name = "USB";
                    rigMode.BackgroundColor = Colors.DodgerBlue;
                    rigMode.ForegroundColor = Colors.White;
                    break;
                case RigModes.CW:
                    rigMode.Name = "CWL";
                    rigMode.BackgroundColor = Colors.Brown;
                    rigMode.ForegroundColor = Colors.White;
                    break;
                case RigModes.FM:
                    rigMode.Name = "FM";
                    rigMode.BackgroundColor = Colors.White;
                    rigMode.ForegroundColor = Colors.Black;
                    break;
                case RigModes.AM:
                    rigMode.Name = "AM";
                    rigMode.BackgroundColor = Colors.Green;
                    rigMode.ForegroundColor = Colors.White;
                    break;
                case RigModes.Rtty:
                    rigMode.Name = "R-L";
                    rigMode.BackgroundColor = Colors.SaddleBrown;
                    rigMode.ForegroundColor = Colors.White;
                    break;
                case RigModes.CW2:
                    rigMode.Name = "CWL";
                    rigMode.BackgroundColor = Colors.Brown;
                    rigMode.ForegroundColor = Colors.White;
                    break;
                case RigModes.Data:
                    rigMode.Name = "D-L";
                    rigMode.BackgroundColor = Colors.SaddleBrown;
                    rigMode.ForegroundColor = Colors.White;
                    break;
                case RigModes.Rtty2:
                    rigMode.Name = "R-U";
                    rigMode.BackgroundColor = Colors.SaddleBrown;
                    rigMode.ForegroundColor = Colors.White;
                    break;
                case RigModes.A_:
                    rigMode.Name = "A-";
                    rigMode.BackgroundColor = Colors.Green;
                    rigMode.ForegroundColor = Colors.White;
                    break;
                case RigModes.B_FM_N:
                    rigMode.Name = "FM";
                    rigMode.BackgroundColor = Colors.SaddleBrown;
                    rigMode.ForegroundColor = Colors.White;
                    break;
                case RigModes.C_DATA:
                    rigMode.Name = "D-L";
                    rigMode.BackgroundColor = Colors.SaddleBrown;
                    rigMode.ForegroundColor = Colors.White;
                    break;
                case RigModes.D_AM_N:
                    rigMode.Name = "AM";
                    rigMode.BackgroundColor = Colors.Green;
                    rigMode.ForegroundColor = Colors.White;
                    break;
                default:
                    rigMode.Name = "NONE";
                    rigMode.BackgroundColor = Colors.Transparent;
                    rigMode.ForegroundColor = Colors.Black;
                    break;
            }

            return rigMode;
        }
    }
}
