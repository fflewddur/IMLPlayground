using LibIML.Instances;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibIML
{
    public class Evidence : ViewModelBase
    {
        public List<Feature> _items;
        public double _classPr;
        public double _confidence;

        public Evidence()
        {
            Confidence = -1;
            ClassPr = -1;
            Items = new List<Feature>();
        }

        #region Properties

        public List<Feature> Items
        {
            get { return _items; }
            set { SetProperty<List<Feature>>(ref _items, value); }
        }

        public double ClassPr
        {
            get { return _classPr; }
            set { SetProperty<double>(ref _classPr, value); }
        }

        public double Confidence
        {
            get { return _confidence; }
            set { SetProperty<double>(ref _confidence, value); }
        }

        #endregion

        #region Override methods

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("Confidence={0:N2} ", Confidence);
            sb.AppendFormat("ClassPr={0:N2} ", ClassPr);
            foreach (Feature item in Items) {
                sb.AppendFormat("{0}={1}*({2:N4}+{3:N4}) ", item.Characters, item.Count, item.SystemWeight, item.UserWeight);
            }

            return sb.ToString();
        }

        #endregion
    }
}
