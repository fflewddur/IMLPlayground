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
    /// Interaction logic for ClassImbalanceExplanation.xaml
    /// </summary>
    public partial class ClassImbalanceGraph : UserControl
    {
        public ClassImbalanceGraph()
        {
            InitializeComponent();
        }

        public Evidence Evidence
        {
            get { return (Evidence)GetValue(EvidenceProperty); }
            set { SetValue(EvidenceProperty, value); }
        }

        public static readonly DependencyProperty EvidenceProperty =
            DependencyProperty.Register("Evidence", typeof(Evidence), typeof(ClassImbalanceGraph));

        public Brush GraphColor
        {
            get { return (Brush)GetValue(GraphColorProperty); }
            set { SetValue(GraphColorProperty, value); }
        }

        public static readonly DependencyProperty GraphColorProperty =
            DependencyProperty.Register("GraphColor", typeof(Brush), typeof(ClassImbalanceGraph));

        public LibIML.Label Label
        {
            get { return (LibIML.Label)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(LibIML.Label), typeof(ClassImbalanceGraph));
    }
}
