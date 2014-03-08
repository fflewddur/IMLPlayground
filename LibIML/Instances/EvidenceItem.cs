using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibIML.Instances
{
    public class EvidenceItem
    {
        private string _word;
        private int _id;
        private int _count;
        private double _systemWeight;
        private double _userWeight;

        public EvidenceItem(string word, int id, int count, double systemWeight, double userWeight)
        {
            _word = word;
            _id = id;
            _count = count;
            _systemWeight = systemWeight;
            _userWeight = userWeight;
        }

        #region Properties

        public string Word
        {
            get { return _word; }
            private set { _word = value; }
        }

        public int Id
        {
            get { return _id; }
            private set { _id = value; }
        }

        public int Count
        {
            get { return _count; }
            private set { _count = value; }
        }

        public double SystemWeight
        {
            get { return _systemWeight; }
            private set { _systemWeight = value; }
        }

        public double UserWeight
        {
            get { return _userWeight; }
            private set { _userWeight = value; }
        }

        #endregion
    }
}
