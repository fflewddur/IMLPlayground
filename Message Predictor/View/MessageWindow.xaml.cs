using MessagePredictor.ViewModel;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace MessagePredictor.View
{
    /// <summary>
    /// Interaction logic for MessageWindow.xaml
    /// </summary>
    public partial class MessageWindow : Window
    {
        MyFormatter _textFormatter;
        MessagePredictorViewModel _vm;

        public MessageWindow(MessagePredictorViewModel vm)
        {
            InitializeComponent();

            //MessagePredictorViewModel vm = this.DataContext as MessagePredictorViewModel;
            _vm = vm;
            _textFormatter = new MyFormatter(_vm);
            RTB.TextFormatter = _textFormatter;
        }

        private void Goto_Click(object sender, RoutedEventArgs e)
        {
            HeatMapViewModel hmvm = this.DataContext as HeatMapViewModel;
            _vm.SelectMessage(hmvm.CurrentMessage);
            this.Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
