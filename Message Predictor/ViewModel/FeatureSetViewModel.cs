using LibIML;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePredictor.ViewModel
{
    class FeatureSetViewModel : ViewModelBase
    {
        ObservableCollection<Feature> _featureSet;

        public FeatureSetViewModel() : base()
        { }

        public ObservableCollection<Feature> FeatureSet
        {
            get { return _featureSet; }
            set { SetProperty<ObservableCollection<Feature>>(ref _featureSet, value); }
        }
    }
}
