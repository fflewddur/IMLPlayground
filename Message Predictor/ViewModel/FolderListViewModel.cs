using LibIML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePredictor.ViewModel
{
    class FolderListViewModel : ViewModelBase
    {
        List<FolderViewModel> _folders;
        Label _unknownLabel;
        FolderViewModel _selectedFolder;

        public FolderListViewModel(List<Label> labels)
            : base()
        {
            _folders = new List<FolderViewModel>();

            // First add an "Unlabeled" folder
            _unknownLabel = new Label("Unknown", "Unknown");
            _folders.Add(new FolderViewModel(_unknownLabel));

            // Now add all of the classification labels
            foreach (Label label in labels) {
                FolderViewModel vm = new FolderViewModel(label);
                _folders.Add(vm);
            }

            //SelectedFolder = _folders[0]; // Select the first folder by default
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

        #endregion

        #region Public methods

        public void SelectFolderByIndex(int index)
        {
            SelectedFolder = _folders[index];
        }

        public void UpdateFolderCounts(IEnumerable<IInstance> messages)
        {
            foreach (FolderViewModel folder in _folders) {
                int count = 0, correct = 0;
                Label label;

                if (folder.Label == _unknownLabel) {
                    label = null;
                } else {
                    label = folder.Label;
                }

                foreach (IInstance message in messages) {
                    if (message.UserLabel == label) {
                        count++;
                        if (message.Prediction.Label == label) {
                            correct++;
                        }
                    }
                }

                folder.MessageCount = count;
                folder.PriorCorrectPredictionCount = folder.CorrectPredictionCount;
                folder.CorrectPredictionCount = correct;
            }
        }

        #endregion
    }
}
