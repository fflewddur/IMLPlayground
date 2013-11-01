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
        IEnumerable<Instance> Subset(int size, params Label[] labels);
        Task<bool> SaveArffFile(string filePath, Vocabulary vocab, params Label[] labels);
    }
}
