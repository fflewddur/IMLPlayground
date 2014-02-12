using LibIML;
using MessagePredictor.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePredictor.ViewModel
{
    class FolderViewModel : ViewModelBase
    {
        private Label _label;
        private int _messageCount;
        private int _correctPredictionCount;
        private int _priorCorrectPredictionCount;
        private NewsItem _selectedMessage;

        public FolderViewModel(Label label)
            : base()
        {
            _label = label;
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

        public int CorrectPredictionCount
        {
            get { return _correctPredictionCount; }
            set { SetProperty<int>(ref _correctPredictionCount, value); }
        }

        public int PriorCorrectPredictionCount
        {
            get { return _priorCorrectPredictionCount; }
            set { SetProperty<int>(ref _priorCorrectPredictionCount, value); }
        }

        public NewsItem SelectedMessage
        {
            get { return _selectedMessage; }
            set { SetProperty<NewsItem>(ref _selectedMessage, value); }
        }

        #endregion
    }
}
