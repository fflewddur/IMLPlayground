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
        private DispatcherTimer _featureWeightEditedTimer;

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
            _featureWeightEditedTimer = new DispatcherTimer();

            HighlightFeature = new RelayCommand<string>(PerformHighlightFeature);
            AddFeatureViaSelection = new RelayCommand<Feature>(PerformAddFeatureViaSelection);
            AddFeature = new RelayCommand<Label>(PerformAddFeature);
            FeatureRemove = new RelayCommand<Feature>(PerformRemoveFeature);
            FeatureVeryImportant = new RelayCommand<Feature>(PerformFeatureVeryImportant);
            FeatureSomewhatImportant = new RelayCommand<Feature>(PerformFeatureSomewhatImportant);

            _classifier.Retrained += classifier_Retrained;
            _vocab.Updated += vocab_Updated;
            _featureTextEditedTimer.Interval = new TimeSpan(2000000); // 200 milliseconds
            _featureTextEditedTimer.Tick += _featureTextEditedTimer_Tick;
            _featureWeightEditedTimer.Interval = new TimeSpan(2000000); // 200 milliseconds
            _featureWeightEditedTimer.Tick += _featureWeightEditedTimer_Tick;

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

        #endregion

        #region Commands

        public RelayCommand<string> HighlightFeature { get; private set; }
        public RelayCommand<Feature> AddFeatureViaSelection { get; private set; }
        public RelayCommand<Label> AddFeature { get; private set; }
        public RelayCommand<Feature> FeatureRemove { get; private set; }
        public RelayCommand<Feature> FeatureVeryImportant { get; private set; }
        public RelayCommand<Feature> FeatureSomewhatImportant { get; private set; }

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

        public class FeatureWeightEditedEventArgs : EventArgs
        {
            public readonly Feature Feature;
            public FeatureWeightEditedEventArgs(Feature feature)
            {
                Feature = feature;
            }
        }

        public event EventHandler<FeatureAddedEventArgs> FeatureAdded;
        public event EventHandler<EventArgs> FeatureRemoved;
        public event EventHandler<FeatureTextEditedEventArgs> FeatureTextEdited;
        public event EventHandler<FeatureWeightEditedEventArgs> FeatureWeightEdited;

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

        protected virtual void OnFeatureWeightEdited(FeatureWeightEditedEventArgs e)
        {
            if (FeatureWeightEdited != null)
                FeatureWeightEdited(this, e);
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
            // If the user previously added this feature manually, remove it from our list
            _userAdded.Remove(feature);
            // Ensure we don't add this feature to the list multiple times
            if (!_userRemoved.Contains(feature))
                _userRemoved.Add(feature);

            // Update the UI right away, even if we don't retrain
            FeatureSet.Remove(feature);

            OnFeatureRemoved(new EventArgs());
        }

        #endregion

        #region Private methods

        private void FeatureSet_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null)
                return;

            foreach (Feature f in e.NewItems) {
                Console.WriteLine("new feature: {0} {1}", f, e.Action);
            }
        }

        private void _featureTextEditedTimer_Tick(object sender, EventArgs e)
        {
            _featureTextEditedTimer.Stop();
            OnFeatureTextEdited(new FeatureTextEditedEventArgs(_featureText));
        }

        private void _featureWeightEditedTimer_Tick(object sender, EventArgs e)
        {
            _featureWeightEditedTimer.Stop();
            // FIXME do we need to pass out the specific feature that changed? Might be enough just to let the main VM
            // know that it needs to update the priors and re-predict.
            OnFeatureWeightEdited(new FeatureWeightEditedEventArgs(null)); 
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

        private void UpdateFeatures()
        {
            Console.WriteLine("UpdateFeatures()");
            FeatureSet.Clear();
            foreach (int id in _vocab.FeatureIds) {
                string word = _vocab.GetWord(id);
                Feature userFeature = _userAdded.Find(p => p.Characters == word);
                // First, see if the user added this feature manually; if so, keep associating it with the label the user requested
                if (userFeature != null) {
                    FeatureSet.Add(userFeature);
                } else {
                    // Otherwise, figure out which label this feature is more important for
                    foreach (Label label in _labels) {
                        Feature f = new Feature(word, label);
                        f.SystemWeight = _classifier.GetFeatureSystemWeight(id, label);
                        f.UserWeight = _classifier.GetFeatureUserWeight(id, label);
                        f.MostImportant = _classifier.IsFeatureMostImportantForLabel(id, label);
                        FeatureSet.Add(f);
                    }
                }
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
