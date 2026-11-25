using FT891S_CatControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using static YAESU_FT_891_Front_End.RigStateChanges;
using YAESU_FT_891_Front_End.Models;

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

        private bool IsVFO_AB = false;

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
            rs.ModeA = RadioMode.USB;
            rs.WattsA = 5;

            MemorySlotDictionary.Add(MemorySlots.VFO_A, rs);//main is the contents of VFO A

            rs = new RadioState();
            rs.VfoAFrequency = 7150000;
            rs.VfoBFrequency = 14200000;
            rs.ModeB = RadioMode.AM;
            rs.WattsB = 10;

            MemorySlotDictionary.Add(MemorySlots.VFO_B, rs);//sub is the contents of VFO B

            foreach (KeyValuePair<int, RadioState> item in MemorySlotDictionary)
            {
                int key = item.Key;
                RadioState value = item.Value;

                Console.Write($"{key} -> {value.VfoAFrequency}");
                Console.WriteLine($"{key} -> {value.ModeA}");
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

        //public void InitVFOs()
        //{
        //    RadioState main;

        //    if (MemorySlotDictionary.TryGetValue((int)MemorySlots.VFO_A, out main))
        //    {
        //        MainRigState = main;
        //        CurrentOccupierOfMainRigState = MemorySlots.VFO_A;
        //    }

        //    RadioState sub;

        //    if (MemorySlotDictionary.TryGetValue((int)MemorySlots.VFO_B, out sub))
        //    {
        //        SubRigState = sub;
        //        CurrentOccupierOfSubRigState = MemorySlots.VFO_B;
        //    }

        //    mainWindow.frequencyManagement.GetFrequency(MemorySlots.VFO_A, mainWindow.MainFrequencyTextBlock);
        //    mainWindow.frequencyManagement.GetFrequency(MemorySlots.VFO_B, mainWindow.SubFrequencyTextBlock);

        //    //mainWindow.MainVFOABLabel.Content = SubRigState.Mode;

        //    RigModeDisplay rmcm = RigStateChanges.ChangeMode(MainRigState.ModeA);

        //    mainWindow.MainRigModeLabelBorder.Background = new SolidColorBrush(rmcm.BackgroundColor);
        //    mainWindow.MainRigModeLabel.Foreground = new SolidColorBrush(rmcm.ForegroundColor);
        //    mainWindow.MainRigModeLabel.Content = rmcm.Name;

        //    RigModeDisplay rmcs = RigStateChanges.ChangeMode(SubRigState.ModeA);

        //    mainWindow.SubRigModeLabelBorder.Background = new SolidColorBrush(rmcs.BackgroundColor);
        //    mainWindow.SubRigModeLabel.Foreground = new SolidColorBrush(rmcs.ForegroundColor);
        //    mainWindow.SubRigModeLabel.Content = rmcs.Name;

        //}
        // 1. Changed from 'async void' to 'async Task' for proper async handling and error tracking
        public async Task GetVFOs()
        {
            //mainWindow._catManager.StopOutgoingDataLoop();

            // 1. Declare out variables explicitly before using them (Required in older C#)
            RadioState vfoAState = new RadioState();
            RadioState vfoBState = new RadioState();

            //retreive VFO A & B Frequency
            await mainWindow._catManager.SendCatCommandAsync("FA", mainWindow._catManager.OutGoingDataLoopDelay);
            vfoAState.VfoAFrequency = FT891S_CatManager.currentRadioState.VfoAFrequency;

            //Note Rig Mode is the mode of VFO-A Main
            //retreive Rig Mode
            await mainWindow._catManager.SendCatCommandAsync("MD", "0", mainWindow._catManager.OutGoingDataLoopDelay);
            vfoAState.ModeA = FT891S_CatManager.currentRadioState.ModeA;

            //Note Rig Power is the Power of VFO-A Main
            //retreive Rig Power
            await mainWindow._catManager.SendCatCommandAsync("PC", mainWindow._catManager.OutGoingDataLoopDelay);
            vfoAState.WattsA = FT891S_CatManager.currentRadioState.WattsA;

            //SV = Swap VFO
            await mainWindow._catManager.SendCatCommandAsync("SV", mainWindow._catManager.OutGoingDataLoopDelay);

            await mainWindow._catManager.SendCatCommandAsync("FA", mainWindow._catManager.OutGoingDataLoopDelay);
            vfoBState.VfoBFrequency = FT891S_CatManager.currentRadioState.VfoBFrequency;

            mainWindow.Mode_ModeChange(FT891S_CatManager.currentRadioState.ModeA);
            vfoBState.ModeB = FT891S_CatManager.currentRadioState.ModeA;

            await mainWindow._catManager.SendCatCommandAsync("PC", new object[] { FT891S_CatManager.currentRadioState.WattsA }, mainWindow._catManager.OutGoingDataLoopDelay);
            vfoBState.WattsB = FT891S_CatManager.currentRadioState.WattsA;

            RadioState retreivedVfoAState;
            RadioState retreivedVfoBState;

            // 2. Fetch both states safely
            if (MemorySlotDictionary.TryGetValue((int)MemorySlots.VFO_A, out retreivedVfoAState) &&
                MemorySlotDictionary.TryGetValue((int)MemorySlots.VFO_B, out retreivedVfoBState))
            {
                retreivedVfoAState.VfoAFrequency = vfoAState.VfoAFrequency;
                retreivedVfoAState.ModeA = vfoAState.ModeA;
                retreivedVfoAState.WattsA = vfoAState.WattsA;
                
                retreivedVfoBState.VfoBFrequency = vfoBState.VfoBFrequency;

                //5.Update local state references based on the new positions
                MainRigState = MemorySlotDictionary[(int)MemorySlots.VFO_A];
                SubRigState = MemorySlotDictionary[(int)MemorySlots.VFO_B];
            }

            // 6. Update the UI Labels
            if (CurrentOccupierOfMainRigState == MemorySlots.VFO_A)
            {
                mainWindow.MainVFOABLabel.Content = "VFO-A";
                mainWindow.SubVFOABLabel.Content = "VFO-B";
            }
            else
            {
                mainWindow.MainVFOABLabel.Content = "VFO-B";
                mainWindow.SubVFOABLabel.Content = "VFO-A";
            }

            // 7. Refresh UI Frequencies
            mainWindow.frequencyManagement.GetFrequency(MemorySlots.VFO_A, mainWindow.MainFrequencyTextBlock);
            mainWindow.frequencyManagement.GetFrequency(MemorySlots.VFO_B, mainWindow.SubFrequencyTextBlock);
        }
        public async Task SwapVFOs(MainWindow mainWindow)
        {
            mainWindow._catManager.StopOutgoingDataLoop();

            mainWindow.SubLargeFrequencyDisplay.Frequency = mainWindow.LargeFrequencyDisplay.Frequency;

            mainWindow.SubRigModeLabelBorder.Background = mainWindow.MainRigModeLabelBorder.Background;
            mainWindow.SubRigModeLabel.Foreground = mainWindow.MainRigModeLabel.Foreground;
            mainWindow.SubRigModeLabel.Content = mainWindow.MainRigModeLabel.Content;

            mainWindow.SubPowerControlLabel.Content = mainWindow.PowerControlLabel.Content;

            // 6. Update the UI Labels
            if (CurrentOccupierOfMainRigState == MemorySlots.VFO_A)
            {
                mainWindow.MainVFOABLabel.Content = "VFO-B";
                mainWindow.SubVFOABLabel.Content = "VFO-A";
               
                CurrentOccupierOfMainRigState = MemorySlots.VFO_B;
            }
            else
            {
                mainWindow.MainVFOABLabel.Content = "VFO-A";
                mainWindow.SubVFOABLabel.Content = "VFO-B";
                
                CurrentOccupierOfMainRigState = MemorySlots.VFO_A;
            }
            //SV = Swap VFO
            await mainWindow._catManager.SendCatCommandAsync("SV", mainWindow._catManager.OutGoingDataLoopDelay);

            mainWindow._catManager.StartOutgoingDataLoop();
        }

    }
}