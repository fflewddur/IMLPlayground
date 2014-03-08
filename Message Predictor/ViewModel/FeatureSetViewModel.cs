using GalaSoft.MvvmLight.Command;
using LibIML;
using MessagePredictor.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;

namespace MessagePredictor.ViewModel
{
    class FeatureSetViewModel : ViewModelBase
    {
        public static readonly double PERCENT_HEIGHT_OF_MAX_BAR = 0.5;

        private IClassifier _classifier;
        private Vocabulary _vocab;
        private IReadOnlyList<Label> _labels;
        private List<Feature> _userAdded;
        private List<Feature> _userRemoved;
        private ObservableCollection<Feature> _featureSet;
        private IReadOnlyList<CollectionViewSource> _collectionViewSourcesOverview;
        private IReadOnlyList<CollectionViewSource> _collectionViewSourcesGraph;
        private string _featureText; // The feature the user is currently typing in
        private DispatcherTimer _featureTextEditedTimer;
        private DispatcherTimer _featurePriorsEditedTimer;
        private DispatcherTimer _featureGraphHeightChangedTimer;
        private double _featureGraphHeight;
        private double _pixelsToWeight; // How many pixels to use to display each unit of feature weight (changes based on display size)

        public FeatureSetViewModel(IClassifier classifier, Vocabulary vocab, IReadOnlyList<Label> labels)
            : base()
        {
            _classifier = classifier;
            _vocab = vocab;
            _labels = labels;
            _featureSet = new ObservableCollection<Feature>();
            _userAdded = new List<Feature>();
            _userRemoved = new List<Feature>();
            _collectionViewSourcesOverview = BuildCollectionViewSourcesOverview(labels);
            _collectionViewSourcesGraph = BuildCollectionViewSourcesGraph(labels);
            _featureText = null;
            _featureTextEditedTimer = new DispatcherTimer();
            _featurePriorsEditedTimer = new DispatcherTimer();
            _featureGraphHeightChangedTimer = new DispatcherTimer();

            HighlightFeature = new RelayCommand<string>(PerformHighlightFeature);
            AddFeatureViaSelection = new RelayCommand<Feature>(PerformAddFeatureViaSelection);
            AddFeature = new RelayCommand<Label>(PerformAddFeature);
            FeatureRemove = new RelayCommand<Feature>(PerformRemoveFeature);
            FeatureVeryImportant = new RelayCommand<Feature>(PerformFeatureVeryImportant);
            FeatureSomewhatImportant = new RelayCommand<Feature>(PerformFeatureSomewhatImportant);
            ApplyFeatureAdjustments = new RelayCommand(PerformApplyFeatureAdjustments);

            _classifier.Retrained += classifier_Retrained;
            _vocab.Updated += vocab_Updated;
            _featureTextEditedTimer.Interval = new TimeSpan(2000000); // 200 milliseconds
            _featureTextEditedTimer.Tick += _featureTextEditedTimer_Tick;
            _featurePriorsEditedTimer.Interval = new TimeSpan(2000000); // 200 milliseconds
            _featurePriorsEditedTimer.Tick += _featurePriorsEditedTimer_Tick;
            _featureGraphHeightChangedTimer.Interval = new TimeSpan(2000000); // 200 milliseconds
            _featureGraphHeightChangedTimer.Tick += _featureGraphHeightChangedTimer_Tick;
            FeatureSet.CollectionChanged += FeatureSet_CollectionChanged;
        }

        #region Properties

        public IReadOnlyList<Label> Labels
        {
            get { return _labels; }
        }

        public IReadOnlyList<Feature> UserAddedFeatures
        {
            get { return _userAdded; }
        }

        public IReadOnlyList<Feature> UserRemovedFeatures
        {
            get { return _userRemoved; }
        }

        public ObservableCollection<Feature> FeatureSet
        {
            get { return _featureSet; }
            set { SetProperty<ObservableCollection<Feature>>(ref _featureSet, value); }
        }

        public IReadOnlyList<CollectionViewSource> FeatureSetViewSourcesOverview
        {
            get { return _collectionViewSourcesOverview; }
            private set { SetProperty<IReadOnlyList<CollectionViewSource>>(ref _collectionViewSourcesOverview, value); }
        }

        public IReadOnlyList<CollectionViewSource> FeatureSetViewSourcesGraph
        {
            get { return _collectionViewSourcesGraph; }
            private set { SetProperty<IReadOnlyList<CollectionViewSource>>(ref _collectionViewSourcesGraph, value); }
        }

