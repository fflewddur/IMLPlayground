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
        private Dictionary<Label, Weight> _weights;
        private Dictionary<Label, double> _systemWeights;
        private Dictionary<Label, double> _userWeights;

        public Feature() : base()
        { }

        #region Properties

        public string Characters
        {
            get { return _characters; }
            set { _characters = value; }
        }

        public Dictionary<Label, Weight> Weights
        {
            get { return _weights; }
            set { _weights = value; }
        }

        public Dictionary<Label, double> SystemWeight
        {
            get { return _systemWeights; }
            set { _systemWeights = value; }
        }

        public Dictionary<Label, double> UserWeight
        {
            get { return _userWeights; }
            set { _userWeights = value; }
        }

        #endregion

        #region Override methods

        public override string ToString()
        {
            return Characters;
        }

        public override bool Equals(object other)
        {
            if (!(other is Feature))
                return false;

            return Equals((Feature)other);
        }

        public bool Equals(Feature other)
        {
            return Characters == other.Characters;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + Characters.GetHashCode();
            return hash;
        }

        #endregion
    }
}
