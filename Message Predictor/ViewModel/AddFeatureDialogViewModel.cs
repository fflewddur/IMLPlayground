using GalaSoft.MvvmLight.Command;
using LibIML;
using LibIML.Features;
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

        public AddFeatureDialogViewModel(IReadOnlyList<Label> labels)
            : base()
        {
            //List<Label> localLabels = new List<Label>();
            //localLabels.Add(new Label())
            Labels = labels;
            //if (selectedLabel != null) {
            //    SelectedLabel = selectedLabel;
            //} else {
            //    SelectedLabel = Labels[0];
            //}
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
            set {
                if (SetProperty<Label>(ref _selectedLabel, value)) {
                    UpdateAddButton();
                }
            }
        }

        public string Word
        {
            get { return _word; }
            set
            {
                if (SetProperty<string>(ref _word, value)) {
                    UpdateAddButton();
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

        #region Events

        public class AddFeatureEventArgs : EventArgs
        {
            public readonly Feature Feature;
            public readonly Label Label;
            public AddFeatureEventArgs(Feature feature, Label label)
            {
                Feature = feature;
                Label = label;
            }
        }

        public event EventHandler<AddFeatureEventArgs> AddFeature;

        protected virtual void OnAddFeature(AddFeatureEventArgs e)
        {
            if (AddFeature != null)
                AddFeature(this, e);
        }

        #endregion

        #region Public methods

        public void ProcessFeatureToAdd()
        {
            Feature f = new Feature(this.Word.ToLower(), this.Labels[0], this.Labels[1], true);
            if (this.SelectedWeight == this.Weights[0]) {
                if (this.SelectedLabel == f.Topic1Importance.Label) {
                    f.Topic1Importance.WeightType = FeatureImportance.Weight.High;
                    f.Topic2Importance.UserPrior = .25;
                } else if (this.SelectedLabel == f.Topic2Importance.Label) {
                    f.Topic2Importance.WeightType = FeatureImportance.Weight.High;
                    f.Topic1Importance.UserPrior = .25;
                }
            } else if (this.SelectedWeight == this.Weights[1]) {
                if (this.SelectedLabel == f.Topic1Importance.Label) {
                    f.Topic1Importance.WeightType = FeatureImportance.Weight.Medium;
                    f.Topic2Importance.UserPrior = .25;
                } else if (this.SelectedLabel == f.Topic2Importance.Label) {
                    f.Topic2Importance.WeightType = FeatureImportance.Weight.Medium;
                    f.Topic1Importance.UserPrior = .25;
                }
            }

            OnAddFeature(new AddFeatureEventArgs(f, this.SelectedLabel));
        }

        #endregion

        private void UpdateAddButton()
        {
            if (string.IsNullOrWhiteSpace(Word) || SelectedLabel == null)
                AddIsEnabled = false;
            else
                AddIsEnabled = true;
        }
    }
}
