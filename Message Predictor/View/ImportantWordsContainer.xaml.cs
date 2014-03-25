using MessagePredictor.ViewModel;
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

namespace MessagePredictor.View
{
    /// <summary>
    /// Interaction logic for ImportantWords.xaml
    /// </summary>
    public partial class ImportantWordsContainer : UserControl
    {
        public ImportantWordsContainer()
        {
            InitializeComponent();
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // We'll use this new height to update the height of our feature graphs' bars
            FeatureSetViewModel vm = this.DataContext as FeatureSetViewModel;
            if (vm != null) {
                vm.FeatureGraphHeight = e.NewSize.Height;
            }
        }

        private void Grid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // We'll use this new height to update the height of our feature graphs' bars
            FeatureSetViewModel vm = this.DataContext as FeatureSetViewModel;
            if (vm != null) {
                vm.FeatureGraphHeight = this.ActualHeight;
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabControl tc = sender as TabControl;
            FeatureSetViewModel vm = this.DataContext as FeatureSetViewModel;
            if (vm != null) {
                string tabName = "Unknown";
                switch (tc.SelectedIndex) {
                    case 0:
                        tabName = "Overview";
                        break;
                    case 1:
                        tabName = "Hockey";
                        break;
                    case 2:
                        tabName = "Baseball";
                        break;
                }
                vm.LogFeatureTabChanged(tabName);
            }
        }
    }
}
