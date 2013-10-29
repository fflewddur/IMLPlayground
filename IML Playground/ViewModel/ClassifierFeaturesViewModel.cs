using IML_Playground.Learning;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IML_Playground.ViewModel
{
    class ClassifierFeaturesViewModel : ViewModelBase
    {
        IClassifier _classifier;

        public ClassifierFeaturesViewModel(IClassifier classifier)
        {
            _classifier = classifier;

            FeaturesPositive = new ObservableCollection<Feature>();
            FeaturesNegative = new ObservableCollection<Feature>();
            LabelPositive = _classifier.Labels[0];
            LabelNegative = _classifier.Labels[1];

            UpdateFeatures(_classifier);
        }

        public Label LabelPositive { get; private set; }
        public Label LabelNegative { get; private set; }
        public ObservableCollection<Feature> FeaturesPositive { get; private set; }
        public ObservableCollection<Feature> FeaturesNegative { get; private set; }

        private void UpdateFeatures(IClassifier classifier)
        {
            FeaturesPositive.Clear();
            FeaturesNegative.Clear();

            foreach (Feature feature in classifier.FeaturesPerClass[classifier.Labels[0]])
            {
                FeaturesPositive.Add(feature);
            }

            foreach (Feature feature in classifier.FeaturesPerClass[classifier.Labels[1]])
            {
                FeaturesNegative.Add(feature);
            }

        }
    }
}
