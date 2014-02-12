using GalaSoft.MvvmLight.Command;
using LibIML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePredictor.ViewModel
{
    public class AddFeatureDialogViewModel : ViewModelBase
    {
        private Label _label;
        private string _word;
        private List<string> _weights;
        private string _selectedWeight;
        private bool _addIsEnabled;

        public AddFeatureDialogViewModel(Label label)
            : base()
        {
            Label = label;
            Word = ""; // use an empty string, not null, to make it easier to style the empty string with placeholder text.
            Weights = new List<string>();
            Weights.Add("Very important");
            Weights.Add("Somewhat important");
            SelectedWeight = Weights[0];
            AddIsEnabled = false;
        }

        public Label Label
        {
            get { return _label; }
            set { SetProperty<Label>(ref _label, value); }
        }

        public string Word
        {
            get { return _word; }
            set
            {
                if (SetProperty<string>(ref _word, value)) {
                    if (string.IsNullOrWhiteSpace(Word))
                        AddIsEnabled = false;
                    else
                        AddIsEnabled = true;
                }
            }
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

        public bool AddIsEnabled
        {
            get { return _addIsEnabled; }
            set { SetProperty<bool>(ref _addIsEnabled, value); }
        }
    }
}
