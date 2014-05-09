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
using System.ComponentModel;

namespace MessagePredictor.View
{
    /// <summary>
    /// Interaction logic for MessageList.xaml
    /// </summary>
    public partial class MessageList : UserControl
    {
        private MessagePredictorViewModel _vm;

        public MessageList()
        {
            InitializeComponent();

            MessagePredictorViewModel vm = this.DataContext as MessagePredictorViewModel;
            if (vm != null) {
                _vm = vm;
                _vm.SelectedMessageProgrammaticallyChanged += _vm_SelectedMessageProgrammaticallyChanged;
            }
        }

        private void _vm_SelectedMessageProgrammaticallyChanged(object sender, MessagePredictorViewModel.SelectedMessageProgrammaticallyChangedEventArgs e)
        {
            MessagePredictorViewModel vm = sender as MessagePredictorViewModel;

            Grid.SelectedItem = e.Message;
            Grid.UpdateLayout();
            bool isVisible = vm.SelectedMessageIsVisible(e.Message);
            if (Grid.SelectedItem != null && isVisible) {
                Grid.ScrollIntoView(Grid.SelectedItem);
            } else {
                Dialog d = new Dialog();
                d.DialogTitle = string.Format("Only showing recently changed messages");
                d.DialogMessage = string.Format("This message wasn't impacted by your most recent change, but you have the \"Only Show Predictions that Just Changed\" option turned on. You'll need to turn it off to see this message in the list.");
                d.Owner = App.Current.MainWindow;
                d.ShowDialog();
            }
        }

        private void DataGrid_Selected(object sender, RoutedEventArgs e)
        {
            
        }

        private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            //Console.WriteLine("Sorting: {0} {1} {2}", e.Column.Header, e.Column.SortDirection, e.Column.SortMemberPath);
            MessagePredictorViewModel vm = this.DataContext as MessagePredictorViewModel;
            // This even fires *before* the sort, so we need to anticipate what will happen. Null -> Ascending -> Descending
            ListSortDirection d;
            if (e.Column.SortDirection == ListSortDirection.Descending || string.IsNullOrEmpty(e.Column.SortDirection.ToString())) {
                d = ListSortDirection.Ascending;
            } else {
                d = ListSortDirection.Descending;
            }
            vm.LogMessageListSorted(e.Column.SortMemberPath, d.ToString());
        }

        private void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            MessagePredictorViewModel vm = this.DataContext as MessagePredictorViewModel;
            if (vm != null && !vm.ShowExplanations) {
                PredictionConfidenceCol.Visibility = Visibility.Collapsed;
            }
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            MessagePredictorViewModel vm = this.DataContext as MessagePredictorViewModel;
            if (vm != null) {
                _vm = vm;
                _vm.SelectedMessageProgrammaticallyChanged += _vm_SelectedMessageProgrammaticallyChanged;
            }
        }

        private void Grid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_vm != null) {
                _vm.LogMessageListScrolled(e.VerticalChange, e.VerticalOffset);
            }
        }

        /// <summary>
        /// Start a drag event for moving items to different folders.
        /// </summary>
        /// <param name="sender">The DataGrid where the drag event originated.</param>
        /// <param name="e"></param>
        //private void DataGrid_PreviewMouseMove(object sender, MouseEventArgs e)
        //{
        //    if (e.LeftButton == MouseButtonState.Pressed)
        //    {
        //        DataGrid grid = sender as DataGrid;
        //        NewsItem selectedItem = grid.SelectedItem as NewsItem;
        //        DataGridRow row = grid.ItemContainerGenerator.ContainerFromItem(selectedItem) as DataGridRow;
                
        //        if (selectedItem != null)
        //        {
        //            DataObject data = new DataObject(typeof(NewsItem), selectedItem);
        //            DragDrop.DoDragDrop(row, data, DragDropEffects.Move);
        //        }
        //    }
        //}
    }
}
