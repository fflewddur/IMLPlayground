using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibIML
{
    public class Evidence
    {
        public Dictionary<int, double> Weights;
        public double ClassPr;
        public double Confidence;
        private Vocabulary Vocab;

        public Evidence(Vocabulary vocab)
        {
            Confidence = -1;
            ClassPr = -1;
            Weights = new Dictionary<int, double>();
            Vocab = vocab;
        }

        #region Override methods

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("Confidence={0:N2} ", Confidence);
            sb.AppendFormat("ClassPr={0:N2} ", ClassPr);
            foreach (KeyValuePair<int, double> pair in Weights)
            {
                sb.AppendFormat("{0}={1:N4} ", Vocab.GetWord(pair.Key), pair.Value);
            }

            return sb.ToString();
        }

        #endregion
    }
}
