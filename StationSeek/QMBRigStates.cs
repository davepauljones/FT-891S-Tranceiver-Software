using Event_Horizon;
using FT891S_CatControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using static YAESU_FT_891_Front_End.MyStructs;

namespace YAESU_FT_891_Front_End
{
    public class QMBRigStates
    {
        public List<RadioState> QMBRigStatesList = new List<RadioState>();

        MainWindow mainWindow;
        public QMBRigStates(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }
        //public void SortQuickMemoryBankDescending()
        //{
        //    QMBRigStatesList.Sort();
        //    PopulateRigStateList(mainWindow.QMBListView);
        //}
        public void ListRigStates()
        {
            if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
            {
                Console.WriteLine(">>>>>>>>>>>>>>> QMBRigStates.ListRigState Start");
                foreach (RadioState rs in QMBRigStatesList)
                {
                    Console.Write(rs.VfoAFrequency.ToString());
                    Console.Write(", ");
                    Console.Write(rs.TXPowerWatts.ToString());
                    Console.Write(", ");
                    Console.Write(rs.OperatingMode.ToString());
                    Console.Write(", ");
                    Console.WriteLine(rs.RFGain.ToString());
                }
                Console.WriteLine(">>>>>>>>>>>>>>> QMBRigStates.ListRigState End");
            }
        }
        public void AddNewRigStateToList(ListView QMBListView, RadioState radioState)
        {
            if (radioState.VfoAFrequency == 0) return;

            int DuplicateFoundCount = 0;

            foreach (RadioState rs in QMBRigStatesList)
            {
               if (radioState.VfoAFrequency == rs.VfoAFrequency) DuplicateFoundCount++;
            }

            if (DuplicateFoundCount == 0)
            {
                int PositionInTheList = QMBRigStatesList.Count + 1;

                radioState.ID = PositionInTheList;

                QMBRigStatesList.Add(radioState);

                PopulateRigStateList(QMBListView);
            }         
        }

        public void PopulateRigStateList(ListView QMBListView)
        {
            QMBListView.Items.Clear();

            foreach (RadioState rs in QMBRigStatesList)
            {
                StationSeekClass station = new StationSeekClass { ID = rs.ID, Frequency = rs.VfoAFrequency, SignalStrength = rs.SMeter };

                QMBListView.Items.Add(new StationScope(mainWindow, station, mainWindow.frequencyManagement));
            }

            UpdateTheQuickMemoryBankListCount();
        }

        public void ClearQuickMemoryBankList()
        {
            EventHorizonRequesterNotification msg = new EventHorizonRequesterNotification(mainWindow, new OracleCustomMessage { MessageTitleTextBlock = "FT891S Information", InformationTextBlock = "Clear All QMB Stations, Are You Sure ?" }, RequesterTypes.NoYes);

            if (msg.ShowDialog() == true)
            {
                QMBRigStatesList.Clear();
                mainWindow.QMBListView.Items.Clear();

                UpdateTheQuickMemoryBankListCount();
            }
        }

        public void RemoveSelectedQMBStation()
        {
            if (mainWindow.QMBListView.SelectedIndex != -1)
            {
                QMBRigStatesList.RemoveAt(mainWindow.QMBListView.SelectedIndex);
                mainWindow.QMBListView.Items.RemoveAt(mainWindow.QMBListView.SelectedIndex);

                RenumberTheQMBRigStatesList();

                UpdateTheQuickMemoryBankListCount();
            }
        }

        public void UpdateTheQuickMemoryBankListCount()
        {
            if (mainWindow.QMBListView.SelectedIndex == -1)
            {
                if (mainWindow.QMBListView.Items.Count > 0)
                {
                    mainWindow.QMBListView.SelectedItem = mainWindow.QMBListView.Items[0];
                    mainWindow.QMBListView.ScrollIntoView(mainWindow.QMBListView.Items[0]);

                    mainWindow.QMBCountLabel.Content = "1 of " + QMBRigStatesList.Count;
                }
                else
                {
                    mainWindow.QMBCountLabel.Content = "No Stations!";
                }
            }
            else if (mainWindow.QMBListView.SelectedIndex != -1)
            {
                RadioState rs = QMBRigStatesList[mainWindow.QMBListView.SelectedIndex];
                mainWindow.QMBCountLabel.Content = rs.ID + " of " + QMBRigStatesList.Count;
            }
        }
        public void RenumberTheQMBRigStatesList()
        {
            Int32 index = 0;
            
            foreach (RadioState rs in QMBRigStatesList)
            {
                index++;

                rs.ID = index;
            }

            PopulateRigStateList(mainWindow.QMBListView);
        }

        public void UpdateSelectedQMBFromVFO()
        {
            if (mainWindow.QMBListView.SelectedIndex != -1)
            {
                Int32 selectedIndex = mainWindow.QMBListView.SelectedIndex;

                RadioState rs = QMBRigStatesList[selectedIndex];
                rs.VfoAFrequency = FT891S_CatManager.currentRadioState.VfoAFrequency;

                PopulateRigStateList(mainWindow.QMBListView);
                mainWindow.QMBListView.SelectedIndex = selectedIndex;
            }
        }

        public void NextQuickMemoryBank()
        {
            int itemCount = mainWindow.QMBListView.Items.Count;
            if (itemCount == 0) return;

            int currentIndex = mainWindow.QMBListView.SelectedIndex;

            // Wrap around if nothing is selected or if we've reached the last item
            if (currentIndex == -1 || currentIndex >= itemCount - 1)
            {
                mainWindow.QMBListView.SelectedIndex = 0;
            }
            else
            {
                mainWindow.QMBListView.SelectedIndex++;
            }

            RadioState radioState = QMBRigStatesList[mainWindow.QMBListView.SelectedIndex];
            SetRigStateFromQuickMemoryBank(radioState);
        }

        public async void SetRigStateFromQuickMemoryBank(RadioState radioState)
        {
            mainWindow.frequencyManagement.SetFrequency(MemorySlot.MemorySlots.VFO_A, radioState.VfoAFrequency, mainWindow.MainFrequencyTextBlock);

            await mainWindow._catManager.SendCatCommandAsync("RG", new object[] { 0, 30 }, mainWindow._catManager.OutGoingDataLoopDelay);
        }

        public async void QMBStore()
        {
            await mainWindow._catManager.SendCatCommandAsync("QI", mainWindow._catManager.OutGoingDataLoopDelay);
            await mainWindow._catManager.SendCatCommandAsync("QR", mainWindow._catManager.OutGoingDataLoopDelay);
        }

    }
}