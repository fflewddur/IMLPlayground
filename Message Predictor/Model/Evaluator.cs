using LibIML;
using System.Collections.Generic;

namespace MessagePredictor.Model
{
    class Evaluator : ViewModelBase
    {
        private Label _label;
        private int _correctPredictionCount;
        private int _totalPredictionCount;
        private int _trainingSetCount;

        public Evaluator(Label label)
            : base()
        {
            _label = label;
            _correctPredictionCount = 0;
            _totalPredictionCount = 0;
            _trainingSetCount = 0;
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
            private set { SetProperty<int>(ref _correctPredictionCount, value); }
        }

        public int TotalPredictionCount
        {
            get { return _totalPredictionCount; }
            private set { SetProperty<int>(ref _totalPredictionCount, value); }
        }

        public int TrainingSetCount
        {
            get { return _trainingSetCount; }
            private set { SetProperty<int>(ref _trainingSetCount, value); }
        }

        #endregion

        #region Public methods

        public void Evaluate(IEnumerable<IInstance> instances)
        {
            _correctPredictionCount = 0;
            _totalPredictionCount = 0;
            foreach (IInstance instance in instances) {
                if (instance.Prediction.Label == _label) {
                    _totalPredictionCount++;
                    if (instance.UserLabel == _label) {
                        _correctPredictionCount++;
                    }
                }
            }
        }

        #endregion
    }
}
