using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibIML
{
    [Serializable]
    public class Label : ViewModelBase, IEquatable<Label>, IComparable<Label>, IComparable
    {
        private string _userLabel;
        private string _systemLabel;
        private static readonly Label _anyLabel;

        public Label(string userLabel, string systemLabel)
        {
            UserLabel = userLabel;
            SystemLabel = systemLabel;
        }

        static Label()
        {
            _anyLabel = new Label("__anyLabel", "__anyLabel");
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

        public static Label AnyLabel
        {
            get { return _anyLabel; }
        }

        #endregion

        #region Override methods

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

        public int CompareTo(Label other)
        {
            if (Equals(other))
                return 0;

            return _userLabel.CompareTo(other._userLabel);
        }

        int IComparable.CompareTo(object other)
        {
            if (!(other is Label))
                throw new InvalidOperationException("CompareTo: Not a Label");

            return CompareTo((Label)other);
        }

        #endregion
    }
}
