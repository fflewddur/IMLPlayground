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
        private int _count;
        private double _weight;

        public string Characters
        {
            get { return _characters; }
            set { _characters = value; }
        }

        public int Count
        {
            get { return _count; }
            set { _count = value; }
        }

        public double Weight
        {
            get { return _weight; }
            set { _weight = value; }
        }
    }
}
