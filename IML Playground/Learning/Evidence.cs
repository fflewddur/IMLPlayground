using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IML_Playground.Learning
{
    class Evidence
    {
        public Dictionary<int, double> Weights;
        public double Confidence;
        private Vocabulary Vocab;

        public Evidence(Vocabulary vocab)
        {
            Confidence = -1;
            Weights = new Dictionary<int, double>();
            Vocab = vocab;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("Confidence={0:0.000} ", Confidence);
            foreach (KeyValuePair<int, double> pair in Weights)
            {
                sb.AppendFormat("{0}={1:0.000} ", Vocab.GetWord(pair.Key), pair.Value);
            }

            return sb.ToString();
        }
    }
}
