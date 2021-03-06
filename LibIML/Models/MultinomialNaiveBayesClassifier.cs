﻿using LibIML.Features;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// WARNINGS: this code is unmaintained. I doubt it still does anything useful.

namespace LibIML
{
    [Serializable]
    public class MultinomialNaiveBayesClassifier : ViewModelBase
    {
        private const double _defaultPrior = 1.0;

        private IEnumerable<Label> _labels;
        private Vocabulary _vocab;
        private Dictionary<Label, Dictionary<int, int>> _perClassFeatureCounts;
        private Dictionary<Label, Dictionary<int, double>> _perClassFeaturePriors;
        private Dictionary<Label, HashSet<IInstance>> _trainingSet;
        private Dictionary<Label, HashSet<Feature>> _featuresPerClass; // This gives us a single property to expose with all of the feature data for each class

        // Store these for efficiency
        private Dictionary<Label, double> _pClass;
        private Dictionary<Label, Dictionary<int, double>> _pWordGivenClass;

        public MultinomialNaiveBayesClassifier(IEnumerable<Label> labels, Vocabulary vocab)
        {
            _perClassFeatureCounts = new Dictionary<Label, Dictionary<int, int>>();
            _perClassFeaturePriors = new Dictionary<Label, Dictionary<int, double>>();
            _featuresPerClass = new Dictionary<Label, HashSet<Feature>>();
            _trainingSet = new Dictionary<Label, HashSet<IInstance>>();
            _pClass = new Dictionary<Label, double>();
            _pWordGivenClass = new Dictionary<Label, Dictionary<int, double>>();

            Name = "MNB";
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
            set
            {
                if (SetProperty<Vocabulary>(ref _vocab, value))
                {
                    ClearInstances(); // If the vocabulary changes, our current instances are invalidated.
                }
            }
        }

        //public IReadOnlyDictionary<Label, HashSet<Feature>> FeaturesPerClass
        //{
        //    get { return _featuresPerClass; }
        //}

        public Label PositiveLabel
        {
            get { return _labels.ToList()[0]; }
        }

