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

        const double MIN_DF_PERCENT = 0.01; // Tokens must appear in at least 5% of documents
        const double MAX_DF_PERCENT = 0.90; // Tokens must not appear in more than 90% of documents

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
        public void AddTokens(IEnumerable<KeyValuePair<string, int>> tokenDocFreqs, int nDocs)
        {
            int min_df = (int)(MIN_DF_PERCENT * nDocs);
            int max_df = (int)(MAX_DF_PERCENT * nDocs);

            foreach (KeyValuePair<string, int> tokenDf in tokenDocFreqs)
            {
                if (tokenDf.Value >= min_df && tokenDf.Value <= max_df)
                {
                    _wordsToIds[tokenDf.Key] = _nextId;
                    _idsToWords[_nextId] = tokenDf.Key;
                    _documentFreqs[_nextId] = tokenDf.Value;
                    _nextId++;
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, int> pair in _wordsToIds)
                sb.Append(string.Format("{0}:{1} ", pair.Key, pair.Value));
            return sb.ToString();
        }

        #endregion
    }
}
