using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibIML
{
    [Serializable]
    public class MultinomialNaiveBayesFeedbackClassifier : ViewModelBase, IClassifier
    {
        private const double _defaultPrior = 1.0;

        private IEnumerable<Label> _labels;
        private Vocabulary _vocab;
        private Dictionary<Label, Dictionary<int, int>> _perClassFeatureCounts;
        private Dictionary<Label, Dictionary<int, double>> _perClassFeaturePriors;
        private Dictionary<Label, int> _perClassFeatureCountSums;
        private Dictionary<Label, double> _perClassFeaturePriorSums;
        private Dictionary<Label, HashSet<IInstance>> _trainingSet;
        //private Dictionary<Label, HashSet<Feature>> _featuresPerClass; // This gives us a single property to expose with all of the feature data for each class

        // Store these for efficiency
        private Dictionary<Label, double> _pClass;
        private Dictionary<Label, Dictionary<int, double>> _pWordGivenClass;

        public MultinomialNaiveBayesFeedbackClassifier(IEnumerable<Label> labels, Vocabulary vocab)
        {
            _perClassFeatureCounts = new Dictionary<Label, Dictionary<int, int>>();
            _perClassFeaturePriors = new Dictionary<Label, Dictionary<int, double>>();
            _perClassFeatureCountSums = new Dictionary<Label, int>();
            _perClassFeaturePriorSums = new Dictionary<Label, double>();
            //_featuresPerClass = new Dictionary<Label, HashSet<Feature>>();
            _trainingSet = new Dictionary<Label, HashSet<IInstance>>();
            _pClass = new Dictionary<Label, double>();
            _pWordGivenClass = new Dictionary<Label, Dictionary<int, double>>();

            Name = "MNB + Feedback";
            Labels = labels;
            Vocab = vocab;

            InitTrainingData();
        }

        #region Properties

        public string Name { get; private set; }

        public IEnumerable<Label> Labels
        {
            get { return _labels; }
            private set { SetProperty<IEnumerable<Label>>(ref _labels, value); }
        }

        public Vocabulary Vocab
        {
            get { return _vocab; }
            private set
            {
                if (SetProperty<Vocabulary>(ref _vocab, value)) {
                    ClearInstances(); // If the vocabulary changes, our current instances are invalidated.
                }
            }
        }

        #endregion

        #region Events

        public event EventHandler<EventArgs> Retrained;

        protected virtual void OnRetrained(EventArgs e)
        {
            if (Retrained != null)
                Retrained(this, e);
        }

        #endregion

        #region Public methods

        public double GetFeatureSystemWeight(int id, Label label)
        {
            int count;
            
            _perClassFeatureCounts[label].TryGetValue(id, out count);
            
            return (double)count / (double)_perClassFeatureCountSums[label];
        }

        public double GetFeatureUserWeight(int id, Label label)
        {
            double prior;

            if (!_perClassFeaturePriors[label].TryGetValue(id, out prior))
                prior = _defaultPrior;

            return (double)prior / (double)_perClassFeaturePriorSums[label];
        }

        public bool IsFeatureMostImportantForLabel(int id, Label label)
        {
            Label mostImportantLabel = null;
            double mostImportantWeight = 0;

            foreach (Label l in _labels) {
                double weight;
                _pWordGivenClass[l].TryGetValue(id, out weight);
                if (weight > mostImportantWeight) {
                    mostImportantWeight = weight;
                    mostImportantLabel = l;
                }
            }

            return (mostImportantLabel == label);
        }

        /// <summary>
        /// When the user adds a new feature, we need to update our document frequency counts for our training set
        /// so that the classifier can estimate the probabilities of this feature per class.
        /// </summary>
        /// <param name="id">The ID of the newly added feature.</param>
        public void UpdateCountsForNewFeature(int id)
        {
            Debug.Assert(id > 0, "The ID of a feature cannot be < 1");

            // Update our feature counts for each item in our training set
            foreach (Label label in _labels) {
                foreach (IInstance instance in _trainingSet[label]) {
                    double count;
                    if (instance.Features.Data.TryGetValue(id, out count)) {
                        int df;
                        _perClassFeatureCounts[instance.GroundTruthLabel].TryGetValue(id, out df);
                        df += (int)count;
                        _perClassFeatureCounts[instance.GroundTruthLabel][id] = df;
                    }
                }
                int totalDf;
                _perClassFeatureCounts[label].TryGetValue(id, out totalDf);
                //Console.WriteLine("Total DF of {0} for {1}: {2}", _vocab.GetWord(id), label, totalDf);
            }
        }

        /// <summary>
        /// Remove all training information.
        /// </summary>
        public void ClearInstances()
        {
            _perClassFeatureCounts.Clear();
            _perClassFeaturePriors.Clear();
            _perClassFeatureCountSums.Clear();
            _perClassFeaturePriorSums.Clear();
            _pClass.Clear();
            _trainingSet.Clear();
            _pWordGivenClass.Clear();

            InitTrainingData();
        }

        /// <summary>
        /// Add a single instance to the classifier's training set. Retrain the classifier after the instance has been added.
        /// </summary>
        /// <param name="instance">The instance to add.</param>
        public void AddInstance(IInstance instance)
        {
            AddInstanceWithoutPrUpdates(instance);
            Train();
        }

        /// <summary>
        /// Add a collection of instances to the classifier's training set. Retrain the classifier after each instance has been added.
        /// </summary>
        /// <param name="instances">The collection of instances to add.</param>
        public void AddInstances(IEnumerable<IInstance> instances)
        {
            foreach (IInstance instance in instances) {
                AddInstanceWithoutPrUpdates(instance);
            }
            Train();
        }

        public void AddPriors(IEnumerable<Feature> priors)
        {
            foreach (Feature f in priors) {
                double weight;
                if (f.WeightType == Feature.Weight.High)
                    weight = 50;
                else if (f.WeightType == Feature.Weight.Medium)
                    weight = 25;
                else
                    weight = f.UserWeight;

                int id = _vocab.GetWordId(f.Characters, true);
                _perClassFeaturePriors[f.Label][id] = weight;
            }
            Train();
        }

        /// <summary>
        /// Run the classifier on a single instance.
        /// </summary>
        /// <param name="instance">The instance whose label we want to predict.</param>
        /// <returns>A prediction for this instance.</returns>
        public Prediction PredictInstance(IInstance instance)
        {
            Label label = null;
            Prediction prediction = new Prediction();

            // Compute Pr(d|c)
            Dictionary<Label, double> pDocGivenClass = new Dictionary<Label, double>();
            foreach (Label l in Labels) {
                Evidence evidence = new Evidence(Vocab); // Store our evidence in favor of each class
                evidence.ClassPr = _pClass[l];
                double prob = 0;
                foreach (KeyValuePair<int, double> pair in instance.Features.Data) {
                    double pWord;
                    if (_pWordGivenClass[l].TryGetValue(pair.Key, out pWord)) {
                        double weight = Math.Exp(pair.Value * Math.Log(pWord));
                        prob += Math.Log(weight);
                        evidence.Weights[pair.Key] = weight;
                    }
                }
                pDocGivenClass[l] = Math.Exp(prob);
                prediction.EvidencePerClass[l] = evidence;
            }

            // Compute Pr(d)
            double pDoc = 0;
            foreach (Label l in Labels) {
                pDoc += _pClass[l] * pDocGivenClass[l];
            }
            //Console.WriteLine("pDoc: {0}", pDoc);

            // Compute Pr(c|d)
            Dictionary<Label, double> pClassGivenDoc = new Dictionary<Label, double>();
            foreach (Label l in Labels) {
                pClassGivenDoc[l] = (_pClass[l] * pDocGivenClass[l]) / pDoc; // For log likelihood, with normalization
            }

            // Find the class with the highest probability for this document
            double maxP = double.MinValue;
            foreach (Label l in Labels) {
                if (double.IsNaN(pClassGivenDoc[l])) {
                    throw new System.ArithmeticException(string.Format("Probability for class {0} is NaN.", l));
                }
                //Console.WriteLine("Label: {0} Probability: {1:0.00000}", l, pClassGivenDoc[l]);
                if (pClassGivenDoc[l] > maxP) {
                    maxP = pClassGivenDoc[l];
                    label = l;
                }
                prediction.EvidencePerClass[l].Confidence = pClassGivenDoc[l];
            }

            prediction.Label = label;
            prediction.Confidence = prediction.EvidencePerClass[label].Confidence;

            return prediction;
        }

        public async Task<bool> SaveArffFile(string filePath)
        {
            bool retval = true;

            // Merge our training set into a single collection of instances
            HashSet<IInstance> instances = new HashSet<IInstance>();
            foreach (Label label in Labels) {
                foreach (IInstance instance in _trainingSet[label]) {
                    instances.Add(instance);
                }
            }

            try {
                await MultinomialNaiveBayesClassifier.SaveArffFile(instances, Vocab, Labels.ToArray(), filePath);
            } catch (IOException e) {
                Console.Error.WriteLine("Error saving ARFF file: {0}.", e.Message);
                retval = false;
            }

            return retval;
        }

        public static async Task<bool> SaveArffFile(IEnumerable<IInstance> instances, Vocabulary vocab, Label[] labels, string filePath)
        {
            using (TextWriter writer = File.CreateText(filePath)) {
                string relation = string.Join("_", labels.Select(l => l.UserLabel));
                await writer.WriteLineAsync(string.Format("@RELATION {0}\n", relation));

                // Write out each feature and its type (for us, they're all numeric)
                foreach (int id in vocab.FeatureIds) {
                    await writer.WriteLineAsync(string.Format("@ATTRIBUTE _{0} NUMERIC", vocab.GetWord(id)));
                }

                // Write out the list of possible output labels
                string[] classAttributeParts = new string[labels.Length];
                for (int i = 0; i < labels.Length; i++)
                    classAttributeParts[i] = labels[i].SystemLabel;
                string classAttribute = string.Format("{{{0}}}", string.Join(",", classAttributeParts));
                await writer.WriteLineAsync(string.Format("@ATTRIBUTE class {0}\n", classAttribute));

                // Write out sparse vectors for each item
                await writer.WriteLineAsync("@DATA");
                int classIndex = vocab.Count; // the class attribute is always the last attribute
                foreach (IInstance instance in instances) {
                    string[] itemFeatures = new string[instance.Features.Data.Count + 1]; // include room for class attribute
                    int index = 0;
                    foreach (KeyValuePair<int, double> pair in instance.Features.Data.OrderBy(pair => pair.Key)) {
                        itemFeatures[index] = string.Format("{0} {1}", pair.Key - 1, pair.Value); // Subtract 1 because ARFF features are 0-indexed, but our are 1-indexed
                        index++;
                    }
                    itemFeatures[index] = string.Format("{0} \"{1}\"", classIndex, instance.GroundTruthLabel.SystemLabel);
                    string featureString = string.Join(", ", itemFeatures);
                    await writer.WriteLineAsync(string.Format("{{{0}}}", featureString));
                }
            }

            return true;
        }

        /// <summary>
        /// Update our probabilities for classes and words, incorporating any user-provided feature priors.
        /// </summary>
        public void Train()
        {
            ComputePrC();
            ComputePrWGivenC();

            TestPrC(_pClass);
            TestPrWGivenC(_pWordGivenClass);
            OnRetrained(new EventArgs());
        }

        #endregion

        /// <summary>
        /// Initialize training data structures using our collection of potential output labels.
        /// </summary>
        private void InitTrainingData()
        {
            foreach (Label l in Labels) {
                _perClassFeatureCounts[l] = new Dictionary<int, int>();
                _perClassFeaturePriors[l] = new Dictionary<int, double>();
                _perClassFeatureCountSums[l] = 0;
                _perClassFeaturePriorSums[l] = 0;
                _trainingSet[l] = new HashSet<IInstance>();
                _pWordGivenClass[l] = new Dictionary<int, double>();
            }
        }

        /// <summary>
        /// Add an instance to our training set, but don't compute probability updates.
        /// </summary>
        /// <param name="instance">The instance to add.</param>
        private void AddInstanceWithoutPrUpdates(IInstance instance)
        {
            // Update our feature counts
            foreach (KeyValuePair<int, double> pair in instance.Features.Data) {
                int count;
                _perClassFeatureCounts[instance.GroundTruthLabel].TryGetValue(pair.Key, out count);
                count += (int)pair.Value;
                _perClassFeatureCounts[instance.GroundTruthLabel][pair.Key] = count;
            }

            // Store this feature vector
            _trainingSet[instance.GroundTruthLabel].Add(instance);
        }

        /// <summary>
        /// Compute the probability of each class.
        /// </summary>
        private void ComputePrC()
        {
            _pClass.Clear();
            int trainingSetSize = 0;

            foreach (Label l in Labels) {
                trainingSetSize += _trainingSet[l].Count;
                _pClass[l] = 0;
            }
            if (trainingSetSize > 0) {
                foreach (Label l in Labels) {
                    _pClass[l] = _trainingSet[l].Count / (double)trainingSetSize;
                }
            }
        }

        /// <summary>
        /// Compute the probability of each feature occuring in each class.
        /// Incorporate user-provided feature priors.
        /// </summary>
        private void ComputePrWGivenC()
        {
            _pWordGivenClass.Clear();

            foreach (Label l in Labels) {
                // Sum up the feature counts
                _pWordGivenClass[l] = new Dictionary<int, double>();
                _perClassFeatureCountSums[l] = 0;
                foreach (int id in Vocab.FeatureIds) {
                    int count;
                    _perClassFeatureCounts[l].TryGetValue(id, out count);
                    _perClassFeatureCountSums[l] += count;
                }

                // Sum up the priors
                _perClassFeaturePriorSums[l] = 0;
                foreach (int id in Vocab.FeatureIds) {
                    double prior;
                    if (!_perClassFeaturePriors[l].TryGetValue(id, out prior))
                        prior = _defaultPrior; // Use a default value if the user didn't provide one.
                    _perClassFeaturePriorSums[l] += prior;
                }
                //Console.Write("PrWGivenC for {0}: ", l);
                foreach (int id in Vocab.FeatureIds) {
                    int countFeature;
                    _perClassFeatureCounts[l].TryGetValue(id, out countFeature);
                    double prior;
                    if (!_perClassFeaturePriors[l].TryGetValue(id, out prior))
                        prior = _defaultPrior;
                    _pWordGivenClass[l][id] = (prior + countFeature) / (_perClassFeaturePriorSums[l] + (double)_perClassFeatureCountSums[l]);
                    //Console.Write("{0}={1:N4} ", Vocab.GetWord(id), _pWordGivenClass[l][id]);
                }
                //Console.WriteLine();
            }
        }

        private bool TestPrC(Dictionary<Label, double> PrC)
        {
            double sum = 0;
            int count = 0;

            foreach (Label l in _labels) {
                sum += PrC[l];
                count++;
            }

            bool retval = (count > 0 && (Math.Round(sum, 7) == Math.Round(1.0, 7)));
            if (!retval)
                throw new ArithmeticException(string.Format("Error in PrC: Sum should be 1, but is {0}", sum));

            return retval;
        }

        private bool TestPrWGivenC(Dictionary<Label, Dictionary<int, double>> PrWGivenC)
        {
            bool retval = true;
            foreach (Label l in _labels) {
                double sum = 0;
                int count = 0;

                foreach (double pr in PrWGivenC[l].Values) {
                    sum += pr;
                    count++;
                }

                if (count > 0 && Math.Round(sum, 7) != Math.Round(1.0, 7)) {
                    retval = false;
                    throw new ArithmeticException(string.Format("Error in PrWGivenC: Sum should be 1, but is {0} for class {1}", sum, l));
                }
            }

            return retval;
        }
    }
}
