using LibIML;
using MessagePredictor.Model;
using System;
using System.Collections.Generic;

namespace MessagePredictor.ViewModel
{
    class EvaluatorViewModel : ViewModelBase
    {
        private List<Evaluator> _evaluators;
        private int _recentlyChangedCount = 0;

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

        #endregion

        #region Public methods

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

        #endregion
    }
}
