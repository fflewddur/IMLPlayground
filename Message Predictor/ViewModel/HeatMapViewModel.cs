using MessagePredictor.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MessagePredictor.ViewModel
{
    class HeatMapViewModel : ViewModelBase
    {
        private NewsCollection _unknownFolder;
        private NewsCollection _topic1Folder;
        private NewsCollection _topic2Folder;
        private CollectionViewSource _unknownView;
        private CollectionViewSource _topic1View;
        private CollectionViewSource _topic2View;
        private string _toHighlight;

        public HeatMapViewModel(NewsCollection unknown, NewsCollection topic1, NewsCollection topic2)
            : base()
        {
            _unknownFolder = unknown;
            _topic1Folder = topic1;
            _topic2Folder = topic2;
            _unknownView = BuildCollectionViewSourceForCollection(unknown);
            _topic1View = BuildCollectionViewSourceForCollection(topic1);
            _topic2View = BuildCollectionViewSourceForCollection(topic2);
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

        public CollectionViewSource UnknownView
        {
            get { return _unknownView; }
            private set { SetProperty<CollectionViewSource>(ref _unknownView, value); }
        }

        public CollectionViewSource Topic1View
        {
            get { return _topic1View; }
            private set { SetProperty<CollectionViewSource>(ref _topic1View, value); }
        }

        public CollectionViewSource Topic2View
        {
            get { return _topic2View; }
            private set { SetProperty<CollectionViewSource>(ref _topic2View, value); }
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
            bool isEmpty = string.IsNullOrWhiteSpace(word);

            //Stopwatch timer = new Stopwatch();
            //timer.Start();

                Parallel.ForEach(_unknownFolder.Concat(_topic1Folder).Concat(_topic2Folder), (item, state, index) =>
                {
                    if (!isEmpty && containsWord.Match(item.AllText).Success)
                        item.IsHighlighted = true;
                    else
                        item.IsHighlighted = false;
                });

            //timer.Stop();
            //Console.WriteLine("Time to highlight word: {0}", timer.Elapsed);
        }

        private CollectionViewSource BuildCollectionViewSourceForCollection(NewsCollection collection)
        {
            CollectionViewSource cvs = new CollectionViewSource();
            cvs.Source = collection;
            //ListCollectionView view = cvs.View as ListCollectionView;
            //view.CustomSort = new SortByHighlight();
            cvs.IsLiveSortingRequested = true;
            cvs.SortDescriptions.Clear();
            cvs.SortDescriptions.Add(new SortDescription("IsHighlighted", ListSortDirection.Descending));
            //view.IsLiveSorting = true;

            return cvs;
        }

        private class SortByHighlight : IComparer<NewsItem>, IComparer
        {
            public int Compare(object a, object b)
            {
                NewsItem na = a as NewsItem;
                NewsItem nb = b as NewsItem;
                if (na == null || nb == null)
                    throw new ArgumentException("SortByHighlight can only sort NewsItem objects");
                else
                    return Compare(na, nb);
            }

            public int Compare(NewsItem a, NewsItem b)
            {
                if (a.IsHighlighted && !b.IsHighlighted)
                    return -1;
                else if (!a.IsHighlighted && b.IsHighlighted)
                    return 1;
                else
                    return 0;
            }
        }

        #endregion

    }
}
