using FT891S_CatControl;
using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace YAESU_FT_891_Front_End
{
    public class FilterWave
    {
        private readonly MainWindow mainWindow;
        private readonly FilterWaveControl uiControl;
        private readonly Dispatcher uiDispatcher;

        private int _nbValue;
        private int _widthValue;
        private int _notchFreq;
        private int _notchDepth;
        private int _isValue;
        private int _dnrValue;
        private bool _dnfEnabled;
        private bool _apfEnabled;
        private bool _contourEnabled;

        public FilterWave(MainWindow mainWindow, FilterWaveControl uiControl)
        {
            this.mainWindow = mainWindow;
            this.uiControl = uiControl ?? throw new ArgumentNullException("uiControl");
            this.uiDispatcher = uiControl.Dispatcher;

            uiControl.UIValueChanged += OnUIValueChanged;
        }

        private void OnUIValueChanged(string parameter, object value)
        {
            Console.WriteLine(parameter);

            Task.Run(async () =>
            {
                try
                {
                    if (mainWindow == null || mainWindow._catManager == null) return;

                    switch (parameter)
                    {
                        case "NB":
                            _nbValue = (int)value;

                            mainWindow.Dispatcher.Invoke(() =>
                            {
                                if (_nbValue == 0)
                                    mainWindow.NBLabelBorder.Visibility = System.Windows.Visibility.Hidden;
                                else
                                    mainWindow.NBLabelBorder.Visibility = System.Windows.Visibility.Visible;
                            });
                           
                            await mainWindow._catManager.SendCatCommandAsync("NB", new object[] { 0, _nbValue }, mainWindow._catManager.OutGoingDataLoopDelay);
                            await mainWindow._catManager.SendCatCommandAsync("NL", new object[] { 0, _nbValue }, mainWindow._catManager.OutGoingDataLoopDelay);
                            break;
                        case "SH":
                            _widthValue = (int)value;
                            if (_widthValue == -1)
                                await mainWindow._catManager.SendCatCommandAsync("SH", new object[] { 0, 0, _widthValue }, mainWindow._catManager.OutGoingDataLoopDelay);
                            else
                                await mainWindow._catManager.SendCatCommandAsync("SH", new object[] { 0, 1, 9 }, mainWindow._catManager.OutGoingDataLoopDelay);
                            break;

                        case "NCH_FREQ":
                            _notchFreq = (int)value;
                            int scaledHz = 10 + (int)((_notchFreq / 100.0) * 3190);
                            // Example: await mainWindow._catManager.SendCatCommandAsync("BP", new object[] { 0, scaledHz }, ...);
                            break;

                        case "NCH_DEPTH":
                            _notchDepth = (int)value;
                            bool active = _notchDepth > 0;
                            // Example: await mainWindow._catManager.SendCatCommandAsync("NT", new object[] { 0, active ? 1 : 0 }, ...);
                            break;

                        case "SHIFT":
                            _isValue = (int)value;

                            Console.WriteLine(">>>>> " + _isValue);
                            
                            char _ShiftDirection = '+';
                            
                            if (_isValue > 0)
                                _ShiftDirection = '+';
                            else if (_isValue < 0)
                                _ShiftDirection = '-';
                            else
                                _ShiftDirection = '+';

                            String BuiltIsCommand = FT891S_CatCommandTypes.BuildIsCommand(0, 1, _ShiftDirection, _isValue);
                            await mainWindow._catManager.SendCatCommandAsync("IS", BuiltIsCommand, mainWindow._catManager.OutGoingDataLoopDelay);
                            break;

                        case "NR":
                            _dnrValue = (int)value;

                            mainWindow.Dispatcher.Invoke(() =>
                            {
                                if (_dnrValue == 0)
                                    mainWindow.DNRLabelBorder.Visibility = System.Windows.Visibility.Hidden;
                                else
                                    mainWindow.DNRLabelBorder.Visibility = System.Windows.Visibility.Visible;
                            });

                            if (_dnrValue == 0)
                            {
                                await mainWindow._catManager.SendCatCommandAsync("NR", new object[] { 0, 0 }, mainWindow._catManager.OutGoingDataLoopDelay);
                            }
                            else
                            {
                                await mainWindow._catManager.SendCatCommandAsync("NR", new object[] { 0, 1 }, mainWindow._catManager.OutGoingDataLoopDelay);
                                await mainWindow._catManager.SendCatCommandAsync("RL", new object[] { 0, _dnrValue }, mainWindow._catManager.OutGoingDataLoopDelay);
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("CAT Error sending " + parameter + ": " + ex.Message);
                }
            });
        }

        public void UpdateFromRadio(string parameter, object value)
        {
            uiDispatcher.BeginInvoke(new Action(() =>
            {
                switch (parameter.ToUpper())
                {
                    case "NB":
                        _nbValue = (int)value;
                        uiControl.SetNbValue(_nbValue);
                        break;
                    case "WD":
                        _widthValue = (int)value;
                        uiControl.SetWidthValue(_widthValue);
                        break;
                    case "NCH":
                        Tuple<int, int> notchVals = value as Tuple<int, int>;
                        if (notchVals != null)
                        {
                            _notchFreq = notchVals.Item1;
                            _notchDepth = notchVals.Item2;
                            uiControl.SetNotchValues(_notchFreq, _notchDepth);
                        }
                        break;
                    case "NR":
                        _dnrValue = (int)value;
                        uiControl.SetDnrValue(_dnrValue);
                        break;
                    case "DNF":
                        _dnfEnabled = (bool)value;
                        uiControl.SetDnfEnabled(_dnfEnabled);
                        break;
                    case "APF":
                        _apfEnabled = (bool)value;
                        uiControl.SetApfEnabled(_apfEnabled);
                        break;
                    case "CONTOUR":
                        _contourEnabled = (bool)value;
                        uiControl.SetContourEnabled(_contourEnabled);
                        break;
                }
            }));
        }
    }
}