using FT891S_CatControl;
using HamRadioControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using static YAESU_FT_891_Front_End.MyStructs;
using static YAESU_FT_891_Front_End.RigStateChanges;

namespace YAESU_FT_891_Front_End
{
    public class FT891S_SerialPort
    {
        MainWindow mainWindow;

        public SerialPort _port;

        public CancellationTokenSource _serialCts;
        public Task _serialTask;

        public FT891S_SerialPort(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }


        //public static SerialPort InitSerialPort(String PortName, SerialDataReceivedEventHandler SerialDataReceivedMethod)
        //{
        //    EnableGlobalWrites = false;

        //    SerialPortExtended sp = null;

        //    List<Ports> ports = ArduCommunication.GetPortNamesExtended();

        //    foreach (Ports p in ports)
        //    {
        //        if (p.PortName == PortName)
        //        {
        //            foreach (KeyValuePair<Int32, EngineV1> ekvp in MainWindow.InstalledEngines)
        //            {
        //                if (ekvp.Value.Engine_Communication.PortName == PortName)
        //                {
        //                    p.EngineID = ekvp.Value.Engine_Engine.Id;
        //                    p.PacketAddress = ekvp.Value.Engine_Packet.PacketAddress;
        //                    p.BaudRate = ekvp.Value.Engine_Communication.BaudRate;
        //                    p.EngineReset = ekvp.Value.Engine_Communication.EngineReset;
        //                }
        //            }

        //            p.Status = ConnectedSerialPortStatus.Initialised;

        //            sp = new SerialPortExtended();

        //            sp.PortName = PortName;

        //            int TranslatedBaudRate = 57600;

        //            switch (p.BaudRate)
        //            {
        //                case ParamGroup_Communication.BaudRateIDs.Baud_300:
        //                    TranslatedBaudRate = ParamGroup_Communication.BaudRate.Baud_300;
        //                    break;
        //                case ParamGroup_Communication.BaudRateIDs.Baud_1200:
        //                    TranslatedBaudRate = ParamGroup_Communication.BaudRate.Baud_1200;
        //                    break;
        //                case ParamGroup_Communication.BaudRateIDs.Baud_2400:
        //                    TranslatedBaudRate = ParamGroup_Communication.BaudRate.Baud_2400;
        //                    break;
        //                case ParamGroup_Communication.BaudRateIDs.Baud_4800:
        //                    TranslatedBaudRate = ParamGroup_Communication.BaudRate.Baud_4800;
        //                    break;
        //                case ParamGroup_Communication.BaudRateIDs.Baud_9600:
        //                    TranslatedBaudRate = ParamGroup_Communication.BaudRate.Baud_9600;
        //                    break;
        //                case ParamGroup_Communication.BaudRateIDs.Baud_19200:
        //                    TranslatedBaudRate = ParamGroup_Communication.BaudRate.Baud_19200;
        //                    break;
        //                case ParamGroup_Communication.BaudRateIDs.Baud_57600:
        //                    TranslatedBaudRate = ParamGroup_Communication.BaudRate.Baud_57600;
        //                    break;
        //                case ParamGroup_Communication.BaudRateIDs.Baud_115200:
        //                    TranslatedBaudRate = ParamGroup_Communication.BaudRate.Baud_115200;
        //                    break;
        //            }

        //            sp.BaudRate = TranslatedBaudRate;
        //            sp.BaudRate = 57600;//take out when implementing changes in baud rate both ends

        //            sp.Handshake = System.IO.Ports.Handshake.None;
        //            sp.Parity = Parity.None;
        //            sp.DataBits = 8;
        //            sp.StopBits = StopBits.Two;
        //            sp.ReadTimeout = 200;
        //            sp.WriteTimeout = 50;

        //            sp.EngineID = p.EngineID;
        //            sp.PacketAddress = p.PacketAddress;
        //            sp.PortNameExtended = p.PortNameExtended;

        //            if (OverrideEngineReset)
        //                sp.EngineReset = ParamGroup_Communication.EngineReset.NoReset;
        //            else
        //                sp.EngineReset = p.EngineReset;

        //            sp.ReadEnabled = p.ReadEnabled;
        //            sp.WriteEnabled = p.WriteEnabled;
        //            sp.Status = p.Status;

        //            MainWindow.MAINWINDOW.statusBar.Status("Init serial port : " + sp.PortName);
        //            //Console.Write("Init serial port : ");
        //            //Console.WriteLine(sp.PortName);

        //            if (!EngineSerialPortDictionary.ContainsKey(sp.PortName))
        //            {
        //                EngineSerialPortDictionary.Add(PortName, sp);
        //                //Console.Write("Serial Port added to EngineSerialPortDictionary on ");
        //                //Console.WriteLine(sp.PortName);
        //            }
        //            else
        //            {
        //                //Console.Write("Failed to add serial port to EngineSerialPortDictionary on ");
        //                //Console.WriteLine(sp.PortName);
        //            }

        //            sp = AddSerialDataReceivedEventHandler(sp);
        //        }
        //    }

        //    if (sp != null)
        //    {
        //        //Console.Write("Returned sp.PortName = ");
        //        //Console.WriteLine(sp.PortName);
        //    }

        //    EnableGlobalWrites = true;

        //    return sp;
        //}
        //public static SerialPortExtended AddSerialDataReceivedEventHandler(SerialPortExtended sp)
        //{
        //    EnableGlobalWrites = false;

        //    if (sp != null)
        //    {
        //        if (sp.IsOpen)
        //        {
        //            RemoveSerialDataReceivedEventHandler(sp);
        //        }

