using FT891S_CatControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using static YAESU_FT_891_Front_End.RigState;
using static YAESU_FT_891_Front_End.RigStateChanges;

namespace YAESU_FT_891_Front_End
{
    public class MemorySlot
    {
        public struct MemorySlots
        {
            public const int VFO_A = 0;//this is Main
            public const int VFO_B = 1;//this is Sub
            public const int BandStart_160M = 2;
            public const int BandFinish_160M = 3;
            public const int BandStart_80M = 4;
            public const int BandFinish_80M = 5;
            public const int BandStart_60M = 6;
            public const int BandFinish_60M = 7;
            public const int BandStart_40M = 8;
            public const int BandFinish_40M = 9;
            public const int MemoryBankStart_1 = 10;
            public const int MemoryBankFinish_1 = 11;
        }

        //MemorySlotDictionary index 0 and 1 are read write
        //MemorySlotDictionary index > 1 are readonly
        public Dictionary<Int32, RadioState> MemorySlotDictionary = new Dictionary<Int32, RadioState> { };

        public byte CurrentOccupierOfMainRigState = MemorySlots.VFO_A;
        public byte CurrentOccupierOfSubRigState = MemorySlots.VFO_B;

        public RadioState MainRigState = new RadioState();
        public RadioState SubRigState = new RadioState();

        MainWindow mainWindow;
        public MemorySlot(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;

            SetUpVFOs();
        }

        private void SetUpVFOs()
        {
            RadioState rs = new RadioState();
            rs.VfoAFrequency = 14200000;
            rs.VfoBFrequency = 7150000;
            rs.OperatingMode = RadioMode.USB;

            MemorySlotDictionary.Add(MemorySlots.VFO_A, rs);//main is the contents of VFO A

            rs = new RadioState();
            rs.VfoAFrequency = 7150000;
            rs.VfoBFrequency = 14200000;
            rs.OperatingMode = RadioMode.AM;

            MemorySlotDictionary.Add(MemorySlots.VFO_B, rs);//sub is the contents of VFO B

            foreach (KeyValuePair<int, RadioState> item in MemorySlotDictionary)
            {
                int key = item.Key;
                RadioState value = item.Value;

                Console.Write($"{key} -> {value.VfoAFrequency}");
                Console.WriteLine($"{key} -> {value.OperatingMode}");
            }

            RadioState main;

            if (MemorySlotDictionary.TryGetValue((int)MemorySlots.VFO_A, out main))
            {
                MainRigState = main;
            }

            RadioState sub;

            if (MemorySlotDictionary.TryGetValue((int)MemorySlots.VFO_B, out sub))
            {
                SubRigState = sub;
            }
        }

