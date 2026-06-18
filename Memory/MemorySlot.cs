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
        // 1. Changed from 'async void' to 'async Task' for proper async handling and error tracking
        public async Task SwapVFOs(MainWindow mainWindow)
        {
            mainWindow._catManager.StopOutgoingDataLoop();

            // 1. Declare out variables explicitly before using them (Required in older C#)
            RadioState vfoAState = null;
            RadioState vfoBState = null;

            // 2. Fetch both states safely
            if (MemorySlotDictionary.TryGetValue((int)MemorySlots.VFO_A, out vfoAState) &&
                MemorySlotDictionary.TryGetValue((int)MemorySlots.VFO_B, out vfoBState))
            {
                // 3. Swap the data payloads in the dictionary
                MemorySlotDictionary[(int)MemorySlots.VFO_A] = vfoBState;
                MemorySlotDictionary[(int)MemorySlots.VFO_B] = vfoAState;

                // 4. Toggle the tracking state using standard if/else statements
                if (CurrentOccupierOfMainRigState == MemorySlots.VFO_A)
                {
                    CurrentOccupierOfMainRigState = MemorySlots.VFO_B;
                    CurrentOccupierOfSubRigState = MemorySlots.VFO_A;
                }
                else
                {
                    CurrentOccupierOfMainRigState = MemorySlots.VFO_A;
                    CurrentOccupierOfSubRigState = MemorySlots.VFO_B;
                }

                // 5. Update local state references based on the new positions
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
            mainWindow.frequencyManagement.GetFrequency(MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, mainWindow.MainFrequencyTextBlock);
            mainWindow.frequencyManagement.GetFrequency(MemorySlots.VFO_B, FrequencyLocations.RXFrequencyHz, mainWindow.SubFrequencyTextBlock);

            // 8. Update UI Mode Visuals
            RigModeClass rmcm = RigStateChanges.ChangeMode(MainRigState.OperatingMode);
            mainWindow.MainRigModeLabelBorder.Background = new SolidColorBrush(rmcm.BackgroundColor);
            mainWindow.MainRigModeLabel.Foreground = new SolidColorBrush(rmcm.ForegroundColor);
            mainWindow.MainRigModeLabel.Content = rmcm.Name;

            RigModeClass rmcs = RigStateChanges.ChangeMode(SubRigState.OperatingMode);
            mainWindow.SubRigModeLabelBorder.Background = new SolidColorBrush(rmcs.BackgroundColor);
            mainWindow.SubRigModeLabel.Foreground = new SolidColorBrush(rmcs.ForegroundColor);
            mainWindow.SubRigModeLabel.Content = rmcs.Name;

            // 9. Send CAT Hardware Commands
            // 9. Send CAT Hardware Commands
            try
            {
                // --- STEP A: Update the CURRENT Main VFO before swapping ---
                // Because the physical rig hasn't flipped yet, the "current active VFO" 
                // on the radio actually needs the data we just put into SubRigState!
                await mainWindow._catManager.SendCatCommandAsync("BA", mainWindow._catManager.OutGoingDataLoopDelay);

                await mainWindow._catManager.SendCatCommandAsync("FA", new object[] { SubRigState.VfoAFrequency }, mainWindow._catManager.OutGoingDataLoopDelay);
                await mainWindow._catManager.SendCatCommandAsync("MD", new object[] { 0, ((int)Convert.ToInt16(SubRigState.OperatingMode)).ToString("X") }, mainWindow._catManager.OutGoingDataLoopDelay);

                await mainWindow._catManager.SendCatCommandAsync("AB", mainWindow._catManager.OutGoingDataLoopDelay);

                // --- STEP B: Swap the radio's active VFO ---
                // This makes the radio flip its context to the other VFO slot.
                if (IsVFO_AB)
                {
                    IsVFO_AB = false;
                    await mainWindow._catManager.SendCatCommandAsync("BA", mainWindow._catManager.OutGoingDataLoopDelay);
                }
                else
                {
                    IsVFO_AB = true;
                    //await mainWindow._catManager.SendCatCommandAsync("AB", mainWindow._catManager.OutGoingDataLoopDelay);
                }

                // --- STEP C: Update the NEW active VFO (which used to be Sub) ---
                // Now that the rig has flipped, the primary VFO register on the radio 
                // is ready to receive our target MainRigState data.
                await mainWindow._catManager.SendCatCommandAsync("FA", new object[] { MainRigState.VfoAFrequency }, mainWindow._catManager.OutGoingDataLoopDelay);
                await mainWindow._catManager.SendCatCommandAsync("MD", new object[] { 0, ((int)Convert.ToInt16(MainRigState.OperatingMode)).ToString("X") }, mainWindow._catManager.OutGoingDataLoopDelay);

                mainWindow._catManager.StartOutgoingDataLoop();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("CAT VFO Swap Failed: " + ex.Message);
            }
        }
    }
}