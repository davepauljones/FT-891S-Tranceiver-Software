using FT891S_CatControl;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static YAESU_FT_891_Front_End.MyStructs;
using static YAESU_FT_891_Front_End.RigState;
using static YAESU_FT_891_Front_End.RigStateChanges;

namespace YAESU_FT_891_Front_End
{ 
    public class StationSeekClass
    {
        public Int32 ID;
        public long Frequency;
        public int NumTimesEmpty;
        public int SignalStrength;
    }

    public class StationSeek
    {
        public List<StationSeekClass> StationSeekActiveList = new List<StationSeekClass>();
        public int LastSMeterReading;
        public int LastSMeterRawReading;
        public bool IsScanning = false;
        public bool RequestToStopScanning = false;

        MainWindow mainWindow;
        public StationSeek(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }
        public void AddActiveStation(StationSeekClass ssc)
        {
            StationSeekActiveList.Add(ssc);
        }

        public void RemoveInactiveStation(StationSeekClass ssc)
        {
            StationSeekActiveList.Remove(ssc);
        }

        public async void SeekActiveStations(MainWindow mainWindow, SerialPort _port, long startFrequency, long endFrequency, int freqStep, int signalStrengthThreshold, Label FoundStationCountLabel)
        {
            if (IsScanning) return;

            IsScanning = true;

            var window = Application.Current.MainWindow as MainWindow;
            window.RigBlurVFOCanvas.Visibility = Visibility.Visible;
            window.RigBlurVFOCanvasBlurEffect.Radius = 4;

            StationSeekActiveList.Clear();
            mainWindow.StationScopeListView.Items.Clear();

            mainWindow._catManager.StopOutgoingDataLoop();

            //mainWindow.yAESU_FT_891_CAT_Dictionary.SetRfGain(_port, 30);
            await mainWindow._catManager.SendCatCommandAsync("RG", new object[] { 0, 30 }, mainWindow._catManager.OutGoingDataLoopDelay);

            Int32 PositionInTheList = 1;

            for (long freq = startFrequency; freq <= endFrequency; freq += freqStep)
            {
                //mainWindow.yAESU_FT_891_CAT_Dictionary.FreqA(_port, freq);
                await mainWindow._catManager.SendCatCommandAsync("FA", new object[] { freq }, 5);
                //mainWindow.frequencyManagement.SetFrequency(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, FT891S_CatManager.currentRadioState.VfoAFrequency, mainWindow.MainFrequencyTextBlock);
                //mainWindow.frequencyManagement.SetFrequency(freq);
                //await Task.Delay(mainWindow._catManager.OutGoingDataLoopDelay);

                mainWindow.frequencyManagement.SetFrequencyUI(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, freq, mainWindow.MainFrequencyTextBlock);

                //await mainWindow._catManager.SendCatCommandAsync("FA", mainWindow._catManager.OutGoingDataLoopDelay);
                //mainWindow._catManager.SendReadQuery("FA");
                //await Task.Delay(mainWindow._catManager.OutGoingDataLoopDelay);
                //mainWindow.yAESU_FT_891_CAT_Dictionary.FreqA(_port, 0);
                //await Task.Delay(10);

                if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOff)
                {
                    await mainWindow._catManager.SendCatCommandAsync("RM", new object[] { (int)MeterTypes.DependsOnFrontPanelMETER }, 5);
                }

                //mainWindow.yAESU_FT_891_CAT_Dictionary.SMeter(_port, SMeters.S);
                //await Task.Delay(20);

                window.RigBlurVFOCanvas.Visibility = Visibility.Visible;
                window.RigBlurVFOCanvasBlurEffect.Radius = 4;

                if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.CurrentDebug)
                {
                    Console.Write("FT891S_CatManager.currentRadioState.CurrentMeterReading is ");
                    Console.WriteLine(FT891S_CatManager.currentRadioState.CurrentMeterReading);
                    Console.Write("signalStrengthThreshold is ");
                    Console.WriteLine(signalStrengthThreshold);
                }

