using HamRadioControls;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using static YAESU_FT_891_Front_End.MyStructs;
using static YAESU_FT_891_Front_End.RigState;
using static YAESU_FT_891_Front_End.RigStateChanges;

namespace YAESU_FT_891_Front_End
{
    public class FT891S_SerialPort
    {
        MainWindow mainWindow;

        public SerialPort _port;

        public CancellationTokenSource _serialCts;
        public Task _serialTask;

        public FT891S_SerialPort(MainWindow mainWindow, String comPortName)
        {
            this.mainWindow = mainWindow;

            //OpenPort(comPortName);
        }

        public bool OpenPort(string portName)
        {
            try
            {
                _port = new SerialPort(portName, 38400, Parity.None, 8, StopBits.One);
                _port.Handshake = Handshake.None;

                _port.DtrEnable = true;
                _port.RtsEnable = true; // optional, not required

                _port.NewLine = ";"; // important for CAT
                _port.DataReceived += OnDataReceived;
                _port.Open();

                if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
                {
                    Console.WriteLine("Connected to FT-891");
                }
                return true;
            }
            catch (Exception ex)
            {
                if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
                {
                    Console.WriteLine("ERROR: " + ex.Message);
                }
                return false;
            }
        }

        public async Task SerialLoop(CancellationToken token)
        {
            if (mainWindow.yAESU_FT_891_CAT_Dictionary == null) return;

            int packet = 0;

            while (!token.IsCancellationRequested)
            {
                if (packet == 8)
                    packet = 0;

                if (!MainWindow.isDragging)
                {
                    mainWindow.yAESU_FT_891_CAT_Dictionary.FreqA(_port, 0);
                    await Task.Delay(5);

                    mainWindow.yAESU_FT_891_CAT_Dictionary.SetMode(_port, 0);
                    await Task.Delay(5);

                    if (RigMode == RigModes.FM)
                    {
                        SendCAT(_port, "BY");
                        await Task.Delay(5);
                    }

                    switch (packet)
                    {
                        case 0:
                            //TXRXState(_port);
                            mainWindow.yAESU_FT_891_CAT_Dictionary.SMeter(_port, SMeters.PO);
                            await Task.Delay(5);
                            break;

                        case 1:
                            if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOn)
                                mainWindow.yAESU_FT_891_CAT_Dictionary.SMeter(_port, SMeters.COMP);
                            break;

                        case 2:
                            if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOn)
                                mainWindow.yAESU_FT_891_CAT_Dictionary.SMeter(_port, SMeters.ALC);
                            break;

                        case 3:
                            if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOn)
                                mainWindow.yAESU_FT_891_CAT_Dictionary.SMeter(_port, SMeters.PO);
                            break;

                        case 4:
                            if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOn)
                                mainWindow.yAESU_FT_891_CAT_Dictionary.SMeter(_port, SMeters.SWR);
                            break;

                        case 5:
                            if (mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOn)
                                mainWindow.yAESU_FT_891_CAT_Dictionary.SMeter(_port, SMeters.IDD);
                            break;

