using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibIML
{
    public class Prediction : ViewModelBase
    {
        private Label _label;
        private double _confidence;
        private Dictionary<Label, Evidence> _evidencePerClass;
        private int _importantWordUniques;
        private int _importantWordTotal;
        private string _importantWordDesc; // Describe how many important words were used for this prediction
        private string _classPrDesc; // Describe how class probability influenced this prediction
        private string _featurePrDesc; // Describe how feature probability influenced this prediction

        public Prediction()
        {
            EvidencePerClass = new Dictionary<Label, Evidence>();
            UpdateImportantWordDesc();
        }

        #region Properties

        public Label Label
        {
            get { return _label; }
            set { SetProperty<Label>(ref _label, value); }
        }

        public double Confidence
        {
            get { return _confidence; }
            set { SetProperty<double>(ref _confidence, value); }
        }

        public Dictionary<Label, Evidence> EvidencePerClass
        {
            get { return _evidencePerClass; }
            set { SetProperty<Dictionary<Label, Evidence>>(ref _evidencePerClass, value); }
        }

        public int ImportantWordUniques
        {
            get { return _importantWordUniques; }
            set
            {
                if (SetProperty<int>(ref _importantWordUniques, value)) {
                    UpdateImportantWordDesc();
                }
            }
        }

        public int ImportantWordTotal
        {
            get { return _importantWordTotal; }
            set
            {
                if (SetProperty<int>(ref _importantWordTotal, value)) {
                    UpdateImportantWordDesc();
                }
            }
        }

        public string ImportantWordDesc
        {
            get { return _importantWordDesc; }
            private set { SetProperty<string>(ref _importantWordDesc, value); }
        }

        public string ClassPrDesc
        {
            get { return _classPrDesc; }
            private set { SetProperty<string>(ref _classPrDesc, value); }
        }

        public string FeaturePrDesc
        {
            get { return _featurePrDesc; }
            private set { SetProperty<string>(ref _featurePrDesc, value);}
        }

        #endregion

        /// <summary>
        /// I'd rather do this in the View, but I can't get inside the Evidence dictionary from inside a multibinding, so now this is happening.
        /// </summary>
        public void UpdatePrDescriptions()
        {
            int smallest = int.MaxValue, largest = int.MinValue;
            Label smallestLabel = null;
            Label largestLabel = null;
            foreach (KeyValuePair<Label, Evidence> pair in _evidencePerClass) {
                if (pair.Value.ClassCount <= smallest) {
                    smallest = pair.Value.ClassCount;
                    smallestLabel = pair.Key;
                }
                if (pair.Value.ClassCount > largest) {
                    largest = pair.Value.ClassCount;
                    largestLabel = pair.Key;
                }
            }

            // Build ClassPrDesc
            StringBuilder sb = new StringBuilder();
            string comparison = "more likely than";
            if (smallest == largest) {
                comparison = "equally likely as";
            }
            sb.AppendFormat("There are {0} messages in the {1} folder vs. {2} messages in the {3} folder, so the computer thinks {1} messages are {4} {3} messages.",
                largest, largestLabel.ToString(), smallest, smallestLabel.ToString(), comparison);
            ClassPrDesc = sb.ToString();

            // Build FeaturePrDesc
            sb.Clear();
            FeaturePrDesc = sb.ToString();
        }

        private void UpdateImportantWordDesc()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("There ");
            if (ImportantWordUniques == 1) {
                sb.Append("is 1 important word");
            } else {
                sb.AppendFormat("are {0} important words", ImportantWordUniques);
            }
            sb.Append(" in this message");
            if (ImportantWordTotal > 0) {
                sb.Append(". ");
                if (ImportantWordTotal == 1) {
                    sb.Append(" It appears a total of 1 time.");
                } else {
                    if (ImportantWordUniques == 1) {
                        sb.AppendFormat(" It appears a total of {0} times.", ImportantWordTotal);
                    } else {
                        sb.AppendFormat(" They appear a total of {0} times.", ImportantWordTotal);
                    }
                }
            } else {
                sb.Append(", so they have no impact on the predicted topic.");
            }

            ImportantWordDesc = sb.ToString();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("Predicted label is {0}\n", Label);
            foreach (KeyValuePair<Label, Evidence> pair in EvidencePerClass)
            {
                sb.AppendFormat("{0}: {1}\n", pair.Key.UserLabel, pair.Value);
            }

            return sb.ToString();
        }
    }
}
