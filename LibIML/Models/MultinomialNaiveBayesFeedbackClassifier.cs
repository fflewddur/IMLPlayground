using LibIML.Features;
using LibIML.Instances;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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

        // Store these for efficiency
        private Dictionary<Label, double> _pClass;
        private Dictionary<Label, Dictionary<int, double>> _pWordGivenClass;

        public MultinomialNaiveBayesFeedbackClassifier(IEnumerable<Label> labels, Vocabulary vocab)
        {
            _perClassFeatureCounts = new Dictionary<Label, Dictionary<int, int>>();
            _perClassFeaturePriors = new Dictionary<Label, Dictionary<int, double>>();
            _perClassFeatureCountSums = new Dictionary<Label, int>();
            _perClassFeaturePriorSums = new Dictionary<Label, double>();
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

        /// <summary>
        /// Get the relative system-determined importance of a given feature for a given label.
        /// </summary>
        /// <param name="id">The ID of the feature to lookup.</param>
        /// <param name="label">The label of the feature to lookup.</param>
        /// <param name="weight">Stores the feature's relative importance for 'label'.</param>
        /// <returns>True on success, false otherwise.</returns>
        public bool TryGetFeatureSystemWeight(int id, Label label, out double weight)
        {
            int count;
            bool retval = false;

            _perClassFeatureCounts[label].TryGetValue(id, out count);

            if (_perClassFeatureCountSums[label] > 0) {
                weight = (double)count / (double)(_perClassFeatureCountSums[label] + _perClassFeaturePriorSums[label]);
                retval = true;
            } else {
                weight = 0;
            }

            return retval;
        }

        /// <summary>
        /// Get the relative user-set importance of a given feature for a given label.
        /// </summary>
        /// <param name="id">The ID of the feature to lookup.</param>
        /// <param name="label">The label of the feature to lookup.</param>
        /// <param name="weight">Stores the feature's relative importance for 'label'.</param>
        /// <returns>True on success, false otherwise.</returns>
        public bool TryGetFeatureUserWeight(int id, Label label, out double weight)
        {
            double prior;
            bool retval = false;

            if (!_perClassFeaturePriors[label].TryGetValue(id, out prior))
                prior = _defaultPrior;

            if (_perClassFeaturePriorSums[label] > 0) {
                weight = (double)prior / (double)(_perClassFeatureCountSums[label] + _perClassFeaturePriorSums[label]);
                retval = true;
            } else {
                weight = 0;
            }

            return retval;
        }

        /// <summary>
        /// Get the user-set prior of a given feature for a given label.
        /// </summary>
        /// <param name="id">The ID of the feature to lookup.</param>
        /// <param name="label">The label of the feature to lookup.</param>
        /// <param name="prior">Stores the feature's prior for 'label'.</param>
        /// <returns>True on success, false otherwise.</returns>
        public bool TryGetFeatureUserPrior(int id, Label label, out double prior)
        {
            bool retval = false;

            if (_perClassFeaturePriors[label].TryGetValue(id, out prior)) {
                retval = true;
            } else {
                prior = _defaultPrior;
                retval = false;
            }

            return retval;
        }

        /// <summary>
        /// Get the summation of all system feature counts for a given label (if available).
        /// </summary>
        /// <param name="label">The label to get the feature count summation for.</param>
        /// <param name="sum">Stores the feature count summation.</param>
        /// <returns>True on success, false otherwise.</returns>
        public bool TryGetSystemFeatureSum(Label label, out double sum)
        {
            bool retval = false;

            if (_perClassFeatureCountSums[label] > 0) {
                sum = _perClassFeatureCountSums[label];
                retval = true;
            } else {
                sum = 0;
            }

            return retval;
        }

        public bool TryGetSystemFeatureSum(out double sum)
        {
            bool retval = true;

            sum = 0;
            foreach (Label l in _labels) {
                double labelSum;
                if (!TryGetSystemFeatureSum(l, out labelSum)) {
                    retval = false;
                }
                sum += labelSum;
            }

            return retval;
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
        /// Add a single instance to the classifier's training set. Do not retrain the classifier after the instance has been added.
        /// </summary>
        /// <param name="instance">The instance to add.</param>
        public void AddInstance(IInstance instance)
        {
            AddInstanceWithoutPrUpdates(instance);
            //Train();
        }

        /// <summary>
        /// Add a collection of instances to the classifier's training set. Do not retrain the classifier.
        /// </summary>
        /// <param name="instances">The collection of instances to add.</param>
        public void AddInstances(IEnumerable<IInstance> instances)
        {
            foreach (IInstance instance in instances) {
                AddInstanceWithoutPrUpdates(instance);
            }
            //Train();
        }

        /// <summary>
        /// Add a collection of user-specified feature priors. Do not retrain the classifier.
        /// </summary>
        /// <param name="priors"></param>
        public void AddPriors(IEnumerable<Feature> priors)
        {
            foreach (Feature f in priors) {
                int id = _vocab.GetWordId(f.Characters, true);
                _perClassFeaturePriors[f.Topic1Importance.Label][id] = f.Topic1Importance.UserPrior;
                _perClassFeaturePriors[f.Topic2Importance.Label][id] = f.Topic2Importance.UserPrior;
            }
            //Train();
        }

        /// <summary>
        /// Run the classifier on a single instance.
        /// </summary>
        /// <param name="instance">The instance whose label we want to predict.</param>
        /// <returns>A prediction for this instance.</returns>
        public Prediction PredictInstance(IInstance instance)
        {
            //Console.WriteLine("PredictInstance {0}", instance.Id);
            Label labelWinner = null;
            Label labelLoser = null;
            Prediction prediction = new Prediction();

            // Compute Pr(d|c)
            Dictionary<Label, double> pDocGivenClassLog = new Dictionary<Label, double>();
            int importantWordsUnique = 0; // how many of the features show up in the message?
            int importantWordsTotal = 0; // how many instances of features show up in the message?
            int trainingSetSize = 0;
            foreach (Label l in Labels) {
                trainingSetSize += _trainingSet[l].Count;
            }

            //List<EvidenceItem> evidenceItems = new List<EvidenceItem>();
            Dictionary<int, EvidenceItem> evidenceItems = new Dictionary<int, EvidenceItem>();
            foreach (KeyValuePair<int, double> pair in instance.Features.Data) {
                string word = _vocab.GetWord(pair.Key);
                EvidenceItem ei = new EvidenceItem(word, pair.Key, (int)pair.Value);
                evidenceItems[pair.Key] = ei;
            }

            // FIXME this only works for binary classification
            Label topic1 = null;
            Label topic2 = null;
            if (Labels.Count() >= 1) {
                topic1 = Labels.ElementAt(0);
                topic2 = Labels.ElementAt(1);
            }

            foreach (Label l in Labels) {
                Evidence evidence = new Evidence(l); // Store our evidence in favor of each class
                evidence.PrClass = _pClass[l];
                evidence.InstanceCount = _trainingSet[l].Count;
                evidence.TotalInstanceCount = trainingSetSize;
                double prob = 0;
                foreach (KeyValuePair<int, double> pair in instance.Features.Data) {
                    double pWord;
                    if (_pWordGivenClass[l].TryGetValue(pair.Key, out pWord)) {
                        if (pair.Value > 0) {
                            evidence.HasFeatures = true;
                        }
                        importantWordsTotal += (int)pair.Value;
                        importantWordsUnique++;
                        double weight = pair.Value * Math.Log(pWord);
                        
                        double userWeight, sysWeight;
                        string word = _vocab.GetWord(pair.Key);
                        //Console.WriteLine("feature {0} probability = {1}, count={2}, log(prob)={3}", word, pWord, pair.Value, Math.Log(weight));
                        TryGetFeatureUserWeight(pair.Key, l, out userWeight);
                        TryGetFeatureSystemWeight(pair.Key, l, out sysWeight);
                        Feature f = new Feature(word, Labels.ElementAt(0), Labels.ElementAt(1));
                        if (l == f.Topic1Importance.Label) {
                            f.Topic1Importance.SystemWeight = sysWeight;
                            f.Topic1Importance.UserWeight = userWeight;
                        } else if (l == f.Topic2Importance.Label) {
                            f.Topic2Importance.SystemWeight = sysWeight;
                            f.Topic2Importance.UserWeight = userWeight;
                        }
                        //Feature f = new Feature(word, l, (int)pair.Value, sysWeight, userWeight);
                        evidence.SourceItems.Add(f);
                        if (l == topic1) {
                            evidenceItems[pair.Key].Label1Pr = userWeight + sysWeight;
                        } else if (l == topic2) {
                            evidenceItems[pair.Key].Label2Pr = userWeight + sysWeight;
                        }
                        //Console.WriteLine("Feature={3}, weight={0}, userWeight={1}, sysWeight={2}, count={4}", weight, userWeight, sysWeight, word, (int)pair.Value);
                        prob += weight;
                    }
                }
                
                pDocGivenClassLog[l] = prob;
                prediction.EvidencePerClass[l] = evidence;
            }
            
            // We increment these for each label, so normalize them by the number of labels.
            prediction.ImportantWordTotal = importantWordsTotal / Labels.Count();
            prediction.ImportantWordUniques = importantWordsUnique / Labels.Count();

            // Get the largest pr(doc | class)
            double largestPDocGivenClassLog = double.MinValue;
            foreach (Label l in Labels) {
                if (pDocGivenClassLog[l] > largestPDocGivenClassLog) {
                    largestPDocGivenClassLog = pDocGivenClassLog[l];
                }
            }

            // Compute Pr(d) and compute pDocGivenClass
            double pDoc = 0;
            Dictionary<Label, double> pDocGivenClass = new Dictionary<Label, double>();
            foreach (Label l in Labels) {
                pDocGivenClass[l] =  Math.Exp(pDocGivenClassLog[l] - largestPDocGivenClassLog);
                pDoc += _pClass[l] * pDocGivenClass[l];
            }
            //Console.WriteLine("pDoc: {0}", pDoc);

            // Compute Pr(c|d)
            Dictionary<Label, double> pClassGivenDoc = new Dictionary<Label, double>();
            foreach (Label l in Labels) {
                pClassGivenDoc[l] = (_pClass[l] * pDocGivenClass[l]) / pDoc; // For log likelihood, with normalization
                prediction.EvidencePerClass[l].NumClasses = Labels.Count();
                prediction.EvidencePerClass[l].PrDoc = pDoc;
                prediction.EvidencePerClass[l].PrDocGivenClass = pDocGivenClass[l];
                //prediction.EvidencePerClass[l].UpdateHeightsForEvidenceExplanation();
                //Console.WriteLine("label={3}, pDocGivenClass={0:N3}, log(pDocGivenClass)={1:N3}, pDoc={2:N3}, pClass={4:N2}, conf={5:N3}, eFeatureWeight={6:N3}", pDocGivenClass[l], Math.Log(pDocGivenClass[l]), pDoc, l, _pClass[l],
                //    pClassGivenDoc[l], prediction.EvidencePerClass[l].PrDocGivenClass);
                //Console.WriteLine("Explanation: {0}", prediction.EvidencePerClass[l].GetExplanationString()); 
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
                    if (labelLoser == null) {
                        labelLoser = labelWinner;
                    }
                    labelWinner = l;
                } else {
                    labelLoser = l;
                }
                prediction.EvidencePerClass[l].Confidence = pClassGivenDoc[l];
            }

            prediction.Label = labelWinner;
            prediction.Confidence = prediction.EvidencePerClass[labelWinner].Confidence;
            //prediction.UpdateEvidenceGraphData();
            //prediction.UpdatePrDescriptions();
            prediction.EvidenceItems = evidenceItems.Values.OrderBy(i => i.FeatureText).ToList();
            ////Console.WriteLine("Prediction: {0} ({1:0%})", labelWinner, prediction.Confidence);
            foreach (EvidenceItem ei in prediction.EvidenceItems) {
                if (labelWinner == topic2) {
                    ei.InvertRatio();
                }

                if (ei.Ratio > 1) {
                    ei.Label = labelWinner;
                    ei.OtherLabel = labelLoser;
                } else if (ei.Ratio < 1) {
                    ei.InvertRatio();
                    ei.Label = labelLoser;
                    ei.OtherLabel = labelWinner;
                }

                //Console.WriteLine("  EvidenceItem = {0}", ei);
            }

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

        public void LogFeatureSet(XmlWriter writer)
        {
            writer.WriteStartElement("FeatureSet");
            foreach (int id in _vocab.FeatureIds) {
                foreach (Label l in _labels) {
                    double sysWeight, userWeight, userPrior;
                    string characters = _vocab.GetWord(id);
                    TryGetFeatureSystemWeight(id, l, out sysWeight);
                    TryGetFeatureUserWeight(id, l, out userWeight);
                    TryGetFeatureUserPrior(id, l, out userPrior);
                    writer.WriteStartElement("Feature");
                    writer.WriteAttributeString("characters", characters);
                    writer.WriteAttributeString("label", l.ToString());
                    writer.WriteAttributeString("systemWeight", sysWeight.ToString());
                    writer.WriteAttributeString("userWeight", userWeight.ToString());
                    writer.WriteAttributeString("userPrior", userPrior.ToString());
                    writer.WriteEndElement();
                }
            }
            writer.WriteEndElement();
        }

        public void LogTrainingSet(XmlWriter writer)
        {
            writer.WriteStartElement("TrainingSet");
            foreach (KeyValuePair<Label, HashSet<IInstance>> pair in _trainingSet) {
                writer.WriteStartElement("Topic");
                writer.WriteAttributeString("label", pair.Key.ToString());
                writer.WriteAttributeString("count", pair.Value.Count.ToString());
                foreach (IInstance instance in pair.Value) {
                    writer.WriteStartElement("Message");
                    writer.WriteAttributeString("id", instance.Id.ToString());
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
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
