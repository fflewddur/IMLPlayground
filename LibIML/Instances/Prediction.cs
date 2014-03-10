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
        private string _importantWordDesc;

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

        #endregion

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
