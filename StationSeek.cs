using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static YAESU_FT_891_Front_End.MyStructs;
using static YAESU_FT_891_Front_End.RigStateChanges;
using static YAESU_FT_891_Front_End.YAESU_FT_891_CAT_Dictionary;
using System.Windows.Controls;
using System.Windows;

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

            mainWindow.fT891S_SerialPort.StopSerialLoop();

            mainWindow.yAESU_FT_891_CAT_Dictionary.SetRfGain(_port, 30);
            await Task.Delay(20);

            Int32 PositionInTheList = 1;

            for (long freq = startFrequency; freq <= endFrequency; freq += freqStep)
            {
                mainWindow.yAESU_FT_891_CAT_Dictionary.FreqA(_port, freq);
                await Task.Delay(10);

                mainWindow.yAESU_FT_891_CAT_Dictionary.FreqA(_port, 0);
                await Task.Delay(10);

                mainWindow.yAESU_FT_891_CAT_Dictionary.SMeter(_port, SMeters.S);
                await Task.Delay(20);

                window.RigBlurVFOCanvas.Visibility = Visibility.Visible;
                window.RigBlurVFOCanvasBlurEffect.Radius = 4;

                if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
                {
                    Console.Write("LastSMeterReading is ");
                    Console.WriteLine(LastSMeterReading);
                }

                if (LastSMeterReading >= signalStrengthThreshold)
                {
                    StationSeekClass station = new StationSeekClass { ID = PositionInTheList, Frequency = freq, NumTimesEmpty = 0, SignalStrength = LastSMeterRawReading };
                    AddActiveStation(station);
                    UpdateFoundStationCountLabel(FoundStationCountLabel, StationSeekActiveList.Count.ToString());
                    mainWindow.StationScopeListView.Items.Add(new StationScope(mainWindow, station, mainWindow.frequencyManagement));
                    PositionInTheList++;
                }

                if (RequestToStopScanning)
                {
                    mainWindow.yAESU_FT_891_CAT_Dictionary.SetRfGain(_port, 0);
                    await Task.Delay(20);

                    mainWindow.fT891S_SerialPort.StartSerialLoop();

                    window.RigBlurVFOCanvas.Visibility = Visibility.Hidden;

                    IsScanning = false;
                    RequestToStopScanning = false;
                    return;
                }
            }

            if (RigMode != RigModes.FM)
                mainWindow.yAESU_FT_891_CAT_Dictionary.SetRfGain(_port, 0);
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

            mainWindow.fT891S_SerialPort.StartSerialLoop();
        }

        public void UpdateFoundStationCountLabel(Label FoundStationCountLabel, string foundStationCountLabel)
        {
            FoundStationCountLabel.Content = foundStationCountLabel;
        }

        public async void ScanFoundStations(SerialPort _port)
        {
            if (IsScanning) return;

            IsScanning = true;

            mainWindow.fT891S_SerialPort.StopSerialLoop();

            mainWindow.yAESU_FT_891_CAT_Dictionary.SetRfGain(_port, 30);
            await Task.Delay(20);

            foreach (StationSeekClass foundStation in StationSeekActiveList)
            {
                mainWindow.yAESU_FT_891_CAT_Dictionary.FreqA(_port, foundStation.Frequency);
                await Task.Delay(10);

                mainWindow.yAESU_FT_891_CAT_Dictionary.FreqA(_port, 0);
                await Task.Delay(10);

                mainWindow.yAESU_FT_891_CAT_Dictionary.SMeter(_port, SMeters.S);
                await Task.Delay(20);

                await Task.Delay(1000);
            }

            mainWindow.yAESU_FT_891_CAT_Dictionary.SetRfGain(_port, 0);
            await Task.Delay(20);

            mainWindow.fT891S_SerialPort.StartSerialLoop();

            IsScanning = false;
        }
    }
}