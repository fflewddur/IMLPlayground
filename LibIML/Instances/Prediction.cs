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
        private string _classPrDesc; // Describe how class probability influenced this prediction
        private string _featurePrDesc; // Describe how feature probability influenced this prediction
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

        public string ConfidenceDesc
        {
            get { return _confidenceDesc; }
            private set { SetProperty<string>(ref _confidenceDesc, value); }
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
        public void UpdatePrDescriptions()
        {
            // Figure out values for ClassPrDesc and FeaturePrDesc
            int smallestCount = int.MaxValue, largestCount = int.MinValue;
            double smallestWeight = double.MaxValue, largestWeight = double.MinValue, 
                smallestConf = double.MaxValue, largestConf = double.MinValue;
            Label smallestCountLabel = null, smallestWeightLabel = null, smallestConfLabel = null, 
                largestCountLabel = null, largestWeightLabel = null, largestConfLabel = null;
            foreach (KeyValuePair<Label, Evidence> pair in _evidencePerClass) {
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
                sb.AppendFormat("Because the size of the {0} words equals the size of the {1} words, the computer thinks this message is equally likely to be about either.",
                    largestWeightLabel, smallestWeightLabel);
            } else {
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

                sb.AppendFormat("Because the {0} words ({3}) are larger than the {2} words ({4}), the computer thinks this message is more likely to be about {0} than {2}.",
                    largestWeightLabel, ratioDesc, smallestWeightLabel, largestWeightLabel.ColorDesc, smallestWeightLabel.ColorDesc);
            }
            FeaturePrDesc = sb.ToString();

            // Also update data for our confidence pie chart
            foreach (KeyValuePair<Label, Evidence> pair in _evidencePerClass) {
                // FIXME Yeah, this is a hack and won't work with different labels.
                if (pair.Key.UserLabel == "Baseball") {
                    // Subtract 90 to rotate the graph so that it starts at (0, 1) instead of (1, 0)
                    double angle = (360 * pair.Value.Confidence) - 90;
                    if (angle < 0) {
                        angle += 360; // Wrap around if we fell into negative territory
                    }
                    ConfidencePieLarge = (angle > 90) && (angle < 270);
                    angle *= Math.PI / 180; // convert to radians
                    ConfidencePiePoint = new Point(50 + Math.Cos(angle) * 50, 50 + Math.Sin(angle) * 50);
                    ConfidencePieSlice = pair.Value.Confidence;
                } else if (pair.Key.UserLabel == "Hockey") {
                    ConfidencePieCircle = pair.Value.Confidence;
                }
            }

            sb.Clear();
            sb.Append("Combining parts one and two, the computer thinks this message is ");
            if (Math.Round(smallestConf, 7) == Math.Round(largestConf, 7)) {
                sb.AppendFormat("equally likely to be about {0} or {1} (ties are broken in favor of {2}).", largestConfLabel, smallestConfLabel, this.Label);
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
                sb.AppendFormat("{0} times more likely to be about {1} than about {2}.", ratioDesc, largestConfLabel, smallestConfLabel);
            }
            ConfidenceDesc = sb.ToString();
        }

        public void UpdateEvidenceGraphData()
        {
            // Get the sum of weights for all labels.
            double weightSum = 0;
            Dictionary<Label, double> perLabelWeightSum = new Dictionary<Label, double>();
            Dictionary<Label, double> perLabelHeight = new Dictionary<Label, double>();
            foreach (KeyValuePair<Label, Evidence> pair in _evidencePerClass) {
                double labelWeight = 0;
                foreach (Feature f in pair.Value.SourceItems) {
                    // Multiply by -1 to reverse the sign; log(<1) will always be negative.
                    double featureWeight = -1 * (Math.Log(f.UserWeight + f.SystemWeight) * f.Count);
                    f.UserHeight = featureWeight; // Store this here because we need it later in this method.
                    labelWeight += featureWeight;
                }
                perLabelWeightSum[pair.Key] = labelWeight;
                weightSum += labelWeight;
            }
            //Console.WriteLine("weightSum={0}", weightSum);
            // Use Confidence to determine the height ratio between different labels
            foreach (KeyValuePair<Label, Evidence> pair in _evidencePerClass) {
                //Console.WriteLine("Confidence for {0} = {1:N6}", pair.Key, pair.Value.Confidence);
                perLabelHeight[pair.Key] = weightSum * pair.Value.Confidence;
            }

            // Now figure out the height ratio for different bars; store the results in EvidenceItems.
            double highestHeight = double.MinValue;
            foreach (KeyValuePair<Label, Evidence> pair in _evidencePerClass) {
                pair.Value.EvidenceItems.Clear();
                foreach (Feature f in pair.Value.SourceItems) {
                    // 1 - % because the smaller the ln(weight), the more important the feature
                    //double featurePercent = 1 - ((f.UserHeight) / perLabelWeightSum[pair.Key]);
                    double featurePercent = ((f.UserHeight) / perLabelWeightSum[pair.Key]);
                    if (featurePercent <= 0) {
                        featurePercent = 1; // If there's only one feature, it was solely responsible.
                    }
                    double featureHeight = (perLabelHeight[pair.Key] * featurePercent) / f.Count;
                    if (featureHeight > highestHeight) {
                        highestHeight = featureHeight;
                    }
                    //Console.WriteLine("featureHeight ({0}, height={3:N3}): '{1}' = {2:N6} ({4:P})", f.Label, f.Characters, featureHeight, perLabelHeight[pair.Key], featurePercent);
                    Feature evidenceFeature = new Feature(f.Characters, f.Label, f.Count, featureHeight, 0);
                    evidenceFeature.PercentOfReason = featurePercent;
                    pair.Value.EvidenceItems.Add(evidenceFeature);
                }
            }

            // Keep our bars within a reasonable pixel height range;
            double heightAdjustmentRatio = 1.0;
            if (highestHeight > MAX_HEIGHT) {
                heightAdjustmentRatio = MAX_HEIGHT / highestHeight;
            } else if (highestHeight < MIN_HEIGHT) {
                heightAdjustmentRatio = MIN_HEIGHT / highestHeight;
            }

            // Make sure each bar is at least 2px tall
            foreach (KeyValuePair<Label, Evidence> pair in _evidencePerClass) {
                foreach (Feature f in pair.Value.EvidenceItems) {
                    f.SystemWeight = f.SystemWeight * heightAdjustmentRatio;
                    if ((f.SystemWeight * f.PixelsToWeight) < 2) {
                        f.SystemWeight = 2 / f.PixelsToWeight;
                    }
                }

                pair.Value.EvidenceItems.Sort();
            }
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
