using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibIML
{
    [Serializable]
    public class Feature : IEquatable<Feature>
    {
        public enum Weight
        {
            None,
            Medium,
            High
        };

        private string _characters;
        private int _countTraining;
        private int _countTesting;
        private double _systemWeight;
        private double _userWeight;

        #region Properties

        public string Characters
        {
            get { return _characters; }
            set { _characters = value; }
        }

        public int CountTraining
        {
            get { return _countTraining; }
            set { _countTraining = value; }
        }

        public int CountTesting
        {
            get { return _countTesting; }
            set { _countTesting = value; }
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
