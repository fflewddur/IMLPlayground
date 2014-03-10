using LibIML.Instances;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibIML
{
    public class Evidence
    {
        public List<EvidenceItem> Items;
        public double ClassPr;
        public double Confidence;

        public Evidence()
        {
            Confidence = -1;
            ClassPr = -1;
            Items = new List<EvidenceItem>();
        }

        #region Override methods

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("Confidence={0:N2} ", Confidence);
            sb.AppendFormat("ClassPr={0:N2} ", ClassPr);
            foreach (EvidenceItem item in Items) {
                sb.AppendFormat("{0}={1}*({2:N4}+{3:N4}) ", item.Word, item.Count, item.SystemWeight, item.UserWeight);
            }

            return sb.ToString();
        }

        #endregion
    }
}
