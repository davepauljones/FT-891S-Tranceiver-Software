using FT891S_CatControl;
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
                        // Dispatcher.Invoke blocks the serial thread until the UI thread finishes processing HandleCAT.
                        // If HandleCAT throws an error, it bubbles up right into this internal catch block.
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            mainWindow._catManager.HandleIncomingData(message);
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