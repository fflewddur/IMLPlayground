using MessagePredictor.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MessagePredictor.ViewModel
{
    class HeatMapViewModel : ViewModelBase
    {
        private NewsCollection _unknownFolder;
        private NewsCollection _topic1Folder;
        private NewsCollection _topic2Folder;
        private string _toHighlight;

        public HeatMapViewModel(NewsCollection unknown, NewsCollection topic1, NewsCollection topic2)
            : base()
        {
            _unknownFolder = unknown;
            _topic1Folder = topic1;
            _topic2Folder = topic2;
        }

        #region Properties

        public NewsCollection UnknownFolder
        {
            get { return _unknownFolder; }
            private set { SetProperty<NewsCollection>(ref _unknownFolder, value); }
        }

        public NewsCollection Topic1Folder
        {
            get { return _topic1Folder; }
            private set { SetProperty<NewsCollection>(ref _topic1Folder, value); }
        }

        public NewsCollection Topic2Folder
        {
            get { return _topic2Folder; }
            private set { SetProperty<NewsCollection>(ref _topic2Folder, value); }
        }

        public string ToHighlight
        {
            get { return _toHighlight; }
            set
            {
                if (SetProperty<string>(ref _toHighlight, value))
                    MarkMessagesContainingWord(ToHighlight);
            }
        }

        #endregion

        #region Private methods

        private void MarkMessagesContainingWord(string word)
        {
            Regex containsWord = new Regex(@"\b(" + word.Trim() + @")\b", RegexOptions.IgnoreCase);

            foreach (NewsItem item in _unknownFolder.Concat(_topic1Folder).Concat(_topic2Folder))
            {
                if (containsWord.Match(item.AllText).Success && !string.IsNullOrWhiteSpace(word))
                    item.IsHighlighted = true;
                else
                    item.IsHighlighted = false;
            }
        }

        #endregion

    }
}
