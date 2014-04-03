using LibIML.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibIML.Features
{
    [Serializable]
    public class Feature : ViewModelBase, IEquatable<Feature>, IComparable<Feature>
    {
        public static readonly int BAR_WIDTH = 25; // The default width (in pixels) for each bar

        //public const double PIXELS_TO_WEIGHT = 100;

        private string _characters;
        private bool _userAdded;
        private Label _mostImportantLabel;
        private double _pixelsToWeight;
        private double _percentOfReason; // Percentage of total evidence 
        //private List<FeatureImportance> _featureImportance;
        private FeatureImportance _topic1Importance;
        private FeatureImportance _topic2Importance;

        public Feature(string characters) : base()
        {
            Characters = characters;
        }

        public Feature(string characters, Label topic1, Label topic2) : this(characters)
        {
            _topic1Importance = new FeatureImportance(topic1);
            _topic2Importance = new FeatureImportance(topic2);
            _topic1Importance.PropertyChanged += topicImportanceChanged;
            _topic2Importance.PropertyChanged += topicImportanceChanged;
        }

        public Feature(string characters, Label topic1, Label topic2, bool userAdded)
            : this(characters, topic1, topic2)
        {
            _userAdded = userAdded;
        }

        // We use this constructor for the FeatureGraphs
        //public Feature(string characters, Label label)
        //    : base()
        //{
        //    _characters = characters;
        //    _label = label;
        //    _count = 1;
        //    _mostImportantLabel = false;
        //    _weightType = Weight.Custom;
        //    _userWeight = WEIGHT_DEFAULT;
        //    _userPrior = WEIGHT_DEFAULT;
        //    _systemWeight = 0;
        //}

        //public Feature(string characters, Label label, bool userAdded) : this(characters, label)
        //{
        //    _userAdded = userAdded;
        //}

        // We use this constructor for the EvidenceGraphs
        //public Feature(string characters, Label label, int count, double sysWeight, double userWeight) : this(characters, label, false)
        //{
        //    _count = count;
        //    _userWeight = userWeight;
        //    _systemWeight = sysWeight;
        //    //Console.WriteLine("weight={0}", _systemWeight);
        //    PixelsToWeight = 10;
        //}

        #region Properties

        public string Characters
        {
            get { return _characters; }
            private set { _characters = value; }
        }

        public bool UserAdded
        {
            get { return _userAdded; }
            private set { _userAdded = value; }
        }

        public Label MostImportantLabel
        {
            get { return _mostImportantLabel; }
            private set { _mostImportantLabel = value; }
        }

        public double PixelsToWeight
        {
            get { return _pixelsToWeight; }
            set
            {
                if (SetProperty<double>(ref _pixelsToWeight, value)) {
                    // FIXME We'll need to revise how we do this
                    //SystemHeight = SystemWeight * PixelsToWeight;
                    //UserHeight = UserWeight * PixelsToWeight;
                    //Width = Count * BAR_WIDTH;
                }
            }
        }

        public double PercentOfReason
        {
            get { return _percentOfReason; }
            set { SetProperty<double>(ref _percentOfReason, value); }
        }

        public FeatureImportance Topic1Importance
        {
            get { return _topic1Importance; }
            private set { SetProperty<FeatureImportance>(ref _topic1Importance, value); }
        }

        public FeatureImportance Topic2Importance
        {
            get { return _topic2Importance; }
            private set { SetProperty<FeatureImportance>(ref _topic2Importance, value); }
        }

        #endregion

        #region Override methods

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            //sb.AppendFormat("{0} (label={1}, weight type={2}, user weight={3}, prior={5}, system weight={4})", Characters, Label, this.WeightType, UserWeight, SystemWeight, UserPrior);
            sb.AppendFormat("{0}", Characters);

            return sb.ToString();
        }

        public override bool Equals(object other)
        {
            if (!(other is Feature))
                return false;

            return Equals((Feature)other);
        }

        public bool Equals(Feature other)
        {
            return (Characters.Equals(other.Characters));
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + Characters.GetHashCode();
            return hash;
        }

        public int CompareTo(Feature other)
        {
            if (other == null) {
                return 1;
            } else {
                return Characters.CompareTo(other.Characters);
            }
        }

        #endregion

        #region Callbacks

        void topicImportanceChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "UserWeight" || e.PropertyName == "SystemWeight") {
                if (_topic1Importance.GetWeight() >= _topic2Importance.GetWeight()) {
                    this.MostImportantLabel = _topic1Importance.Label;
                } else {
                    this.MostImportantLabel = _topic2Importance.Label;
                }
            }
        }

        #endregion
    }
}
