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
        private List<Dictionary<int, int>> _perClassFeatureCounts;
        private List<Dictionary<int, double>> _perClassFeaturePriors;
        private Dictionary<Label, List<SparseVector>> _trainingSet;

        public MultinomialNaiveBayesClassifier(List<Label> labels, Vocabulary vocab)
        {
            Labels = labels;
            Vocab = vocab;
            _perClassFeatureCounts = new List<Dictionary<int, int>>();
            _perClassFeaturePriors = new List<Dictionary<int, double>>();
            foreach (Label c in Labels)
            {
                _perClassFeatureCounts.Add(new Dictionary<int, int>());
                _perClassFeaturePriors.Add(new Dictionary<int, double>());
            }
            _trainingSet = new Dictionary<Label, List<SparseVector>>();
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
        }
    }
}
