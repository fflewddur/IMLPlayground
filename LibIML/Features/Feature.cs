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

        private string _characters;
        private Label _label;
        private Weight _weightType;
        private double _systemWeight;
        private double _userWeight;

        public Feature(string characters, Label label) : base()
        {
            _characters = characters;
            _label = label;
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
            set { _weightType = value; }
        }

        public double SystemWeight
        {
            get { return _systemWeight; }
            set { _systemWeight = value; }
        }

        public double UserWeight
        {
            get { return _userWeight; }
            set { _userWeight = value; }
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
