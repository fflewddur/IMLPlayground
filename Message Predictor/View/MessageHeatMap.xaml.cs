using MessagePredictor.Model;
using MessagePredictor.ViewModel;
using System;
using System.Collections.Generic;
using System.Globalization;
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

            this.DataContextChanged += MessageHeatMap_DataContextChanged;
        }

        void MessageHeatMap_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            HeatMapViewModel vm = this.DataContext as HeatMapViewModel;
            if (vm != null) {
                vm.HighlightTextChanged += vm_HighlightTextChanged;
            }
        }

        private void vm_HighlightTextChanged(object sender, HeatMapViewModel.HighlightTextChangedEventArgs e)
        {
            ScrollToTop();
        }

        private void ScrollToTop()
        {
            HeatMap.ScrollIntoView(HeatMap.Items[0]);
        }
    }
}
