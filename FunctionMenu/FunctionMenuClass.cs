using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace YAESU_FT_891_Front_End
{
    public struct FunctionMenu
    {
        public const byte Level = 0;
        public const byte Peak = 1;
        public const byte Marker = 2;
        public const byte Color = 3;
        public const byte Contrast = 4;
        public const byte Dimmer = 5;
        public const byte MGroup = 6;
        public const byte MicGain = 7;
        public const byte MicEq = 8;
        public const byte ProcLevel = 9;
        public const byte AmcLevel = 10;
        public const byte VoxLevel = 11;
        public const byte VoxDelay = 12;
        public const byte AntiVox = 13;
        public const byte RfPower = 14;
        public const byte MoniLevel = 15;
        public const byte Keyer = 16;
        public const byte BkIn = 17;
        public const byte CwSpeed = 18;
        public const byte CwPitch = 19;
        public const byte BkDelay = 20;
        public const byte Message = 21;
        public const byte Record = 22;
        public const byte Play = 23;
        public const byte Txw = 24;
        public const byte Aess = 25;
        public const byte AessCf = 26;
        public const byte RadioSetting = 27;
        public const byte CwSetting = 28;
        public const byte OperationSetting = 29;
        public const byte ExtensionSetting = 30;
        public const byte Back = 31;
    }

    public struct MenuDirections
    {
        public const byte NoChange = 0;
        public const byte GoLeft = 1;
        public const byte GoRight = 2;
        public const byte GoUp = 3;
        public const byte GoDown = 4;
    }

    public struct FunctionMenuScaleTypes
    {
        public const byte Level_dB = 0;
        public const byte Peak_LV1_To_LV5 = 1;
        public const byte Marker_ON_Or_Off = 2;
        public const byte Color_1_To_11 = 3;
        public const byte Contrast_0_To_20 = 4;
        public const byte Dimmer_0_To_20 = 5;
        public const byte MGroup_1_To_255 = 5; //increases value based on amount of memory groups
        public const byte MicGain_0_To_100 = 6;
        public const byte MicEQ_0N_Or_Off = 7;
        public const byte ProcLevel_Off_To_100 = 8;
        public const byte AMCLevel_1_To_100 = 9;
        //too carry on to complete all functions
    }
    public class FunctionMenuMinMaxScaleType
    {
        public int Min = 0;
        public int Max = 0;
        public int currentValue = 0;
        public String ScaleName = String.Empty;
        public byte ScaleType = 0;
    }

    public class FunctionMenuClass
    {
        public static byte FunctionMenuSelectedItem = FunctionMenu.Level;
        private static Grid FunctionMenuGrid;
        private static Label FunctionModeLabel;

        public static byte FunctionModeMaxFunction = 20;

        public static List<FunctionMenuMinMaxScaleType> FunctionMenuMinMaxScaleTypeList = new List<FunctionMenuMinMaxScaleType>();

        private static readonly string[] MenuNames = new string[]
        {
            "LEVEL", "PEAK", "MARKER", "COLOR", "CONTRAST", "DIMMER", "M-GROUP",
            "MIC GAIN", "MIC EQ", "PROC LEVEL", "AMC LEVEL", "VOX GAIN", "VOX DELAY",
            "ANTI VOX", "RF POWER", "MONI LEVEL", "KEYER", "BK-IN", "CW SPEED",
            "CW PITCH", "BK DELAY", "MESSAGE", "RECORD", "PLAY", "TXW", "AESS",
            "AESS-CF", "RADIO SETTING", "CW SETTING", "OPERATION SETTING",
            "EXTENSION SETTING", "BACK"
        };

        public FunctionMenuClass(Grid FunctionMenuGrid, Label FunctionModeLabel)
        {
            FunctionMenuClass.FunctionMenuGrid = FunctionMenuGrid;
            FunctionMenuClass.FunctionModeLabel = FunctionModeLabel;

            GetBorderByTag(Convert.ToInt16(FunctionMenuSelectedItem));
        }

        private void Setup_FunctionMenuMinMaxScaleTypeList()
        {
            FunctionMenuMinMaxScaleType level = new FunctionMenuMinMaxScaleType
            {
                Min = -30,
                Max = 30,
                currentValue = 0,
                ScaleName = "Level",
                ScaleType = FunctionMenuScaleTypes.Level_dB
            };
            FunctionMenuMinMaxScaleTypeList.Add(level);

            FunctionMenuMinMaxScaleType peak = new FunctionMenuMinMaxScaleType
            {
                Min = 1,
                Max = 5,
                currentValue = 1,
                ScaleName = "Peak",
                ScaleType = FunctionMenuScaleTypes.Peak_LV1_To_LV5
            };
            FunctionMenuMinMaxScaleTypeList.Add(peak);

            FunctionMenuMinMaxScaleType marker = new FunctionMenuMinMaxScaleType
            {
                Min = 0,
                Max = 1,
                currentValue = 1,
                ScaleName = "Marker",
                ScaleType = FunctionMenuScaleTypes.Marker_ON_Or_Off
            };
            FunctionMenuMinMaxScaleTypeList.Add(marker);

            FunctionMenuMinMaxScaleType color = new FunctionMenuMinMaxScaleType
            {
                Min = 1,
                Max = 11,
                currentValue = 1,
                ScaleName = "Color",
                ScaleType = FunctionMenuScaleTypes.Color_1_To_11
            };
            FunctionMenuMinMaxScaleTypeList.Add(color);

            //to be completed
        }

        public static string GetName(byte index)
        {
            if (index < MenuNames.Length) return MenuNames[index];
            return "Unknown";
        }

        public static void ChangeFunctionMenu(byte direction)
        {
            switch (direction)
            {
                case MenuDirections.NoChange:
                    //do nothing or maybe refresh UI
                    break;
                case MenuDirections.GoLeft:
                    if (FunctionMenuSelectedItem > 0)
                    {
                        FunctionMenuSelectedItem--;
                    }
                    break;
                case MenuDirections.GoRight:
                    if (FunctionMenuSelectedItem < 32) FunctionMenuSelectedItem++;
                    break;
                case MenuDirections.GoUp:
                    //nothing yet
                    break;
                case MenuDirections.GoDown:
                    //nothing yet
                    break;
                default:
                    FunctionMenuSelectedItem = MenuDirections.NoChange;
                    break;
            }

            GetBorderByTag(Convert.ToInt16(FunctionMenuSelectedItem));
            
            if (FunctionMenuSelectedItem <= FunctionModeMaxFunction)
                FunctionModeLabel.Content = GetName(FunctionMenuSelectedItem);
        }

        public static void GetBorderByTag( int targetTagValue)
        {
            Border matchedBorder;

            for (int borderTag=0;borderTag<33;borderTag++)
            {
                matchedBorder = FunctionMenuGrid.Children.OfType<Border>().FirstOrDefault(b => b.Tag?.ToString() == borderTag.ToString());

                matchedBorder.BorderBrush = System.Windows.Media.Brushes.Gray;
                matchedBorder.CornerRadius = new CornerRadius(3);
            }
            // Search only the immediate children of the parent container
            matchedBorder = FunctionMenuGrid.Children.OfType<Border>().FirstOrDefault(b => b.Tag?.ToString() == targetTagValue.ToString());

            if (matchedBorder != null)
            {
                //Do something with your border here (e.g., change color)
                matchedBorder.BorderBrush = System.Windows.Media.Brushes.Orange;
                matchedBorder.CornerRadius = new CornerRadius(1);

                FunctionMenuSelectedItem = Convert.ToByte(targetTagValue);
            }

        }

    }
}