                if (MainWindow.GetSMeterInteger(FT891S_CatManager.currentRadioState.CurrentMeterReading) >= signalStrengthThreshold)
                {
                    StationSeekClass station = new StationSeekClass { ID = PositionInTheList, Frequency = freq, NumTimesEmpty = 0, SignalStrength = FT891S_CatManager.currentRadioState.CurrentMeterReading };
                    AddActiveStation(station);
                    UpdateFoundStationCountLabel(FoundStationCountLabel, StationSeekActiveList.Count.ToString());
                    mainWindow.StationScopeListView.Items.Add(new StationScope(mainWindow, station, mainWindow.frequencyManagement));
                    PositionInTheList++;
                }

                if (RequestToStopScanning)
                {
                    //mainWindow.yAESU_FT_891_CAT_Dictionary.SetRfGain(_port, 0);
                    await mainWindow._catManager.SendCatCommandAsync("RG", new object[] { 0, 0 }, mainWindow._catManager.OutGoingDataLoopDelay);

                    mainWindow._catManager.StartOutgoingDataLoop();

                    window.RigBlurVFOCanvas.Visibility = Visibility.Hidden;

                    IsScanning = false;
                    RequestToStopScanning = false;
                    return;
                }
            }

            if (RigMode != RigModes.FM)
                //mainWindow.yAESU_FT_891_CAT_Dictionary.SetRfGain(_port, 0);
                await mainWindow._catManager.SendCatCommandAsync("RG", new object[] { 0, 0 }, mainWindow._catManager.OutGoingDataLoopDelay);
            else
                mainWindow.fT891S_SerialPort.SendCAT(_port, "SQ015");

            await Task.Delay(20);

            if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
            {
                Console.Write("StationSeekActiveList Count is ");
                Console.WriteLine(StationSeekActiveList.Count);
            }
            await Task.Delay(20);

            window.RigBlurVFOCanvas.Visibility = Visibility.Hidden;
            IsScanning = false;

            if (mainWindow.StationScopeListView.Items.Count > 0)
            {
                mainWindow.StationScopeListView.SelectedItem = mainWindow.StationScopeListView.Items[0];
                mainWindow.StationScopeListView.ScrollIntoView(mainWindow.StationScopeListView.Items[0]);

                UpdateFoundStationCountLabel(FoundStationCountLabel, "1 of " + StationSeekActiveList.Count);
            }
            else
            {
                UpdateFoundStationCountLabel(FoundStationCountLabel, "No Stations Found!");
            }

            //ScanFoundStations(_port);

            mainWindow._catManager.StartOutgoingDataLoop();
        }

        public void UpdateFoundStationCountLabel(Label FoundStationCountLabel, string foundStationCountLabel)
        {
            FoundStationCountLabel.Content = foundStationCountLabel;
        }

        //public async void ScanFoundStations(SerialPort _port)
        //{
        //    if (IsScanning) return;

        //    IsScanning = true;

        //    mainWindow._catManager.StopOutgoingDataLoop();

        //    mainWindow.yAESU_FT_891_CAT_Dictionary.SetRfGain(_port, 30);
        //    await Task.Delay(20);

        //    foreach (StationSeekClass foundStation in StationSeekActiveList)
        //    {
        //        mainWindow.yAESU_FT_891_CAT_Dictionary.FreqA(_port, foundStation.Frequency);
        //        await Task.Delay(10);

        //        mainWindow.yAESU_FT_891_CAT_Dictionary.FreqA(_port, 0);
        //        await Task.Delay(10);

        //        mainWindow.yAESU_FT_891_CAT_Dictionary.SMeter(_port, SMeters.S);
        //        await Task.Delay(20);

        //        await Task.Delay(1000);
        //    }

        //    mainWindow.yAESU_FT_891_CAT_Dictionary.SetRfGain(_port, 0);
        //    await Task.Delay(20);

        //    mainWindow._catManager.StartOutgoingDataLoop();

        //    IsScanning = false;
        //}
    }
}