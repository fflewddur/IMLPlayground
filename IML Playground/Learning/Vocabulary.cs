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

        public Vocabulary()
        {
            _nextId = 1;
            _wordsToIds = new Dictionary<string, int>();
            _idsToWords = new Dictionary<int, string>();
        }

        public void AddTokens(IEnumerable<string> tokens)
        {
            foreach (string token in tokens)
            {
                _wordsToIds[token] = _nextId;
                _idsToWords[_nextId] = token;
                _nextId++;
            }
        }
    }
}
