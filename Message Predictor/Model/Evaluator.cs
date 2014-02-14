using LibIML;
using MessagePredictor.Model;
using System;
using System.Collections.Generic;

namespace MessagePredictor.Model
{
    class Evaluator : ViewModelBase
    {
        private Label _label;
        private int _correctPredictionCount;
        private int _priorCorrectPredictionCount;
        private int _totalPredictionCount;
        private int _priorTotalPredictionCount;
        private int _trainingSetCount;
        private Direction _correctPredictionDirection;
        private Direction _totalPredictionDirection;
        private int _totalPredictionDifference;
        private int _correctPredictionDifference;

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

        #endregion

        #region Public methods

        /// <summary>
        /// Evaluates how many predictions are correct for this label.
        /// </summary>
        /// <param name="instances">The full set of instances.</param>
        /// <returns>The number of predictions that changed output labels</returns>
        public int Evaluate(IEnumerable<IInstance> instances)
        {
            int correctPredictionCount = 0;
            int totalPredictionCount = 0;
            int trainingSetCount = 0;
            int changedCount = 0;
            foreach (IInstance instance in instances) {
                if (instance.Prediction.Label == _label) {
                    totalPredictionCount++;
                    if (instance.UserLabel == _label) {
                        correctPredictionCount++;
                    }
                    if (instance.PreviousPrediction != null && instance.Prediction.Label != instance.PreviousPrediction.Label) {
                        changedCount++;
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

            return changedCount;
        }

        #endregion
    }
}
