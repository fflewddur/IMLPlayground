using LibIML;
using MessagePredictor.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;

namespace MessagePredictor
{
    /// <summary>
    /// Interaction logic for ImportantWordsOverview.xaml
    /// </summary>
    public partial class ImportantWordsOverview : UserControl
    {
        public ImportantWordsOverview()
        {
            InitializeComponent();
        }

        private void CloseParentDropDownButton(DependencyObject o)
        {

            //DropDownButton ddb = Utilities.FindParent<DropDownButton>(o);
            //if (ddb != null)
            //    ddb.IsOpen = false;
        }

        private void FeatureVeryImportant_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            if (item != null)
            {
                FeatureSetViewModel fsvm = this.DataContext as FeatureSetViewModel;
                Feature f = item.DataContext as Feature;
                if (fsvm != null && f != null)
                {
                    fsvm.FeatureVeryImportant.Execute(f);
                }
                CloseParentDropDownButton(item);
            }
        }

        private void FeatureSomewhatImportant_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            if (item != null)
            {
                FeatureSetViewModel fsvm = this.DataContext as FeatureSetViewModel;
                Feature f = item.DataContext as Feature;
                if (fsvm != null && f != null)
                {
                    fsvm.FeatureSomewhatImportant.Execute(f);
                }
                CloseParentDropDownButton(item);
            }
        }

        private void FeatureRemove_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            if (item != null)
            {
                FeatureSetViewModel fsvm = this.DataContext as FeatureSetViewModel;
                Feature f = item.DataContext as Feature;
                if (fsvm != null && f != null)
                {
                    fsvm.FeatureRemove.Execute(f);
                }
                CloseParentDropDownButton(item);
            }
        }
    }
}
