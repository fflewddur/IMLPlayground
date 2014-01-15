using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibIML
{
    public interface IInstances
    {
        int Count { get; }

        IEnumerable<Instance> ToInstances();
        IEnumerable<Instance> Subset(int size, params Label[] labels);
        IInstances ItemsSubset(int size, params Label[] labels);
        void ComputeFeatureVectors(Vocabulary vocab);
        IEnumerable<int> GetHighIGFeatures(Vocabulary vocab, IEnumerable<Label> labels, int nFeatures);
        Task<bool> SaveArffFile(string filePath, Vocabulary vocab, params Label[] labels);
    }
}
