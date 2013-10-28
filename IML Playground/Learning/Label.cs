using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IML_Playground.Learning
{
    [Serializable]
    class Label : IML_Playground.ViewModel.ViewModelBase, IEquatable<Label>
    {
        private string _userLabel;
        private string _systemLabel;

        public Label(string userLabel, string systemLabel)
        {
            UserLabel = userLabel;
            SystemLabel = systemLabel;
        }

        #region Properties

        /// <summary>
        /// The user-visible string for denoting different classes.
        /// </summary>
        public string UserLabel
        {
            get { return _userLabel; }
            private set { SetProperty<string>(ref _userLabel, value); }
        }

        /// <summary>
        /// A string used internally to denote different classes.
        /// </summary>
        public string SystemLabel
        {
            get { return _systemLabel; }
            private set { SetProperty<string>(ref _systemLabel, value); }
        }
        
        #endregion

        public override string ToString()
        {
            return UserLabel;
        }

        public override bool Equals(object other)
        {
            if (!(other is Label))
                return false;

            return Equals((Label)other);
        }

        public bool Equals(Label other)
        {
            return SystemLabel == other.SystemLabel && UserLabel == other.UserLabel;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + SystemLabel.GetHashCode();
            hash = hash * 31 + UserLabel.GetHashCode();
            return hash;
        }
    }
}
