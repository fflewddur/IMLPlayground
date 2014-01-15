using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibIML
{
    [Serializable]
    public class Instance : ViewModelBase
    {
        private Label _label;
        private SparseVector _features;

        #region Properties

        public Label Label
        {
            get { return _label; }
            set { SetProperty<Label>(ref _label, value); }
        }

        public SparseVector Features
        {
            get { return _features; }
            set { SetProperty<SparseVector>(ref _features, value); }
        }

        #endregion
    }
}
