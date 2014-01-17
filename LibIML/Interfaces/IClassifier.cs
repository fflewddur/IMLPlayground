using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibIML
{
    public interface IClassifier
    {
        string Name { get; }
        IEnumerable<Label> Labels { get; }
        Vocabulary Vocab { get; set; }
        IReadOnlyDictionary<Label, HashSet<Feature>> FeaturesPerClass { get; }
        Label PositiveLabel { get; }
        Label NegativeLabel { get; }

        void AddInstance(IInstance instance);
        void AddInstances(IEnumerable<IInstance> instances);
        Prediction PredictInstance(IInstance instance);
        void UpdatePrior(Label label, int feature, double prior);
        void Train(); // Train the classifier on all added training instances
        void ClearInstances(); // Remove all existing training instances

        Task<bool> SaveArffFile(string filePath);
    }
}
