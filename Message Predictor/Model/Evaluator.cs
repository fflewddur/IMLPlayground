using LibIML;
using MessagePredictor.Model;
using System;
using System.Collections.Generic;

namespace MessagePredictor.Model
{
    public class Evaluator : ViewModelBase
    {
        private Label _label;
        private int _correctPredictionCount;
        private int _priorCorrectPredictionCount;
        private int _totalPredictionCount;
        private int _priorTotalPredictionCount;
        private int _trainingSetCount;
        private Direction _correctPredictionDirection;
        private Direction _totalPredictionDirection;
        private Direction _averageConfidenceDirection;
        private int _totalPredictionDifference;
        private int _correctPredictionDifference;
        private double _averageConfidence;
        private double _priorAverageConfidence;
        private double _averageConfidenceDifference;

        public Evaluator(Label label)
            : base()
        {
            _label = label;
            _correctPredictionDirection = Direction.None;
            _totalPredictionDirection = Direction.None;
        }

        #region Properties

        public Label Label
        {
            get { return _label; }
            private set { SetProperty<Label>(ref _label, value); }
        }

        public int CorrectPredictionCount
        {
            get { return _correctPredictionCount; }
            private set
            {
                int prior = _correctPredictionCount;
                SetProperty<int>(ref _correctPredictionCount, value);
                PriorCorrectPredictionCount = prior;
                CorrectPredictionDifference = Math.Abs(CorrectPredictionCount - PriorCorrectPredictionCount);
                if (prior < value) {
                    CorrectPredictionDirection = Direction.Up;
                } else if (prior > value) {
                    CorrectPredictionDirection = Direction.Down;
                } else {
                    CorrectPredictionDirection = Direction.None;
                }
            }
        }

        public int TotalPredictionCount
        {
            get { return _totalPredictionCount; }
            private set
            {
                int prior = _totalPredictionCount;
                SetProperty<int>(ref _totalPredictionCount, value);
                PriorTotalPredictionCount = prior;
                TotalPredictionDifference = Math.Abs(TotalPredictionCount - PriorTotalPredictionCount);
                if (prior < value) {
                    TotalPredictionDirection = Direction.Up;
                } else if (prior > value) {
                    TotalPredictionDirection = Direction.Down;
                } else {
                    TotalPredictionDirection = Direction.None;
                }
            }
        }

        public int TrainingSetCount
        {
            get { return _trainingSetCount; }
            private set { SetProperty<int>(ref _trainingSetCount, value); }
        }

        public int PriorCorrectPredictionCount
        {
            get { return _priorCorrectPredictionCount; }
            private set { SetProperty<int>(ref _priorCorrectPredictionCount, value); }
        }

        public int PriorTotalPredictionCount
        {
            get { return _priorTotalPredictionCount; }
            private set { SetProperty<int>(ref _priorTotalPredictionCount, value); }
        }

        public Direction CorrectPredictionDirection
        {
            get { return _correctPredictionDirection; }
            private set { SetProperty<Direction>(ref _correctPredictionDirection, value); }
        }

        public Direction TotalPredictionDirection
        {
            get { return _totalPredictionDirection; }
            private set { SetProperty<Direction>(ref _totalPredictionDirection, value); }
        }
        
        public int TotalPredictionDifference
        {
            get { return _totalPredictionDifference; }
            private set { SetProperty<int>(ref _totalPredictionDifference, value); }
        }

        public int CorrectPredictionDifference
        {
            get { return _correctPredictionDifference; }
            private set { SetProperty<int>(ref _correctPredictionDifference, value); }
        }

        public double AverageConfidence
        {
            get { return _averageConfidence; }
            private set {
                double prior = _averageConfidence;
                SetProperty<double>(ref _averageConfidence, value);
                PriorAverageConfidence = prior;
                AverageConfidenceDifference = Math.Abs(AverageConfidence - PriorAverageConfidence);
                if (prior < value) {
                    AverageConfidenceDirection = Direction.Up;
                } else if (prior > value) {
                    AverageConfidenceDirection = Direction.Down;
                } else {
                    AverageConfidenceDirection = Direction.None;
                }
            }
        }

        public double PriorAverageConfidence
        {
            get { return _priorAverageConfidence; }
            private set { SetProperty<double>(ref _priorAverageConfidence, value); }
        }

        public double AverageConfidenceDifference
        {
            get { return _averageConfidenceDifference; }
            private set { SetProperty<double>(ref _averageConfidenceDifference, value); }
        }

        public Direction AverageConfidenceDirection
        {
            get { return _averageConfidenceDirection; }
            private set { SetProperty<Direction>(ref _averageConfidenceDirection, value); }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Evaluates how many predictions are correct for this label.
        /// </summary>
        /// <param name="instances">The full set of instances.</param>
        /// <returns>The number of predictions that changed output labels</returns>
        public void Evaluate(IEnumerable<IInstance> instances)
        {
            int correctPredictionCount = 0;
            int totalPredictionCount = 0;
            int trainingSetCount = 0;
            double confidenceSum = 0;

            foreach (IInstance instance in instances) {
                if (instance.Prediction.Label == _label) {
                    totalPredictionCount++;
                    confidenceSum += instance.Prediction.Confidence;
                    if (instance.UserLabel == _label) {
                        correctPredictionCount++;
                    }
                }
                if (instance.UserLabel == _label) {
                    trainingSetCount++;
                }
            }

            // Now update our properties, which will tell the UI to refresh
            CorrectPredictionCount = correctPredictionCount;
            TotalPredictionCount = totalPredictionCount;
            TrainingSetCount = trainingSetCount;
            AverageConfidence = confidenceSum / (double)totalPredictionCount;
        }

        #endregion
    }
}
