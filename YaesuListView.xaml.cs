using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace YAESU_FT_891_Front_End
{
    public partial class YaesuListView : UserControl
    {
        public ObservableCollection<object> Items { get; } =
            new ObservableCollection<object>();

        public YaesuListView()
        {
            InitializeComponent();

            ListView.ItemsSource = Items;

            Items.CollectionChanged += Items_CollectionChanged;

            UpdateCount();
        }

        // =========================
        // TOOLBAR SLOT (XAML + CODE)
        // =========================
        public object Toolbar
        {
            get => ToolbarHost.Content;
            set => ToolbarHost.Content = value;
        }

        // =========================
        // COUNT UPDATE
        // =========================
        private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateCount();
        }

        private void UpdateCount()
        {
            if (CountLabel == null) return;

            CountLabel.Content = $"{Items.Count} items";
        }
    }
}