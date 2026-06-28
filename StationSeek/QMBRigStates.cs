using FT891S_CatControl;
using System;
using System.Collections.Generic;
using System.Linq;
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
                QMBRigStatesList.Add(radioState);

                int PositionInTheList = QMBRigStatesList.Count-1;

                StationSeekClass station = new StationSeekClass { ID = PositionInTheList, Frequency = radioState.VfoAFrequency, SignalStrength = radioState.SMeter };

                QMBListView.Items.Add(new StationScope(mainWindow, station, mainWindow.frequencyManagement));
            }
        }
    }
}