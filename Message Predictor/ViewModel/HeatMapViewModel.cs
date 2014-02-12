using MessagePredictor.Model;
using MessagePredictor.View;
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
        private NewsCollection _messages;
        //private NewsCollection _unknownFolder;
        //private NewsCollection _topic1Folder;
        //private NewsCollection _topic2Folder;
        private CollectionViewSource _heatMapView;
        //private CollectionViewSource _unknownView;
        //private CollectionViewSource _topic1View;
        //private CollectionViewSource _topic2View;
        private string _toHighlight;
        private NewsItem _currentMessage;
        private MessageWindow _messageWindow;

        public HeatMapViewModel(NewsCollection messages)
            : base()
        {
            _messages = messages;
            _heatMapView = BuildCollectionViewSourceForCollection(_messages);
            //_unknownFolder = unknown;
            //_topic1Folder = topic1;
            //_topic2Folder = topic2;
            //_unknownView = BuildCollectionViewSourceForCollection(unknown);
            //_topic1View = BuildCollectionViewSourceForCollection(topic1);
            //_topic2View = BuildCollectionViewSourceForCollection(topic2);
            _currentMessage = null;
        }

        #region Properties

        //public NewsCollection UnknownFolder
        //{
        //    get { return _unknownFolder; }
        //    private set { SetProperty<NewsCollection>(ref _unknownFolder, value); }
        //}

        //public NewsCollection Topic1Folder
        //{
        //    get { return _topic1Folder; }
        //    private set { SetProperty<NewsCollection>(ref _topic1Folder, value); }
        //}

        //public NewsCollection Topic2Folder
        //{
        //    get { return _topic2Folder; }
        //    private set { SetProperty<NewsCollection>(ref _topic2Folder, value); }
        //}

        //public CollectionViewSource UnknownView
        //{
        //    get { return _unknownView; }
        //    private set { SetProperty<CollectionViewSource>(ref _unknownView, value); }
        //}

        //public CollectionViewSource Topic1View
        //{
        //    get { return _topic1View; }
        //    private set { SetProperty<CollectionViewSource>(ref _topic1View, value); }
        //}

        //public CollectionViewSource Topic2View
        //{
        //    get { return _topic2View; }
        //    private set { SetProperty<CollectionViewSource>(ref _topic2View, value); }
        //}

        public string ToHighlight
        {
            get { return _toHighlight; }
            set
            {
                if (SetProperty<string>(ref _toHighlight, value))
                {
                    OnHighlightTextChanged(new HighlightTextChangedEventArgs(_toHighlight));
                    MarkMessagesContainingWord(ToHighlight);
                    if (CurrentMessage != null)
                        CurrentMessage.HighlightWithWord(ToHighlight);
                }
            }
        }

        public NewsItem CurrentMessage
        {
            get { return _currentMessage; }
            set
            {
                NewsItem temp = _currentMessage;
                if (SetProperty<NewsItem>(ref _currentMessage, value))
                {
                    if (temp != null)
                        temp.IsSelected = false;

                    if (CurrentMessage != null)
                    {
                        CurrentMessage.IsSelected = true;
                        CurrentMessage.HighlightWithWord(ToHighlight);
                        if (_messageWindow == null)
                        {
                            _messageWindow = new MessageWindow();
                            _messageWindow.Closed += _messageWindow_Closed;
                            _messageWindow.DataContext = this;
                            _messageWindow.Owner = App.Current.MainWindow;
                            _messageWindow.Show();
                        }
                        else
                        {
                            _messageWindow.Activate();
                        }
                    }
                }
            }
        }

        #endregion

        #region Events

        public class HighlightTextChangedEventArgs : EventArgs
        {
            public readonly string Text;

            public HighlightTextChangedEventArgs(string tokens)
            {
                Text = tokens;
            }
        }

        public event EventHandler<HighlightTextChangedEventArgs> HighlightTextChanged;

        protected virtual void OnHighlightTextChanged(HighlightTextChangedEventArgs e)
        {
            if (HighlightTextChanged != null)
                HighlightTextChanged(this, e);
        }

        #endregion

        #region Private methods

        void _messageWindow_Closed(object sender, EventArgs e)
        {
            _messageWindow = null;
            CurrentMessage = null;
        }

        private void MarkMessagesContainingWord(string word)
        {
            Regex containsWord = new Regex(@"\b(" + word.Trim() + @")\b", RegexOptions.IgnoreCase);
            bool isEmpty = string.IsNullOrWhiteSpace(word);

            Stopwatch timer = new Stopwatch();
            timer.Start();
            
            using (_heatMapView.DeferRefresh())
            {
                Parallel.ForEach(_messages, (item, state, index) =>
                {
                    if (!isEmpty && containsWord.Match(item.AllText).Success)
                        item.IsHighlighted = true;
                    else
                        item.IsHighlighted = false;
                });
            }

            timer.Stop();
            Console.WriteLine("Time to highlight word: {0}", timer.Elapsed);
        }

        private CollectionViewSource BuildCollectionViewSourceForCollection(NewsCollection collection)
        {
            CollectionViewSource cvs = new CollectionViewSource();
            cvs.Source = collection;
            ListCollectionView view = cvs.View as ListCollectionView;
            view.CustomSort = new SortByHighlight();
            cvs.IsLiveSortingRequested = true;
            //cvs.SortDescriptions.Clear();
            //cvs.SortDescriptions.Add(new SortDescription("IsHighlighted", ListSortDirection.Descending));
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
