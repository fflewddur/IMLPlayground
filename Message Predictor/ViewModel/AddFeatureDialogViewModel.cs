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
        private List<string> _weights;
        private string _selectedWeight;

        public AddFeatureDialogViewModel() : base()
        {
            Topic = "Unknown";
            Word = ""; // use an empty string, not null, to make it easier to style the empty string with placeholder text.
            Weights = new List<string>();
            Weights.Add("Very important");
            Weights.Add("Somewhat important");
            SelectedWeight = Weights[0];
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

        public List<string> Weights
        {
            get { return _weights; }
            set { SetProperty<List<String>>(ref _weights, value); }
        }

        public string SelectedWeight
        {
            get { return _selectedWeight; }
            set { SetProperty<string>(ref _selectedWeight, value); }
        }
    }
}
