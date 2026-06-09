using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static YAESU_FT_891_Front_End.MyStructs;

namespace YAESU_FT_891_Front_End
{
    public class FT891S_CatStates
    {
        public String Set;
        public String Read;
        public String Answer;
    }
    public struct FT891S_CatCommandTypes
    {
        //Name the cat command like the cat pdf FA_FREQUENCY_VFO_A_P1P1P1P1P1P1P1P1P1 & GT_AGC_FUNCTION_P1_P2
        public const byte FA = 0;
        public const byte FB = 1;
        public const byte BY = 2;
    }
    public struct YaesuCatCommandReturnTypes
    {
        public const byte Fixed = 0;
        public const byte Bool = 1;
        public const byte HexNibble = 2;
        public const byte Byte = 3;
        public const byte Word = 4;
        public const byte Long = 5;
        public const byte Char = 6;
        public const byte String = 7;
    }
    public struct YaesuCatCommandReadWriteStatus
    {
        public const byte ReadOnly = 0;
        public const byte WriteOnly = 1;
        public const byte ReadWrite = 2;
    }
    public class YaesuCatCommandParameter
    {
        public Int32 ParameterReturnType { get; set; }
        public Int32 ParameterStartPosition { get; set; }
        public Int32 ParameterNumberOfCharToReturn { get; set; }

    }
    public class FT891S_CatCommand
    {
        public Int32 FT891S_CatCommand_ID { get; set; }
        public String YaesuCatName { get; set; }
        public String YaesuCatDescription { get; set; }
        public byte YaesuCatCommandReadWrite { get; set; }
        public YaesuCatCommandParameter YaesuCatCommand_P0 { get; set; }
        public YaesuCatCommandParameter YaesuCatCommand_P1 { get; set; }
        public YaesuCatCommandParameter YaesuCatCommand_P2 { get; set; }
        public YaesuCatCommandParameter YaesuCatCommand_P3 { get; set; }
        public YaesuCatCommandParameter YaesuCatCommand_P4 { get; set; }
        public YaesuCatCommandParameter YaesuCatCommand_P5 { get; set; }
        public YaesuCatCommandParameter YaesuCatCommand_P6 { get; set; }
        public YaesuCatCommandParameter YaesuCatCommand_P7 { get; set; }
        public YaesuCatCommandParameter YaesuCatCommand_P8 { get; set; }
        public YaesuCatCommandParameter YaesuCatCommand_P9 { get; set; }
        public YaesuCatCommandParameter YaesuCatCommand_P10 { get; set; }
        public YaesuCatCommandParameter YaesuCatCommand_P11 { get; set; }
        public YaesuCatCommandParameter YaesuCatCommand_P12 { get; set; }

        public FT891S_CatCommand(int fT891S_CatCommand_ID, string yaesuCatName, string yaesuCatDescription, byte yaesuCatCommandReadWrite, YaesuCatCommandParameter yaesuCatCommand_P0 = null
           , YaesuCatCommandParameter yaesuCatCommand_P1 = null, YaesuCatCommandParameter yaesuCatCommand_P2 = null, YaesuCatCommandParameter yaesuCatCommand_P3 = null
           , YaesuCatCommandParameter yaesuCatCommand_P4 = null, YaesuCatCommandParameter yaesuCatCommand_P5 = null, YaesuCatCommandParameter yaesuCatCommand_P6 = null
           , YaesuCatCommandParameter yaesuCatCommand_P7 = null, YaesuCatCommandParameter yaesuCatCommand_P8 = null, YaesuCatCommandParameter yaesuCatCommand_P9 = null
           , YaesuCatCommandParameter yaesuCatCommand_P10 = null, YaesuCatCommandParameter yaesuCatCommand_P11 = null, YaesuCatCommandParameter yaesuCatCommand_P12 = null)
        {
            FT891S_CatCommand_ID = fT891S_CatCommand_ID;
            YaesuCatName = yaesuCatName;
            YaesuCatDescription = yaesuCatDescription;
            YaesuCatCommandReadWrite = yaesuCatCommandReadWrite;
            YaesuCatCommand_P0 = yaesuCatCommand_P0;
            YaesuCatCommand_P1 = yaesuCatCommand_P1;
            YaesuCatCommand_P2 = yaesuCatCommand_P2;
            YaesuCatCommand_P3 = yaesuCatCommand_P3;
            YaesuCatCommand_P4 = yaesuCatCommand_P4;
            YaesuCatCommand_P5 = yaesuCatCommand_P5;
            YaesuCatCommand_P6 = yaesuCatCommand_P6;
            YaesuCatCommand_P7 = yaesuCatCommand_P7;
            YaesuCatCommand_P8 = yaesuCatCommand_P8;
            YaesuCatCommand_P9 = yaesuCatCommand_P9;
            YaesuCatCommand_P10 = yaesuCatCommand_P10;
            YaesuCatCommand_P11 = yaesuCatCommand_P11;
            YaesuCatCommand_P12 = yaesuCatCommand_P12;
        }
        public class FT891S_CatCommands
        {

