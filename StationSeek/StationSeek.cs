using FT891S_CatControl;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YAESU_FT_891_Front_End.Models;
using static YAESU_FT_891_Front_End.MyStructs;
using static YAESU_FT_891_Front_End.RigStateChanges;
using static YAESU_FT_891_Front_End.SimulatedWaterfall;

namespace YAESU_FT_891_Front_End
{ 
    public struct ScanBands
    {
        public const byte _80M = 0;
        public const byte _40M = 1;
        public const byte _20M = 2;
        public const byte _15M = 3;
        public const byte _12M = 4;
        public const byte _11M = 5;
        public const byte _10M = 6;
        public const byte _6M = 7;
        public const byte _START = 8;
        public const byte _STOP = 9;
    }
    public class StationSeekCriteriaClass
    {
        public byte ScanBand = ScanBands._20M;
        public long StartFreq = 14000000;
        public long EndFreq = 14350000;
        public int StepFreq = 1000;
        public int Threshold = 8;
    }
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
        public byte CurrentScanBand = ScanBands._20M;
        public StationSeekCriteriaClass currentStationSeekCriteria;

        MainWindow mainWindow;
        public StationSeek(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;

            mainWindow.ScanBandTextBlock.Text = "20M";
            mainWindow.ScanBandStartFreqTextBlock.Text = "14.000.000";
            mainWindow.ScanBandEndFreqTextBlock.Text = "14.350.000";
            mainWindow.ScanBandStepFreqTextBlock.Text = "1000";
            mainWindow.StationScopeSignalStrengthThresholdNumericUpDown.Value = 8;

            currentStationSeekCriteria = new StationSeekCriteriaClass { ScanBand = ScanBands._20M, StartFreq = 14000000, EndFreq = 14350000, StepFreq = 500, Threshold = 8 };
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

            await Task.Delay(100);

            await mainWindow._catManager.SendCatCommandAsync("RG", new object[] { 0, 30 }, mainWindow._catManager.OutGoingDataLoopDelay);

            await mainWindow._catManager.SendCatCommandAsync("FA", new object[] { startFrequency }, mainWindow._catManager.OutGoingDataLoopDelay);

            await Task.Delay(100);

            if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOff)
            {
                await mainWindow._catManager.SendCatCommandAsync("RM", new object[] { (int)MeterTypes.DependsOnFrontPanelMETER }, mainWindow._catManager.OutGoingDataLoopDelay);
            }

            await Task.Delay(100);

            Int32 PositionInTheList = 1;

            for (long freq = startFrequency; freq <= endFrequency; freq += freqStep)
            {
                if (freq != startFrequency) await mainWindow._catManager.SendCatCommandAsync("FA", new object[] { freq }, 5);

                mainWindow.frequencyManagement.SetFrequencyUI(MemorySlot.MemorySlots.VFO_A, freq, mainWindow.MainFrequencyTextBlock);
                mainWindow.LargeFrequencyDisplay.Frequency = freq;

                if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOff)
                {
                    if (freq != startFrequency) await mainWindow._catManager.SendCatCommandAsync("RM", new object[] { (int)MeterTypes.DependsOnFrontPanelMETER }, 5);
                }

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
                    await mainWindow._catManager.SendCatCommandAsync("RG", new object[] { 0, 0 }, mainWindow._catManager.OutGoingDataLoopDelay);

                    mainWindow._catManager.StartOutgoingDataLoop();

                    window.RigBlurVFOCanvas.Visibility = Visibility.Hidden;

