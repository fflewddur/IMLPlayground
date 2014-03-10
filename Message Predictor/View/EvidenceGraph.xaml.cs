using LibIML;
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
    /// Interaction logic for EvidenceGraph.xaml
    /// </summary>
    public partial class EvidenceGraph : UserControl
    {
        public EvidenceGraph()
        {
            InitializeComponent();
        }

        public Evidence Evidence
        {
            get { return (Evidence)GetValue(EvidenceProperty); }
            set { SetValue(EvidenceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EvidenceProperty =
            DependencyProperty.Register("Evidence", typeof(Evidence), typeof(EvidenceGraph));

        
    }
}
