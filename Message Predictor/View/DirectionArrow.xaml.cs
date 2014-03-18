using MessagePredictor.Model;
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
    /// Interaction logic for DirectionArrow.xaml
    /// </summary>
    public partial class DirectionArrow : UserControl
    {
        public DirectionArrow()
        {
            InitializeComponent();
        }

        public string UnitOfMeasure
        {
            get { return (string)GetValue(UnitOfMeasureProperty); }
            set { SetValue(UnitOfMeasureProperty, value); }
        }

        public static readonly DependencyProperty UnitOfMeasureProperty =
            DependencyProperty.Register("UnitOfMeasure", typeof(string), typeof(DirectionArrow));

        public string Difference
        {
            get { return (string)GetValue(DifferenceProperty); }
            set { SetValue(DifferenceProperty, value); }
        }

        public static void OnDifferenceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            (obj as DirectionArrow).OnDifferenceChanged(args);
        }

        private void OnDifferenceChanged(DependencyPropertyChangedEventArgs args)
        {
            UpdateTooltip(Direction, Difference, UnitOfMeasure);
        }

        public static readonly DependencyProperty DifferenceProperty =
            DependencyProperty.Register("Difference", typeof(string), typeof(DirectionArrow), new PropertyMetadata(OnDifferenceChanged));

        public Direction Direction
        {
            get { return (Direction)GetValue(DirectionProperty); }
            set { SetValue(DirectionProperty, value); }
        }

        public static void OnDirectionChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            (obj as DirectionArrow).OnDirectionChanged(args);
        }

        private void OnDirectionChanged(DependencyPropertyChangedEventArgs args)
        {
            UpdateTooltip(Direction, Difference, UnitOfMeasure);
        }

        public static readonly DependencyProperty DirectionProperty =
            DependencyProperty.Register("Direction", typeof(Direction), typeof(DirectionArrow), new PropertyMetadata(OnDirectionChanged));

        /// <summary>
        /// When the direction or difference changes, update our tooltip appropriately.
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="difference"></param>
        private void UpdateTooltip(Direction direction, string difference, string unitOfMeasure)
        {
            if (direction == Direction.Down) {
                Arrow.ToolTip = string.Format("{0} decreased by {1}", unitOfMeasure, difference);
            } else if (direction == Direction.Up) {
                Arrow.ToolTip = string.Format("{0} increased by {1}", unitOfMeasure, difference);
            } else {
                Arrow.ToolTip = null;
            }
        }
    }
}
