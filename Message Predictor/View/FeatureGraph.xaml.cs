using LibIML;
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

        public FeatureGraph()
        {
            InitializeComponent();
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
        }
        [BindableAttribute(true)]
        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemsSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(FeatureGraph), new PropertyMetadata(OnItemsSourceChanged));

        public static void OnItemsSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            (obj as FeatureGraph).OnItemsSourceChanged(args);
        }

        private void OnItemsSourceChanged(DependencyPropertyChangedEventArgs args)
        {
            return;
        }

        private void Rectangle_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _currentFeature != null && _mouseOrigY >= 0) {
                double y = e.GetPosition(this).Y;
                double delta = _mouseOrigY - y;
                _currentFeature.UserHeight += delta;
                _mouseOrigY = y;
                
                Console.WriteLine("Dragging {0}", delta);
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
    }
}