        public Label NegativeLabel
        {
            get { return _labels.ToList()[1]; }
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

        /// <summary>
        /// Remove all training information.
        /// </summary>
        public void ClearInstances()
        {
            _perClassFeatureCounts.Clear();
            _perClassFeaturePriors.Clear();
            _featuresPerClass.Clear();
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
            foreach (IInstance instance in instances)
            {
                AddInstanceWithoutPrUpdates(instance);
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
            foreach (Label l in Labels)
            {
                Evidence evidence = new Evidence(l); // Store our evidence in favor of each class
                double prob = 0;
                foreach (KeyValuePair<int, double> pair in instance.Features.Data)
                {
                    double pWord;
                    if (_pWordGivenClass[l].TryGetValue(pair.Key, out pWord))
                    {
                        double weight = pair.Value * Math.Log(pWord);
                        prob += weight;
                        //evidence.Weights[pair.Key] = weight;
                    }
                }
                pDocGivenClass[l] = prob;
                prediction.EvidencePerClass[l] = evidence;
            }

            // Compute Pr(d)
            double pDoc = 0;
            foreach (Label l in Labels)
            {
                pDoc += (_pClass[l] * Math.Pow(Math.E, pDocGivenClass[l]));
                //pDoc += (Math.Log(_pClass[l]) + pDocGivenClass[l]); // for log likelihood
            }
            //Console.WriteLine("pDoc: {0}", pDoc);

            // Compute Pr(c|d)
            Dictionary<Label, double> pClassGivenDoc = new Dictionary<Label, double>();
            foreach (Label l in Labels)
            {
                //pClassGivenDoc[l] = _pClass[l] * Math.Pow(Math.E, pDocGivenClass[l]) / pDoc; // no log likelihood, with normalization
                //pClassGivenDoc[l] = (Math.Log(_pClass[l]) + pDocGivenClass[l]) / pDoc; // for log likelihood, with normalization
                pClassGivenDoc[l] = Math.Log(_pClass[l]) + pDocGivenClass[l]; // For log likelihood, no normalization
            }

            // Find the class with the highest probability for this document
            double maxP = double.MinValue;
            foreach (Label l in Labels)
            {
                if (double.IsNaN(pClassGivenDoc[l]))
                {
                    throw new System.ArithmeticException(string.Format("Probability for class {0} is NaN.", l));
                }
                //Console.WriteLine("Label: {0} Probability: {1:0.00000}", l, pClassGivenDoc[l]);
                if (pClassGivenDoc[l] > maxP)
                {
                    maxP = pClassGivenDoc[l];
                    label = l;
                }
                prediction.EvidencePerClass[l].Confidence = pClassGivenDoc[l];
            }

            prediction.Label = label;
            return prediction;
        }

        /// <summary>
        /// Update the prior weight for a given feature, with respect to a given class.
        /// </summary>
        /// <param name="label">The class which this prior value applies to.</param>
        /// <param name="feature">The feature which this prior value applies to.</param>
        /// <param name="prior">The new prior value for this feature/class tuple.</param>
        public void UpdatePrior(Label label, int feature, double prior)
        {
            _perClassFeaturePriors[label][feature] = prior;
        }

        public async Task<bool> SaveArffFile(string filePath)
        {
            bool retval = true;

            // Merge our training set into a single collection of instances
            HashSet<IInstance> instances = new HashSet<IInstance>();
            foreach (Label label in Labels)
            {
                foreach (IInstance instance in _trainingSet[label])
                {
                    instances.Add(instance);
                }
            }

            try
            {
                await MultinomialNaiveBayesClassifier.SaveArffFile(instances, Vocab, Labels.ToArray(), filePath);
            }
            catch (IOException e)
            {
                Console.Error.WriteLine("Error saving ARFF file: {0}.", e.Message);
                retval = false;
            }

            return retval;
        }

        public static async Task<bool> SaveArffFile(IEnumerable<IInstance> instances, Vocabulary vocab, Label[] labels, string filePath)
        {
            using (TextWriter writer = File.CreateText(filePath))
            {
                string relation = string.Join("_", labels.Select(l => l.UserLabel));
                await writer.WriteLineAsync(string.Format("@RELATION {0}\n", relation));

                // Write out each feature and its type (for us, they're all numeric)
                foreach (int id in vocab.FeatureIds)
                {
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
                foreach (IInstance instance in instances)
                {
                    string[] itemFeatures = new string[instance.Features.Data.Count + 1]; // include room for class attribute
                    int index = 0;
                    foreach (KeyValuePair<int, double> pair in instance.Features.Data.OrderBy(pair => pair.Key))
                    {
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
            //UpdateFeaturesPerClass();
        }

        /// <summary>
        /// Initialize training data structures using our collection of potential output labels.
        /// </summary>
        private void InitTrainingData()
        {
            foreach (Label l in Labels)
            {
                _perClassFeatureCounts[l] = new Dictionary<int, int>();
                _perClassFeaturePriors[l] = new Dictionary<int, double>();
                _trainingSet[l] = new HashSet<IInstance>();
                _featuresPerClass[l] = new HashSet<Feature>();
            }
        }

        /// <summary>
        /// Add an instance to our training set, but don't compute probability updates.
        /// </summary>
        /// <param name="instance">The instance to add.</param>
        private void AddInstanceWithoutPrUpdates(IInstance instance)
        {
            // Update our feature counts
            foreach (KeyValuePair<int, double> pair in instance.Features.Data)
            {
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

            foreach (Label l in Labels)
            {
                trainingSetSize += _trainingSet[l].Count;
                _pClass[l] = 0;
            }
            if (trainingSetSize > 0)
            {
                foreach (Label l in Labels)
                {
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

            foreach (Label l in Labels)
            {
                // Sum up the feature counts
                _pWordGivenClass[l] = new Dictionary<int, double>();
                int sumFeatures = _perClassFeatureCounts[l].Values.Sum();

                // Sum up the priors
                double sumPriors = 0;
                foreach (int id in Vocab.FeatureIds)
                {
                    double prior;
                    if (!_perClassFeaturePriors[l].TryGetValue(id, out prior))
                        prior = _defaultPrior; // Use a default value if the user didn't provide one.
                    sumPriors += prior;
                }

                foreach (int id in Vocab.FeatureIds)
                {
                    int countFeature;
                    _perClassFeatureCounts[l].TryGetValue(id, out countFeature);
                    double prior;
                    if (!_perClassFeaturePriors[l].TryGetValue(id, out prior))
                        prior = _defaultPrior;
                    _pWordGivenClass[l][id] = (prior + countFeature) / ((double)(sumFeatures + sumPriors));
                }
            }
        }

        /// <summary>
        /// Create collections of Features for each Label, using our current training set as the source data.
        /// </summary>
        /// 
        /*
        private void UpdateFeaturesPerClass()
        {
            _featuresPerClass.Clear();

            foreach (Label label in Labels)
            {
                _featuresPerClass[label] = new HashSet<Feature>();
            }

            foreach (int featureId in Vocab.FeatureIds)
            {

                foreach (Label label in Labels)
                {
                    string characters = _vocab.GetWord(featureId);
                    int count;
                    _perClassFeatureCounts[label].TryGetValue(featureId, out count);
                    double userWeight;
                    if (!_perClassFeaturePriors[label].TryGetValue(featureId, out userWeight))
                        userWeight = _defaultPrior;
                    double systemWeight;
                    _pWordGivenClass[label].TryGetValue(featureId, out systemWeight);

                    _featuresPerClass[label].Add(new Feature { Characters = characters, CountTraining = count, SystemWeight = systemWeight, UserWeight = userWeight });
                }
            }
        }
         */
    }
}
