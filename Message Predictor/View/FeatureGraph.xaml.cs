using LibIML;
using LibIML.Features;
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
        //private bool _editingUnusedWeight;

        public FeatureGraph()
        {
            InitializeComponent();
            //_editingUnusedWeight = false;
            //UnusedWeight = Feature.MINIMUM_HEIGHT;
            GraphColorSystem = Brushes.Red; // Be obvious that we're using default values
            GraphColorUser = Brushes.Pink;
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

        public Brush GraphColorUser
        {
            get { return (Brush)GetValue(GraphColorUserProperty); }
            set { SetValue(GraphColorUserProperty, value); }
        }

        // Using a DependencyProperty as the backing store for GraphColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GraphColorUserProperty =
            DependencyProperty.Register("GraphColorUser", typeof(Brush), typeof(FeatureGraph));

        public Brush GraphColorSystem
        {
            get { return (Brush)GetValue(GraphColorSystemProperty); }
            set { SetValue(GraphColorSystemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for GraphColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GraphColorSystemProperty =
            DependencyProperty.Register("GraphColorSystem", typeof(Brush), typeof(FeatureGraph));

        public LibIML.Label Label
        {
            get { return (LibIML.Label)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Label.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(LibIML.Label), typeof(FeatureGraph));

        
        private void Rectangle_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _currentFeature != null && _mouseOrigY >= 0) {
                FeatureSetViewModel vm = this.DataContext as FeatureSetViewModel;
                double y = e.GetPosition(this).Y;
                double delta = _mouseOrigY - y;
                //vm.AdjustUserFeatureHeight(_currentFeature, delta);
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
            FeatureSetViewModel vm = this.DataContext as FeatureSetViewModel;
            vm.LogFeatureAdjustBegin(feature);
        }

        private void Rectangle_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            StopAdjustingFeature();
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            StopAdjustingFeature();
        }

        private void StopAdjustingFeature()
        {
            if (_currentFeature != null) {
                FeatureSetViewModel vm = this.DataContext as FeatureSetViewModel;
                vm.LogFeatureAdjustEnd(_currentFeature);
                _mouseOrigY = -1;
                _currentFeature = null;
                Mouse.OverrideCursor = null;
            }
        }

        //private void UnusedWeight_PreviewMouseMove(object sender, MouseEventArgs e)
        //{
        //    if (e.LeftButton == MouseButtonState.Pressed && _editingUnusedWeight && _mouseOrigY >= 0) {
        //        FeatureSetViewModel vm = this.DataContext as FeatureSetViewModel;
        //        double y = e.GetPosition(this).Y;
        //        double delta = _mouseOrigY - y;
        //        // FIXME We should probably base our PIXELS_TO_WEIGHT value on the height of the graph
        //        if (UnusedWeight + delta < Feature.MINIMUM_HEIGHT) {
        //            UnusedWeight = Feature.MINIMUM_HEIGHT;
        //        }
        //        else {
        //            UnusedWeight += delta;
        //        }
        //        _mouseOrigY = y;

        //        //Console.WriteLine("Dragging {0}", delta);
        //    }
        //}

        //private void UnusedWeight_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    _mouseOrigY = e.GetPosition(this).Y;
        //    FrameworkElement fe = sender as FrameworkElement;
        //    _editingUnusedWeight = true;
        //    Mouse.OverrideCursor = Cursors.SizeNS;
        //}

        //private void UnusedWeight_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        //{
        //    _mouseOrigY = -1;
        //    _editingUnusedWeight = false;
        //    Mouse.OverrideCursor = null;
        //}

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            FeatureSetViewModel vm = this.DataContext as FeatureSetViewModel;
            FrameworkElement fe = sender as FrameworkElement;

            if (vm != null && fe != null) {
                Feature f = fe.DataContext as Feature;
                vm.FeatureRemove.Execute(f);
            }
        }

        private void Find_Click(object sender, RoutedEventArgs e)
        {
            FeatureSetViewModel vm = this.DataContext as FeatureSetViewModel;
            FrameworkElement fe = sender as FrameworkElement;

            if (vm != null && fe != null) {
                Feature f = fe.DataContext as Feature;
                vm.HighlightFeature.Execute(f.Characters);
            }
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            FeatureSetViewModel vm = this.DataContext as FeatureSetViewModel;
            if (vm != null && e.HorizontalChange != 0) {
                //vm.LogFeatureGraphScrolled(Label.ToString(), e.HorizontalChange, e.HorizontalOffset);
            }
        }
    }
}
