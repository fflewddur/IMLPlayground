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
            // Figure out values for ClassPrDesc and FeaturePrDesc
            int smallestCount = int.MaxValue, largestCount = int.MinValue;
            double smallestWeight = double.MaxValue, largestWeight = double.MinValue;
            Label smallestCountLabel = null, smallestWeightLabel = null, largestCountLabel = null, largestWeightLabel = null;
            foreach (KeyValuePair<Label, Evidence> pair in _evidencePerClass) {
                if (pair.Value.ClassCount <= smallestCount) {
                    smallestCount = pair.Value.ClassCount;
                    smallestCountLabel = pair.Key;
                }
                if (pair.Value.ClassCount > largestCount) {
                    largestCount = pair.Value.ClassCount;
                    largestCountLabel = pair.Key;
                }
                if (pair.Value.FeatureWeight <= smallestWeight) {
                    smallestWeight = pair.Value.FeatureWeight;
                    smallestWeightLabel = pair.Key;
                }
                if (pair.Value.FeatureWeight > largestWeight) {
                    largestWeight = pair.Value.FeatureWeight;
                    largestWeightLabel = pair.Key;
                }
            }

            // Build ClassPrDesc
            StringBuilder sb = new StringBuilder();
            if (smallestCount == largestCount) {
                sb.AppendFormat("There are an equal number of messages in each folder, so the computer thinks each Unknown message is equally likely to be about {0} or {1}.",
                    largestCountLabel, smallestCountLabel);
            } else {
                double ratio = Math.Round(largestCount / (double)smallestCount, 1);
                sb.AppendFormat("There are {0:N1} times more messages in the {1} folder than the {2} folder, so the computer thinks each Unknonw message is {0:N1} times more likely to be about {1} than {2}.",
                    ratio, largestCountLabel.ToString(), smallestCountLabel.ToString());
            }
            ClassPrDesc = sb.ToString();

            // Build FeaturePrDesc
            sb.Clear();
            if (Math.Round(smallestWeight, 14) == Math.Round(largestWeight, 14)) {
                sb.AppendFormat("The area of the {0} bars equals the area of the {1} bars, so the computer thinks this message is equally likely to be either. ({2} vs {3})",
                    largestWeightLabel, smallestWeightLabel, largestWeight, smallestWeight);
            } else {
                double ratio = Math.Round(largestWeight / smallestWeight, 1);
                sb.AppendFormat("The area of the {0} bars is {1:N1} times larger than the {2} bars, so the computer thinks this message is {1:N1} times more likely to be about {0} than {2}. ({3} vs {4})",
                    largestWeightLabel, ratio, smallestWeightLabel, largestWeight, smallestWeight);
            }
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
