using LibIML;
using MessagePredictor.Model;

namespace MessagePredictor.ViewModel
{
    class FolderViewModel : ViewModelBase
    {
        private Label _label;
        private int _messageCount;
        private NewsItem _selectedMessage;
        private Evaluator _evaluator; // Reference to the evaluator for this folder.

        public FolderViewModel(Label label, Evaluator evaluator)
            : base()
        {
            _label = label;
            _evaluator = evaluator;
        }

        #region Properties

        public Label Label
        {
            get { return _label; }
            private set { SetProperty<Label>(ref _label, value); }
        }

        public int MessageCount
        {
            get { return _messageCount; }
            set { SetProperty<int>(ref _messageCount, value); }
        }

        public Evaluator Evaluator
        {
            get { return _evaluator; }
            private set { SetProperty<Evaluator>(ref _evaluator, value); }
        }

        public NewsItem SelectedMessage
        {
            get { return _selectedMessage; }
            set { SetProperty<NewsItem>(ref _selectedMessage, value); }
        }

        #endregion
    }
}
