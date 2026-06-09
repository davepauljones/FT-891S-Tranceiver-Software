using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace YAESU_FT_891_Front_End
{
    /// <summary>
    /// Interaction logic for StationScope.xaml
    /// </summary>
    public partial class StationScope : UserControl
    {
        MainWindow mainWindow;
        public StationSeekClass station;
        FrequencyManagement frequencyManagement;
        public StationScope(MainWindow mainWindow, StationSeekClass station, FrequencyManagement frequencyManagement)
        {
            InitializeComponent();

            this.mainWindow = mainWindow;
            this.station = station;
            this.frequencyManagement = frequencyManagement;

            LoadStation();
        }

        private void LoadStation()
        {
            StationIDTextBlock.Text = station.ID.ToString();
            StationFrequencyTextBlock.Text = frequencyManagement.FormatFrequency(station.Frequency);
            mainWindow.UpdateMeter(SignalStrengthBarGraphRectangle, station.SignalStrength);
            BarGraphTextBlock.Text = MainWindow.GetSMeterInteger(station.SignalStrength) + " dB";
        }
    }
}