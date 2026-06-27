using FT891S_CatControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using static YAESU_FT_891_Front_End.TranceiverDisplayModes;
using YAESU_FT_891_Front_End.Models;

namespace YAESU_FT_891_Front_End
{
    public class CATCommandLogClass
    {
        public DateTime Created { get; set; }
        public String SentCAT { get; set; }
        public String ReceivedCAT { get; set; }
        public long SendFreq { get; set; }
        public long ReceiveFreq { get; set; }
        public RadioMode Mode { get; set; }
        public int Power { get; set; }
        public String CallSign { get; set; }
    }
    public class CATCommandLog
    {
        MainWindow mainWindow;

        List<CATCommandLogClass> cATCommandLogList = new List<CATCommandLogClass>();
        public CATCommandLog(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;

            CATCommandLogClass a = new CATCommandLogClass { Created = DateTime.Now, SentCAT = "FA;", ReceivedCAT = "FA014252500;", SendFreq = 14252500, ReceiveFreq = 14252500, Mode = RadioMode.USB, Power = 5, CallSign = "G7UIV" };
            CATCommandLogClass b = new CATCommandLogClass { Created = DateTime.Now, SentCAT = "FA;", ReceivedCAT = "FA014252500;", SendFreq = 14252500, ReceiveFreq = 14252500, Mode = RadioMode.USB, Power = 5, CallSign = "G7UIV" };

            CreateCATCommandLog(a);
            CreateCATCommandLog(b);

            //SwitchToADisplayMode(mainWindow.TabControlTabControl, TranceiverModes.CatCommandLog, mainWindow.TranceiverModeLabel);
        }

        public void CreateCATCommandLog(CATCommandLogClass catCommandLogClass)
        {
            cATCommandLogList.Add(catCommandLogClass);
            mainWindow.CATCommandLogListView.Items.Add(catCommandLogClass);

            Console.WriteLine("CreateCATCommandLog");
        }
    }
}
