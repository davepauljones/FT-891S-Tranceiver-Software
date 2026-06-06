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
        public List<RigState> QMBRigStatesList = new List<RigState>();

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
                foreach (RigState rs in QMBRigStatesList)
                {
                    Console.Write(rs.RXFrequencyHz.ToString());
                    Console.Write(", ");
                    Console.Write(rs.TXPowerWatts.ToString());
                    Console.Write(", ");
                    Console.Write(rs.Mode.ToString());
                    Console.Write(", ");
                    Console.WriteLine(rs.RFGain.ToString());
                }
                Console.WriteLine(">>>>>>>>>>>>>>> QMBRigStates.ListRigState End");
            }
        }
        public void AddNewRigStateToList(ListView QMBListView, RigState rigState)
        {
            if (rigState.RXFrequencyHz == 0) return;

            int DuplicateFoundCount = 0;

            foreach (RigState rs in QMBRigStatesList)
            {
               if (rigState.RXFrequencyHz == rs.RXFrequencyHz) DuplicateFoundCount++;
            }

            if (DuplicateFoundCount == 0)
            {
                QMBRigStatesList.Add(rigState);

                int PositionInTheList = QMBRigStatesList.Count-1;

                StationSeekClass station = new StationSeekClass { ID = PositionInTheList, Frequency = rigState.RXFrequencyHz, SignalStrength = rigState.SMeter };

                QMBListView.Items.Add(new StationScope(mainWindow, station, mainWindow.frequencyManagement));
            }
        }
    }
}