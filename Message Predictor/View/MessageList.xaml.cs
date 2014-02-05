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
    /// Interaction logic for MessageList.xaml
    /// </summary>
    public partial class MessageList : UserControl
    {
        public MessageList()
        {
            InitializeComponent();
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
