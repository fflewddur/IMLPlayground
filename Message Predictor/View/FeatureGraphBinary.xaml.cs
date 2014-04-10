using LibIML;
using LibIML.Features;
using MessagePredictor.Model;
using MessagePredictor.ViewModel;
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
    /// Interaction logic for FeatureGraphBinary.xaml
    /// </summary>
    public partial class FeatureGraphBinary : UserControl
    {
        private double _mouseOrigY;
        private bool _currentFeatureAdjusted;
        private Feature _currentFeature;
        private FeatureImportance _currentFeatureImportance;
        private UserAction _currentAction;

        #region Dependency properties

        //[BindableAttribute(true)]
        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemsSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(FeatureGraphBinary));

        public Brush GraphColor1
        {
            get { return (Brush)GetValue(GraphColor1Property); }
            set { SetValue(GraphColor1Property, value); }
        }

        public static readonly DependencyProperty GraphColor1Property =
            DependencyProperty.Register("GraphColor1", typeof(Brush), typeof(FeatureGraphBinary));

        public Brush GraphColor2
        {
            get { return (Brush)GetValue(GraphColor2Property); }
            set { SetValue(GraphColor2Property, value); }
        }

        public static readonly DependencyProperty GraphColor2Property =
            DependencyProperty.Register("GraphColor2", typeof(Brush), typeof(FeatureGraphBinary));

        #endregion

        public FeatureGraphBinary()
        {
            InitializeComponent();
        }

        #region Callbacks

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
                vm.LogFeatureGraphScrolled(e.HorizontalChange, e.HorizontalOffset);
            }
        }

        private void Rectangle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement fe = sender as FrameworkElement;
            FrameworkElement parent = fe.Parent as FrameworkElement;
            FrameworkElement grandParent = parent.Parent as FrameworkElement;
            FeatureImportance fi = fe.DataContext as FeatureImportance;
            Feature f = grandParent.DataContext as Feature;
            
            StartAdjustingFeature(e.GetPosition(this).Y, f, fi);
        }

        private void Rectangle_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            StopAdjustingFeature();

            // Also select this feature
            FeatureSetViewModel vm = this.DataContext as FeatureSetViewModel;
            FrameworkElement fe = sender as FrameworkElement;

            if (vm != null && fe != null) {
                Feature f = fe.DataContext as Feature;
                vm.HighlightFeature.Execute(f.Characters);
                vm.SelectedFeature = f;
            }
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            StopAdjustingFeature();
        }

        private void StartAdjustingFeature(double mousePositionY, Feature f, FeatureImportance fi)
        {
            _mouseOrigY = mousePositionY;
            _currentFeatureAdjusted = false;
            _currentFeatureImportance = fi;
            _currentFeature = f;
            _currentAction = new UserAction(UserAction.ActionType.AdjustFeaturePrior, f, fi.Label);
            Mouse.OverrideCursor = Cursors.SizeNS;
            FeatureSetViewModel vm = this.DataContext as FeatureSetViewModel;
            vm.LogFeatureAdjustBegin(f, fi);
        }

        private void StopAdjustingFeature()
        {
            if (_currentFeatureImportance != null) {
                FeatureSetViewModel vm = this.DataContext as FeatureSetViewModel;
                vm.LogFeatureAdjustEnd(_currentFeature, _currentFeatureImportance);
                if (_currentFeatureAdjusted) {
                    // If the feature was adjusted, add it to our list of undo-able actions
                    vm.AddUserAction(_currentAction);
                }
                _mouseOrigY = -1;
                _currentFeatureImportance = null;
                _currentFeature = null;
                _currentFeatureAdjusted = false;
                _currentAction = null;
                vm.ApplyFeatureAdjustments.Execute(null);
                Mouse.OverrideCursor = null;
            }
        }

        private void Rectangle_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _currentFeatureImportance != null && _mouseOrigY >= 0) {
                FeatureSetViewModel vm = this.DataContext as FeatureSetViewModel;
                double y = e.GetPosition(this).Y;
                double delta = _mouseOrigY - y;
                if (delta != 0) {
                    _currentFeatureAdjusted = true;
                }
                vm.AdjustUserFeatureHeight(_currentFeatureImportance, delta);
                _mouseOrigY = y;

                //Console.WriteLine("Dragging {0}", delta);
            }
        }

        #endregion
    }
}
