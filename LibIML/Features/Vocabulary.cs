using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibIML
{
    [Serializable]
    public class Vocabulary
    {
        private int _nextId;
        private Dictionary<string, int> _wordsToIds;
        private Dictionary<int, string> _idsToWords;
        private Dictionary<int, int> _documentFreqs; // number of documents each word appears in

        public const double MIN_DF_PERCENT = 0.01; // Tokens must appear in at least 1% of documents
        public const double MAX_DF_PERCENT = 0.90; // Tokens must not appear in more than 90% of documents

        public Vocabulary()
        {
            _nextId = 1;
            _wordsToIds = new Dictionary<string, int>();
            _idsToWords = new Dictionary<int, string>();
            _documentFreqs = new Dictionary<int, int>();
        }

        #region Properties

        public int Count
        {
            get { return _wordsToIds.Count; }
        }

        public int[] FeatureIds
        {
            get { return _wordsToIds.Values.ToArray(); }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Create a deep copy of this Vocabulary.
        /// </summary>
        /// <returns>A new Vocabulary.</returns>
        public Vocabulary Clone()
        {
            Vocabulary v = new Vocabulary();
            
            v._nextId = this._nextId;
            foreach (KeyValuePair<string, int> pair in _wordsToIds)
            {
                _wordsToIds[pair.Key] = pair.Value;
            }
            foreach (KeyValuePair<int, string> pair in _idsToWords)
            {
                _idsToWords[pair.Key] = pair.Value;
            }
            foreach (KeyValuePair<int, int> pair in _documentFreqs)
            {
                _documentFreqs[pair.Key] = pair.Value;
            }

            return v;
        }

        /// <summary>
        /// Get the number of documents a word appears in.
        /// </summary>
        /// <param name="wordId">The ID of the word to lookup.</param>
        /// <returns>Number of documents wordId appears in.</returns>
        public int GetDocFreq(int wordId)
        {
            int freq;
            _documentFreqs.TryGetValue(wordId, out freq);
            return freq;
        }

        /// <summary>
        /// Return the unique ID associated with a given word.
        /// </summary>
        /// <param name="word">The word whose ID we want.</param>
        /// <returns>The ID of the provided word, or -1 if the ID is invalid.</returns>
        public int GetWordId(string word)
        {
            int id;
            if (!_wordsToIds.TryGetValue(word, out id))
                id = -1;
            return id;
        }

        /// <summary>
        /// Return the word associated with a given unique ID.
        /// </summary>
        /// <param name="id">The ID whose word we want.</param>
        /// <returns>The word associated with the provided ID, or "[NOT FOUND]" if the ID is invalid.</returns>
        public string GetWord(int id)
        {
            string word;
            if (!_idsToWords.TryGetValue(id, out word))
                word = "[NOT FOUND]";
            return word;
        }

        /// <summary>
        /// Add a collection of tokens and their corresponding frequency counts to our vocabulary.
        /// </summary>
        /// <param name="tokenDocFreqs"></param>
        /// <param name="nDocs"></param>
        public void AddTokens(IEnumerable<KeyValuePair<string, int>> tokenDocFreqs, int nDocs = -1, double min_df_percent = 0, double max_df_percent = 1.0)
        {
            int min_df = (int)(min_df_percent * nDocs);
            int max_df = (int)(max_df_percent * nDocs);

            foreach (KeyValuePair<string, int> tokenDf in tokenDocFreqs)
            {
                if (nDocs < 0 || (tokenDf.Value >= min_df && tokenDf.Value <= max_df))
                {
                    _wordsToIds[tokenDf.Key] = _nextId;
                    _idsToWords[_nextId] = tokenDf.Key;
                    _documentFreqs[_nextId] = tokenDf.Value;
                    _nextId++;
                }
            }
        }

        /// <summary>
        /// Remove vocabulary elements that do not exist in a given set of Instances.
        /// </summary>
        /// <param name="instances">The collection of Instances to restrict our vocabulary to.</param>
        public void RestrictToInstances(IEnumerable<IInstance> instances)
        {
            HashSet<int> inInstances = new HashSet<int>(); // Track the feature IDs in instances
            HashSet<int> notInInstances = new HashSet<int>(); // Track the feature IDs to remove

            // Build a set of features in instances
            foreach (IInstance instance in instances)
            {
                foreach (KeyValuePair<int, double> pair in instance.Features.Data)
                {
                    inInstances.Add(pair.Key);
                }
            }

            // Build a set of features to remove
            foreach (int id in _idsToWords.Keys)
            {
                if (!inInstances.Contains(id))
                    notInInstances.Add(id);
            }

            // Remove features
            RemoveElements(notInInstances);

            return;
        }

        public Vocabulary GetSubset(IEnumerable<IInstance> instances)
        {
            HashSet<int> inInstances = new HashSet<int>(); // Track the feature IDs in instances

            // Build a set of features in instances
            foreach (IInstance instance in instances)
            {
                foreach (KeyValuePair<int, double> pair in instance.Features.Data)
                {
                    inInstances.Add(pair.Key);
                }
            }

            return GetSubset(inInstances, instances.Count());
        }

        /// <summary>
        /// Build a new Vocabulary that is restricted to the given collection of IDs.
        /// </summary>
        /// <param name="ids">The collection of IDs to restrict the new vocabulary to.</param>
        /// <returns>The new vocabulary object.</returns>
        public Vocabulary GetSubset(IEnumerable<int> ids, int nDocs)
        {
            Vocabulary v = new Vocabulary();
            Dictionary<string, int> tokens = new Dictionary<string, int>();

            foreach (int id in ids)
            {
                tokens.Add(_idsToWords[id], _documentFreqs[id]);
            }

            v.AddTokens(tokens, nDocs, 0, 1.0);

            return v;
        }

        /// <summary>
        /// Remove vocabulary elements that don't exist in a given collection of IDs.
        /// </summary>
        /// <param name="ids">The collection of IDs to restrict our vocabulary to.</param>
        public void RestrictToSubset(IEnumerable<int> ids)
        {
            HashSet<int> toRemove = new HashSet<int>();

            // Build a set of features to remove
            foreach (int id in _idsToWords.Keys)
            {
                if (!ids.Contains(id))
                {
                    toRemove.Add(id);
                }
            }

            // Remove features
            RemoveElements(toRemove);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, int> pair in _wordsToIds)
                sb.Append(string.Format("{0}:{1} ", pair.Key, pair.Value));
            return sb.ToString();
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Remove a specific element from this vocabulary.
        /// </summary>
        /// <param name="id">The ID of the element to remove.</param>
        private void RemoveElement(int id)
        {
            string word = _idsToWords[id];
            _wordsToIds.Remove(word);
            _idsToWords.Remove(id);
            _documentFreqs.Remove(id);
        }

        /// <summary>
        /// Remove a collection of elements from this vocabulary.
        /// </summary>
        /// <param name="ids">The collection of element IDs to remove.</param>
        private void RemoveElements(IEnumerable<int> ids)
        {
            foreach (int id in ids)
            {
                RemoveElement(id);
            }
        }

        #endregion

        #region Information Gain code

        private Dictionary<Label, double> ComputePrC(IEnumerable<IInstance> instances, IEnumerable<Label> labels)
        {
            Dictionary<Label, double> PrC = new Dictionary<Label, double>();
            Dictionary<Label, int> labelCounts = new Dictionary<Label, int>(); // The number of instances in each class

            foreach (Label label in labels)
            {
                labelCounts[label] = 0;
            }

            foreach (IInstance instance in instances)
            {
                labelCounts[instance.Label]++;
            }

            foreach (Label label in labels)
            {
                PrC[label] = (double)labelCounts[label] / (double)labelCounts.Count;
            }

            return PrC;
        }

        private Dictionary<int, double> ComputePrT(IEnumerable<IInstance> instances, IEnumerable<Label> labels)
        {
            Dictionary<int, double> PrT = new Dictionary<int, double>();
            Dictionary<int, int> featureCounts = new Dictionary<int, int>(); // The number of documents containing each feature

            foreach (int featureId in _idsToWords.Keys)
            {
                featureCounts[featureId] = 0;
            }

            foreach (IInstance instance in instances)
            {
                foreach (int featureId in instance.Features.Data.Keys)
                {
                    featureCounts[featureId]++; // Increment the number of document's we've seen this feature in
                }
            }

            foreach (int featureId in _idsToWords.Keys)
            {
                PrT[featureId] = (double)featureCounts[featureId] / (double)featureCounts.Count;
            }

            return PrT;
        }

        private Dictionary<Label, Dictionary<int, double>> ComputePrCGivenT(IEnumerable<IInstance> instances, IEnumerable<Label> labels)
        {
            Dictionary<Label, Dictionary<int, double>> PrCGivenT = new Dictionary<Label, Dictionary<int, double>>();
            Dictionary<int, int> featureCounts = new Dictionary<int, int>();
            Dictionary<Label, Dictionary<int, int>> featureCountsPerClass = new Dictionary<Label, Dictionary<int, int>>();

            foreach (Label label in labels)
            {
                featureCountsPerClass[label] = new Dictionary<int, int>();
                PrCGivenT[label] = new Dictionary<int, double>();
            }

            foreach (IInstance instance in instances)
            {
                foreach (int featureId in instance.Features.Data.Keys)
                {
                    int count;
                    featureCountsPerClass[instance.Label].TryGetValue(featureId, out count);
                    featureCountsPerClass[instance.Label][featureId] = count + 1; // Increment this feature's count for this class
                    featureCounts.TryGetValue(featureId, out count);
                    featureCounts[featureId] = count + 1; // Increment the number of document's we've seen this feature in

                }
            }

            foreach (Label label in labels)
            {
                foreach (int featureId in _idsToWords.Keys)
                {
                    int count;
                    featureCountsPerClass[label].TryGetValue(featureId, out count);
                    PrCGivenT[label][featureId] = (double)(count + 1) / (double)(featureCounts[featureId] + labels.Count()); // Add a smoothing term
                }
            }

            return PrCGivenT;
        }

        private Dictionary<Label, Dictionary<int, double>> ComputePrCGivenNotT(IEnumerable<IInstance> instances, IEnumerable<Label> labels)
        {
            Dictionary<Label, Dictionary<int, double>> PrCGivenNotT = new Dictionary<Label, Dictionary<int, double>>();
            Dictionary<Label, Dictionary<int, int>> featureAbsencesPerClass = new Dictionary<Label, Dictionary<int, int>>();
            Dictionary<int, int> featureAbsences = new Dictionary<int, int>(); // The number of documents *not* containing each feature

            foreach (Label label in labels)
            {
                featureAbsencesPerClass[label] = new Dictionary<int, int>();
                PrCGivenNotT[label] = new Dictionary<int, double>();
            }

            foreach (int featureId in _idsToWords.Keys)
            {
                foreach (IInstance instance in instances)
                {
                    if (!instance.Features.Contains(featureId))
                    {
                        int count;
                        featureAbsencesPerClass[instance.Label].TryGetValue(featureId, out count);
                        featureAbsencesPerClass[instance.Label][featureId] = count + 1;
                        featureAbsences.TryGetValue(featureId, out count);
                        featureAbsences[featureId] = count + 1;
                    }
                }
            }

            foreach (Label label in labels)
            {
                foreach (int featureId in _idsToWords.Keys)
                {
                    int count;
                    featureAbsencesPerClass[label].TryGetValue(featureId, out count);
                    PrCGivenNotT[label][featureId] = (double)(count + 1) / (double)(featureAbsences[featureId] + labels.Count()); // Add a smoothing term
                }
            }

            return PrCGivenNotT;
        }

        private void RestrictToHighIG(IEnumerable<IInstance> instances, IEnumerable<Label> labels, int vocabSize)
        {
            Dictionary<Label, double> PrC; // Probability of each class
            Dictionary<int, double> PrT; // Probability of each feature
            Dictionary<Label, Dictionary<int, double>> PrCGivenT;
            Dictionary<Label, Dictionary<int, double>> PrCGivenNotT;
            Dictionary<int, double> IG = new Dictionary<int, double>();                

            // Compute class and feature probabilities
            PrC = ComputePrC(instances, labels);
            PrT = ComputePrT(instances, labels);
            PrCGivenT = ComputePrCGivenT(instances, labels);
            PrCGivenNotT = ComputePrCGivenNotT(instances, labels);

            // Compute information gain for each feature
            foreach (int featureId in _idsToWords.Keys)
            {
                double PrCSum = 0;
                double PrTSum = 0;
                double PrNotTSum = 0;
                foreach (Label label in labels)
                {
                    PrCSum += PrC[label] * Math.Log(PrC[label]);
                    PrTSum += PrCGivenT[label][featureId] * Math.Log(PrCGivenT[label][featureId]);
                    PrNotTSum += PrCGivenNotT[label][featureId] * Math.Log(PrCGivenNotT[label][featureId]);
                }
                IG[featureId] = (-1.0 * PrCSum) + (PrT[featureId] * PrTSum) + ((1.0 - PrT[featureId]) * PrNotTSum);
            }

            // Find the features with the highest information gain
            List<KeyValuePair<int, double>> HighIG = IG.OrderBy(entry => entry.Value).Take(vocabSize).ToList();

            // Reduce our vocabulary to this subset
            List<int> featureIdsToRemove = _idsToWords.Keys.ToList();
            foreach (KeyValuePair<int, double> pair in HighIG)
            {
                featureIdsToRemove.Remove(pair.Key);
            }
            this.RemoveElements(featureIdsToRemove);
        }

        #endregion

        #region Static methods

        /// <summary>
        /// Create a new Vocabulary from a collection of instances.
        /// </summary>
        /// <param name="instances">A collection of items to use as the basis for this vocabulary.</param>
        /// <returns>A new Vocabulary object.</returns>
        public static Vocabulary CreateVocabulary(IEnumerable<IInstance> instances, IEnumerable<Label> labels, int desiredVocabSize)
        {
            ConcurrentDictionary<string, int> tokenDocCounts = new ConcurrentDictionary<string, int>();

            // Tokenize and stem each document, returning a collection of tokens and corresponding counts.
            Parallel.ForEach(instances, (instance, state, index) =>
                {
                    PorterStemmer stemmer = new PorterStemmer(); // PorterStemmer isn't threadsafe, so we need one for each operation.
                    Dictionary<string, int> tokens = Tokenizer.TokenizeAndStem(instance.AllText, stemmer) as Dictionary<string, int>;
                    instance.TokenCounts = tokens;
                    foreach (KeyValuePair<string, int> pair in tokens)
                    {
                        // If this key doesn't exist yet, add it with a value of 1. Otherwise, increment its value by 1.
                        tokenDocCounts.AddOrUpdate(pair.Key, 1, (key, value) => value + 1);
                    }
                });

            Vocabulary vocab = new Vocabulary();
            vocab.AddTokens(tokenDocCounts, instances.Count());

            // Update the Feature vector for each instance
            Parallel.ForEach(instances, (instance, state, index) =>
                {
                    instance.ComputeFeatureVector(vocab);
                });

            // Restrict our vocabulary to terms with high information gain
            vocab.RestrictToHighIG(instances, labels, desiredVocabSize);

            return vocab;
        }

        #endregion
    }
}
