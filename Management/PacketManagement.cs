using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace YAESU_FT_891_Front_End
{
    public class PacketManagement
    {
        MainWindow mainWindow;
        DateTime SendRedLEDElapsedTimeSinceLastPacket = DateTime.MinValue;
        DateTime ReceiveGreenLEDElapsedTimeSinceLastPacket = DateTime.MinValue;
        DateTime SendRedLEDONDateTime = DateTime.MinValue;
        DateTime ReceiveGreenLEDONDateTime = DateTime.MinValue;

        long LastFPS;
        long currentFPS;
        public static long FramesPerSecond { get; set; }

        public PacketManagement(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        public void UpdateSendFPS()
        {
            if (DateTime.Now > (SendRedLEDElapsedTimeSinceLastPacket + TimeSpan.FromSeconds(1)))
            {
                mainWindow.FPSTextBlock.Text = currentFPS.ToString();

                mainWindow.SendRedLEDRectangle.Opacity = 0.5;

                SendRedLEDElapsedTimeSinceLastPacket = DateTime.Now;
                currentFPS = 0;
            }
            if (DateTime.Now > (SendRedLEDElapsedTimeSinceLastPacket + TimeSpan.FromMilliseconds(200)))
            {
                mainWindow.SendRedLEDRectangle.Opacity = 0.2;
            }

            currentFPS++;
        }

        public void UpdateReceiveFPS()
        {
            if (DateTime.Now > (ReceiveGreenLEDElapsedTimeSinceLastPacket + TimeSpan.FromSeconds(1)))
            {
                mainWindow.ReceiveGreenLEDRectangle.Opacity = 0.5;

                ReceiveGreenLEDElapsedTimeSinceLastPacket = DateTime.Now;
            }
            if (DateTime.Now > (ReceiveGreenLEDElapsedTimeSinceLastPacket + TimeSpan.FromMilliseconds(200)))
            {
                mainWindow.ReceiveGreenLEDRectangle.Opacity = 0.2;
            }
        }
    }
}