        //        try
        //        {
        //            //Arduino reset via DTR
        //            if (sp.EngineReset == ParamGroup_Communication.EngineReset.ResetOnConnect)
        //            {
        //                sp.DtrEnable = true;
        //            }

        //            if (!sp.IsOpen) sp.Open();

        //            if (sp.EngineReset == ParamGroup_Communication.EngineReset.ResetOnConnect)
        //            {
        //                System.Threading.Thread.Sleep(1000);//must be 1000, no less
        //                sp.DtrEnable = false;
        //            }
        //            //Arduino reset via DTR

        //            sp.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(MainWindow.MAINWINDOW.PACKET.scanForNewPacket);

        //            sp.Status = ConnectedSerialPortStatus.Normal;

        //            MainWindow.MAINWINDOW.statusBar.Status("Added Serial Data Received Event Handler to " + sp.PortName);
        //            //Console.Write("AddSerialDataReceivedEventHandler to ");
        //            //Console.WriteLine(sp.PortName);
        //        }
        //        catch (Exception ex)
        //        {
        //            if (ex is IOException)
        //            {
        //                Console.Write("IOException in EngineSerialPort.ConnectSerialPortPacket on Serial Port: ");
        //                Console.WriteLine(sp.PortName);
        //            }
        //            else if (ex is InvalidOperationException)
        //            {
        //                Console.Write("InvalidOperationException in EngineSerialPort.ConnectSerialPortPacket on Serial Port: ");
        //                Console.WriteLine(sp.PortName);
        //            }
        //            else if (ex is UnauthorizedAccessException)
        //            {
        //                Console.Write("UnauthorizedAccessException in EngineSerialPort.ConnectSerialPortPacket on Serial Port: ");
        //                Console.WriteLine(sp.PortName);
        //            }
        //            else
        //            {
        //                Console.Write("Exception in EngineSerialPort.ConnectSerialPortPacket on Serial Port: ");
        //                Console.WriteLine(sp.PortName);
        //                Console.WriteLine(ex);
        //            }
        //        }
        //    }

        //    EnableGlobalWrites = true;

        //    return sp;
        //}
        //public static SerialPortExtended RemoveSerialDataReceivedEventHandler(SerialPortExtended sp)
        //{
        //    EnableGlobalWrites = false;

        //    if (sp != null)
        //    {
        //        sp.DataReceived -= new System.IO.Ports.SerialDataReceivedEventHandler(MainWindow.MAINWINDOW.PACKET.scanForNewPacket);

        //        sp.Status = ConnectedSerialPortStatus.ReadDisabled;

        //        MainWindow.MAINWINDOW.statusBar.Status("Removed Serial Data Received Event Handler on " + sp.PortName);
        //        //Console.Write("RemoveSerialDataReceivedEventHandler on ");
        //        //Console.WriteLine(sp.PortName);
        //    }

        //    EnableGlobalWrites = true;

        //    return sp;
        //}
       
        //public static void RemoveSerialPort(SerialPortExtended sp, bool FromRemoveAllSerialPorts = false)
        //{
        //    EnableGlobalWrites = false;

        //    if (sp != null)
        //    {
        //        if (sp.IsOpen)
        //        {
        //            RemoveSerialDataReceivedEventHandler(sp);
        //        }

        //        try
        //        {
        //            sp.Close();
        //        }
        //        catch (Exception ex)
        //        {
        //            if (ex is IOException)
        //            {
        //                Console.Write("IOException in RemoveSerialPort - Error sp.Close() on Serial Port: ");
        //                Console.WriteLine(sp.PortName);
        //                //Console.WriteLine(ex);
        //            }
        //            else
        //            {
        //                Console.Write("Exception in RemoveSerialPort - Error sp.Close() on Serial Port: ");
        //                Console.WriteLine(sp.PortName);
        //                Console.WriteLine(ex);
        //            }
        //        }

        //        if (!FromRemoveAllSerialPorts) EngineSerialPortDictionary.Remove(sp.PortName);

        //        MainWindow.MAINWINDOW.statusBar.Status("Remove serial port : " + sp.PortName);
        //        //Console.Write("Remove serial port : ");
        //        //Console.WriteLine(sp.PortName);

        //        sp = null;
        //    }

        //    EnableGlobalWrites = true;
        //}



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

                if (_port != null)
                {
                    mainWindow.ComportTextBlock.Text = _port.PortName;

                    if(_port.IsOpen)
                        mainWindow.ComportGreenRectangle.Opacity = 0.5;
                    else
                        mainWindow.ComportGreenRectangle.Opacity = 0.2;
                }
                else
                {
                    mainWindow.ComportTextBlock.Text = "NONE";
                    mainWindow.ComportGreenRectangle.Opacity = 0.2;
                }
                

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
                        // 1. Guard against Application or Dispatcher becoming null during shutdown
                        var dispatcher = Application.Current?.Dispatcher;
                        if (dispatcher == null) break;

                        dispatcher.Invoke(() =>
                        {
                            // 2. Double-check that mainWindow and its manager are still valid
                            if (mainWindow?._catManager != null)
                            {
                                mainWindow._catManager.HandleIncomingData(message);
                            }
                        });
                    }
                    catch (Exception uiEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"UI Dispatcher Error handling CAT message '{message}': {uiEx.Message}");
                    }
                }
            }
            catch (Exception serialEx)
            {
                System.Diagnostics.Debug.WriteLine($"Serial Port or Buffer Processing Error: {serialEx.Message}");
                // Handle serial thread failures here (e.g., if the device is abruptly unplugged)
            }
        }

        public void LoadComPorts()
        {
            // Get available serial ports
            string[] ports = SerialPort.GetPortNames();

            // Feed the array directly to the ItemsControl
            mainWindow.IcComPorts.ItemsSource = ports;
        }

    }
}