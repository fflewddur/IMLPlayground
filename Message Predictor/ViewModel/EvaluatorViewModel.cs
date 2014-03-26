using LibIML;
using MessagePredictor.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MessagePredictor.ViewModel
{
    class EvaluatorViewModel : ViewModelBase
    {
        private List<Evaluator> _evaluators;
        private int _recentlyChangedCount = 0;
        private int _truePositives;
        private int _trueNegatives;
        private int _falsePositives;
        private int _falseNegatives;
        private double _f1Positive;
        private double _f1Negative;
        private double _f1Weighted;

        public EvaluatorViewModel(IReadOnlyList<Label> labels)
            : base()
        {
            _evaluators = new List<Evaluator>();
            foreach (Label label in labels) {
                _evaluators.Add(new Evaluator(label));
            }
        }

        #region Properties

        public List<Evaluator> Evaluators
        {
            get { return _evaluators; }
            private set { SetProperty<List<Evaluator>>(ref _evaluators, value); }
        }

        public int RecentlyChangedCount
        {
            get { return _recentlyChangedCount; }
            private set { SetProperty<int>(ref _recentlyChangedCount, value); }
        }

        public int TruePositives
        {
            get { return _truePositives; }
            private set { SetProperty<int>(ref _truePositives, value); }
        }

        public int FalseNegatives
        {
            get { return _falseNegatives; }
            private set { SetProperty<int>(ref _falseNegatives, value); }
        }

        public int FalsePositives
        {
            get { return _falsePositives; }
            private set { SetProperty<int>(ref _falsePositives, value); }
        }

        public int TrueNegatives
        {
            get { return _trueNegatives; }
            private set { SetProperty<int>(ref _trueNegatives, value); }
        }

        public double F1Positive
        {
            get { return _f1Positive; }
            private set { SetProperty<double>(ref _f1Positive, value); }
        }

        public double F1Negative
        {
            get { return _f1Negative; }
            private set { SetProperty<double>(ref _f1Negative, value); }
        }

        public double F1Weighted
        {
            get { return _f1Weighted; }
            private set { SetProperty<double>(ref _f1Weighted, value); }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Evaluate the correctness of each prediction. Used for our overview of current predictions.
        /// </summary>
        /// <param name="instances"></param>
        public void EvaluatePredictions(IEnumerable<IInstance> instances)
        {
            int recentlyChanged = 0;
            foreach (Evaluator evaluator in _evaluators) {
                evaluator.Evaluate(instances);
            }
            foreach (IInstance instance in instances) {
                if (instance.RecentlyChanged) {
                    recentlyChanged++;
                }
            }
            RecentlyChangedCount = recentlyChanged;
        }

        /// <summary>
        /// Evaluate the classifier as a whole, using standard ML metrics.
        /// </summary>
        /// <param name="instances">The predicted instances. Each prediction must be one of the Labels passed as parameters.</param>
        /// <param name="positive">The label to use as positive</param>
        /// <param name="negative">The label to use as negative</param>
        public void EvaluateClassifier(IEnumerable<IInstance> instances, Label positive, Label negative)
        {
            int truePositives = 0;
            int trueNegatives = 0;
            int falsePositives = 0;
            int falseNegatives = 0;
            int positiveGroundTruthCount, negativeGroundTruthCount;
            double precisionPositive, precisionNegative;
            double recallPositive, recallNegative;

            foreach (IInstance instance in instances) {
                if (instance.GroundTruthLabel == positive) {
                    if (instance.Prediction.Label == positive) {
                        truePositives++;
                    } else if (instance.Prediction.Label == negative) {
                        falseNegatives++;
                    } else {
                        Debug.Assert(false, "An evaluated instance has a predicted label that is neither positive nor negative.");
                    }

                } else if (instance.GroundTruthLabel == negative) {
                    if (instance.Prediction.Label == positive) {
                        falsePositives++;
                    } else if (instance.Prediction.Label == negative) {
                        trueNegatives++;
                    } else {
                        Debug.Assert(false, "An evaluated instance has a predicted label that is neither positive nor negative.");
                    }
                } else {
                    Debug.Assert(false, "An evaluated instance has a ground-truth label that is neither positive nor negative.");
                }
            }

            positiveGroundTruthCount = truePositives + falseNegatives;
            negativeGroundTruthCount = trueNegatives + falsePositives;
            precisionPositive = truePositives / (double)(truePositives + falsePositives);
            recallPositive = truePositives / (double)(truePositives + falseNegatives);
            precisionNegative = trueNegatives / (double)(trueNegatives + falseNegatives);
            recallNegative = trueNegatives / (double)(trueNegatives + falsePositives);

            F1Positive = (2.0 * precisionPositive * recallPositive) / (precisionPositive + recallPositive);
            F1Negative = (2.0 * precisionNegative * recallNegative) / (precisionNegative + recallNegative);
            F1Weighted = (F1Positive * positiveGroundTruthCount / (positiveGroundTruthCount + negativeGroundTruthCount)) + 
                         (F1Negative * negativeGroundTruthCount / (positiveGroundTruthCount + negativeGroundTruthCount));

            TruePositives = truePositives;
            TrueNegatives = trueNegatives;
            FalsePositives = falsePositives;
            FalseNegatives = falseNegatives;

        }

        #endregion
    }
}
