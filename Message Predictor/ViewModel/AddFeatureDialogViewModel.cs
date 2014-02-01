using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePredictor.ViewModel
{
    public class AddFeatureDialogViewModel : ViewModelBase
    {
        private string _topic;
        private string _word;
        
        public AddFeatureDialogViewModel() : base()
        {
            Topic = "Unknown";
            Word = ""; // use an empty string, not null, to make it easier to style the empty string with placeholder text.
        }

        public string Topic
        {
            get { return _topic; }
            set { SetProperty<string>(ref _topic, value); }
        }

        public string Word
        {
            get { return _word; }
            set { SetProperty<string>(ref _word, value); }
        }
    }
}
