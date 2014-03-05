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
        public static readonly int MINIMUM_HEIGHT = 2; // The minimum height in pixels for each bar

        public enum Weight
        {
            None,
            Custom,
            Medium,
            High
        };

        public const double PIXELS_TO_WEIGHT = 100;
        public const double WEIGHT_NONE = 0;
        public const double WEIGHT_DEFAULT = 1;
        public const double WEIGHT_MEDIUM = 10;
        public const double WEIGHT_HIGH = 20;

        private string _characters;
        private Label _label;
        private Weight _weightType;
        private double _systemWeight;
        private double _userWeight;
        private double _userPrior;
        private bool _mostImportantLabel;
        private double _systemHeight;
        private double _userHeight;

        public Feature(string characters, Label label)
            : base()
        {
            _characters = characters;
            _label = label;
            _mostImportantLabel = false;
            _weightType = Weight.Custom;
            _userWeight = WEIGHT_DEFAULT;
            _userPrior = WEIGHT_DEFAULT;
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
            set
            {
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

        /// <summary>
        /// The system-determined weight, relative to all other feature weights.
        /// This is used for explaining the relative importance of each feature.
        /// </summary>
        public double SystemWeight
        {
            get { return _systemWeight; }
            set
            {
                if (SetProperty<double>(ref _systemWeight, value)) {
                    SystemHeight = SystemWeight * PIXELS_TO_WEIGHT;
                }
            }
        }

        /// <summary>
        /// The user-specified prior, relative to all other priors.
        /// This is used for explaining the relative importance of each feature.
        /// </summary>
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

        public double UserPrior
        {
            get { return _userPrior; }
            set { SetProperty<double>(ref _userPrior, value); }
        }

        public bool MostImportant
        {
            get { return _mostImportantLabel; }
            set { _mostImportantLabel = value; }
        }

        public double SystemHeight
        {
            get { return _systemHeight; }
            private set { SetProperty<double>(ref _systemHeight, value); }
        }

        public double UserHeight
        {
            get { return _userHeight; }
            set
            {
                // Minimum height (so the user can drag the bar back up)
                if (value < MINIMUM_HEIGHT) {
                    value = MINIMUM_HEIGHT;
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
