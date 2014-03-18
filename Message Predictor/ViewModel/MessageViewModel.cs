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
    class MessageViewModel : ViewModelBase
    {
        private NewsItem _message;

        public NewsItem Message
        {
            get { return _message; }
            set { SetProperty<NewsItem>(ref _message, value); }
        }
    }
}
