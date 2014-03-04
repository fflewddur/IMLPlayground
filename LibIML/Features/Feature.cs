using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibIML
{
    [Serializable]
    public class Feature : ViewModelBase, IEquatable<Feature>
    {
        public enum Weight
        {
            None,
            Custom,
            Medium,
            High
        };

        public const double PIXELS_TO_WEIGHT = 100;
        public const double WEIGHT_NONE = 3;
        public const double WEIGHT_MEDIUM = 10;
        public const double WEIGHT_HIGH = 20;

        private string _characters;
        private Label _label;
        private Weight _weightType;
        private double _systemWeight;
        private double _userWeight;
        private bool _mostImportantLabel;
        private double _height;
        private double _userHeight;

        public Feature(string characters, Label label)
            : base()
        {
            _characters = characters;
            _label = label;
            _mostImportantLabel = false;
            _weightType = Weight.None;
            _userWeight = WEIGHT_NONE;
            _systemWeight = 0;
        }

        #region Properties

        public string Characters
        {
            get { return _characters; }
            private set { _characters = value; }
        }

        public Label Label
        {
            get { return _label; }
            private set { _label = Label; }
        }

        public Weight WeightType
        {
            get { return _weightType; }
            set {
                if (SetProperty<Weight>(ref _weightType, value)) {
                    switch (_weightType) {
                        case Weight.None:
                            UserWeight = WEIGHT_NONE;
                            break;
                        case Weight.Medium:
                            UserWeight = WEIGHT_MEDIUM;
                            break;
                        case Weight.High:
                            UserWeight = WEIGHT_HIGH;
                            break;
                    }
                }
            }
        }

        public double SystemWeight
        {
            get { return _systemWeight; }
            set
            {
                if (SetProperty<double>(ref _systemWeight, value)) {
                    Height = SystemWeight * PIXELS_TO_WEIGHT;
                }
            }
        }

        public double UserWeight
        {
            get { return _userWeight; }
            set
            {
                if (SetProperty<double>(ref _userWeight, value)) {
                    UserHeight = UserWeight * PIXELS_TO_WEIGHT; // FIXME
                }
            }
        }

        public bool MostImportant
        {
            get { return _mostImportantLabel; }
            set { _mostImportantLabel = value; }
        }

        public double Height
        {
            get { return _height; }
            private set { SetProperty<double>(ref _height, value); }
        }

        public double UserHeight
        {
            get { return _userHeight; }
            private set
            {
                // Minimum height (so the user can drag the bar back up)
                if (value < 3) {
                    value = 3;
                }
                SetProperty<double>(ref _userHeight, value);
            }
        }

        #endregion

        #region Override methods

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("{0} (label={1}, weight type={2}, user weight={3}, system weight={4})", Characters, Label, this.WeightType, UserWeight, SystemWeight);

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
            if (Label != Label.AnyLabel && other.Label != Label.AnyLabel)
                return (Characters == other.Characters) && (Label == other.Label);
            else
                return (Characters == other.Characters);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + Characters.GetHashCode();
            //if (Label != Label.AnyLabel)
            hash += hash * 31 + Label.GetHashCode();
            return hash;
        }

        #endregion
    }
}
