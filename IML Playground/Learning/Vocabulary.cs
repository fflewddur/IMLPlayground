using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IML_Playground.Learning
{
    class Vocabulary
    {
        private int _nextId;
        private Dictionary<string, int> _wordsToIds;
        private Dictionary<int, string> _idsToWords;

        const int MIN_DF = 3; // Minimum document frequency; tokens below this threshold will not be added to our vocabulary.

        public Vocabulary()
        {
            _nextId = 1;
            _wordsToIds = new Dictionary<string, int>();
            _idsToWords = new Dictionary<int, string>();
        }

        public void AddTokens(IEnumerable<KeyValuePair<string, int>> tokenDocFreqs)
        {
            foreach (KeyValuePair<string, int> tokenDf in tokenDocFreqs)
            {
                if (tokenDf.Value >= MIN_DF)
                {
                    _wordsToIds[tokenDf.Key] = _nextId;
                    _idsToWords[_nextId] = tokenDf.Key;
                    _nextId++;
                }
            }
        }

        public int Count
        {
            get { return _wordsToIds.Count; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, int> pair in _wordsToIds)
                sb.Append(string.Format("{0}:{1} ", pair.Key, pair.Value));
            return sb.ToString();
        }
    }
}
