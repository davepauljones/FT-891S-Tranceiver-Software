using FT891S_CatControl;
using System;
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
        private int _dnrValue;
        private bool _dnfEnabled;
        private bool _apfEnabled;
        private bool _contourEnabled;

        public FilterWave(MainWindow mainWindow, FilterWaveControl uiControl)
        {
            this.mainWindow = mainWindow;
            this.uiControl = uiControl ?? throw new ArgumentNullException(nameof(uiControl));
            this.uiDispatcher = uiControl.Dispatcher;

            // Direct internal event subscription hookup
            uiControl.UIValueChanged += OnUIValueChanged;
        }

        private async void OnUIValueChanged(string parameter, object value)
        {
            switch (parameter)
            {
                case "NB":
                    _nbValue = (int)value;
                    // Add Radio CAT code out here: e.g., SendCommand($"NB{_nbValue};");
                    Console.WriteLine("NB Changed");
                    
                    await mainWindow._catManager.SendCatCommandAsync("NB", new object[] { 0, _nbValue }, mainWindow._catManager.OutGoingDataLoopDelay);

                    await mainWindow._catManager.SendCatCommandAsync("NL", new object[] { 0, _nbValue }, mainWindow._catManager.OutGoingDataLoopDelay);
                    break;
                case "WD":
                    _widthValue = (int)value;
                    Console.WriteLine("WB Changed");
                    break;
                case "NR":
                    _dnrValue = (int)value;
                    Console.WriteLine("NR Changed");

                    if (_dnrValue == 0)
                        await mainWindow._catManager.SendCatCommandAsync("NR", new object[] { 0, 0 }, mainWindow._catManager.OutGoingDataLoopDelay);
                    else
                    {
                        await mainWindow._catManager.SendCatCommandAsync("NR", new object[] { 0, 1 }, mainWindow._catManager.OutGoingDataLoopDelay);

                        await mainWindow._catManager.SendCatCommandAsync("RL", new object[] { 0, _dnrValue }, mainWindow._catManager.OutGoingDataLoopDelay);
                    }

                    break;
                case "AUTO DNF":
                    _dnfEnabled = (bool)value;
                    Console.WriteLine("AUTO DNF Changed");
                    break;
                case "CW APF":
                    _apfEnabled = (bool)value;
                    Console.WriteLine("CW APF Changed");
                    break;
                case "CONTOUR":
                    _contourEnabled = (bool)value;
                    Console.WriteLine("CONTOUR Changed");
                    break;
            }
        }

        // Call this when background radio serial data threads detect state variations
        public void UpdateFromRadio(string parameter, object value)
        {
            uiDispatcher.Invoke(() =>
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
            });
        }
    }
}