                    IsScanning = false;
                    RequestToStopScanning = false;
                    return;
                }
            }

            if (RigMode != RadioMode.FM)
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

        public void ButtonSelection(byte buttonClicked)
        {
            switch (buttonClicked)
            {
                case ScanBands._80M:
                    CurrentScanBand = ScanBands._80M;
                    mainWindow.ScanBandTextBlock.Text = "80M";
                    mainWindow.ScanBandStartFreqTextBlock.Text = "3.500.000";
                    mainWindow.ScanBandEndFreqTextBlock.Text = "3.800.000";
                    mainWindow.ScanBandStepFreqTextBlock.Text = "1000";
                    
                    currentStationSeekCriteria.ScanBand = ScanBands._80M;
                    currentStationSeekCriteria.StartFreq = 3500000;
                    currentStationSeekCriteria.EndFreq = 3800000;
                    currentStationSeekCriteria.StepFreq = 500;
                    currentStationSeekCriteria.Threshold = 4;

                    mainWindow.StationScopeSignalStrengthThresholdNumericUpDown.Value = currentStationSeekCriteria.Threshold;
                    break;
                case ScanBands._40M:
                    CurrentScanBand = ScanBands._40M;
                    mainWindow.ScanBandTextBlock.Text = "40M";
                    mainWindow.ScanBandStartFreqTextBlock.Text = "7.000.000";
                    mainWindow.ScanBandEndFreqTextBlock.Text = "7.200.000";
                    mainWindow.ScanBandStepFreqTextBlock.Text = "1000";

                    currentStationSeekCriteria.ScanBand = ScanBands._40M;
                    currentStationSeekCriteria.StartFreq = 7000000;
                    currentStationSeekCriteria.EndFreq = 7200000;
                    currentStationSeekCriteria.StepFreq = 500;
                    currentStationSeekCriteria.Threshold = 6;

                    mainWindow.StationScopeSignalStrengthThresholdNumericUpDown.Value = currentStationSeekCriteria.Threshold;
                    break;
                case ScanBands._20M:
                    CurrentScanBand = ScanBands._20M;
                    mainWindow.ScanBandTextBlock.Text = "20M";
                    mainWindow.ScanBandStartFreqTextBlock.Text = "14.000.000";
                    mainWindow.ScanBandEndFreqTextBlock.Text = "14.350.000";
                    mainWindow.ScanBandStepFreqTextBlock.Text = "1000";

                    currentStationSeekCriteria.ScanBand = ScanBands._20M;
                    currentStationSeekCriteria.StartFreq = 14000000;
                    currentStationSeekCriteria.EndFreq = 14350000;
                    currentStationSeekCriteria.StepFreq = 500;
                    currentStationSeekCriteria.Threshold = 8;

                    mainWindow.StationScopeSignalStrengthThresholdNumericUpDown.Value = currentStationSeekCriteria.Threshold;
                    break;
                case ScanBands._15M:
                    CurrentScanBand = ScanBands._15M;
                    mainWindow.ScanBandTextBlock.Text = "15M";
                    mainWindow.ScanBandStartFreqTextBlock.Text = "21.000.000";
                    mainWindow.ScanBandEndFreqTextBlock.Text = "21.450.000";
                    mainWindow.ScanBandStepFreqTextBlock.Text = "1000";

                    currentStationSeekCriteria.ScanBand = ScanBands._15M;
                    currentStationSeekCriteria.StartFreq = 21000000;
                    currentStationSeekCriteria.EndFreq = 21450000;
                    currentStationSeekCriteria.StepFreq = 500;
                    currentStationSeekCriteria.Threshold = 8;

                    mainWindow.StationScopeSignalStrengthThresholdNumericUpDown.Value = currentStationSeekCriteria.Threshold;
                    break;
                case ScanBands._12M:
                    CurrentScanBand = ScanBands._12M;
                    mainWindow.ScanBandTextBlock.Text = "12M";
                    mainWindow.ScanBandStartFreqTextBlock.Text = "24.890.000";
                    mainWindow.ScanBandEndFreqTextBlock.Text = "24.990.000";
                    mainWindow.ScanBandStepFreqTextBlock.Text = "1000";

                    currentStationSeekCriteria.ScanBand = ScanBands._12M;
                    currentStationSeekCriteria.StartFreq = 24890000;
                    currentStationSeekCriteria.EndFreq = 24990000;
                    currentStationSeekCriteria.StepFreq = 500;
                    currentStationSeekCriteria.Threshold = 8;

                    mainWindow.StationScopeSignalStrengthThresholdNumericUpDown.Value = currentStationSeekCriteria.Threshold;
                    break;
                case ScanBands._11M:
                    CurrentScanBand = ScanBands._11M;
                    mainWindow.ScanBandTextBlock.Text = "11M";
                    mainWindow.ScanBandStartFreqTextBlock.Text = "26.200.000";
                    mainWindow.ScanBandEndFreqTextBlock.Text = "27.991.250";
                    mainWindow.ScanBandStepFreqTextBlock.Text = "1000";

                    currentStationSeekCriteria.ScanBand = ScanBands._11M;
                    currentStationSeekCriteria.StartFreq = 26200000;
                    currentStationSeekCriteria.EndFreq = 27991250;
                    currentStationSeekCriteria.StepFreq = 500;
                    currentStationSeekCriteria.Threshold = 8;

                    mainWindow.StationScopeSignalStrengthThresholdNumericUpDown.Value = currentStationSeekCriteria.Threshold;
                    break;
                case ScanBands._10M:
                    CurrentScanBand = ScanBands._10M;
                    mainWindow.ScanBandTextBlock.Text = "10M";
                    mainWindow.ScanBandStartFreqTextBlock.Text = "28.000.000";
                    mainWindow.ScanBandEndFreqTextBlock.Text = "29.700.000";
                    mainWindow.ScanBandStepFreqTextBlock.Text = "1000";

                    currentStationSeekCriteria.ScanBand = ScanBands._10M;
                    currentStationSeekCriteria.StartFreq = 28000000;
                    currentStationSeekCriteria.EndFreq = 29700000;
                    currentStationSeekCriteria.StepFreq = 500;
                    currentStationSeekCriteria.Threshold = 8;

                    mainWindow.StationScopeSignalStrengthThresholdNumericUpDown.Value = currentStationSeekCriteria.Threshold;
                    break;
                case ScanBands._6M:
                    CurrentScanBand = ScanBands._6M;
                    mainWindow.ScanBandTextBlock.Text = "6M";
                    mainWindow.ScanBandStartFreqTextBlock.Text = "50.000.000";
                    mainWindow.ScanBandEndFreqTextBlock.Text = "52.000.000";
                    mainWindow.ScanBandStepFreqTextBlock.Text = "1000";

                    currentStationSeekCriteria.ScanBand = ScanBands._6M;
                    currentStationSeekCriteria.StartFreq = 50000000;
                    currentStationSeekCriteria.EndFreq = 52000000;
                    currentStationSeekCriteria.StepFreq = 500;
                    currentStationSeekCriteria.Threshold = 2;

                    mainWindow.StationScopeSignalStrengthThresholdNumericUpDown.Value = currentStationSeekCriteria.Threshold;
                    break;
                case ScanBands._START:
                    if (IsScanning == true) RequestToStopScanning = true;

                    mainWindow.FoundStationCountGrid.Visibility = System.Windows.Visibility.Visible;
                    mainWindow.FoundStationCountLabel.Content = 0;

                    if (DateTime.Now > mainWindow.LastTimeThresholdChangedDateTime + TimeSpan.FromSeconds(5))
                    {
                        mainWindow.StationScopeSignalStrengthThresholdNumericUpDown.Value = currentStationSeekCriteria.Threshold;
                        SeekActiveStations(mainWindow, mainWindow.fT891S_SerialPort._port, currentStationSeekCriteria.StartFreq, currentStationSeekCriteria.EndFreq, currentStationSeekCriteria.StepFreq, currentStationSeekCriteria.Threshold, mainWindow.FoundStationCountLabel);
                    }
                    else
                    {
                        SeekActiveStations(mainWindow, mainWindow.fT891S_SerialPort._port, currentStationSeekCriteria.StartFreq, currentStationSeekCriteria.EndFreq, currentStationSeekCriteria.StepFreq, Convert.ToInt16(mainWindow.StationScopeSignalStrengthThresholdNumericUpDown.Value), mainWindow.FoundStationCountLabel);
                    }
                    break;
                case ScanBands._STOP:
                    if (IsScanning == true) RequestToStopScanning = true;

                    mainWindow.FoundStationCountGrid.Visibility = System.Windows.Visibility.Visible;
                    mainWindow.FoundStationCountLabel.Content = 0;

                    //SeekActiveStations(mainWindow, mainWindow.fT891S_SerialPort._port, currentStationSeekCriteria.StartFreq, currentStationSeekCriteria.EndFreq, currentStationSeekCriteria.StepFreq, currentStationSeekCriteria.Threshold, mainWindow.FoundStationCountLabel);
                    break;
                default:
                    CurrentScanBand = ScanBands._20M;
                    mainWindow.ScanBandTextBlock.Text = "20M";
                    mainWindow.ScanBandStartFreqTextBlock.Text = "14.000.000";
                    mainWindow.ScanBandEndFreqTextBlock.Text = "14.350.000";
                    mainWindow.ScanBandStepFreqTextBlock.Text = "1000";

                    currentStationSeekCriteria.ScanBand = ScanBands._20M;
                    currentStationSeekCriteria.StartFreq = 14000000;
                    currentStationSeekCriteria.EndFreq = 14350000;
                    currentStationSeekCriteria.StepFreq = 500;
                    currentStationSeekCriteria.Threshold = 8;

                    mainWindow.StationScopeSignalStrengthThresholdNumericUpDown.Value = currentStationSeekCriteria.Threshold;
                    break;
            }
            
        }
    }
}