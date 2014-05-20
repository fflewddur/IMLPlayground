using LibIML;
using MessagePredictor.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePredictor.ViewModel
{
    /// <summary>
    /// Handle the display of the currently-selected message
    /// </summary>
    public class MessageViewModel : ViewModelBase
    {
        private NewsItem _message;
        private bool _featurePrChanged;
        private bool _classPrChanged;
        private bool _confChanged;

        public MessageViewModel()
            : base()
        {
            _featurePrChanged = false;
            _classPrChanged = false;
            _confChanged = false;
        }

        public NewsItem Message
        {
            get { return _message; }
            set
            {
                if (_message != null) {
                    _message.PropertyChanged -= _message_PropertyChanged;
                }

                if (SetProperty<NewsItem>(ref _message, value)) {
                    UpdateMessageForViewing(_message, false);
                }

                if (_message != null) {
                    _message.PropertyChanged += _message_PropertyChanged;
                }
            }
        }

        public bool FeaturePrChanged
        {
            get { return _featurePrChanged; }
            private set { SetProperty<bool>(ref _featurePrChanged, value); }
        }

        public bool ClassPrChanged
        {
            get { return _classPrChanged; }
            private set { SetProperty<bool>(ref _classPrChanged, value); }
        }

        public bool ConfChanged
        {
            get { return _confChanged; }
            private set { SetProperty<bool>(ref _confChanged, value); }
        }

        /// <summary>
        /// When this item's prediction is updated, also update the UI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _message_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Prediction") {
                UpdateMessageForViewing(Message, true);
            }
        }

        private void UpdateMessageForViewing(NewsItem message, bool showChanges)
        {
            if (message != null && message.Prediction != null) {
                message.Prediction.UpdatePrDescriptions();
                if (showChanges && message.PreviousPrediction != null) {
                    message.PreviousPrediction.UpdatePrDescriptions();
                    if (message.Prediction.FeaturePrDesc != message.PreviousPrediction.FeaturePrDesc) {
                        FeaturePrChanged = !FeaturePrChanged;
                    }

                    if (message.Prediction.ClassPrDesc != message.PreviousPrediction.ClassPrDesc) {
                        ClassPrChanged = !ClassPrChanged;
                    }

                    if (message.Prediction.ConfidenceDesc != message.PreviousPrediction.ConfidenceDesc) {
                        ConfChanged = !ConfChanged;
                    }
                } else if (!showChanges && message.PreviousPrediction != null) {
                    message.PreviousPrediction.UpdatePrDescriptions();
                }
            }
        }
    }
}
