using MessagePredictor.Model;
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
    /// Interaction logic for MessageHeatMap.xaml
    /// </summary>
    public partial class MessageHeatMap : UserControl
    {
        public MessageHeatMap()
        {
            InitializeComponent();
        }

        private void Message_Selected(object sender, MouseEventArgs e)
        {
            HeatMapViewModel vm = this.DataContext as HeatMapViewModel;
            FrameworkElement element = sender as FrameworkElement;
            NewsItem item = null;
            if (element != null)
                item = element.DataContext as NewsItem;

            if (vm != null && item != null)
            {
                vm.CurrentMessage = item;
            }
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            NewsItem item = null;
            if (element != null)
                item = element.DataContext as NewsItem;

            if (item != null)
            {
                item.IsMouseOver = true;
            }
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            NewsItem item = null;
            if (element != null)
                item = element.DataContext as NewsItem;

            if (item != null)
            {
                item.IsMouseOver = false;
            }
        }
    }
}
