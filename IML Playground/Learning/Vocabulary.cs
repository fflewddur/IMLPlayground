using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IML_Playground.Learning
{
    [Serializable]
    class Vocabulary
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
        public void AddTokens(IEnumerable<KeyValuePair<string, int>> tokenDocFreqs, int nDocs = -1, double min_df_percent = MIN_DF_PERCENT, double max_df_percent = MAX_DF_PERCENT)
        {
            int min_df = (int)(min_df_percent * nDocs);
            int max_df = (int)(max_df_percent * nDocs);

            foreach (KeyValuePair<string, int> tokenDf in tokenDocFreqs)
            {
                if (nDocs > 0 && tokenDf.Value >= min_df && tokenDf.Value <= max_df)
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
        public void RestrictToInstances(IInstances instances)
        {
            HashSet<int> inInstances = new HashSet<int>(); // Track the feature IDs in instances
            HashSet<int> notInInstances = new HashSet<int>(); // Track the feature IDs to remove

            // Build a set of features in instances
            foreach (Instance instance in instances.ToInstances())
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

        /// <summary>
        /// Build a new Vocabulary that is restricted to the given collection of IDs.
        /// </summary>
        /// <param name="ids">The collection of IDs to restrict the new vocabulary to.</param>
        /// <returns>The new vocabulary object.</returns>
        public Vocabulary GetSubset(IEnumerable<int> ids)
        {
            Vocabulary v = new Vocabulary();
            Dictionary<string, int> tokens = new Dictionary<string,int>();

            foreach (int id in ids)
            {
                tokens.Add(_idsToWords[id],  _documentFreqs[_nextId]);
            }

            v.AddTokens(tokens);

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
    }
}
