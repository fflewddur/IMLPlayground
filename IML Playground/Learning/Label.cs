using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IML_Playground.Learning
{
    [Serializable]
    class Label : IML_Playground.ViewModel.ViewModelBase
    {
        private string _userLabel;
        private string _systemLabel;
        private int _id;

        private static int _nextId;

        static Label()
        {
            _nextId = 1;
        }

        public Label(string userLabel, string systemLabel)
        {
            Id = _nextId;
            _nextId++;
            UserLabel = userLabel;
            SystemLabel = systemLabel;
        }

        #region Properties

        public int Id
        {
            get { return _id; }
            private set { SetProperty<int>(ref _id, value); }
        }

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
    }
}
