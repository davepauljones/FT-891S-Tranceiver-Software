using FT891.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;
using static YAESU_FT_891_Front_End.MyStructs;
using static YAESU_FT_891_Front_End.RigStateChanges;

namespace YAESU_FT_891_Front_End
{
    public class FT891S_DisplayLoop
    {
        MainWindow mainWindow;
        FrequencyManagement frequencyManagement;
        FT891Cat radio;

        public CancellationTokenSource _serialCts;
        public Task _serialTask;

        public FT891S_DisplayLoop(MainWindow mainWindow, FrequencyManagement frequencyManagement, FT891Cat radio)
        {
            this.mainWindow = mainWindow;
            this.frequencyManagement = frequencyManagement;
            this.radio = radio;
        }

        public void StartDisplayLoop()
        {
            _serialCts = new CancellationTokenSource();
            _serialTask = Task.Run(() => DoDisplayLoop(_serialCts.Token));
        }
        public void StopDisplayLoop()
        {
            _serialCts?.Cancel();
        }

        public async Task DoDisplayLoop(CancellationToken token)
        {
            if (mainWindow.yAESU_FT_891_CAT_Dictionary == null) return;

            int packet = 0;

            while (!token.IsCancellationRequested)
            {
                if (packet == 8)
                    packet = 0;

                if (!MainWindow.isDragging)
                {
                    mainWindow.MainFrequencyTextBlock.Text = frequencyManagement.FormatFrequency(await radio.GetVfoAFrequencyAsync());

                    var rigMode = await radio.GetModeAsync();
                    UpdateUIRigMode(mainWindow.MainRigModeLabelBorder, mainWindow.MainRigModeLabel, (int)rigMode);
                    if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
                    {
                        Console.WriteLine("rigMode = " + rigMode); // Will now correctly print '5'
                        Console.WriteLine("DateTime Now = " + DateTime.Now.ToString("ss"));
                    }

                    if (RigMode == RigModes.FM)
                    {
                        bool busyMode = await radio.GetBusyAsync();

                        if (RigMode == RigModes.FM && mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOff)
                        {
                            if (!busyMode)
                                mainWindow.SetRigLEDColor(RigLEDColors.LightGray);
                            else if (busyMode)
                                mainWindow.SetRigLEDColor(RigLEDColors.Green);
                        }
                    }

                    //switch (packet)
                    //{
                    //    case 0:
                    //        //TXRXState(_port);
                    //        mainWindow.yAESU_FT_891_CAT_Dictionary.SMeter(_port, SMeters.PO);
                    //        await Task.Delay(5);
                    //        break;

                    //    case 1:
                    //        if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOn)
                    //            mainWindow.yAESU_FT_891_CAT_Dictionary.SMeter(_port, SMeters.COMP);
                    //        break;

                    //    case 2:
                    //        if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOn)
                    //            mainWindow.yAESU_FT_891_CAT_Dictionary.SMeter(_port, SMeters.ALC);
                    //        break;

                    //    case 3:
                    //        if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOn)
                    //            mainWindow.yAESU_FT_891_CAT_Dictionary.SMeter(_port, SMeters.PO);
                    //        break;

                    //    case 4:
                    //        if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOn)
                    //            mainWindow.yAESU_FT_891_CAT_Dictionary.SMeter(_port, SMeters.SWR);
                    //        break;

                    //    case 5:
                    //        if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOn)
                    //            mainWindow.yAESU_FT_891_CAT_Dictionary.SMeter(_port, SMeters.IDD);
                    //        break;

                    //    case 6:
                    //        if (mainWindow.TranceiverTXRXState != TranceiverStates.RadioTXOn)
                    //            mainWindow.yAESU_FT_891_CAT_Dictionary.SMeter(_port, SMeters.S);
                    //        break;
                    //    case 7:
                    //        SendCAT(_port, "PC");
                    //        break;
                    //}
                }

                packet++;
            }
        }

    }
}