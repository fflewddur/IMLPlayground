using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IML_Playground.Learning
{
    interface IClassifier
    {
        List<Label> Labels { get; }

        void AddInstance(Instance instance);
        void AddInstances(IEnumerable<Instance> instances);
        Label PredictInstance(Instance instance);
    }
}
