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

namespace MessagePredictor
{
    /// <summary>
    /// Interaction logic for FolderList.xaml
    /// </summary>
    public partial class FolderList : UserControl
    {
        public FolderList()
        {
            InitializeComponent();
        }

        //private void Folder_Drop(object sender, DragEventArgs e)
        //{
        //    Console.WriteLine("Folder_Drop()");

        //    if (e.Data.GetDataPresent(typeof(NewsItem)))
        //    {
        //        NewsItem item = e.Data.GetData(typeof(NewsItem)) as NewsItem;
        //        FrameworkElement element = sender as FrameworkElement;
        //        NewsCollection collection = element.DataContext as NewsCollection;
        //        MessagePredictorViewModel mpvm = this.DataContext as MessagePredictorViewModel;
        //        mpvm.MoveMessageToFolder(item, collection);
        //    }
        //}

        //private void Folder_CheckDropTarget(object sender, DragEventArgs e)
        //{
        //    Console.WriteLine("CHeckDropTarget()");
        //    NewsItem item = sender as NewsItem;
        //    if (item == null)
        //    {
        //        e.Effects = DragDropEffects.None;
        //    }
        //}
    }
}
