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
        IClassifier _classifier;
        Vocabulary _vocab;

        ObservableCollection<Feature> _featureSet;

        public FeatureSetViewModel(IClassifier classifier, Vocabulary vocab) : base()
        {
            _classifier = classifier;
            _vocab = vocab;

            _classifier.Retrained += classifier_Retrained;
            _vocab.Updated += vocab_Updated;
        }

        void classifier_Retrained(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        void vocab_Updated(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        public ObservableCollection<Feature> FeatureSet
        {
            get { return _featureSet; }
            set { SetProperty<ObservableCollection<Feature>>(ref _featureSet, value); }
        }
    }
}
