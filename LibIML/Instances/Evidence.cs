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
        private List<Feature> _items;
        private int _classCount; // Number of training set instances with this label
        private int _totalClassCount; // Total number of training set instances
        private double _classPr; // Probability of a given item belonging to this class
        private double _confidence; // Classifier's confidence in this label being correct
        private double _featureWeight; // Sum of feature weights * feature count for this label
        private double _pDoc; // Probability of this document

        public Evidence()
        {
            Confidence = -1;
            ClassPr = -1;
            ClassCount = -1;
            TotalClassCount = -1;
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

        public int ClassCount
        {
            get { return _classCount; }
            set { SetProperty<int>(ref _classCount, value); }
        }

        public int TotalClassCount
        {
            get { return _totalClassCount; }
            set { SetProperty<int>(ref _totalClassCount, value); }
        }

        public double Confidence
        {
            get { return _confidence; }
            set { SetProperty<double>(ref _confidence, value); }
        }

        public double FeatureWeight
        {
            get { return _featureWeight; }
            set { SetProperty<double>(ref _featureWeight, value); }
        }

        public double PrDoc
        {
            get { return _pDoc; }
            set { SetProperty<double>(ref _pDoc, value); }
        }

        #endregion

        #region Override methods

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("Confidence={0:N2} ", Confidence);
            sb.AppendFormat("ClassPr={0:N2} ", ClassPr);
            sb.AppendFormat("PrDoc={0} ", PrDoc);
            foreach (Feature item in Items) {
                sb.AppendFormat("{0}={4:N4} ({1}*({2:N4}+{3:N4})) ", item.Characters, item.Count, item.SystemWeight, item.UserWeight, 
                    item.Count * (item.SystemWeight + item.UserWeight));
            }

            return sb.ToString();
        }

        #endregion
    }
}