            public Dictionary<Int32, FT891S_CatCommand> FT891S_CatCommandDictionary = new Dictionary<Int32, FT891S_CatCommand>
            {
                { 0, new FT891S_CatCommand(0, "FA", "FREQUENCY VFO-A", YaesuCatCommandReadWriteStatus.ReadWrite
                   , null
                   , new YaesuCatCommandParameter{ParameterReturnType = YaesuCatCommandReturnTypes.Long, ParameterStartPosition = 3, ParameterNumberOfCharToReturn = 9 }) },

                { 1, new FT891S_CatCommand(1, "FB", "FREQUENCY VFO-B", YaesuCatCommandReadWriteStatus.ReadWrite
                   , null
                   , new YaesuCatCommandParameter{ParameterReturnType = YaesuCatCommandReturnTypes.Long, ParameterStartPosition = 3, ParameterNumberOfCharToReturn = 9 }) },

                { 2, new FT891S_CatCommand(2, "BY", "BUSY", YaesuCatCommandReadWriteStatus.ReadOnly
                   , null
                   , new YaesuCatCommandParameter{ParameterReturnType = YaesuCatCommandReturnTypes.Bool, ParameterStartPosition = 3, ParameterNumberOfCharToReturn = 1 }
                   , new YaesuCatCommandParameter{ParameterReturnType = YaesuCatCommandReturnTypes.Fixed, ParameterStartPosition = 4, ParameterNumberOfCharToReturn = 1 }) },

                { 3, new FT891S_CatCommand(3, "MD", "OPERATING MODE", YaesuCatCommandReadWriteStatus.ReadOnly
                   , null
                   , new YaesuCatCommandParameter{ParameterReturnType = YaesuCatCommandReturnTypes.Fixed, ParameterStartPosition = 4, ParameterNumberOfCharToReturn = 1 }
                   , new YaesuCatCommandParameter{ParameterReturnType = YaesuCatCommandReturnTypes.HexNibble, ParameterStartPosition = 5, ParameterNumberOfCharToReturn = 1 }) }
            };

            MainWindow mainWindow;
            public FT891S_CatCommands(MainWindow mainWindow)
            {
                this.mainWindow = mainWindow;
            }


            public void FT891S_DoCatCommand(byte FT891S_CatCommand, byte YaesuCatCommandReadWrite, Action _CallBackAction)
            {
                FT891S_CatCommand catCommand;

                if (FT891S_CatCommandDictionary.TryGetValue(FT891S_CatCommand, out catCommand))
                {
                    String CATCommandToSend = String.Empty;

                    CATCommandToSend += catCommand.YaesuCatName;

                    if (catCommand.YaesuCatCommand_P0 != null)
                    {
                        if (catCommand.YaesuCatCommand_P0.ParameterReturnType == YaesuCatCommandReturnTypes.Fixed)
                            CATCommandToSend += "0";
                    }

                    if (catCommand.YaesuCatCommand_P1 != null)
                    {
                        if (catCommand.YaesuCatCommand_P1.ParameterReturnType == YaesuCatCommandReturnTypes.Fixed)
                            CATCommandToSend += "0";
                    }

                    if (YaesuCatCommandReadWrite == YaesuCatCommandReadWriteStatus.ReadOnly)
                    {
                        if (catCommand.YaesuCatCommand_P1.ParameterStartPosition == catCommand.YaesuCatName.Length + 1)
                        {
                            CATCommandToSend += ";";

                            if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
                            {
                                Console.WriteLine("FT891S_DoCatCommand");
                                Console.Write("Send ReadOnly Cat Command = ");
                                Console.WriteLine(CATCommandToSend);
                            }

                            mainWindow.fT891S_SerialPort.SendCAT(mainWindow.fT891S_SerialPort._port, CATCommandToSend);
                        }
                    }
                    else if (YaesuCatCommandReadWrite == YaesuCatCommandReadWriteStatus.WriteOnly)
                    {
                        if (catCommand.YaesuCatCommand_P1.ParameterStartPosition == catCommand.YaesuCatName.Length + 1)
                        {
                            CATCommandToSend += ";";

                            if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
                            {
                                Console.WriteLine("FT891S_DoCatCommand");
                                Console.Write("Send ReadOnly Cat Command = ");
                                Console.WriteLine(CATCommandToSend);
                            }
                        }
                    }
                }
            }
        }
    }
}