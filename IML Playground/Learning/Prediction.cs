using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IML_Playground.Learning
{
    class Prediction
    {
        public Prediction()
        {
            EvidencePerClass = new Dictionary<Label, Evidence>();
        }

        #region Properties

        public Label Label;
        public Dictionary<Label, Evidence> EvidencePerClass;

        #endregion

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (KeyValuePair<Label, Evidence> pair in EvidencePerClass)
            {
                sb.AppendFormat("{0}: {1}\n", pair.Key.UserLabel, pair.Value);
            }

            return sb.ToString();
        }
    }
}
