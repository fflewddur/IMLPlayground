using LibIML;
using MessagePredictor.ViewModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for FeatureGraph.xaml
    /// </summary>
    public partial class FeatureGraph : UserControl
    {
        private double _mouseOrigY;
        private Feature _currentFeature;
        private bool _editingUnusedWeight;

        public FeatureGraph()
        {
            InitializeComponent();
            _editingUnusedWeight = false;
            UnusedWeight = Feature.MINIMUM_HEIGHT;
        }

        //[BindableAttribute(true)]
        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemsSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(FeatureGraph));

        public double UnusedWeight
        {
            get { return (double)GetValue(UnusedWeightProperty); }
            set { SetValue(UnusedWeightProperty, value); }
        }

        public static readonly DependencyProperty UnusedWeightProperty =
            DependencyProperty.Register("UnusedWeight", typeof(double), typeof(FeatureGraph));

        
        private void Rectangle_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _currentFeature != null && _mouseOrigY >= 0) {
                FeatureSetViewModel vm = this.DataContext as FeatureSetViewModel;
                double y = e.GetPosition(this).Y;
                double delta = _mouseOrigY - y;
                // FIXME We should probably base our PIXELS_TO_WEIGHT value on the height of the graph
                bool apply = (UnusedWeight == Feature.MINIMUM_HEIGHT);
                if (delta > (UnusedWeight - Feature.MINIMUM_HEIGHT)) {
                    delta = (UnusedWeight - Feature.MINIMUM_HEIGHT);
                }
                UnusedWeight -= vm.AdjustUserFeatureHeight(_currentFeature, delta, apply);
                _mouseOrigY = y;
                
                //Console.WriteLine("Dragging {0}", delta);
            }
        }

        private void Rectangle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _mouseOrigY = e.GetPosition(this).Y;
            FrameworkElement fe = sender as FrameworkElement;
            Feature feature = fe.DataContext as Feature;
            _currentFeature = feature;
            Mouse.OverrideCursor = Cursors.SizeNS;
        }

        private void Rectangle_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _mouseOrigY = -1;
            _currentFeature = null;
            Mouse.OverrideCursor = null;
        }

        private void UnusedWeight_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _editingUnusedWeight && _mouseOrigY >= 0) {
                FeatureSetViewModel vm = this.DataContext as FeatureSetViewModel;
                double y = e.GetPosition(this).Y;
                double delta = _mouseOrigY - y;
                // FIXME We should probably base our PIXELS_TO_WEIGHT value on the height of the graph
                if (UnusedWeight + delta < Feature.MINIMUM_HEIGHT) {
                    UnusedWeight = Feature.MINIMUM_HEIGHT;
                } else {
                    UnusedWeight += delta;
                }
                _mouseOrigY = y;
                
                //Console.WriteLine("Dragging {0}", delta);
            }
        }

        private void UnusedWeight_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _mouseOrigY = e.GetPosition(this).Y;
            FrameworkElement fe = sender as FrameworkElement;
            _editingUnusedWeight = true;
            Mouse.OverrideCursor = Cursors.SizeNS;
        }

        private void UnusedWeight_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _mouseOrigY = -1;
            _editingUnusedWeight = false;
            Mouse.OverrideCursor = null;
        }
    }
}
