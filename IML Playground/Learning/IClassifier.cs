﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IML_Playground.Learning
{
    interface IClassifier
    {
        List<Label> Labels { get; }
        Vocabulary Vocab { get; }
        Dictionary<Label, HashSet<Feature>> FeaturesPerClass { get; }

        void AddInstance(Instance instance);
        void AddInstances(IEnumerable<Instance> instances);
        Prediction PredictInstance(Instance instance);
        void UpdatePrior(Label label, int feature, double prior);
        void Train(); // Train the classifier on all added training instances
        void ClearInstances(); // Remove all existing training instances

        Task<bool> SaveArffFile(string filePath);
    }
}
