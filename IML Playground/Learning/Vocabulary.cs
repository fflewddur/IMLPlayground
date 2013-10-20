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

        const int MIN_DF = 3; // Minimum document frequency; tokens below this threshold will not be added to our vocabulary.

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

        public int GetWordId(string word)
        {
            int id;
            if (!_wordsToIds.TryGetValue(word, out id))
                id = -1;
            return id;
        }

        public string GetWord(int id)
        {
            string word;
            if (!_idsToWords.TryGetValue(id, out word))
                word = "[NOT FOUND]";
            return word;
        }

        public void AddTokens(IEnumerable<KeyValuePair<string, int>> tokenDocFreqs)
        {
            foreach (KeyValuePair<string, int> tokenDf in tokenDocFreqs)
            {
                if (tokenDf.Value >= MIN_DF)
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
