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
        private List<Feature> _sourceItems;
        private List<Feature> _evidenceItems;
        private int _nClasses; // Number of classes (needed to normalize feature importance; we assume uniform class distribution)
        private int _instanceCount; // Number of training set instances with this label
        private int _totalInstanceCount; // Total number of training set instances
        private double _prClass; // Probability of a given item belonging to this class
        private double _confidence; // Classifier's confidence in this label being correct
        private double _prDocGivenClass; // Sum of feature weights * feature count for this label
        private double _pDoc; // Probability of this document
        private string _classImbalanceTooltip;
        private Label _label;

        public Evidence(Label label)
        {
            Confidence = -1;
            PrClass = -1;
            InstanceCount = -1;
            TotalInstanceCount = -1;
            SourceItems = new List<Feature>();
            EvidenceItems = new List<Feature>();
            Label = label;
        }

        #region Properties

        /// <summary>
        /// Holds the original item weights from the classifier.
        /// </summary>
        public List<Feature> SourceItems
        {
            get { return _sourceItems; }
            set { SetProperty<List<Feature>>(ref _sourceItems, value); }
        }

        /// <summary>
        /// Holds Features with updated weights to show their relative contribution to the prediction.
        /// </summary>
        public List<Feature> EvidenceItems
        {
            get { return _evidenceItems; }
            set { SetProperty<List<Feature>>(ref _evidenceItems, value); }
        }

        public int NumClasses
        {
            get { return _nClasses; }
            set { SetProperty<int>(ref _nClasses, value); }
        }

        public int InstanceCount
        {
            get { return _instanceCount; }
            set
            {
                if (SetProperty<int>(ref _instanceCount, value)) {
                    ClassImbalanceTooltip = string.Format("There are {0:N0} messages in the '{1}' folder.", InstanceCount, Label);
                }
            }
        }

        public int TotalInstanceCount
        {
            get { return _totalInstanceCount; }
            set { SetProperty<int>(ref _totalInstanceCount, value); }
        }

        public double Confidence
        {
            get { return _confidence; }
            set { SetProperty<double>(ref _confidence, value); }
        }

        public double PrClass
        {
            get { return _prClass; }
            set { SetProperty<double>(ref _prClass, value); }
        }

        public double PrDocGivenClass
        {
            get { return _prDocGivenClass; }
            set { SetProperty<double>(ref _prDocGivenClass, value); }
        }

        public double PrDoc
        {
            get { return _pDoc; }
            set { SetProperty<double>(ref _pDoc, value); }
        }

        public Label Label
        {
            get { return _label; }
            private set { SetProperty<Label>(ref _label, value); }
        }

        public string ClassImbalanceTooltip
        {
            get { return _classImbalanceTooltip; }
            private set { SetProperty<string>(ref _classImbalanceTooltip, value); }
        }

        #endregion

        //public void UpdateHeightsForEvidenceExplanation()
        //{
        //    EvidenceItems.Clear();
        //    foreach (Feature item in SourceItems) {
        //        //double weight = Math.Pow(item.UserWeight + item.SystemWeight, item.Count);
        //        //double contribution = (1 / ((double)NumClasses * SourceItems.Count)) * (weight) / PrDoc;
        //        //Console.WriteLine("Contribution for '{1}' = {0:N4} (weight={2:N4}, PrD={3:N4}, PrD|C={4:N4})", contribution, item.Characters, weight, PrDoc, PrDocGivenClass);
        //        //Feature evidenceItem = new Feature(item.Characters, item.Label, item.Count, contribution, 0);
        //        Feature evidenceItem = new Feature(item.Characters, item.Label, item.Count, item.SystemWeight, item.UserWeight);
        //        EvidenceItems.Add(evidenceItem);
        //    }
        //}

        public string GetExplanationString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("PrDocGivenClass = {0} | ", PrDocGivenClass);
            foreach (Feature item in SourceItems) {
                double contribution = 0.5 * (Math.Pow(item.UserWeight + item.SystemWeight, item.Count)) / PrDoc;
                sb.AppendFormat("{0} ({1}) = {2} ({3} + {4})^{5} ", item.Characters, item.Label, contribution, item.UserWeight, item.SystemWeight, item.Count);
            }

            return sb.ToString();
        }

        #region Override methods

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("Confidence={0:N2} ", Confidence);
            sb.AppendFormat("ClassPr={0:N2} ", PrClass);
            sb.AppendFormat("PrDoc={0} ", PrDoc);
            foreach (Feature item in SourceItems) {
                sb.AppendFormat("{0}={4:N4} ({1}*({2:N4}+{3:N4})) ", item.Characters, item.Count, item.SystemWeight, item.UserWeight, 
                    item.Count * (item.SystemWeight + item.UserWeight));
            }

            return sb.ToString();
        }

        #endregion
    }
}
