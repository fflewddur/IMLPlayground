using LibIML;
using MessagePredictor.Model;
using System.Collections.Generic;

namespace MessagePredictor.ViewModel
{
    class EvaluatorViewModel : ViewModelBase
    {
        private List<Evaluator> _evaluators;

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

        #endregion

        #region Public methods

        public void EvaluatePredictions(IEnumerable<IInstance> instances)
        {
            foreach (Evaluator evaluator in _evaluators) {
                evaluator.Evaluate(instances);
            }
        }

        #endregion
    }
}
