using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IML_Playground.Learning
{
    class Evaluator
    {
        IClassifier _classifier;
        int[,] _confusionMatrix;
        Dictionary<Label, int> _labelIndices; // This lets us know where each label belongs in the confusion matrix
        double _f1;

        public Evaluator()
        {
            _labelIndices = new Dictionary<Label, int>();
            _f1 = -1;
        }

        public IClassifier Classifier
        {
            get { return _classifier; }
            set
            {
                _classifier = value;
                _labelIndices.Clear();
                int index = 0;
                foreach (Label l in _classifier.Labels)
                {
                    _labelIndices.Add(l, index);
                    index++;
                }
            }
        }

        public int[,] ConfusionMatrix
        {
            get { return _confusionMatrix; }
            private set { _confusionMatrix = value; }
        }

        public double WeightedF1
        {
            get { return _f1; }
            private set { _f1 = value; }
        }

        public void EvaluateOnTestSet(IEnumerable<Instance> instances)
        {
            Debug.Assert(Classifier != null);

            _confusionMatrix = new int[_classifier.Labels.Count(), _classifier.Labels.Count()];

            int correct = 0;
            int incorrect = 0;
            foreach (Instance instance in instances)
            {
                Prediction prediction = Classifier.PredictInstance(instance);
                if (prediction.Label == instance.Label)
                {
                    // Correct prediction.
                    int index = _labelIndices[prediction.Label];
                    _confusionMatrix[index, index]++;
                    correct++;
                }
                else
                {
                    // Incorrect prediction. Increment [column(actual class), row(predicted class)]
                    int indexPredicted = _labelIndices[prediction.Label];
                    int indexActual = _labelIndices[instance.Label];
                    _confusionMatrix[indexPredicted, indexActual]++;
                    incorrect++;
                }
            }

            UpdateF1Weighted();
        }

        private void UpdateF1Weighted()
        {
            Dictionary<Label, double> weightedF1Scores = new Dictionary<Label, double>();

            foreach (KeyValuePair<Label, int> pair in _labelIndices)
            {
                int truePositives = 0;
                int falsePositives = 0;
                int falseNegatives = 0;
                int trueNegatives = 0;

                for (int i = 0; i < _labelIndices.Count; i++)
                {
                    for (int j = 0; j < _labelIndices.Count; j++)
                    {
                        if (pair.Value == i && i == j) // True positives
                            truePositives = _confusionMatrix[i, j];
                        else if (pair.Value == i) // False positives
                            falsePositives += _confusionMatrix[i, j];
                        else if (pair.Value == j) // False negatives
                            falseNegatives += _confusionMatrix[i, j];
                        else
                            trueNegatives += _confusionMatrix[i, j];
                    }
                }

                double recall = truePositives / (double)(truePositives + falseNegatives);
                double precision = truePositives / (double)(truePositives + falsePositives);
                double f1 = (2.0 * precision * recall) / (precision + recall);
                double f1Weight = (truePositives + falseNegatives) / (double)(truePositives + trueNegatives + falsePositives + falseNegatives);
                weightedF1Scores[pair.Key] = f1 * f1Weight;
                //Console.WriteLine("Label: {0} Precision: {1:0.000} Recall: {2:0.000} F1: {3:0.000} F1 Weight: {7:0.000} TP: {4} FP: {5} FN: {6}", pair.Key.UserLabel, precision, recall, f1, truePositives, falsePositives, falseNegatives, f1Weight);
            }

            // Compute the single weighted F1 score
            double f1Weighted = 0;
            foreach (double f1 in weightedF1Scores.Values)
            {
                f1Weighted += f1;
            }
            WeightedF1 = f1Weighted;

            // HACK this assumes a 2-dimensional array
            //Console.WriteLine("Confusion matrix:");
            //for (int i = 0; i < _confusionMatrix.GetLength(0); i++)
            //{
            //    for (int j = 0; j < _confusionMatrix.GetLength(1); j++)
            //    {
            //        Console.Write("{0}\t", _confusionMatrix[i, j]);
            //    }
            //    Console.WriteLine();
            //}
            //Console.WriteLine();
        }
    }
}