        public double FeatureGraphHeight
        {
            get { return _featureGraphHeight; }
            set
            {
                if (SetProperty<double>(ref _featureGraphHeight, value)) {
                    // Wait until this stops changing before updating the layout
                    _featureGraphHeightChangedTimer.Stop();
                    _featureGraphHeightChangedTimer.Start();
                }
            }
        }

        #endregion

        #region Commands

        public RelayCommand<string> HighlightFeature { get; private set; }
        public RelayCommand<Feature> AddFeatureViaSelection { get; private set; }
        public RelayCommand<Label> AddFeature { get; private set; }
        public RelayCommand<Feature> FeatureRemove { get; private set; }
        public RelayCommand<Feature> FeatureVeryImportant { get; private set; }
        public RelayCommand<Feature> FeatureSomewhatImportant { get; private set; }
        public RelayCommand ApplyFeatureAdjustments { get; private set; }

        #endregion

        #region Events

        public class FeatureAddedEventArgs : EventArgs
        {
            public readonly Feature Feature;
            public FeatureAddedEventArgs(Feature feature)
            {
                Feature = feature;
            }
        }

        public class FeatureTextEditedEventArgs : EventArgs
        {
            public readonly string Tokens;
            public FeatureTextEditedEventArgs(string tokens)
            {
                Tokens = tokens;
            }
        }

        public class FeaturePriorEditedEventArgs : EventArgs
        {
            public readonly Feature Feature;
            public FeaturePriorEditedEventArgs(Feature feature)
            {
                Feature = feature;
            }
        }

        public event EventHandler<FeatureAddedEventArgs> FeatureAdded;
        public event EventHandler<EventArgs> FeatureRemoved;
        public event EventHandler<FeatureTextEditedEventArgs> FeatureTextEdited;
        public event EventHandler<FeaturePriorEditedEventArgs> FeaturePriorEdited;

        protected virtual void OnFeatureAdded(FeatureAddedEventArgs e)
        {
            if (FeatureAdded != null)
                FeatureAdded(this, e);
        }

        protected virtual void OnFeatureRemoved(EventArgs e)
        {
            if (FeatureRemoved != null)
                FeatureRemoved(this, e);
        }

        protected virtual void OnFeatureTextEdited(FeatureTextEditedEventArgs e)
        {
            if (FeatureTextEdited != null)
                FeatureTextEdited(this, e);
        }

        protected virtual void OnFeaturePriorEdited(FeaturePriorEditedEventArgs e)
        {
            if (FeaturePriorEdited != null)
                FeaturePriorEdited(this, e);
        }

        #endregion

        #region Public methods

        public void AddUserFeature(Feature feature)
        {
            // If the user previously removed this feature, clear it from the list of removed features
            _userRemoved.Remove(feature);
            // If the user already added this feature, overwrite the previous version
            _userAdded.Remove(feature);
            _userAdded.Add(feature);

            // Update the UI right away, even if we don't retrain
            FeatureSet.Add(feature);

            OnFeatureAdded(new FeatureAddedEventArgs(feature));
        }

        public void RemoveUserFeature(Feature feature)
        {
            // Create a feature with the same word, but that will match any label
            Feature anyLabel = new Feature(feature.Characters, Label.AnyLabel);

            // If the user previously added this feature manually, remove it from our list
            while (_userAdded.Remove(anyLabel)) ;

            // Ensure we don't add this feature to the list multiple times
            if (!_userRemoved.Contains(anyLabel))
                _userRemoved.Add(anyLabel);

            // Update the UI right away, even if we don't retrain
            while (FeatureSet.Remove(anyLabel)) ;

            OnFeatureRemoved(new EventArgs());
        }

        public double AdjustUserFeatureHeight(Feature feature, double heightDelta, bool apply)
        {
            if (feature.UserHeight + heightDelta < Feature.MINIMUM_HEIGHT) {
                heightDelta = -1 * (feature.UserHeight - Feature.MINIMUM_HEIGHT);
            }
            feature.UserHeight += heightDelta;

            return heightDelta;
        }

        #endregion

        #region Private methods

        private void FeatureSet_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null)
                return;

