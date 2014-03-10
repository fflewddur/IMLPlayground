using LibIML;
using System;
using System.Collections;
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

        public static readonly DependencyProperty EvidenceProperty =
            DependencyProperty.Register("Evidence", typeof(Evidence), typeof(EvidenceGraph));

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(EvidenceGraph));

        public Brush GraphColorUser
        {
            get { return (Brush)GetValue(GraphColorUserProperty); }
            set { SetValue(GraphColorUserProperty, value); }
        }

        public static readonly DependencyProperty GraphColorUserProperty =
            DependencyProperty.Register("GraphColorUser", typeof(Brush), typeof(EvidenceGraph));

        public Brush GraphColorSystem
        {
            get { return (Brush)GetValue(GraphColorSystemProperty); }
            set { SetValue(GraphColorSystemProperty, value); }
        }

        public static readonly DependencyProperty GraphColorSystemProperty =
            DependencyProperty.Register("GraphColorSystem", typeof(Brush), typeof(EvidenceGraph));

        public LibIML.Label Label
        {
            get { return (LibIML.Label)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(LibIML.Label), typeof(EvidenceGraph));
    }
}
