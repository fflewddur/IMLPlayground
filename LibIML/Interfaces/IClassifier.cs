using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LibIML
{
    public interface IClassifier
    {
        event EventHandler<EventArgs> Retrained;

        string Name { get; }
        IEnumerable<Label> Labels { get; }
        Vocabulary Vocab { get; }

        void AddInstance(IInstance instance);
        void AddInstances(IEnumerable<IInstance> instances);
        void AddPriors(IEnumerable<Feature> priors);
        void UpdateCountsForNewFeature(int id);
        Prediction PredictInstance(IInstance instance);
        void Train(); // Train the classifier on all added training instances
        void ClearInstances(); // Remove all existing training instances
        bool TryGetFeatureSystemWeight(int id, Label label, out double weight);
        bool TryGetFeatureUserWeight(int id, Label label, out double weight);
        bool TryGetFeatureUserPrior(int id, Label label, out double prior);
        bool TryGetSystemFeatureSum(Label label, out double sum);
        bool IsFeatureMostImportantForLabel(int id, Label label);
        void LogFeatureSet(XmlWriter writer);
        void LogTrainingSet(XmlWriter writer);

        Task<bool> SaveArffFile(string filePath);
    }
}