                        case 6:
                            if (mainWindow.TranceiverTXRXState != TranceiverStates.RadioTXOn)
                                mainWindow.yAESU_FT_891_CAT_Dictionary.SMeter(_port, SMeters.S);
                            break;
                        case 7:
                            SendCAT(_port, "PC");
                            break;
                    }
                }

                packet++;

                // controls timing (this replaces DispatcherTimer)
                await Task.Delay(5, token);
            }
        }

        public async void SendCAT(SerialPort _port, string command)
        {
            if (_port != null && _port.IsOpen)
            {
                if (!command.EndsWith(";"))
                    command += ";";

                _port.Write(command);
                await Task.Delay(5);

                if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
                {
                    Console.WriteLine("SendCAT: " + command);
                }
            }
        }

        private void HandleCAT(string msg)
        {
            try
            {
                if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
                {
                    Console.WriteLine("HandleCAT RX: " + msg);
                }

                if (string.IsNullOrWhiteSpace(msg))
                    return;

                msg = msg.Trim();

                if (msg.StartsWith("FA"))
                {
                    string digits = msg.Substring(2).TrimEnd(';');

                    if (long.TryParse(digits, out long freq))
                    {
                        //if (frequencyManagement != null)
                        mainWindow.frequencyManagement.SetFrequency(MemorySlot.MemorySlots.VFO_A, FrequencyLocations.RXFrequencyHz, freq, mainWindow.MainFrequencyTextBlock);
                    }
                    else
                    {
                        if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
                        {
                            Console.WriteLine("**** Invalid FA format: " + msg);
                        }
                    }
                }
                else if (msg.StartsWith("RM"))
                {
                    string digits = msg.Substring(3, 3).TrimEnd(';');
                    byte meterToUpdate = Convert.ToByte(msg.Substring(2, 1).TrimEnd(';'));

                    if (int.TryParse(digits, out int meterReading))
                    {
                        switch (meterToUpdate)
                        {
                            case SMeters.S:
                                mainWindow.UpdateMeter(mainWindow.BarGraphRectangle, meterReading);
                                mainWindow.stationSeek.LastSMeterRawReading = meterReading;
                                mainWindow.stationSeek.LastSMeterReading = MainWindow.GetSMeterInteger(meterReading);

                                mainWindow.SignalMeter.Value = AnalogMeter.ConvertDoubleToPercentage(Convert.ToDouble(meterReading));

                                byte spriteWidth = Convert.ToByte(AnalogMeter.ConvertDoubleToPercentage(Convert.ToDouble(meterReading)));
                                mainWindow.sprite.GenerateSprite(spriteWidth, 295, 2);

                                if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
                                {
                                    Console.WriteLine("meterReading = " + meterReading);
                                }

                                break;
                            case SMeters.COMP:
                                mainWindow.UpdateMeter(mainWindow.BarGraphRectangle, meterReading);
                                break;
                            case SMeters.ALC:
                                mainWindow.UpdateMeter(mainWindow.ALCBarGraphRectangle, meterReading);
                                break;
                            case SMeters.PO:
                                mainWindow.UpdateMeter(mainWindow.POBarGraphRectangle, meterReading);
                                if (meterReading > 0)
                                    mainWindow.UpdateTranceiverTXRXState(TranceiverStates.RadioTXOn);
                                else
                                    mainWindow.UpdateTranceiverTXRXState(TranceiverStates.RadioTXOff);

                                //change to correct meter some day
                                mainWindow.SignalMeter.Value = meterReading;

                                break;
                            case SMeters.SWR:
                                mainWindow.UpdateMeter(mainWindow.SWRBarGraphRectangle, meterReading);

                                //change to correct meter some day
                                mainWindow.SignalMeter.Value = MainWindow.GetSMeterInteger(meterReading);

                                break;
                            case SMeters.IDD:
                                mainWindow.UpdateMeter(mainWindow.IDDBarGraphRectangle, meterReading);
                                break;
                        }
                    }
                    else
                    {
                        if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
                        {
                            Console.WriteLine("*** Invalid RM format: " + msg);
                        }
                    }
                }
                else if (msg.StartsWith("MD0"))
                {
                    string modeChar = msg.Substring(3, 1);

                    if (int.TryParse(modeChar, System.Globalization.NumberStyles.HexNumber,
                                     null, out int rigMode))
                    {
                        UpdateUIRigMode(mainWindow.MainRigModeLabelBorder, mainWindow.MainRigModeLabel, rigMode);

                        if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
                        {
                            Console.WriteLine("rigMode = " + rigMode); // Will now correctly print '5'
                            Console.WriteLine("modeChar = " + modeChar);
                            Console.WriteLine("msg = " + msg);
                            Console.WriteLine("DateTime Now = " + DateTime.Now.ToString("ss"));
                        }
                    }
                }
                else if (msg.StartsWith("BY"))
                {
                    byte busyMode = Convert.ToByte(msg.Substring(2, 1).TrimEnd(';'));

                    if (RigMode == RigModes.FM && mainWindow.TranceiverTXRXState == TranceiverStates.RadioTXOff)
                    {
                        if (busyMode == 0)
                            mainWindow.SetRigLEDColor(RigLEDColors.LightGray);
                        else if (busyMode == 1)
                            mainWindow.SetRigLEDColor(RigLEDColors.Green);
                    }
                }
                else if (msg.StartsWith("PC"))
                {
                    string digits = msg.Substring(2).TrimEnd(';');

                    if (long.TryParse(digits, out long power))
                    {
                        mainWindow.PowerControlLabel.Content = power.ToString() + "W";
                        mainWindow.RfPowerFunctionTextBlock.Text = power.ToString() + "W";
                    }
                    else
                    {
                        if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
                        {
                            Console.WriteLine("**** Invalid FA format: " + msg);
                        }
                    }
                }
                else
                {
                    if (mainWindow.ConsoleDebugLevel == ConsoleDebugLevels.All)
                    {
                        Console.WriteLine("*** Error None: ");
                    }
                }
             }
            catch
            {
                Console.WriteLine("*** Error Bad CAT Returned: ");
            }
        }

        public void StartSerialLoop()
        {
            _serialCts = new CancellationTokenSource();
            _serialTask = Task.Run(() => SerialLoop(_serialCts.Token));
        }
        public void StopSerialLoop()
        {
            _serialCts?.Cancel();
        }

        private string _buffer = "";
        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string incoming = _port.ReadExisting();
                _buffer += incoming;

                while (_buffer.Contains(";"))
                {
                    int idx = _buffer.IndexOf(";");
                    string message = _buffer.Substring(0, idx + 1);
                    _buffer = _buffer.Substring(idx + 1);

                    try
                    {
                        // Dispatcher.Invoke blocks the serial thread until the UI thread finishes processing HandleCAT.
                        // If HandleCAT throws an error, it bubbles up right into this internal catch block.
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            HandleCAT(message);
                        });
                    }
                    catch (Exception uiEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"UI Dispatcher Error handling CAT message '{message}': {uiEx.Message}");
                        // Handle or log UI-specific errors here without breaking the main serial reading loop
                    }
                }
            }
            catch (Exception serialEx)
            {
                System.Diagnostics.Debug.WriteLine($"Serial Port or Buffer Processing Error: {serialEx.Message}");
                // Handle serial thread failures here (e.g., if the device is abruptly unplugged)
            }
        }
    }
}