        public void InitVFOs()
        {
            RadioState main;

            if (MemorySlotDictionary.TryGetValue((int)MemorySlots.VFO_A, out main))
            {
                MainRigState = main;
                CurrentOccupierOfMainRigState = MemorySlots.VFO_A;
            }

            RadioState sub;

            if (MemorySlotDictionary.TryGetValue((int)MemorySlots.VFO_B, out sub))
            {
                SubRigState = sub;
                CurrentOccupierOfSubRigState = MemorySlots.VFO_B;
            }

            mainWindow.frequencyManagement.GetFrequency(MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, mainWindow.MainFrequencyTextBlock);
            mainWindow.frequencyManagement.GetFrequency(MemorySlots.VFO_B, FrequencyLocations.RXFrequencyHz, mainWindow.SubFrequencyTextBlock);

            //mainWindow.MainVFOABLabel.Content = SubRigState.Mode;

            RigModeClass rmcm = RigStateChanges.ChangeMode(MainRigState.OperatingMode);

            mainWindow.MainRigModeLabelBorder.Background = new SolidColorBrush(rmcm.BackgroundColor);
            mainWindow.MainRigModeLabel.Foreground = new SolidColorBrush(rmcm.ForegroundColor);
            mainWindow.MainRigModeLabel.Content = rmcm.Name;

            RigModeClass rmcs = RigStateChanges.ChangeMode(SubRigState.OperatingMode);

            mainWindow.SubRigModeLabelBorder.Background = new SolidColorBrush(rmcs.BackgroundColor);
            mainWindow.SubRigModeLabel.Foreground = new SolidColorBrush(rmcs.ForegroundColor);
            mainWindow.SubRigModeLabel.Content = rmcs.Name;

        }
        public async void SwapVFOs(MainWindow mainWindow)
        {
            if (CurrentOccupierOfMainRigState == MemorySlots.VFO_A)
            {
                RadioState sub;

                if (MemorySlotDictionary.TryGetValue((int)MemorySlots.VFO_B, out sub))
                {
                    MainRigState = sub;
                    CurrentOccupierOfMainRigState = MemorySlots.VFO_B;
                }

                RadioState main;

                if (MemorySlotDictionary.TryGetValue((int)MemorySlots.VFO_A, out main))
                {
                    SubRigState = main;
                    CurrentOccupierOfSubRigState = MemorySlots.VFO_A;
                }
            }
            else if (CurrentOccupierOfMainRigState == MemorySlots.VFO_B)
            {
                RadioState main;

                if (MemorySlotDictionary.TryGetValue((int)MemorySlots.VFO_A, out main))
                {
                    MainRigState = main;
                    CurrentOccupierOfMainRigState = MemorySlots.VFO_A;
                }

                RadioState sub;

                if (MemorySlotDictionary.TryGetValue((int)MemorySlots.VFO_B, out sub))
                {
                    SubRigState = sub;
                    CurrentOccupierOfSubRigState = MemorySlots.VFO_B;
                }
            }

            if (CurrentOccupierOfMainRigState == MemorySlots.VFO_A)
            {
                mainWindow.MainVFOABLabel.Content = "VFO-A";
                mainWindow.SubVFOABLabel.Content = "VFO-B";
            }
            else if (CurrentOccupierOfMainRigState == MemorySlots.VFO_B)
            {
                mainWindow.MainVFOABLabel.Content = "VFO-B";
                mainWindow.SubVFOABLabel.Content = "VFO-A";
            }

            mainWindow.frequencyManagement.GetFrequency(MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, mainWindow.MainFrequencyTextBlock);
            mainWindow.frequencyManagement.GetFrequency(MemorySlots.VFO_B, FrequencyLocations.RXFrequencyHz, mainWindow.SubFrequencyTextBlock);

            RigModeClass rmcm = RigStateChanges.ChangeMode(MainRigState.OperatingMode);

            mainWindow.MainRigModeLabelBorder.Background = new SolidColorBrush(rmcm.BackgroundColor);
            mainWindow.MainRigModeLabel.Foreground = new SolidColorBrush(rmcm.ForegroundColor);
            mainWindow.MainRigModeLabel.Content = rmcm.Name;

            RigModeClass rmcs = RigStateChanges.ChangeMode(SubRigState.OperatingMode);

            mainWindow.SubRigModeLabelBorder.Background = new SolidColorBrush(rmcs.BackgroundColor);
            mainWindow.SubRigModeLabel.Foreground = new SolidColorBrush(rmcs.ForegroundColor);
            mainWindow.SubRigModeLabel.Content = rmcs.Name;

            //FT891SerialPort.StopSerialLoop();

            //Main
            //write sub mode to main
            //await mainWindow._catManager.SendCatCommandAsync("MD", new object[] { 0, Convert.ToInt16(MainRigState.OperatingMode) }, mainWindow._catManager.OutGoingDataLoopDelay);

            //update UI incase rig is turned off or not connected
            //UpdateUIRigMode(mainWindow.MainRigModeLabelBorder, mainWindow.MainRigModeLabel, MainRigState.OperatingMode);


            //Sub
            //to switch A into B you have to do it in A the swap it
            await mainWindow._catManager.SendCatCommandAsync("AB", mainWindow._catManager.OutGoingDataLoopDelay);

            //update UI incase rig is turned off or not connected
            //UpdateUIRigMode(mainWindow.SubRigModeLabelBorder, mainWindow.SubRigModeLabel, SubRigState.OperatingMode);

            //await mainWindow._catManager.SendCatCommandAsync("MD", new object[] { 0, Convert.ToInt16(SubRigState.OperatingMode) }, mainWindow._catManager.OutGoingDataLoopDelay);

        }
    }
}