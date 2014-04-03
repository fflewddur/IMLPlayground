using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibIML.Features
{
    public class FeatureImportance : ViewModelBase
    {
        public static readonly int MINIMUM_HEIGHT = 2; // The minimum height in pixels for each bar

        public enum Weight
        {
            None,
            Custom,
            Medium,
            High
        };

        public const double WEIGHT_NONE = 0;
        public const double WEIGHT_DEFAULT = 1;
        public const double WEIGHT_MEDIUM = 2;
        public const double WEIGHT_HIGH = 4;

        private double _systemHeight;
        private double _userHeight;
        private Label _label;
        private Weight _weightType;
        private double _systemWeight;
        private double _userWeight;
        private double _userPrior;
        private int _count;
        private double _pixelsToWeight;

        public FeatureImportance(Label label)
            : base()
        {
            _label = label;
            _weightType = Weight.Custom;
            _userWeight = WEIGHT_DEFAULT;
            _userPrior = WEIGHT_DEFAULT;
            _systemWeight = 0;
        }

        #region Properties

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
                            UserPrior = WEIGHT_NONE;
                            break;
                        case Weight.Medium:
                            UserPrior = WEIGHT_MEDIUM;
                            break;
                        case Weight.High:
                            UserPrior = WEIGHT_HIGH;
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
                    SystemHeight = SystemWeight * PixelsToWeight;
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
                    UserHeight = UserWeight * PixelsToWeight;
                }
            }
        }

        public double UserPrior
        {
            get { return _userPrior; }
            set { SetProperty<double>(ref _userPrior, value); }
        }

        /// <summary>
        /// Number of times this feature occurs in the given document.
        /// Used by Evidence class.
        /// </summary>
        public int Count
        {
            get { return _count; }
            set { SetProperty<int>(ref _count, value); }
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

        public double PixelsToWeight
        {
            get { return _pixelsToWeight; }
            set
            {
                if (SetProperty<double>(ref _pixelsToWeight, value)) {
                    SystemHeight = SystemWeight * PixelsToWeight;
                    UserHeight = UserWeight * PixelsToWeight;
                }
            }
        }

        #endregion

        #region Public methods

        public double GetWeight()
        {
            return _userWeight + _systemWeight;
        }

        #endregion
    }
}
