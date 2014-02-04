using LibIML;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePredictor.ViewModel
{
    class FeatureSetViewModel : ViewModelBase
    {
        private IClassifier _classifier;
        private Vocabulary _vocab;
        private List<Feature> _userAdded;
        private List<Feature> _userRemoved;
        private ObservableCollection<Feature> _featureSet;

        public FeatureSetViewModel(IClassifier classifier, Vocabulary vocab)
            : base()
        {
            _classifier = classifier;
            _vocab = vocab;
            _featureSet = new ObservableCollection<Feature>();
            _userAdded = new List<Feature>();
            _userRemoved = new List<Feature>();

            _classifier.Retrained += classifier_Retrained;
            _vocab.Updated += vocab_Updated;
        }

        #region Properties

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

        #endregion

        #region Public methods

        public void AddUserFeature(Feature feature)
        {
            // If the user previously removed this feature, clear it from the list of removed features
            _userRemoved.Remove(feature);
            // If the user already added this feature, overwrite the previous version
            _userAdded.Remove(feature);
            _userAdded.Add(feature);
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

        }

        #endregion
    }
}
