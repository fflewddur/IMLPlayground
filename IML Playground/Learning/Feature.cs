using IML_Playground.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IML_Playground.Learning
{
    [Serializable]
    class Feature
    {
        private string _characters;
        private int _countTraining;
        private int _countTesting;
        private double _weight;

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

        public double Weight
        {
            get { return _weight; }
            set { _weight = value; }
        }
    }
}
