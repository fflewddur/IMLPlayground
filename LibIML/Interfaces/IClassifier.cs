using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibIML
{
    public interface IClassifier
    {
        event EventHandler<EventArgs> Retrained;

        string Name { get; }
        IEnumerable<Label> Labels { get; }
        Vocabulary Vocab { get; }
        //IReadOnlyDictionary<Label, HashSet<Feature>> FeaturesPerClass { get; }
        //Label PositiveLabel { get; }
        //Label NegativeLabel { get; }

        void AddInstance(IInstance instance);
        void AddInstances(IEnumerable<IInstance> instances);
        Prediction PredictInstance(IInstance instance);
        //void UpdatePrior(Label label, int feature, double prior);
        void Train(); // Train the classifier on all added training instances
        //IEnumerable<Feature> GetFeatures();
        void ClearInstances(); // Remove all existing training instances
        double GetFeatureWeight(int id, Label label);
        bool IsFeatureMostImportantForLabel(int id, Label label);

        Task<bool> SaveArffFile(string filePath);
    }
}
