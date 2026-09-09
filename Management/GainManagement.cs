using FT891S_CatControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YAESU_FT_891_Front_End
{
    public enum ControlGains
    {
        AF,
        RF,
        SQ
    }
    public enum ControlModes
    {
        ReadOnly,
        SetOnly,
        OpenUserControl,
        CloseUserControl
    }
    public class GainManagement
    {
        MainWindow mainWindow;
        public ControlGains currentControlGain = ControlGains.AF;
        public GainManagement(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }
        public async Task<int> ManageGain(ControlGains controlGains, ControlModes controlModes, int value)
        {
            currentControlGain = controlGains;

            switch (controlModes)
            {
                case ControlModes.ReadOnly:
                    switch (controlGains)
                    {
                        case ControlGains.AF:
                            value = FT891S_CatManager.currentRadioState.AFGain;
                            await mainWindow._catManager.SendCatCommandAsync("AG", "0", mainWindow._catManager.OutGoingDataLoopDelay);                    
                            break;
                        case ControlGains.RF:
                            value = FT891S_CatManager.currentRadioState.RFGain;
                            await mainWindow._catManager.SendCatCommandAsync("RG", "0", mainWindow._catManager.OutGoingDataLoopDelay);
                            break;
                        case ControlGains.SQ:
                            value = FT891S_CatManager.currentRadioState.SQGain;
                            //to create squelch cat command
                            //await mainWindow._catManager.SendCatCommandAsync("SQ", "0", mainWindow._catManager.OutGoingDataLoopDelay);
                            break;
                    }
                    break;
                case ControlModes.SetOnly:
                    switch (controlGains)
                    {
                        case ControlGains.AF:
                            FT891S_CatManager.currentRadioState.AFGain = value;
                            await mainWindow._catManager.SendCatCommandAsync("AG", new object[] { 0, FT891S_CatManager.currentRadioState.AFGain }, mainWindow._catManager.OutGoingDataLoopDelay);
                            break;
                        case ControlGains.RF:
                            FT891S_CatManager.currentRadioState.RFGain = value;
                            await mainWindow._catManager.SendCatCommandAsync("RG", new object[] { 0, FT891S_CatManager.currentRadioState.RFGain }, mainWindow._catManager.OutGoingDataLoopDelay);
                            break;
                        case ControlGains.SQ:
                            FT891S_CatManager.currentRadioState.SQGain = value;
                            //to do
                            //await mainWindow._catManager.SendCatCommandAsync("SQ", new object[] { 0, FT891S_CatManager.currentRadioState.SQGain }, mainWindow._catManager.OutGoingDataLoopDelay);
                            break;
                    }
                    break;
                case ControlModes.OpenUserControl:
                    switch (controlGains)
                    {
                        case ControlGains.AF:
                            mainWindow.gainUserControl.GainTitleTextBlock.Text = "AF GAIN";
                            mainWindow.gainUserControl.Minimum = 0;
                            mainWindow.gainUserControl.Maximum = 90;
                            mainWindow.gainUserControl.DefaultGain = 70;
                            mainWindow.gainUserControl.ChangeGain(FT891S_CatManager.currentRadioState.AFGain);
                            break;
                        case ControlGains.RF:
                            mainWindow.gainUserControl.GainTitleTextBlock.Text = "RF GAIN";
                            mainWindow.gainUserControl.Minimum = 0;
                            mainWindow.gainUserControl.Maximum = 30;
                            mainWindow.gainUserControl.DefaultGain = 10;
                            mainWindow.gainUserControl.ChangeGain(FT891S_CatManager.currentRadioState.RFGain);
                            break;
                        case ControlGains.SQ:
                            mainWindow.gainUserControl.GainTitleTextBlock.Text = "SQ GAIN";
                            mainWindow.gainUserControl.Minimum = 0;
                            mainWindow.gainUserControl.Maximum = 255;
                            mainWindow.gainUserControl.DefaultGain = 70;
                            mainWindow.gainUserControl.ChangeGain(FT891S_CatManager.currentRadioState.SQGain);
                            break;
                    }
                    mainWindow.gainUserControl.Visibility = System.Windows.Visibility.Visible;
                    break;
                case ControlModes.CloseUserControl:
                    switch (controlGains)
                    {
                        case ControlGains.AF:
                            break;
                        case ControlGains.RF:
                            break;
                        case ControlGains.SQ:
                            break;
                    }
                    break;
            }

            return value;
        }
    }
}
