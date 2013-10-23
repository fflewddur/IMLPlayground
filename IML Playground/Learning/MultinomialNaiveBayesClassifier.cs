using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IML_Playground.Learning
{
    [Serializable]
    class MultinomialNaiveBayesClassifier : IML_Playground.Framework.Model
    {
        // Data we need:
        // Frequency count of each feature, per class
        // Number of classes
        // Vocabulary size
        // Prior (weight) for each feature, per class
        // Feature vectors for each training document, per class
        private List<Label> _labels;
        private Vocabulary _vocab;
        private Dictionary<Label, Dictionary<int, int>> _perClassFeatureCounts;
        private Dictionary<Label, Dictionary<int, double>> _perClassFeaturePriors;
        private Dictionary<Label, List<SparseVector>> _trainingSet;

        public MultinomialNaiveBayesClassifier(List<Label> labels, Vocabulary vocab)
        {
            Labels = labels;
            Vocab = vocab;
            _perClassFeatureCounts = new Dictionary<Label, Dictionary<int, int>>();
            _perClassFeaturePriors = new Dictionary<Label, Dictionary<int, double>>();
            _trainingSet = new Dictionary<Label, List<SparseVector>>();
            foreach (Label l in Labels)
            {
                _perClassFeatureCounts[l] = new Dictionary<int, int>();
                _perClassFeaturePriors[l] = new Dictionary<int, double>();
                _trainingSet[l] = new List<SparseVector>();
            }

        }

        #region Properties

        public List<Label> Labels
        {
            get { return _labels; }
            private set { SetProperty<List<Label>>(ref _labels, value); }
        }

        public Vocabulary Vocab
        {
            get { return _vocab; }
            private set { SetProperty<Vocabulary>(ref _vocab, value); }
        }

        #endregion

        public void AddInstance(Label classification, SparseVector features)
        {
            // TODO: handle prior values here?
            
            // Update our feature counts
            foreach (KeyValuePair<int, double> pair in features.Data)
            {
                int count;
                _perClassFeatureCounts[classification].TryGetValue(pair.Key, out count);
                count += (int)pair.Value;
                _perClassFeatureCounts[classification][pair.Key] = count;
            }

            // Store this feature vector
            _trainingSet[classification].Add(features);
        }

        public Label PredictInstance(SparseVector features)
        {
            Label label = null;
            Dictionary<Label, double> pClass = new Dictionary<Label, double>();

            // Compute Pr(c)
            int trainingSetSize = 0;
            foreach (Label l in Labels)
            {
                trainingSetSize += _trainingSet[l].Count;
                pClass[l] = 0;
            }
            if (trainingSetSize > 0)
            {
                foreach (Label l in Labels)
                {
                    pClass[l] = _trainingSet[l].Count / (double)trainingSetSize;
                }
            }

            // Compute Pr(w|c) [Pr(c|w)Pr(
            Dictionary<Label, int> featuresPerClass = new Dictionary<Label, int>();
            Dictionary<Label, Dictionary<int, double>> pWordGivenClass = new Dictionary<Label, Dictionary<int, double>>();
            foreach (Label l in Labels)
            {
                pWordGivenClass[l] = new Dictionary<int, double>();
                int sumFeatures = 0;
                foreach (int value in _perClassFeatureCounts[l].Values)
                {
                    sumFeatures += value;
                }

                // Sum up the priors
                double sumPriors = 0;
                foreach (int id in Vocab.FeatureIds)
                {
                    double prior;
                    if (!_perClassFeaturePriors[l].TryGetValue(id, out prior))
                        prior = 1;
                    sumPriors += prior;
                }

                //Console.WriteLine("Count of features in this class: {0}", sumFeatures);
                foreach (int id in Vocab.FeatureIds)
                {
                    int countFeature;
                    _perClassFeatureCounts[l].TryGetValue(id, out countFeature);
                    double prior;
                    if (!_perClassFeaturePriors[l].TryGetValue(id, out prior))
                        prior = 1;
                    pWordGivenClass[l][id] = (prior + countFeature) / ((double)sumFeatures + sumPriors);
                    //Console.WriteLine("Pr({0}|{1}) = {2:0.000}", Vocab.GetWord(id), l.UserLabel, pWordGivenClass[l][id]);
                }
            }

            // Compute Pr(d|c) [take the log of this and Pr(c), shown in EQ 9]
            Dictionary<Label, double> pDocGivenClass = new Dictionary<Label,double>();
            foreach (Label l in Labels)
            {

                double prob = 0;
                foreach (KeyValuePair<int, double> pair in features.Data)
                {
                    double pWord;
                    if (pWordGivenClass[l].TryGetValue(pair.Key, out pWord))
                    {
//                        pWord = Math.Pow(pWord, pair.Value);
//                        prob *= pWord;
                        prob += (pair.Value * Math.Log10(pWord));
                    }
                }
                pDocGivenClass[l] = prob;
            }

            // Compute Pr(d)
            double pDoc = 0;
            foreach (Label l in Labels)
            {
                pDoc += (pClass[l] * pDocGivenClass[l]);
            }

            // Compute Pr(c|d)
            Dictionary<Label, double> pClassGivenDoc = new Dictionary<Label, double>();
            foreach (Label l in Labels)
            {
                //pClassGivenDoc[l] = (pClass[l] * pDocGivenClass[l]) / pDoc;
                pClassGivenDoc[l] = Math.Log10(pClass[l]) + pDocGivenClass[l];
            }

            // Find the class with the highest probability for this document
            double maxP = double.MinValue;
            foreach (Label l in Labels)
            {
                if (double.IsNaN(pClassGivenDoc[l]))
                    Console.Error.WriteLine("Error: Probability for class {0} is NaN.", l);
                Console.WriteLine("Label: {0} Probability: {1:0.00000}", l, pClassGivenDoc[l]);
                // These are NaN because I'm dividing by 0 somewhere, let's fix that.
                if (pClassGivenDoc[l] > maxP)
                {
                    maxP = pClassGivenDoc[l];
                    label = l;
                }
            }

            return label;
        }
    }
}
