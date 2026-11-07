using FT891S_CatControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YAESU_FT_891_Front_End
{
    public class SelectRadio
    {
        public struct RadioIdentifications
        {
            public const int FT_891 = 650;
            public const int FT_710 = 800;
            public const int FTX_1 = 840;
        }

        MainWindow mainWindow;
        public SelectRadio(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }
        public int GetRFGainMaxValueByRadioID(int RadioID)
        {
            int ReturnRFGainValue;

            switch (RadioID)
            {
                case RadioIdentifications.FT_891:
                    ReturnRFGainValue = 30;
                    break;
                case RadioIdentifications.FT_710:
                    ReturnRFGainValue = 255;
                    break;
                case RadioIdentifications.FTX_1:
                    ReturnRFGainValue = 255;
                    break;
                default:
                    ReturnRFGainValue = 255;
                    break;
            }

            return ReturnRFGainValue;
        }
    }
}
