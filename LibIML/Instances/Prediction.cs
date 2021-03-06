﻿using LibIML.Features;
using LibIML.Instances;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace LibIML
{
    public class Prediction : ViewModelBase
    {
        public static readonly double MIN_HEIGHT = 4.0;
        public static readonly double MAX_HEIGHT = 12.0; 

        private Label _label;
        private double _confidence;
        private Dictionary<Label, Evidence> _evidencePerClass;
        private List<EvidenceItem> _evidenceItems;
        private int _importantWordUniques;
        private int _importantWordTotal;
        private string _importantWordDesc; // Describe how many important words were used for this prediction
        private string _classPrHeader;
        private string _classPrDesc; // Describe how class probability influenced this prediction
        private string _featurePrHeader;
        private string _featurePrDesc; // Describe how feature probability influenced this prediction
        private string _confidenceHeader;
        private System.Windows.Point _confidencePiePoint; // Where should the smaller confidence arc end?
        private bool _confidencePieLarge; // Is the pie slice more or less than 50%?
        private double _confidencePieCircle; // The confidence value of the pie background
        private double _confidencePieSlice; // The confidence value of the pie slice
        private string _confidenceDesc;

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
            set {
                // Never let confidence go round up to 100%
                double newVal = value;
                if (newVal > .994) {
                    newVal = .994;
                }
                SetProperty<double>(ref _confidence, newVal);
            }
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

        public string ClassPrHeader
        {
            get { return _classPrHeader; }
            private set { SetProperty<string>(ref _classPrHeader, value); }
        }

        public string ClassPrDesc
        {
            get { return _classPrDesc; }
            private set { SetProperty<string>(ref _classPrDesc, value); }
        }

        public string FeaturePrHeader
        {
            get { return _featurePrHeader; }
            private set { SetProperty<string>(ref _featurePrHeader, value); }
        }

        public string FeaturePrDesc
        {
            get { return _featurePrDesc; }
            private set { SetProperty<string>(ref _featurePrDesc, value);}
        }

        public string ConfidenceHeader
        {
            get { return _confidenceHeader; }
            private set { SetProperty<string>(ref _confidenceHeader, value); }
        }

        public string ConfidenceDesc
        {
            get { return _confidenceDesc; }
            private set { SetProperty<string>(ref _confidenceDesc, value); }
        }

        public Point ConfidencePiePoint
        {
            get { return _confidencePiePoint; }
            private set { SetProperty<Point>(ref _confidencePiePoint, value); }
        }

        public bool ConfidencePieLarge
        {
            get { return _confidencePieLarge; }
            private set { SetProperty<bool>(ref _confidencePieLarge, value); }
        }

        public double ConfidencePieSlice
        {
            get { return _confidencePieSlice; }
            private set { SetProperty<double>(ref _confidencePieSlice, value); }
        }

        public double ConfidencePieCircle
        {
            get { return _confidencePieCircle; }
            private set { SetProperty<double>(ref _confidencePieCircle, value); }
        }

        public List<EvidenceItem> EvidenceItems
        {
            get { return _evidenceItems; }
            set { SetProperty<List<EvidenceItem>>(ref _evidenceItems, value); }
        }

        #endregion

        /// <summary>
        /// I'd rather do this in the View, but I can't get inside the Evidence dictionary from inside a multibinding, so now this is happening.
        /// </summary>
        public void UpdatePrDescriptions(IReadOnlyList<Label> labels)
        {
            // Figure out values for ClassPrDesc and FeaturePrDesc
            int smallestCount = int.MaxValue, largestCount = int.MinValue;
            double smallestWeight = double.MaxValue, largestWeight = double.MinValue, 
                smallestConf = double.MaxValue, largestConf = double.MinValue;
            Label smallestCountLabel = null, smallestWeightLabel = null, smallestConfLabel = null, 
                largestCountLabel = null, largestWeightLabel = null, largestConfLabel = null;
            bool hasFeatures = false;
            bool hasClassImbalance = false;

            foreach (KeyValuePair<Label, Evidence> pair in _evidencePerClass) {
                if (!hasFeatures) {
                    hasFeatures = pair.Value.HasFeatures;
                }
                if (pair.Value.InstanceCount <= smallestCount) {
                    smallestCount = pair.Value.InstanceCount;
                    smallestCountLabel = pair.Key;
                }
                if (pair.Value.InstanceCount > largestCount) {
                    largestCount = pair.Value.InstanceCount;
                    largestCountLabel = pair.Key;
                }
                if (pair.Value.PrDocGivenClass <= smallestWeight) {
                    smallestWeight = pair.Value.PrDocGivenClass;
                    smallestWeightLabel = pair.Key;
                }
                if (pair.Value.PrDocGivenClass > largestWeight) {
                    largestWeight = pair.Value.PrDocGivenClass;
                    largestWeightLabel = pair.Key;
                }
                if (pair.Value.Confidence <= smallestConf) {
                    smallestConf = pair.Value.Confidence;
                    smallestConfLabel = pair.Key;
                }
                if (pair.Value.Confidence > largestConf) {
                    largestConf = pair.Value.Confidence;
                    largestConfLabel = pair.Key;
                }
            }

            StringBuilder sbHeader = new StringBuilder();
            StringBuilder sbDesc = new StringBuilder();

            // Build ClassPrHeader and ClassPrDesc
            sbHeader.Clear();
            if (smallestCount == largestCount) {
                hasClassImbalance = false;
                sbHeader.AppendFormat("The {0} folder has as many messages as the {1} folder", largestCountLabel, smallestCountLabel);
                if (Math.Round(smallestWeight, 14) == Math.Round(largestWeight, 14)) {
                    sbDesc.AppendFormat("So the computer think each Unknown message is equally likely to be about {0} or {1}.",
                       largestCountLabel, smallestCountLabel);
                } else {
                    sbDesc.AppendFormat("When folder sizes are equal, only Important Words will be used to predict this message's topic.");
                }
            } else {
                hasClassImbalance = true;
                sbHeader.AppendFormat("The {0} folder has more messages than the {1} folder", largestCountLabel, smallestCountLabel);
                double ratio = Math.Round(largestCount / (double)smallestCount, 1);
                sbDesc.AppendFormat("The difference makes the computer thinks each Unknown message is {0:N1} times more likely to be about {1} than {2}.",
                    ratio, largestCountLabel.ToString(), smallestCountLabel.ToString());
            }
            ClassPrHeader = sbHeader.ToString();
            ClassPrDesc = sbDesc.ToString();

            // Build ClassPrDesc
            //sbDesc.Clear();
            //if (smallestCount == largestCount) {
            //    sbDesc.AppendFormat("There are an equal number of messages in each folder, so the computer thinks each Unknown message is equally likely to be about {0} or {1}.",
            //        largestCountLabel, smallestCountLabel);
            //} else {
            //    double ratio = Math.Round(largestCount / (double)smallestCount, 1);
            //    sbDesc.AppendFormat("There are {0:N1} times more messages in the {1} folder than the {2} folder, so the computer thinks each Unknonw message is {0:N1} times more likely to be about {1} than {2}.",
            //        ratio, largestCountLabel.ToString(), smallestCountLabel.ToString());
            //}
            //ClassPrDesc = sbDesc.ToString();

            // Build FeaturePrHeader
            // Build FeaturePrDesc
            sbHeader.Clear();
            sbDesc.Clear();
            if (Math.Round(smallestWeight, 14) == Math.Round(largestWeight, 14)) {
                if (!hasFeatures) {
                    sbHeader.Append("No important words occur in this message");
                    sbDesc.AppendFormat("Without important words, only Folder Size will be used to predict this message's topic.",
                        largestWeightLabel, smallestWeightLabel);
                } else {
                    sbHeader.AppendFormat("This words in this message are equally important to {0} and {1}", largestWeightLabel, smallestWeightLabel);
                    sbDesc.AppendFormat("Because the size of the {0} words equals the size of the {1} words, only Folder Size will be used to predict this message's topic.",
                        largestWeightLabel, smallestWeightLabel);
                }
            } else {
                sbHeader.AppendFormat("This message has more important words about {0} than about {1}", largestWeightLabel, smallestWeightLabel);
                double ratio = Math.Round(largestWeight / smallestWeight, 1);
                string ratioDesc;
                if (ratio > 1000) {
                    ratioDesc = "over 1,000";
                } else {
                    if (ratio > 100) {
                        int ratioInt = (int)(ratio / 10);
                        ratioDesc = string.Format("{0:N0}", ratioInt * 10);
                    } else if (ratio > 10) {
                        ratioDesc = string.Format("{0:N0}", ratio);
                    } else {
                        ratioDesc = string.Format("{0:N1}", ratio);
                    }
                }

                sbDesc.AppendFormat("The difference makes the computer think this message is {1} times more likely to be about {0} than {2}.",
                    largestWeightLabel, ratioDesc, smallestWeightLabel);
            }
            FeaturePrHeader = sbHeader.ToString();
            FeaturePrDesc = sbDesc.ToString();

            // Also update data for our confidence pie chart
            foreach (KeyValuePair<Label, Evidence> pair in _evidencePerClass) {
                // FIXME Yeah, this is a hack and won't work with different labels.
                if (pair.Key.UserLabel == labels[1].UserLabel) {
                    // Subtract 90 to rotate the graph so that it starts at (0, 1) instead of (1, 0)
                    double angle = (360 * pair.Value.Confidence) - 90;
                    if (angle < 0) {
                        angle += 360; // Wrap around if we fell into negative territory
                    }
                    ConfidencePieLarge = (angle > 90) && (angle < 270);
                    angle *= Math.PI / 180; // convert to radians
                    ConfidencePiePoint = new Point(50 + Math.Cos(angle) * 50, 50 + Math.Sin(angle) * 50);
                    ConfidencePieSlice = pair.Value.Confidence;
                } else if (pair.Key.UserLabel == labels[0].UserLabel) {
                    ConfidencePieCircle = pair.Value.Confidence;
                }
            }

            sbHeader.Clear();
            sbHeader.AppendFormat("{0:0%} probability this message is about {1}", _confidence, _label);
            ConfidenceHeader = sbHeader.ToString();

            sbDesc.Clear();
            if (hasFeatures) {
                if (!hasClassImbalance) {
                    sbDesc.Append("Thus, the 'Important words' make the computer think this message is ");
                } else {
                    sbDesc.Append("Combining 'Important words' and 'Folder size' makes the computer think this message is ");
                }
            } else {
                sbDesc.Append("Thus, 'Folder size' makes the computer think this message is ");
            }
            if (Math.Round(smallestConf, 7) == Math.Round(largestConf, 7)) {
                sbDesc.AppendFormat("equally likely to be about {0} or {1} (ties are broken in favor of {2}).", largestConfLabel, smallestConfLabel, this.Label);
            } else {
                double oddsRatio = Math.Round((1 / smallestConf) - 1, 1);
                string ratioDesc;
                if (oddsRatio > 1000) {
                    ratioDesc = "over 1,000";
                } else {
                    if (oddsRatio > 100) {
                        int ratioInt = (int)(oddsRatio / 10);
                        ratioDesc = string.Format("{0:N0}", ratioInt * 10);
                    } else if (oddsRatio > 10) {
                        ratioDesc = string.Format("{0:N0}", oddsRatio);
                    } else {
                        ratioDesc = string.Format("{0:N1}", oddsRatio);
                    }
                }
                sbDesc.AppendFormat("{0} times more likely to be about {1} than about {2}.", ratioDesc, largestConfLabel, smallestConfLabel);
            }
            ConfidenceDesc = sbDesc.ToString();
        }

        private void UpdateImportantWordDesc()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("There ");
            if (ImportantWordUniques == 1) {
                sb.Append("is 1 important word");
            } else if (_importantWordUniques == 0) {
                sb.Append("are no important words");
            } else {
                sb.AppendFormat("are {0} important words", ImportantWordUniques);
            }
            sb.Append(" in this message");
            if (ImportantWordTotal > 0) {
                sb.Append(": ");
/*                if (ImportantWordTotal == 1) {
                    sb.Append(" It appears a total of 1 time.");
                } else {
                    if (ImportantWordUniques == 1) {
                        sb.AppendFormat(" It appears a total of {0} times.", ImportantWordTotal);
                    } else {
                        sb.AppendFormat(" They appear a total of {0} times.", ImportantWordTotal);
                    }
                }
 */
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
