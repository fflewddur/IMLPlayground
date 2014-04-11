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

        private void ShowImportantWordsExplanation(object sender, RoutedEventArgs e)
        {
            FeatureSetViewModel vm = this.DataContext as FeatureSetViewModel;
            if (vm != null) {
                vm.LogShowImportantWordsExplanationStart();
                Dialog d = new Dialog();
                d.Owner = App.Current.MainWindow;
                d.DialogTitle = "Important words";
                d.DialogMessage = "The computer only looks for a small number of words in each message; all of them are listed under 'Important words'.\n\n" +
                    "You can tell the computer about new words it should look for using the 'Add a new word or phrase' button, or tell the computer not to look for certain words by removing them from this list.\n\n" +
                    "If a message doesn't contain any of these important words, the computer will have difficulty predicting its topic.";
                d.ShowDialog();
                vm.LogShowImportantWordsExplanationEnd();
            }
        }
    }
}