            //foreach (Feature f in e.NewItems) {
            //    Console.WriteLine("new feature: {0} {1}", f, e.Action);
            //}
        }

        private void _featureTextEditedTimer_Tick(object sender, EventArgs e)
        {
            _featureTextEditedTimer.Stop();
            OnFeatureTextEdited(new FeatureTextEditedEventArgs(_featureText));
        }

        private void _featurePriorsEditedTimer_Tick(object sender, EventArgs e)
        {
            _featurePriorsEditedTimer.Stop();
            // FIXME do we need to pass out the specific feature that changed? Might be enough just to let the main VM
            // know that it needs to update the priors and re-predict.
            OnFeaturePriorEdited(new FeaturePriorEditedEventArgs(null));
        }

        private void _featureGraphHeightChangedTimer_Tick(object sender, EventArgs e)
        {
            _featureGraphHeightChangedTimer.Stop();
            UpdateFeatureHeights();
        }

        private void classifier_Retrained(object sender, EventArgs e)
        {
            // The system's feature weights may have changed, we need to update the display
            UpdateFeatures();
        }

        private void vocab_Updated(object sender, EventArgs e)
        {
            // The system's vocab may have changed, we need to show the new vocab items
            UpdateFeatures();
        }

        private void UpdateFeatureHeights()
        {
            // Computer our new pixelsToWeight ratio
            double maxWeight = 0;
            foreach (Feature f in FeatureSet) {
                double weight = f.SystemWeight + f.UserWeight;
                if (maxWeight < weight) {
                    maxWeight = weight;
                }
            }
            // Subtract ~50px because of the tabbed item headers
            _pixelsToWeight = (PERCENT_HEIGHT_OF_MAX_BAR * (_featureGraphHeight - 50)) / maxWeight;

            // Update the PixelsToWeight value for each Feature
            foreach (Feature f in FeatureSet) {
                f.PixelsToWeight = _pixelsToWeight;
            }
        }

        private void UpdateFeatures()
        {
            Console.WriteLine("UpdateFeatures()");
            List<Feature> toRemove = new List<Feature>();

            // Add the features we don't already have in our set
            foreach (int id in _vocab.FeatureIds) {
                string word = _vocab.GetWord(id);
                Feature toFind = new Feature(word, Label.AnyLabel);
                if (!FeatureSet.Contains(toFind)) {
                    // These will be system-determined features, so be sure to add them to both labels.
                    // Also include the default user weight and prior.
                    foreach (Label label in _labels) {
                        Feature f = new Feature(word, label);
                        f.PixelsToWeight = _pixelsToWeight;
                        double prior, weight;
                        _classifier.TryGetFeatureUserPrior(id, label, out prior);
                        f.UserPrior = prior;
                        _classifier.TryGetFeatureUserWeight(id, label, out weight);
                        f.UserWeight = weight;

                        FeatureSet.Add(f);
                    }
                }
            }

            // Go through each feature and update it with the system's current weight, or mark it for removal
            foreach (Feature f in FeatureSet) {
                int id = _vocab.GetWordId(f.Characters, true);

                // Figure out if we need to remove this feature
                if (id < 0) {
                    toRemove.Add(f);
                }

                // Update this feature's system-determined weight
                double weight;
                if (_classifier.TryGetFeatureSystemWeight(id, f.Label, out weight)) {
                    f.SystemWeight = weight;
                }
                if (_classifier.TryGetFeatureUserWeight(id, f.Label, out weight)) {
                    f.UserWeight = weight;
                }

                // Figure out which label this feature is most important for
                f.MostImportant = _classifier.IsFeatureMostImportantForLabel(id, f.Label);
            }

            // Remove anything marked for removal
            foreach (Feature f in toRemove) {
                FeatureSet.Remove(f);
            }

            UpdateFeatureHeights();
            // Refresh our source views
            foreach (CollectionViewSource cvs in _collectionViewSourcesOverview) {
                cvs.View.Refresh();
            }
        }

        private void PerformHighlightFeature(string text)
        {
            FeatureTextChanged(text);
        }

        private void PerformAddFeatureViaSelection(Feature feature)
        {

        }

        private void PerformAddFeature(Label label)
        {
            AddFeatureDialog dialog = new AddFeatureDialog();
            dialog.Owner = App.Current.MainWindow;
            AddFeatureDialogViewModel vm = new AddFeatureDialogViewModel(label);
            vm.Word = _featureText;
            vm.PropertyChanged += AddFeatureVM_PropertyChanged;
            dialog.DataContext = vm;
            bool? result = dialog.ShowDialog();
            if (result == true) {
                Console.WriteLine("add word: {0} with weight {1} to topic {2}", vm.Word, vm.SelectedWeight, vm.Label);
                Feature f = new Feature(vm.Word.ToLower(), label);
                f.PixelsToWeight = _pixelsToWeight;
                if (vm.SelectedWeight == vm.Weights[0])
                    f.WeightType = Feature.Weight.High;
                else if (vm.SelectedWeight == vm.Weights[1])
                    f.WeightType = Feature.Weight.Medium;
                f.MostImportant = true; // If the user added this to a given label, then it's always most important to that label.

                AddUserFeature(f);
            }

            // Let anyone watching know that the user has closed the dialog, so _featureText is now empty.
            _featureText = "";
            OnFeatureTextEdited(new FeatureTextEditedEventArgs(_featureText));
        }

        private void AddFeatureVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Word") {
                AddFeatureDialogViewModel vm = sender as AddFeatureDialogViewModel;
                if (vm != null)
                    FeatureTextChanged(vm.Word);
            }
        }

        private void FeatureTextChanged(string text)
        {
            _featureTextEditedTimer.Stop();
            _featureText = text;
            _featureTextEditedTimer.Start();
        }

        private void PerformRemoveFeature(Feature feature)
        {
            RemoveUserFeature(feature);
        }

        private void PerformFeatureVeryImportant(Feature feature)
        {
            Console.WriteLine("PerformFeatureVeryImportant on {0}", feature);
        }

        private void PerformFeatureSomewhatImportant(Feature feature)
        {
            Console.WriteLine("PerformFeatureSomewhatImportant on {0}", feature);
        }

        private void PerformApplyFeatureAdjustments()
        {
            Console.WriteLine("Apply feature adjustments");

            foreach (Label label in Labels) {
                UpdateFeaturePriors(label);
            }
        }

        private void UpdateFeaturePriors(Label label)
        {
            // Don't fire a featureWeightEdited event while we're updating these values
            _featurePriorsEditedTimer.Stop();

            Console.WriteLine("UpdateFeaturePriors for {0}", label);

            IEnumerable<Feature> labelFeatures = _featureSet.Where(f => f.Label == label);

            // Get the sum of user heights for each feature
            double userHeightSum = 0;
            foreach (Feature f in labelFeatures) {
                userHeightSum += f.UserHeight;
            }

            // Get the sum of system heights for each feature
            double systemHeightSum = 0;
            foreach (Feature f in labelFeatures) {
                systemHeightSum += f.SystemHeight;
            }

            // Get the sum of counts for each feature
            double systemFeatureCountSum = 0;
            _classifier.TryGetSystemFeatureSum(label, out systemFeatureCountSum);

            // Get the ratio of area of user importance vs. area of system importance
            // (This tells us what the sum of user priors should be, relative to the sum of feature counts)
            double userToSystemRatio = userHeightSum / systemHeightSum;
            double desiredPriorSum = userToSystemRatio * systemFeatureCountSum;

            // Get the percentage of the total prior value that should assigned to each feature
            Console.WriteLine("userHeightSum: {0} systemHeightSum: {1} userToSystemRatio: {2} systemFeatureCountSum: {3}", userHeightSum, systemHeightSum, userToSystemRatio, systemFeatureCountSum);
            foreach (Feature f in labelFeatures) {
                Console.Write("Old prior for {0} = {1}, ", f.Characters, f.UserPrior);
                f.UserPrior = (f.UserHeight / userHeightSum) * desiredPriorSum;
                Console.WriteLine("new prior = {0}", f.UserPrior);
            }

            _featurePriorsEditedTimer.Start();
        }

        private IReadOnlyList<CollectionViewSource> BuildCollectionViewSourcesOverview(IReadOnlyList<Label> labels)
        {
            List<CollectionViewSource> collectionViewSources = new List<CollectionViewSource>();
            foreach (Label label in labels) {
                CollectionViewSource cvs = new CollectionViewSource();
                cvs.Source = FeatureSet;
                cvs.Filter += (o, e) =>
                {
                    Feature f = e.Item as Feature;
                    if (f != null) {
                        // Only display features that are more important for this label than other labels
                        if (f.Label == label && f.MostImportant)
                            e.Accepted = true;
                        else
                            e.Accepted = false;
                    }
                };
                collectionViewSources.Add(cvs);
            }

            return collectionViewSources;
        }

        private IReadOnlyList<CollectionViewSource> BuildCollectionViewSourcesGraph(IReadOnlyList<Label> labels)
        {
            List<CollectionViewSource> collectionViewSources = new List<CollectionViewSource>();
            foreach (Label label in labels) {
                CollectionViewSource cvs = new CollectionViewSource();
                cvs.Source = FeatureSet;
                cvs.Filter += (o, e) =>
                {
                    Feature f = e.Item as Feature;
                    if (f != null) {
                        // Only display features that are more important for this label than other labels
                        if (f.Label == label)
                            e.Accepted = true;
                        else
                            e.Accepted = false;
                    }
                };
                collectionViewSources.Add(cvs);
            }

            return collectionViewSources;
        }

        #endregion
    }
}
