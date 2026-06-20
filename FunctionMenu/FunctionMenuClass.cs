using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using YourNamespace;
using static YAESU_FT_891_Front_End.MyStructs;

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
        public const byte VOXGain_0_To_100 = 10;
        public const byte VOXDelay_30_To_3000 = 11;
        public const byte AntiVox_0_To_100 = 12;
        public const byte RFPower_5_To_100 = 13;
        public const byte MoniLevel_0_To_100 = 14;
        public const byte Keyer_0_To_1 = 15;
        public const byte BkIn_0_To_1 = 16;
        public const byte CwSpeed_4_To_60 = 17;
        public const byte CwPitch_300_To_1050 = 18;
        public const byte BkDelay_30_To_1050 = 19;
        public const byte Message_1_To_5 = 20;
        public const byte Record_0_To_1 = 21;
        public const byte Play_1_To_6 = 22;
        public const byte Txw_0 = 23;
        public const byte Aess_0_To_100 = 24;
        public const byte AessCf_700_To_1000 = 25;
    }
    public struct FunctionMenuScaleNamePositions
    {
        public const byte None = 0;
        public const byte ToLeft = 1;
        public const byte ToRight = 2;
    }
    public class FunctionMenuMinMaxScaleType
    {
        public int Min = 0;
        public int Max = 0;
        public int Default = 0;
        public int currentValue = 0;
        public String ScaleName = String.Empty;
        public int ScaleNamePosition = FunctionMenuScaleNamePositions.ToRight;
        public byte ScaleType = 0;
    }

    public class FunctionMenuClass
    {
        private static MainWindow mainWindow;
        private static Grid FunctionMenuGrid;
        private static Label FunctionModeLabel;

        public static byte FunctionMenuSelectedItem = FunctionMenu.Level;

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

        public FunctionMenuClass(MainWindow mainWindow, Grid FunctionMenuGrid, Label FunctionModeLabel)
        {
            FunctionMenuClass.mainWindow = mainWindow;
            FunctionMenuClass.FunctionMenuGrid = FunctionMenuGrid;
            FunctionMenuClass.FunctionModeLabel = FunctionModeLabel;

            GetBorderByTag(Convert.ToInt16(FunctionMenuSelectedItem));

            Setup_FunctionMenuMinMaxScaleTypeList();
        }

        private void Setup_FunctionMenuMinMaxScaleTypeList()
        {
            FunctionMenuMinMaxScaleType level = new FunctionMenuMinMaxScaleType
            {
                Min = -30,
                Max = 30,
                Default = 0,
                currentValue = 0,
                ScaleName = "dB",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToRight,
                ScaleType = FunctionMenuScaleTypes.Level_dB
            };
            FunctionMenuMinMaxScaleTypeList.Add(level);

            FunctionMenuMinMaxScaleType peak = new FunctionMenuMinMaxScaleType
            {
                Min = 1,
                Max = 5,
                Default = 1,
                currentValue = 1,
                ScaleName = "LV",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.Peak_LV1_To_LV5
            };
            FunctionMenuMinMaxScaleTypeList.Add(peak);

            FunctionMenuMinMaxScaleType marker = new FunctionMenuMinMaxScaleType
            {
                Min = 0,
                Max = 1,
                Default = 1,
                currentValue = 1,
                ScaleName = "MARKER",
                ScaleNamePosition = FunctionMenuScaleNamePositions.None,
                ScaleType = FunctionMenuScaleTypes.Marker_ON_Or_Off
            };
            FunctionMenuMinMaxScaleTypeList.Add(marker);

            FunctionMenuMinMaxScaleType color = new FunctionMenuMinMaxScaleType
            {
                Min = 1,
                Max = 11,
                Default = 1,
                currentValue = 1,
                ScaleName = "COLOR",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.Color_1_To_11
            };
            FunctionMenuMinMaxScaleTypeList.Add(color);

            FunctionMenuMinMaxScaleType contrast = new FunctionMenuMinMaxScaleType
            {
                Min = 0,
                Max = 20,
                Default = 10,
                currentValue = 10,
                ScaleName = "CONTRAST",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.Contrast_0_To_20
            };
            FunctionMenuMinMaxScaleTypeList.Add(contrast);

            FunctionMenuMinMaxScaleType dimmer = new FunctionMenuMinMaxScaleType
            {
                Min = 0,
                Max = 20,
                Default = 16,
                currentValue = 16,
                ScaleName = "DIMMER",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.Dimmer_0_To_20
            };
            FunctionMenuMinMaxScaleTypeList.Add(dimmer);

            FunctionMenuMinMaxScaleType mgroup = new FunctionMenuMinMaxScaleType
            {
                Min = 1,
                Max = 255,
                Default = 1,
                currentValue = 1,
                ScaleName = "M-GROUP",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.MGroup_1_To_255
            };
            FunctionMenuMinMaxScaleTypeList.Add(mgroup);

            FunctionMenuMinMaxScaleType micgain = new FunctionMenuMinMaxScaleType
            {
                Min = 0,
                Max = 100,
                Default = 50,
                currentValue = 50,
                ScaleName = "MIC GAIN",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.MicGain_0_To_100
            };
            FunctionMenuMinMaxScaleTypeList.Add(micgain);

            FunctionMenuMinMaxScaleType miceq = new FunctionMenuMinMaxScaleType
            {
                Min = 0,
                Max = 1,
                Default = 1,
                currentValue = 1,
                ScaleName = "MIC EQ",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.MicEQ_0N_Or_Off
            };
            FunctionMenuMinMaxScaleTypeList.Add(miceq);

            FunctionMenuMinMaxScaleType proclevel = new FunctionMenuMinMaxScaleType
            {
                Min = 0,
                Max = 100,
                Default = 0,
                currentValue = 0,
                ScaleName = "PROC LEVEL",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.ProcLevel_Off_To_100
            };
            FunctionMenuMinMaxScaleTypeList.Add(proclevel);

            FunctionMenuMinMaxScaleType amclevel = new FunctionMenuMinMaxScaleType
            {
                Min = 1,
                Max = 100,
                Default = 1,
                currentValue = 1,
                ScaleName = "AMC LEVEL",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.AMCLevel_1_To_100
            };
            FunctionMenuMinMaxScaleTypeList.Add(amclevel);

            FunctionMenuMinMaxScaleType voxgain = new FunctionMenuMinMaxScaleType
            {
                Min = 0,
                Max = 100,
                Default = 50,
                currentValue = 50,
                ScaleName = "VOX GAIN",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.VOXGain_0_To_100
            };
            FunctionMenuMinMaxScaleTypeList.Add(voxgain);

            FunctionMenuMinMaxScaleType voxdelay = new FunctionMenuMinMaxScaleType
            {
                Min = 30,
                Max = 3000,
                Default = 200,
                currentValue = 200,
                ScaleName = "VOX DELAY",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.VOXDelay_30_To_3000
            };
            FunctionMenuMinMaxScaleTypeList.Add(voxdelay);

            FunctionMenuMinMaxScaleType antivox = new FunctionMenuMinMaxScaleType
            {
                Min = 0,
                Max = 100,
                Default = 50,
                currentValue = 50,
                ScaleName = "ANTI VOX",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.AntiVox_0_To_100
            };
            FunctionMenuMinMaxScaleTypeList.Add(antivox);

            FunctionMenuMinMaxScaleType rfpower = new FunctionMenuMinMaxScaleType
            {
                Min = 5,
                Max = 100,
                Default = 5,
                currentValue = 5,
                ScaleName = "RF POWER",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.RFPower_5_To_100
            };
            FunctionMenuMinMaxScaleTypeList.Add(rfpower);

            FunctionMenuMinMaxScaleType monilevel = new FunctionMenuMinMaxScaleType
            {
                Min = 0,
                Max = 100,
                Default = 0,
                currentValue = 0,
                ScaleName = "MONI LEVEL",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.MoniLevel_0_To_100
            };
            FunctionMenuMinMaxScaleTypeList.Add(monilevel);

            FunctionMenuMinMaxScaleType keyer = new FunctionMenuMinMaxScaleType
            {
                Min = 0,
                Max = 1,
                Default = 0,
                currentValue = 0,
                ScaleName = "KEYER",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.Keyer_0_To_1
            };
            FunctionMenuMinMaxScaleTypeList.Add(keyer);

            FunctionMenuMinMaxScaleType bkin = new FunctionMenuMinMaxScaleType
            {
                Min = 0,
                Max = 1,
                Default = 0,
                currentValue = 0,
                ScaleName = "BK-IN",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.BkIn_0_To_1
            };
            FunctionMenuMinMaxScaleTypeList.Add(bkin);

            FunctionMenuMinMaxScaleType cwspeed = new FunctionMenuMinMaxScaleType
            {
                Min = 4,
                Max = 60,
                Default = 20,
                currentValue = 20,
                ScaleName = "CW SPEED",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.CwSpeed_4_To_60
            };
            FunctionMenuMinMaxScaleTypeList.Add(cwspeed);

            FunctionMenuMinMaxScaleType cwpitch = new FunctionMenuMinMaxScaleType
            {
                Min = 300,
                Max = 1050,
                Default = 700,
                currentValue = 700,
                ScaleName = "CW PITCH",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.CwPitch_300_To_1050
            };
            FunctionMenuMinMaxScaleTypeList.Add(cwpitch);

            FunctionMenuMinMaxScaleType bkdelay = new FunctionMenuMinMaxScaleType
            {
                Min = 30,
                Max = 1050,
                Default = 200,
                currentValue = 200,
                ScaleName = "BK-DELAY",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.BkDelay_30_To_1050
            };
            FunctionMenuMinMaxScaleTypeList.Add(bkdelay);

            FunctionMenuMinMaxScaleType message = new FunctionMenuMinMaxScaleType
            {
                Min = 1,
                Max = 5,
                Default = 1,
                currentValue = 1,
                ScaleName = "MESSAGE",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.Message_1_To_5
            };
            FunctionMenuMinMaxScaleTypeList.Add(message);

            FunctionMenuMinMaxScaleType record = new FunctionMenuMinMaxScaleType
            {
                Min = 0,
                Max = 1,
                Default = 0,
                currentValue = 0,
                ScaleName = "RECORD",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.Record_0_To_1
            };
            FunctionMenuMinMaxScaleTypeList.Add(record);

            FunctionMenuMinMaxScaleType play = new FunctionMenuMinMaxScaleType
            {
                Min = 1,
                Max = 6,
                Default = 1,
                currentValue = 1,
                ScaleName = "PLAY",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.Play_1_To_6
            };
            FunctionMenuMinMaxScaleTypeList.Add(play);

            FunctionMenuMinMaxScaleType txw = new FunctionMenuMinMaxScaleType
            {
                Min = 0,
                Max = 0,
                Default = 0,
                currentValue = 0,
                ScaleName = "TXW",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.Txw_0
            };
            FunctionMenuMinMaxScaleTypeList.Add(txw);

            FunctionMenuMinMaxScaleType aess = new FunctionMenuMinMaxScaleType
            {
                Min = 0,
                Max = 100,
                Default = 50,
                currentValue = 50,
                ScaleName = "AESS",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.Aess_0_To_100
            };
            FunctionMenuMinMaxScaleTypeList.Add(aess);

            FunctionMenuMinMaxScaleType aesscf = new FunctionMenuMinMaxScaleType
            {
                Min = 70,
                Max = 1000,
                Default = 700,
                currentValue = 700,
                ScaleName = "AESS-CF",
                ScaleNamePosition = FunctionMenuScaleNamePositions.ToLeft,
                ScaleType = FunctionMenuScaleTypes.AessCf_700_To_1000
            };
            FunctionMenuMinMaxScaleTypeList.Add(aesscf);
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

            SetFunctionMenuSelectedItemLevel(0, mainWindow.FunctionValueTextBlock, true);
        }

        public static void SetFunctionMenuSelectedItemLevel(Double delta, TextBlock FunctionValueTextBlock, bool LoadOnly = false)
        {
            FunctionMenuMinMaxScaleType f = FunctionMenuMinMaxScaleTypeList[FunctionMenuSelectedItem];

            if (!(LoadOnly))
            {
                if (delta > 0 && f.currentValue < f.Max)
                {
                    f.currentValue++;
                }
                else if (delta <= 0 && f.currentValue > f.Min)
                {
                    f.currentValue--;
                }
            }

            if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.CurrentDebug)
            {
                Console.WriteLine("f.currentValue = " + f.currentValue);
            }

            FunctionMenuMinMaxScaleTypeList[FunctionMenuSelectedItem].currentValue = f.currentValue;

            String FunctionValueTextBlockText = String.Empty;
            switch (f.ScaleNamePosition)
            {
                case FunctionMenuScaleNamePositions.None:
                    switch (f.ScaleType)
                    {
                        case FunctionMenuScaleTypes.Marker_ON_Or_Off:
                            FunctionValueTextBlockText += " ";
                            
                            if (f.currentValue > 0)
                                FunctionValueTextBlockText += "ON";
                            else
                                FunctionValueTextBlockText += "OFF";
                            break;
                    }
                    break;
                case FunctionMenuScaleNamePositions.ToLeft:
                    FunctionValueTextBlockText += f.ScaleName;
                    FunctionValueTextBlockText += " ";
                    FunctionValueTextBlockText += f.currentValue;
                    break;
                case FunctionMenuScaleNamePositions.ToRight:
                    FunctionValueTextBlockText += f.currentValue;
                    FunctionValueTextBlockText += " ";
                    FunctionValueTextBlockText += f.ScaleName;
                    break;
            }

            FunctionValueTextBlock.Text = FunctionValueTextBlockText;
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
