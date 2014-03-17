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

namespace MessagePredictor.View
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

        private void CloseDropDownButton(object o)
        {
            DropDownButton ddb = o as DropDownButton;
            if (ddb != null)
                ddb.IsOpen = false;
        }

        // Surely there's a better way to close a DropDownButton after the user interacts with it?
        //private void CloseAllDropDownButtons()
        //{
        //    for (int i = 0; i < Topic1Features.Items.Count; i++)
        //    {
        //        var uiElement = (UIElement)Topic1Features.ItemContainerGenerator.ContainerFromIndex(i).FindVisualChild<DropDownButton>();
        //        CloseDropDownButton(uiElement);
        //    }
        //    for (int i = 0; i < Topic2Features.Items.Count; i++)
        //    {
        //        UIElement uiElement = (UIElement)Topic2Features.ItemContainerGenerator.ContainerFromIndex(i);
        //        CloseDropDownButton(uiElement);
        //    }
        //}

        private void CheckMenuItem(MenuItem item)
        {
            StackPanel panel = item.Parent as StackPanel;
            if (panel != null) {
                foreach (UIElement element in panel.Children) {
                    MenuItem otherItem = element as MenuItem;
                    if (otherItem != null) {
                        if (otherItem != item)
                            otherItem.IsChecked = false;
                        else
                            otherItem.IsChecked = true;
                    }
                }
            }
        }

        private void FeatureVeryImportant_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            if (item != null) {
                FeatureSetViewModel fsvm = this.DataContext as FeatureSetViewModel;
                Feature f = item.DataContext as Feature;
                if (fsvm != null && f != null) {
                    fsvm.FeatureVeryImportant.Execute(f);
                    CheckMenuItem(item);
                }
            }
        }

        private void FeatureSomewhatImportant_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            if (item != null) {
                FeatureSetViewModel fsvm = this.DataContext as FeatureSetViewModel;
                Feature f = item.DataContext as Feature;
                if (fsvm != null && f != null) {
                    fsvm.FeatureSomewhatImportant.Execute(f);
                    CheckMenuItem(item);
                }
            }
        }

        private void FeatureRemove_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            if (item != null) {
                FeatureSetViewModel fsvm = this.DataContext as FeatureSetViewModel;
                Feature f = item.DataContext as Feature;
                if (fsvm != null && f != null) {
                    fsvm.FeatureRemove.Execute(f);
                }
            }
        }

        private void FeatureFind_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            if (item != null) {
                FeatureSetViewModel fsvm = this.DataContext as FeatureSetViewModel;
                Feature f = item.DataContext as Feature;
                if (fsvm != null && f != null) {
                    fsvm.HighlightFeature.Execute(f.Characters);
                }

                for (int i = 0; i < Topic1Features.Items.Count; i++) {
                    ContentPresenter c = (ContentPresenter)Topic1Features.ItemContainerGenerator.ContainerFromIndex(i);
                    DropDownButton b = c.ContentTemplate.FindName("dropDownButton", c) as DropDownButton;
                    if (b != null)
                        b.IsOpen = false;
                }

                for (int i = 0; i < Topic2Features.Items.Count; i++) {
                    ContentPresenter c = (ContentPresenter)Topic2Features.ItemContainerGenerator.ContainerFromIndex(i);
                    DropDownButton b = c.ContentTemplate.FindName("dropDownButton", c) as DropDownButton;
                    if (b != null)
                        b.IsOpen = false;
                }
            }
        }

        private void ShowImportantWordsExplanation(object sender, RoutedEventArgs e)
        {
            Dialog d = new Dialog();
            d.Owner = App.Current.MainWindow;
            d.DialogTitle = "Important words";
            d.DialogMessage = "The computer only looks for a small number of words in each message; all of them are listed under 'Important words'.\n\n" +
                "You can tell the computer about new words it should look for using the 'Add a word about...' buttons, or tell the computer not to look for certain words by removing them from this list.\n\n" +
                "If a message doesn't contain any of these important words, the computer will have difficulty predicting its topic.";
            d.ShowDialog();
        }
    }
}
