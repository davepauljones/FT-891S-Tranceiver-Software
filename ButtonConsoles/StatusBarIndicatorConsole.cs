using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using FontAwesome.WPF;

namespace YAESU_FT_891_Front_End
{
    public class StatusBarIndicatorConsole : ButtonConsole
    {
        public struct Console
        {
            public const byte Recognition = 0;
        }
        public override List<ButtonConsoleClass> ButtonConsoleList
        {
            get
            {
                return new List<ButtonConsoleClass>
                {
                    new ButtonConsoleClass() { Icons = FontAwesomeIcon.MicrophoneSlash,
                                               IconAlts = FontAwesomeIcon.Microphone,
                                               IconColors = (Color)ColorConverter.ConvertFromString("#7F000000"),
                                               IconOnColors = (Color)ColorConverter.ConvertFromString("#33FFFFFF"),
                                               IconOn2Colors = (Color)ColorConverter.ConvertFromString("#FF39FF14"),
                                               ButtonType = ButtonTypes.Button,
                                               IsThreeStep = false,
                                               ToolTipOff = "RECOGNITION",
                                               ToolTipOn = "",
                                               NoviceToolTipOff = "",
                                               NoviceToolTipOn = "",
                                               StateIsOn = false },
                };
            }
        }

        public StatusBarIndicatorConsole(StackPanel ParentStackPanel, double FontSize, Color Foreground, ButtonClickCallBackFunction ButtonClickCBF)
            : base(ParentStackPanel, FontSize, Foreground, ButtonClickCBF)
        {

        }

    }
}
