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
        private IReadOnlyList<CollectionViewSource> _collectionViewSources;

        public FeatureSetViewModel(IClassifier classifier, Vocabulary vocab, IReadOnlyList<Label> labels)
            : base()
        {
            _classifier = classifier;
            _vocab = vocab;
            _labels = labels;
            _featureSet = new ObservableCollection<Feature>();
            _userAdded = new List<Feature>();
            _userRemoved = new List<Feature>();
            _collectionViewSources = BuildCollectionViewSources(labels);

            AddFeature = new RelayCommand<Label>(PerformAddFeature);

            _classifier.Retrained += classifier_Retrained;
            _vocab.Updated += vocab_Updated;
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

        public IReadOnlyList<CollectionViewSource> FeatureSetViewSources
        {
            get { return _collectionViewSources; }
            private set { SetProperty<IReadOnlyList<CollectionViewSource>>(ref _collectionViewSources, value); }
        }

        #endregion

        #region Commands

        public RelayCommand<Label> AddFeature { get; private set; }

        #endregion

        #region Public methods

        public void AddUserFeature(Feature feature)
        {
            // If the user previously removed this feature, clear it from the list of removed features
            _userRemoved.Remove(feature);
            // If the user already added this feature, overwrite the previous version
            _userAdded.Remove(feature);
            _userAdded.Add(feature);
            // Ensure the vocabulary includes to token for this feature
            _vocab.AddToken(feature.Characters, 1); // FIXME get the right document frequency
        }

        public void RemoveUserFeature(Feature feature)
        {
            // If the user previously added this feature manually, remove it from our list
            _userAdded.Remove(feature);
            // Ensure we don't add this feature to the list multiple times
            if (!_userRemoved.Contains(feature))
                _userRemoved.Add(feature);
        }

        #endregion

        #region Private methods

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
            foreach (int id in _vocab.FeatureIds)
            {
                foreach (Label label in _labels)
                {
                    try
                    {
                        Feature f = new Feature(_vocab.GetWord(id), label);
                        f.SystemWeight = _classifier.GetFeatureWeight(id, label);
                        f.MostImportant = _classifier.IsFeatureMostImportantForLabel(id, label);
                        FeatureSet.Add(f);
                    } catch (KeyNotFoundException e)
                    {
                        Console.Error.WriteLine("Warning: {0}", e.Message);
                    }
                }

            }
        }

        private void PerformAddFeature(Label label)
        {
            AddFeatureDialog dialog = new AddFeatureDialog();
            dialog.Owner = App.Current.MainWindow;
            AddFeatureDialogViewModel vm = new AddFeatureDialogViewModel(label);
            dialog.DataContext = vm;
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                Console.WriteLine("add word: {0} with weight {1} to topic {2}", vm.Word, vm.SelectedWeight, vm.Label);
                Feature f = new Feature(vm.Word, label);
                if (vm.SelectedWeight == vm.Weights[0])
                    f.WeightType = Feature.Weight.High;
                else if (vm.SelectedWeight == vm.Weights[1])
                    f.WeightType = Feature.Weight.Medium;

                AddUserFeature(f);
            }
        }



        private void PerformRemoveFeature(Feature feature)
        {
            Console.WriteLine("PerformRemoveFeature() on {0}", feature);
        }

        private IReadOnlyList<CollectionViewSource> BuildCollectionViewSources(IReadOnlyList<Label> labels)
        {
            List<CollectionViewSource> collectionViewSources = new List<CollectionViewSource>();
            foreach (Label label in labels)
            {
                CollectionViewSource cvs = new CollectionViewSource();
                cvs.Source = FeatureSet;
                cvs.Filter += (o, e) =>
                {
                    Feature f = e.Item as Feature;
                    if (f != null)
                    {
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

        #endregion
    }
}
