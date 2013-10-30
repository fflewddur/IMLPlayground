using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IML_Playground.Learning
{
    interface IInstances
    {
        int Count { get; }

        IEnumerable<Instance> ToInstances();
    }
}
