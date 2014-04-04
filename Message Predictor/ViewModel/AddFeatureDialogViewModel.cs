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
        private IReadOnlyList<Label> _labels;
        private string _word;
        private List<string> _weights;
        private Label _selectedLabel;
        private string _selectedWeight;
        private bool _addIsEnabled;

        public AddFeatureDialogViewModel(IReadOnlyList<Label> labels, Label selectedLabel)
            : base()
        {
            Labels = labels;
            if (selectedLabel != null) {
                SelectedLabel = selectedLabel;
            } else {
                SelectedLabel = Labels[0];
            }
            Word = ""; // use an empty string, not null, to make it easier to style the empty string with placeholder text.
            Weights = new List<string>();
            Weights.Add("Very important");
            Weights.Add("Somewhat important");
            SelectedWeight = Weights[0];
            AddIsEnabled = false;
        }

        public IReadOnlyList<Label> Labels
        {
            get { return _labels; }
            set { SetProperty<IReadOnlyList<Label>>(ref _labels, value); }
        }

        public Label SelectedLabel
        {
            get { return _selectedLabel; }
            set { SetProperty<Label>(ref _selectedLabel, value); }
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
