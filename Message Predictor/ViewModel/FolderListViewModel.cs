using LibIML;
using MessagePredictor.Model;
using System;
using System.Collections.Generic;

namespace MessagePredictor.ViewModel
{
    public class FolderListViewModel : ViewModelBase
    {
        private List<FolderViewModel> _folders;
        private Label _unknownLabel;
        private FolderViewModel _selectedFolder;

        public FolderListViewModel(IReadOnlyList<Evaluator> evaluators)
            : base()
        {
            _folders = new List<FolderViewModel>();

            // First add an "Unlabeled" folder
            _unknownLabel = new Label("Unknown", "Unknown");
            FolderViewModel vm = new FolderViewModel(_unknownLabel, null);
            vm.SelectedMessageChanged += vm_SelectedMessageChanged;
            _folders.Add(vm);

            // Now add all of the classification labels
            foreach (Evaluator evaluator in evaluators) {
                vm = new FolderViewModel(evaluator.Label, evaluator);
                vm.SelectedMessageChanged += vm_SelectedMessageChanged;
                _folders.Add(vm);
            }
        }

        void vm_SelectedMessageChanged(object sender, FolderViewModel.SelectedMessageChangedEventArgs e)
        {
            OnSelectedMessageChanged(e);
        }

        #region Properties

        public List<FolderViewModel> Folders
        {
            get { return _folders; }
            private set { SetProperty<List<FolderViewModel>>(ref _folders, value); }
        }

        public Label UnknownLabel
        {
            get { return _unknownLabel; }
        }

        public FolderViewModel SelectedFolder
        {
            get { return _selectedFolder; }
            set
            {
                if (SetProperty<FolderViewModel>(ref _selectedFolder, value)) {
                    OnSelectedFolderChanged(new SelectedFolderChangedEventArgs(value));
                }
            }
        }

        #endregion

        #region Events

        public class SelectedFolderChangedEventArgs : EventArgs
        {
            public readonly FolderViewModel Folder;

            public SelectedFolderChangedEventArgs(FolderViewModel folder)
            {
                Folder = folder;
            }
        }

        public event EventHandler<SelectedFolderChangedEventArgs> SelectedFolderChanged;

        protected virtual void OnSelectedFolderChanged(SelectedFolderChangedEventArgs e)
        {
            if (SelectedFolderChanged != null)
                SelectedFolderChanged(this, e);
        }

        public event EventHandler<FolderViewModel.SelectedMessageChangedEventArgs> SelectedMessageChanged;

        protected virtual void OnSelectedMessageChanged(FolderViewModel.SelectedMessageChangedEventArgs e)
        {
            if (SelectedMessageChanged != null)
                SelectedMessageChanged(this, e);
        }

        #endregion

        #region Public methods

        public void SelectFolderByIndex(int index)
        {
            SelectedFolder = _folders[index];
        }

        /// <summary>
        /// Count the number of messages in this folder.
        /// </summary>
        /// <param name="messages">The set of all messages (in all folders)</param>
        public void UpdateFolderCounts(IEnumerable<IInstance> messages)
        {
            foreach (FolderViewModel folder in _folders) {
                int count = 0;
                Label label;

                if (folder.Label == _unknownLabel) {
                    label = null;
                } else {
                    label = folder.Label;
                }

                foreach (IInstance message in messages) {
                    if (message.UserLabel == label) {
                        count++;
                    }
                }

                folder.MessageCount = count;
            }
        }

        #endregion
    }
}
