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
        public const byte _160M = 0;
        public const byte _80M = 1;
        public const byte _40M = 2;
        public const byte _30M = 3;
        public const byte _20M = 4;
        public const byte _15M = 5;
        public const byte _12M = 6;
        public const byte _11M = 7;
        public const byte _10M = 8;
        public const byte _6M = 9;
        public const byte _START = 10;
        public const byte _STOP = 11;
    }
    public struct StationScopeTransportButtons
    {
        public const int CopyToQMB = 0;
        public const int IncreaseThresholdValue = 1;
        public const int DecreaseThresholdValue = 2;
        public const int RemoveSelectedQMB = 3;
        public const int ClearQuickMemoryBankList = 4;
    }
    public class StationSeekCriteriaClass
    {
        public byte ScanBand = ScanBands._20M;
        public long StartFreq = 14000000;
        public long EndFreq = 14350000;
        public RadioMode RadioMode;
        public int StepFreq = 500;
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
        public int CurrentThresholdValue = 7;

        MainWindow mainWindow;
        public StationSeek(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;

            mainWindow.ScanBandStartFreqTextBlock.Text = "14.000.000";
            mainWindow.ScanBandEndFreqTextBlock.Text = "14.350.000";
            mainWindow.ScanBandModeTextBlock.Text = "USB";
            mainWindow.ScanBandStepFreqTextBlock.Text = "500";

            currentStationSeekCriteria = new StationSeekCriteriaClass { ScanBand = ScanBands._20M, StartFreq = 14000000, EndFreq = 14350000, StepFreq = 500, Threshold = 8 };

            ButtonSelection(ScanBands._20M);
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

                if (signalStrengthThreshold != CurrentThresholdValue) signalStrengthThreshold = CurrentThresholdValue;

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

                    ButtonSelection(ScanBands._STOP);

                    return;
                }
            }

            IsScanning = false;
            RequestToStopScanning = false;
            ButtonSelection(ScanBands._STOP);

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
            if (buttonClicked != ScanBands._START && buttonClicked != ScanBands._STOP)
            {
                if (IsScanning) return;
                mainWindow._160MATextBlock.Foreground = new SolidColorBrush(Colors.White);
                mainWindow._80MATextBlock.Foreground = new SolidColorBrush(Colors.White);
                mainWindow._40MATextBlock.Foreground = new SolidColorBrush(Colors.White);
                mainWindow._30MATextBlock.Foreground = new SolidColorBrush(Colors.White);
                mainWindow._20MATextBlock.Foreground = new SolidColorBrush(Colors.White);
                mainWindow._15MATextBlock.Foreground = new SolidColorBrush(Colors.White);
                mainWindow._12MATextBlock.Foreground = new SolidColorBrush(Colors.White);
                mainWindow._11MATextBlock.Foreground = new SolidColorBrush(Colors.White);
                mainWindow._10MATextBlock.Foreground = new SolidColorBrush(Colors.White);
                mainWindow._6MATextBlock.Foreground = new SolidColorBrush(Colors.White);
            }

            switch (buttonClicked)
            {
                case ScanBands._160M:
                    CurrentScanBand = ScanBands._160M;
                    mainWindow.ScanBandStartFreqTextBlock.Text = "1.810.000";
                    mainWindow.ScanBandEndFreqTextBlock.Text = "2.000.000";
                    mainWindow.ScanBandModeTextBlock.Text = "LSB";
                    mainWindow.ScanBandStepFreqTextBlock.Text = "500";

                    currentStationSeekCriteria.ScanBand = ScanBands._160M;
                    currentStationSeekCriteria.StartFreq = 1810000;
                    currentStationSeekCriteria.EndFreq = 2000000;
                    currentStationSeekCriteria.RadioMode = RadioMode.LSB;
                    currentStationSeekCriteria.StepFreq = 500;
                    currentStationSeekCriteria.Threshold = 2;

                    CurrentThresholdValue = currentStationSeekCriteria.Threshold;
                    mainWindow.ThresholdValueLabel.Content = CurrentThresholdValue.ToString();
                    mainWindow._160MATextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                    break;
                case ScanBands._80M:
                    CurrentScanBand = ScanBands._80M;
                    mainWindow.ScanBandStartFreqTextBlock.Text = "3.500.000";
                    mainWindow.ScanBandEndFreqTextBlock.Text = "3.800.000";
                    mainWindow.ScanBandModeTextBlock.Text = "LSB";
                    mainWindow.ScanBandStepFreqTextBlock.Text = "500";

                    currentStationSeekCriteria.ScanBand = ScanBands._80M;
                    currentStationSeekCriteria.StartFreq = 3500000;
                    currentStationSeekCriteria.EndFreq = 3800000;
                    currentStationSeekCriteria.RadioMode = RadioMode.LSB;
                    currentStationSeekCriteria.StepFreq = 500;
                    currentStationSeekCriteria.Threshold = 2;

                    CurrentThresholdValue = currentStationSeekCriteria.Threshold;
                    mainWindow.ThresholdValueLabel.Content = CurrentThresholdValue.ToString();
                    mainWindow._80MATextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                    break;
                case ScanBands._40M:
                    CurrentScanBand = ScanBands._40M;
                    mainWindow.ScanBandStartFreqTextBlock.Text = "7.000.000";
                    mainWindow.ScanBandEndFreqTextBlock.Text = "7.200.000";
                    mainWindow.ScanBandModeTextBlock.Text = "LSB";
                    mainWindow.ScanBandStepFreqTextBlock.Text = "500";

                    currentStationSeekCriteria.ScanBand = ScanBands._40M;
                    currentStationSeekCriteria.StartFreq = 7000000;
                    currentStationSeekCriteria.EndFreq = 7200000;
                    currentStationSeekCriteria.RadioMode = RadioMode.LSB;
                    currentStationSeekCriteria.StepFreq = 500;
                    currentStationSeekCriteria.Threshold = 4;

                    CurrentThresholdValue = currentStationSeekCriteria.Threshold;
                    mainWindow.ThresholdValueLabel.Content = CurrentThresholdValue.ToString();
                    mainWindow._40MATextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                    break;
                case ScanBands._30M:
                    CurrentScanBand = ScanBands._30M;
                    mainWindow.ScanBandStartFreqTextBlock.Text = "10.100.000";
                    mainWindow.ScanBandEndFreqTextBlock.Text = "10.150.000";
                    mainWindow.ScanBandModeTextBlock.Text = "LSB";
                    mainWindow.ScanBandStepFreqTextBlock.Text = "500";

                    currentStationSeekCriteria.ScanBand = ScanBands._30M;
                    currentStationSeekCriteria.StartFreq = 10100000;
                    currentStationSeekCriteria.EndFreq = 10150000;
                    currentStationSeekCriteria.RadioMode = RadioMode.LSB;
                    currentStationSeekCriteria.StepFreq = 500;
                    currentStationSeekCriteria.Threshold = 3;

                    CurrentThresholdValue = currentStationSeekCriteria.Threshold;
                    mainWindow.ThresholdValueLabel.Content = CurrentThresholdValue.ToString();
                    mainWindow._30MATextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                    break;
                case ScanBands._20M:
                    CurrentScanBand = ScanBands._20M;
                    mainWindow.ScanBandStartFreqTextBlock.Text = "14.000.000";
                    mainWindow.ScanBandEndFreqTextBlock.Text = "14.350.000";
                    mainWindow.ScanBandModeTextBlock.Text = "USB";
                    mainWindow.ScanBandStepFreqTextBlock.Text = "500";

                    currentStationSeekCriteria.ScanBand = ScanBands._20M;
                    currentStationSeekCriteria.StartFreq = 14000000;
                    currentStationSeekCriteria.EndFreq = 14350000;
                    currentStationSeekCriteria.RadioMode = RadioMode.USB;
                    currentStationSeekCriteria.StepFreq = 500;
                    currentStationSeekCriteria.Threshold = 7;

                    CurrentThresholdValue = currentStationSeekCriteria.Threshold;
                    mainWindow.ThresholdValueLabel.Content = CurrentThresholdValue.ToString();
                    mainWindow._20MATextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                    break;
                case ScanBands._15M:
                    CurrentScanBand = ScanBands._15M;
                    mainWindow.ScanBandStartFreqTextBlock.Text = "21.000.000";
                    mainWindow.ScanBandEndFreqTextBlock.Text = "21.450.000";
                    mainWindow.ScanBandModeTextBlock.Text = "USB";
                    mainWindow.ScanBandStepFreqTextBlock.Text = "500";

                    currentStationSeekCriteria.ScanBand = ScanBands._15M;
                    currentStationSeekCriteria.StartFreq = 21000000;
                    currentStationSeekCriteria.EndFreq = 21450000;
                    currentStationSeekCriteria.RadioMode = RadioMode.USB;
                    currentStationSeekCriteria.StepFreq = 500;
                    currentStationSeekCriteria.Threshold = 6;

                    CurrentThresholdValue = currentStationSeekCriteria.Threshold;
                    mainWindow.ThresholdValueLabel.Content = CurrentThresholdValue.ToString();
                    mainWindow._15MATextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                    break;
                case ScanBands._12M:
                    CurrentScanBand = ScanBands._12M;
                    mainWindow.ScanBandStartFreqTextBlock.Text = "24.890.000";
                    mainWindow.ScanBandEndFreqTextBlock.Text = "24.990.000";
                    mainWindow.ScanBandModeTextBlock.Text = "USB";
                    mainWindow.ScanBandStepFreqTextBlock.Text = "500";

                    currentStationSeekCriteria.ScanBand = ScanBands._12M;
                    currentStationSeekCriteria.StartFreq = 24890000;
                    currentStationSeekCriteria.EndFreq = 24990000;
                    currentStationSeekCriteria.RadioMode = RadioMode.USB;
                    currentStationSeekCriteria.StepFreq = 500;
                    currentStationSeekCriteria.Threshold = 4;

                    CurrentThresholdValue = currentStationSeekCriteria.Threshold;
                    mainWindow.ThresholdValueLabel.Content = CurrentThresholdValue.ToString();
                    mainWindow._12MATextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                    break;
                case ScanBands._11M:
                    CurrentScanBand = ScanBands._11M;
                    mainWindow.ScanBandStartFreqTextBlock.Text = "27.601.250";
                    mainWindow.ScanBandEndFreqTextBlock.Text = "27.991.250";
                    mainWindow.ScanBandModeTextBlock.Text = "FM";
                    mainWindow.ScanBandStepFreqTextBlock.Text = "10000";

                    currentStationSeekCriteria.ScanBand = ScanBands._11M;
                    currentStationSeekCriteria.StartFreq = 27601250;
                    currentStationSeekCriteria.EndFreq = 27991250;
                    currentStationSeekCriteria.RadioMode = RadioMode.FM;
                    currentStationSeekCriteria.StepFreq = 10000;
                    currentStationSeekCriteria.Threshold = 3;

                    CurrentThresholdValue = currentStationSeekCriteria.Threshold;
                    mainWindow.ThresholdValueLabel.Content = CurrentThresholdValue.ToString();
                    mainWindow._11MATextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                    break;
                case ScanBands._10M:
                    CurrentScanBand = ScanBands._10M;
                    mainWindow.ScanBandStartFreqTextBlock.Text = "28.000.000";
                    mainWindow.ScanBandEndFreqTextBlock.Text = "29.700.000";
                    mainWindow.ScanBandModeTextBlock.Text = "USB";
                    mainWindow.ScanBandStepFreqTextBlock.Text = "500";

                    currentStationSeekCriteria.ScanBand = ScanBands._10M;
                    currentStationSeekCriteria.StartFreq = 28000000;
                    currentStationSeekCriteria.EndFreq = 29700000;
                    currentStationSeekCriteria.RadioMode = RadioMode.USB;
                    currentStationSeekCriteria.StepFreq = 500;
                    currentStationSeekCriteria.Threshold = 3;

                    CurrentThresholdValue = currentStationSeekCriteria.Threshold;
                    mainWindow.ThresholdValueLabel.Content = CurrentThresholdValue.ToString();
                    mainWindow._10MATextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                    break;
                case ScanBands._6M:
                    CurrentScanBand = ScanBands._6M;
                    mainWindow.ScanBandStartFreqTextBlock.Text = "50.000.000";
                    mainWindow.ScanBandEndFreqTextBlock.Text = "52.000.000";
                    mainWindow.ScanBandModeTextBlock.Text = "USB";
                    mainWindow.ScanBandStepFreqTextBlock.Text = "500";

                    currentStationSeekCriteria.ScanBand = ScanBands._6M;
                    currentStationSeekCriteria.StartFreq = 50000000;
                    currentStationSeekCriteria.EndFreq = 52000000;
                    currentStationSeekCriteria.RadioMode = RadioMode.USB;
                    currentStationSeekCriteria.StepFreq = 500;
                    currentStationSeekCriteria.Threshold = 1;

                    CurrentThresholdValue = currentStationSeekCriteria.Threshold;
                    mainWindow.ThresholdValueLabel.Content = CurrentThresholdValue.ToString();
                    mainWindow._6MATextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                    break;
                case ScanBands._START:
                    if (IsScanning == true)
                    {
                        //stop the scanning toggle
                        RequestToStopScanning = true;
                        mainWindow.StartStopTextBlock.Foreground = new SolidColorBrush(Colors.White);
                        mainWindow.StartStopTextBlock.Text = "START";
                    }
                    else
                    {
                        //start the scanning
                        RequestToStopScanning = false;
                        mainWindow.StartStopTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                        mainWindow.StartStopTextBlock.Text = "STOP";
                    }

                    mainWindow.FoundStationCountGrid.Visibility = System.Windows.Visibility.Visible;
                    mainWindow.FoundStationCountLabel.Content = 0;

                    if (DateTime.Now > mainWindow.LastTimeThresholdChangedDateTime + TimeSpan.FromSeconds(5))
                    {
                        CurrentThresholdValue = currentStationSeekCriteria.Threshold;
                        SeekActiveStations(mainWindow, mainWindow.fT891S_SerialPort._port, currentStationSeekCriteria.StartFreq, currentStationSeekCriteria.EndFreq, currentStationSeekCriteria.StepFreq, currentStationSeekCriteria.Threshold, mainWindow.FoundStationCountLabel);
                    }
                    else
                    {
                        SeekActiveStations(mainWindow, mainWindow.fT891S_SerialPort._port, currentStationSeekCriteria.StartFreq, currentStationSeekCriteria.EndFreq, currentStationSeekCriteria.StepFreq, CurrentThresholdValue, mainWindow.FoundStationCountLabel);
                    }
                    break;
                case ScanBands._STOP:
                    if (IsScanning == true) RequestToStopScanning = true;

                    mainWindow.FoundStationCountGrid.Visibility = System.Windows.Visibility.Visible;
                    mainWindow.FoundStationCountLabel.Content = 0;

                    mainWindow.StartStopTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    mainWindow.StartStopTextBlock.Text = "START";
                    break;
                default:
                    CurrentScanBand = ScanBands._20M;
                    mainWindow.ScanBandStartFreqTextBlock.Text = "14.000.000";
                    mainWindow.ScanBandEndFreqTextBlock.Text = "14.350.000";
                    mainWindow.ScanBandModeTextBlock.Text = "USB";
                    mainWindow.ScanBandStepFreqTextBlock.Text = "500";

                    currentStationSeekCriteria.ScanBand = ScanBands._20M;
                    currentStationSeekCriteria.StartFreq = 14000000;
                    currentStationSeekCriteria.EndFreq = 14350000;
                    currentStationSeekCriteria.RadioMode = RadioMode.USB;
                    currentStationSeekCriteria.StepFreq = 500;
                    currentStationSeekCriteria.Threshold = 7;

                    CurrentThresholdValue = currentStationSeekCriteria.Threshold;
                    mainWindow.ThresholdValueLabel.Content = CurrentThresholdValue.ToString();
                    mainWindow._20MATextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                    break;
            }

            mainWindow.Mode_ModeChange(currentStationSeekCriteria.RadioMode);
        }
        public void TransportButtonSelection(byte buttonClicked)
        {
            switch (buttonClicked)
            {
                case StationScopeTransportButtons.CopyToQMB:
                    if (mainWindow.StationScopeListView.SelectedIndex != -1)
                    {
                        RadioState radioState = new RadioState { VfoAFrequency = StationSeekActiveList[mainWindow.stationScopeListViewSelectedItem].Frequency, SMeter = StationSeekActiveList[mainWindow.stationScopeListViewSelectedItem].SignalStrength };

                        mainWindow.qMBRigStates.AddNewRigStateToList(mainWindow.QMBListView, radioState);
                    }

                    if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
                    {
                        Console.WriteLine("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");
                        Console.Write("QMBRigStatesList.Count = ");
                        Console.WriteLine(mainWindow.qMBRigStates.QMBRigStatesList.Count);
                        Console.WriteLine("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");
                    }

                    mainWindow.qMBRigStates.ListRigStates();
                    break;
                case StationScopeTransportButtons.IncreaseThresholdValue:
                    if (CurrentThresholdValue < 9) CurrentThresholdValue++;

                    mainWindow.ThresholdValueLabel.Content = CurrentThresholdValue.ToString();
                    break;
                case StationScopeTransportButtons.DecreaseThresholdValue:
                    if (CurrentThresholdValue > 0) CurrentThresholdValue--;
                    mainWindow.ThresholdValueLabel.Content = CurrentThresholdValue.ToString();
                    break;
                case StationScopeTransportButtons.RemoveSelectedQMB:
                    mainWindow.qMBRigStates.RemoveSelectedQMBStation();
                    break;
                case StationScopeTransportButtons.ClearQuickMemoryBankList:
                    mainWindow.qMBRigStates.ClearQuickMemoryBankList();
                    break;
            }
        }
    }
}