using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibIML
{
    [Serializable]
    public class Vocabulary
    {
        public enum Restriction
        {
            None,
            HighIG
        };

        private Restriction _restriction;
        private int _nextId;
        private Dictionary<string, int> _allWordsToIds;
        private Dictionary<int, string> _allIdsToWords;
        private Dictionary<int, int> _allDocumentFreqs; // number of documents each word appears in
        private HashSet<int> _restrictedIds;
        private bool _hasUpdatedTokens;

        public Vocabulary()
        {
            _nextId = 1;
            _allWordsToIds = new Dictionary<string, int>();
            _allIdsToWords = new Dictionary<int, string>();
            _allDocumentFreqs = new Dictionary<int, int>();
            _restrictedIds = new HashSet<int>();
            _hasUpdatedTokens = false;
        }

        public Vocabulary(Restriction restriction)
            : this()
        {
            _restriction = restriction;
        }

        #region Properties

        public bool HasUpdatedTokens
        {
            get { return _hasUpdatedTokens; }
            private set { _hasUpdatedTokens = value; }
        }

        public int Count
        {
            get
            {
                int result = 0;
                switch (_restriction)
                {
                    case Restriction.None:
                        result = _allWordsToIds.Count;
                        break;
                    case Restriction.HighIG:
                        result = _restrictedIds.Count;
                        break;
                    default:
                        Console.Error.WriteLine("Error: Unknown restriction type!");
                        break;
                }
                return result;
            }
        }

        public int[] FeatureIds
        {
            get
            {
                int[] result = null;
                switch (_restriction)
                {
                    case Restriction.None:
                        result = _allWordsToIds.Values.ToArray();
                        break;
                    case Restriction.HighIG:
                        result = _restrictedIds.ToArray();
                        break;
                    default:
                        Console.Error.WriteLine("Error: Unknown restriction type!");
                        break;
                }
                return result;
            }
        }

        #endregion

        #region Events

        public event EventHandler<EventArgs> Updated;

        protected virtual void OnUpdated(EventArgs e)
        {
            if (Updated != null)
                Updated(this, e);
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Get the number of documents a word appears in.
        /// </summary>
        /// <param name="wordId">The ID of the word to lookup.</param>
        /// <returns>Number of documents wordId appears in.</returns>
        public int GetAllDocFreq(int wordId)
        {
            int freq;
            _allDocumentFreqs.TryGetValue(wordId, out freq);
            return freq;
        }

        /// <summary>
        /// Return the unique ID associated with a given word.
        /// </summary>
        /// <param name="word">The word whose ID we want.</param>
        /// <returns>The ID of the provided word, or -1 if the ID is invalid.</returns>
        public int GetWordId(string word, bool isRestricted)
        {
            int id = -1;

            if (isRestricted)
            {
                switch (_restriction)
                {
                    case Restriction.None:
                        if (!_allWordsToIds.TryGetValue(word, out id))
                            id = -1;
                        break;
                    case Restriction.HighIG:
                        if (!_allWordsToIds.TryGetValue(word, out id))
                            id = -1;
                        else if (!_restrictedIds.Contains(id))
                            id = -1;
                        break;
                }
            }
            else
            {
                if (!_allWordsToIds.TryGetValue(word, out id))
                    id = -1;
            }

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
            if (!_allIdsToWords.TryGetValue(id, out word))
                word = "[NOT FOUND]";
            return word;
        }

        public List<string> GetFeatureWords()
        {
            List<string> words = new List<string>();

            foreach (int id in FeatureIds)
            {
                words.Add(_allIdsToWords[id]);
            }

            return words;
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
            int oldNextId = _nextId;

            foreach (KeyValuePair<string, int> tokenDf in tokenDocFreqs)
            {
                if (nDocs < 0 || (tokenDf.Value >= min_df && tokenDf.Value <= max_df))
                {
                    _allWordsToIds[tokenDf.Key] = _nextId;
                    _allIdsToWords[_nextId] = tokenDf.Key;
                    _allDocumentFreqs[_nextId] = tokenDf.Value;
                    _nextId++;
                }
            }

            if (oldNextId != _nextId)
            {
                // We've updated our tokens
                HasUpdatedTokens = true;
            }
        }

        /// <summary>
        /// Update vocabulary with a new IInstance. Add any tokens that aren't already in the vocab, and update frequency counts.
        /// </summary>
        /// <param name="instance">The new IInstance to add.</param>
        public void AddInstanceTokens(IInstance instance)
        {
            int oldNextId = _nextId;

            foreach (string token in instance.TokenCounts.Keys)
            {
                int id;
                if (!_allWordsToIds.TryGetValue(token, out id))
                {
                    _allWordsToIds[token] = _nextId;
                    _allIdsToWords[_nextId] = token;
                    _allDocumentFreqs[_nextId] = 1;
                    _nextId++;
                }
                else
                {
                    _allDocumentFreqs[id]++;
                }
            }

            if (oldNextId != _nextId)
            {
                // We've updated our tokens
                HasUpdatedTokens = true;
            }
        }

        /// <summary>
        /// Update vocabulary by removing an existing IInstance. Decrement frequency counts.
        /// </summary>
        /// <param name="instance"></param>
        public void RemoveInstanceTokens(IInstance instance)
        {
            bool updated = false;

            foreach (string token in instance.TokenCounts.Keys)
            {
                int id;
                if (_allWordsToIds.TryGetValue(token, out id))
                {
                    _allDocumentFreqs[id]--;
                    updated = true;
                }
            }

            if (updated)
            {
                // We've updated our tokens
                HasUpdatedTokens = true;
            }
        }

        /// <summary>
        /// Add a new token to the vocabulary, along with its document frequency.
        /// </summary>
        /// <param name="token">New token to add</param>
        /// <param name="df"></param>
        public void AddToken(string token, int df)
        {
            int oldNextId = _nextId;

            int id;
            if (!_allWordsToIds.TryGetValue(token, out id)) // Ensure this token doesn't already exist
            {
                _allWordsToIds[token] = _nextId;
                _allIdsToWords[_nextId] = token;
                _allDocumentFreqs[_nextId] = df;
                _nextId++;
            }

            if (oldNextId != _nextId)
            {
                // We've updated our tokens
                HasUpdatedTokens = true;
            }
        }

        public bool RestrictVocab(IEnumerable<IInstance> instances, IEnumerable<Label> labels, IEnumerable<Feature> forceInclude, IEnumerable<Feature> forceExclude, int size)
        {
            bool retval = false;

            if (_restriction == Restriction.HighIG)
            {
                RestrictToHighIG(instances, labels, size, forceInclude, forceExclude);
                retval = true;
                HasUpdatedTokens = false;
            }

            return retval;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            switch (_restriction)
            {
                case Restriction.None:
                    foreach (KeyValuePair<string, int> pair in _allWordsToIds)
                        sb.Append(string.Format("{0}:{1} ", pair.Key, pair.Value));
                    break;
                case Restriction.HighIG:
                    foreach (int id in _restrictedIds)
                        sb.Append(string.Format("{0}:{1} ", id, _allIdsToWords[id]));
                    break;
            }

            return sb.ToString();
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Remove a specific element from this vocabulary.
        /// </summary>
        /// <param name="id">The ID of the element to remove.</param>
        //private void RemoveElementFromRestricted(int id)
        //{
        //    _restrictedIds.Remove(id);
        //}

        /// <summary>
        /// Remove a collection of elements from this vocabulary.
        /// </summary>
        /// <param name="ids">The collection of element IDs to remove.</param>
        //private void RemoveElementsFromRestricted(IEnumerable<int> ids)
        //{
        //    foreach (int id in ids)
        //    {
        //        RemoveElementFromRestricted(id);
        //    }
        //}

        private void AddElementToRestricted(int id)
        {
            _restrictedIds.Add(id);
        }

        //private void AddElementsToRestricted(IEnumerable<int> ids)
        //{
        //    foreach (int id in ids)
        //    {
        //        AddElementToRestricted(id);
        //    }
        //}

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

            foreach (int featureId in _allIdsToWords.Keys)
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

            foreach (int featureId in _allIdsToWords.Keys)
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
                foreach (int featureId in _allIdsToWords.Keys)
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
            Dictionary<Label, int> instancesPerLabel = new Dictionary<Label, int>();

            foreach (Label label in labels)
            {
                featureAbsencesPerClass[label] = new Dictionary<int, int>();
                PrCGivenNotT[label] = new Dictionary<int, double>();
                instancesPerLabel[label] = 0;

                // How many instances are there for each label?
                foreach (IInstance instance in instances)
                {
                    if (instance.Label == label)
                    {
                        instancesPerLabel[label]++;
                    }
                }
            }

            foreach (int featureId in _allIdsToWords.Keys)
            {
                featureAbsences[featureId] = instances.Count();
                foreach (Label label in labels)
                {
                    featureAbsencesPerClass[label][featureId] = instancesPerLabel[label];
                }
            }

            foreach (IInstance instance in instances)
            {
                foreach (int featureId in instance.Features.Data.Keys)
                {
                    // Start off assuming each feature is absent from each document; decrement counter each time we see a given feature.
                    featureAbsencesPerClass[instance.Label][featureId]--;
                    featureAbsences[featureId]--;
                }
            }

            foreach (Label label in labels)
            {
                foreach (int featureId in _allIdsToWords.Keys)
                {
                    int count;
                    featureAbsencesPerClass[label].TryGetValue(featureId, out count);
                    PrCGivenNotT[label][featureId] = (double)(count + 1) / (double)(featureAbsences[featureId] + labels.Count()); // Add a smoothing term
                }
            }

            return PrCGivenNotT;
        }

        private void RestrictToHighIG(IEnumerable<IInstance> instances, IEnumerable<Label> labels, int vocabSize,
            IEnumerable<Feature> forceInclude = null, IEnumerable<Feature> forceExclude = null)
        {
            Dictionary<Label, double> PrC; // Probability of each class
            Dictionary<int, double> PrT; // Probability of each feature
            Dictionary<Label, Dictionary<int, double>> PrCGivenT;
            Dictionary<Label, Dictionary<int, double>> PrCGivenNotT;
            Dictionary<int, double> IG = new Dictionary<int, double>();

            // Compute class and feature probabilities
            Stopwatch timer = new Stopwatch();
            timer.Start();
            PrC = ComputePrC(instances, labels);
            timer.Stop();
            Console.WriteLine("Vocab ComputePrC took {0}", timer.Elapsed);
            timer.Restart();
            PrT = ComputePrT(instances, labels);
            timer.Stop();
            Console.WriteLine("Vocab ComputePrT took {0}", timer.Elapsed);
            timer.Restart();
            PrCGivenT = ComputePrCGivenT(instances, labels);
            timer.Stop();
            Console.WriteLine("Vocab ComputePrCGivenT took {0}", timer.Elapsed);
            timer.Restart();
            PrCGivenNotT = ComputePrCGivenNotT(instances, labels);
            timer.Stop();
            Console.WriteLine("Vocab ComputePrCGivenNotT took {0}", timer.Elapsed);
            timer.Restart();

            // Compute information gain for each feature
            foreach (int featureId in _allIdsToWords.Keys)
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

            timer.Stop();
            Console.WriteLine("Vocab computing IG took {0}", timer.Elapsed);

            // Find the features with the highest information gain
            List<KeyValuePair<int, double>> HighIG = IG.OrderBy(entry => entry.Value).Take(vocabSize).ToList();

            // Add these features to our "restricted" set
            _restrictedIds.Clear();
            foreach (KeyValuePair<int, double> pair in HighIG)
            {
                string word = _allIdsToWords[pair.Key];
                Feature feature = new Feature(word, Label.AnyLabel);
                // Ensure the user didn't remove this feature
                if (forceExclude == null || !forceExclude.Contains(feature))
                {
                    AddElementToRestricted(pair.Key);
                }
            }
            // Now add features the user requested
            if (forceInclude != null)
            {
                foreach (Feature f in forceInclude)
                {
                    int id;
                    if (_allWordsToIds.TryGetValue(f.Characters, out id))
                    {
                        AddElementToRestricted(id);
                    }
                    else
                        Console.Error.WriteLine("Error: could not find '{0}' in vocabulary", f.Characters);
                }
            }

            // Fire the Updated event
            this.OnUpdated(new EventArgs());
        }

        #endregion

        #region Static methods

        /// <summary>
        /// Create a new Vocabulary from a collection of instances.
        /// </summary>
        /// <param name="instances">A collection of items to use as the basis for this vocabulary.</param>
        /// <returns>A new Vocabulary object.</returns>
        public static Vocabulary CreateVocabulary(IEnumerable<IInstance> instances, IEnumerable<Label> labels, Restriction restriction, int desiredVocabSize)
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

            Vocabulary vocab = new Vocabulary(restriction);
            vocab.AddTokens(tokenDocCounts, instances.Count());

            // Update the Feature vector for each instance
            Parallel.ForEach(instances, (instance, state, index) =>
                {
                    instance.ComputeFeatureVector(vocab, false);
                });

            // Restrict our vocabulary to terms with high information gain
            if (restriction == Restriction.HighIG)
            {
                vocab.RestrictToHighIG(instances, labels, desiredVocabSize);
            }

            return vocab;
        }

        #endregion
    }
}
