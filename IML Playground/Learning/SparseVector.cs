using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IML_Playground.Learning
{
    [Serializable]
    class SparseVector
    {
        private Dictionary<int, double> _data;

        public SparseVector()
        {
            _data = new Dictionary<int, double>();
        }

        public SparseVector(SparseVector toCopy) : this()
        {
            foreach (KeyValuePair<int, double> pair in toCopy.Data)
                this.Set(pair.Key, pair.Value);
        }

        public void Set(int key, double value)
        {
            _data[key] = value;
        }

        public double Get(int key)
        {
            double value;

            if (!TryGet(key, out value))
                throw new KeyNotFoundException(key.ToString());

            return value;
        }

        public bool TryGet(int key, out double value)
        {
            if (_data.ContainsKey(key))
            {
                value = _data[key];
                return true;
            }
            else
            {
                value = default(double);
                return false;
            }
        }

        public bool Contains(int key)
        {
            return _data.ContainsKey(key);
        }

        private Dictionary<int, double>.KeyCollection Keys { get { return _data.Keys; } }
        private Dictionary<int, double>.ValueCollection Values { get { return _data.Values; } }
        private Dictionary<int, double> Data { get { return _data; } }

        // Use @vocab to print out the value associated with each key, but with the output of each key replaced by vocab[key]
        public string ToPrettyString(Vocabulary vocab)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Items: {0} Values: ", _data.Count);

            var sorted = (from entry in _data orderby entry.Value descending select entry);
            foreach (KeyValuePair<int, double> pair in sorted)
            {
                sb.AppendFormat("{0} = {1:0.00}, ", vocab.GetWord(pair.Key), pair.Value);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Compute the cosine similarity between two vectors.
        /// </summary>
        /// <param name="other">Vector to compare against.</param>
        /// <returns>Cosine similarity.</returns>
        public double CosineSimilarity(SparseVector other)
        {
            double similarity = 0;

            // Compute the dot product
            double sum = 0;
            foreach (KeyValuePair<int, double> pair in _data)
            {
                double otherVal;
                if (other.TryGet(pair.Key, out otherVal))
                    sum += pair.Value * otherVal;
            }

            // Compute the magnitude of each vector
            double mag1 = 0;
            double mag2 = 0;
            foreach (double value in _data.Values)
                mag1 += Math.Pow(value, 2);
            mag1 = Math.Sqrt(mag1);
            foreach (double value in other.Values)
                mag2 += Math.Pow(value, 2);
            mag2 = Math.Sqrt(mag2);

            // Similarity = dot product over magnitude, or 0 if magnitude is 0
            double magnitude = mag1 * mag2;
            if (magnitude != 0)
                similarity = sum / (mag1 * mag2);

            return similarity;
        }

        /// <summary>
        /// Merge a SparseVector into this object. If a feature exists in both vectors, sum the values together and log-normalize it.
        /// </summary>
        /// <param name="v">SparseVector to combine with this object.</param>
        public void MergeWith(SparseVector v)
        {
            foreach (KeyValuePair<int, double> pair in v.Data)
            {
                if (this.Contains(pair.Key))
                {
                    double value = Math.Log(this.Get(pair.Key) + pair.Value);
                    this.Set(pair.Key, value);
                }
                else
                    this.Set(pair.Key, pair.Value);
            }
        }
    }
}
