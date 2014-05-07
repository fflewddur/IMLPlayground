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

        public MessageViewModel()
            : base()
        {

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
                    UpdateMessageForViewing(_message);
                }

                if (_message != null) {
                    _message.PropertyChanged += _message_PropertyChanged;
                }
            }
        }

        /// <summary>
        /// When this item's prediction is updated, also update the UI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _message_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Prediction") {
                Message.Prediction.UpdatePrDescriptions();
            }
        }

        private void UpdateMessageForViewing(NewsItem message)
        {
            if (message != null && message.Prediction != null) {
                message.Prediction.UpdatePrDescriptions();
            }
        }
    }
}
