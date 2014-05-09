using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibIML.Instances
{
    public class EvidenceItem
    {
        private string _featureText;
        private int _featureId;
        private Label _label;
        private Label _otherLabel;
        private double _label1Pr;
        private double _label2Pr;
        private int _count;
        private double _ratio;
        private double _fontSize;
        private string _tooltipText;

        public EvidenceItem(string featureText, int featureId, int count)
        {
            _featureText = featureText;
            _featureId = featureId;
            _count = count;
            UpdateTooltip();
        }

        #region Properties

        public string FeatureText
        {
            get { return _featureText; }
            private set { _featureText = value; }
        }

        public int FeatureId
        {
            get { return _featureId; }
            private set { _featureId = value; }
        }

        public Label Label
        {
            get { return _label; }
            set { 
                _label = value;
                UpdateTooltip();
            }
        }

        public Label OtherLabel
        {
            get { return _otherLabel; }
            set
            {
                _otherLabel = value;
                UpdateTooltip();
            }
        }

        public double Label1Pr
        {
            get { return _label1Pr; }
            set
            {
                _label1Pr = value;
                UpdateRatio();
                UpdateTooltip();
            }
        }

        public double Label2Pr
        {
            get { return _label2Pr; }
            set
            {
                _label2Pr = value;
                UpdateRatio();
                UpdateTooltip();
            }
        }

        public int Count
        {
            get { return _count; }
            private set { _count = value; }
        }

        public double Ratio
        {
            get { return _ratio; }
            private set
            {
                _ratio = value;
                FontSize = Math.Log(_ratio + Math.E) * 10;
            }
        }

        public double FontSize
        {
            get { return _fontSize; }
            private set { _fontSize = value; }
        }

        public string TooltipText
        {
            get { return _tooltipText; }
            private set { _tooltipText = value; }
        }

        #endregion

        public void InvertRatio()
        {
            double tmp = Label1Pr;
            Label1Pr = Label2Pr;
            Label2Pr = tmp;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("{0} label={5} topic1Pr={1:N6} topic2Pr={2:N6} count={3} ratio={4:N3} fontSize={6:N1}", FeatureText, Label1Pr, Label2Pr, Count, Ratio, Label, FontSize);

            return sb.ToString();
        }

        private void UpdateRatio()
        {
            if (_label1Pr != 0 && _label2Pr != 0) {
                Ratio = _label1Pr / _label2Pr;
            } else {
                Ratio = double.NaN;
            }
        }

        private void UpdateTooltip()
        {
            string times;
            if (Count != 1) {
                times = "times";
            } else {
                times = "time";
            }

            string ratioDesc;
            if (Ratio > 1000) {
                ratioDesc = "over 1,000";
            } else {
                if (Ratio > 100) {
                    int ratioInt = (int)(Ratio / 10);
                    ratioDesc = string.Format("{0:N0}", ratioInt * 10);
                } else if (Ratio > 10) {
                    ratioDesc = string.Format("{0:N0}", Ratio);
                } else {
                    ratioDesc = string.Format("{0:N1}", Ratio);
                }
            }

            TooltipText = string.Format("'{0}' is in this message {1} {2}.\nThat makes the computer think this message is {4} times\nmore likely to be about be about {3} than {5}", 
                FeatureText, Count, times, Label, ratioDesc, OtherLabel);
        }
    }
}
