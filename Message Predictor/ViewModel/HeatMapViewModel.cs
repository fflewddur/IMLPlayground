using LibIML;
using MessagePredictor.Converters;
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
        private CollectionViewSource _heatMapView;
        private string _toHighlight;
        private NewsItem _currentMessage;
        private MessageWindow _messageWindow;
        private string _tooltipContent;
        private Label _unknownLabel;
        private Logger _logger;

        public HeatMapViewModel(NewsCollection messages, Label unknownLabel, Logger logger)
            : base()
        {
            _logger = logger;
            _messages = messages;
            _heatMapView = BuildCollectionViewSourceForCollection(_messages);
            _currentMessage = null;
            _unknownLabel = unknownLabel;
            _toHighlight = "";
        }

        #region Properties

        public CollectionViewSource HeatMapView
        {
            get { return _heatMapView; }
            private set { SetProperty<CollectionViewSource>(ref _heatMapView, value); }
        }

        public string ToHighlight
        {
            get { return _toHighlight; }
            set
            {
                if (SetProperty<string>(ref _toHighlight, value)) {
                    Console.WriteLine("Updated ToHighlight to {0}", value);
                    OnHighlightTextChanged(new HighlightTextChangedEventArgs(_toHighlight));
                    MarkMessagesContainingWord(ToHighlight);
                    UpdateTooltipContent(ToHighlight);
                    if (CurrentMessage != null) {
                        CurrentMessage.HighlightWithWord(ToHighlight);
                    }
                }
            }
        }

        public NewsItem CurrentMessage
        {
            get { return _currentMessage; }
            set
            {
                if (SetProperty<NewsItem>(ref _currentMessage, value)) {
                    if (CurrentMessage != null) {
                        CurrentMessage.HighlightWithWord(ToHighlight);
                        if (_messageWindow == null) {
                            _logger.Writer.WriteStartElement("OpenMessageWindow");
                            _logger.Writer.WriteAttributeString("message", CurrentMessage.Id.ToString());
                            if (CurrentMessage.UserLabel != null) {
                                _logger.Writer.WriteAttributeString("userFolder", CurrentMessage.UserLabel.ToString());
                            } else {
                                _logger.Writer.WriteAttributeString("userFolder", "Unknown");
                            }
                            _logger.Writer.WriteAttributeString("predictedTopic", CurrentMessage.Prediction.Label.ToString());
                            _logger.Writer.WriteAttributeString("isHighlighted", CurrentMessage.IsHighlighted.ToString());
                            _logger.logTime();
                            _messageWindow = new MessageWindow();
                            _messageWindow.Closed += _messageWindow_Closed;
                            _messageWindow.DataContext = this;
                            _messageWindow.Owner = App.Current.MainWindow;
                            _messageWindow.Show();
                        } else {
                            _logger.logEndElement(); // We changed our message
                            _logger.Writer.WriteStartElement("OpenMessageWindow");
                            _logger.Writer.WriteAttributeString("message", CurrentMessage.Id.ToString());
                            if (CurrentMessage.UserLabel != null) {
                                _logger.Writer.WriteAttributeString("userFolder", CurrentMessage.UserLabel.ToString());
                            } else {
                                _logger.Writer.WriteAttributeString("userFolder", "Unknown");
                            }
                            _logger.Writer.WriteAttributeString("predictedTopic", CurrentMessage.Prediction.Label.ToString());
                            _logger.Writer.WriteAttributeString("isHighlighted", CurrentMessage.IsHighlighted.ToString());
                            _logger.logTime();
                            _messageWindow.Activate();
                        }
                    }
                }
            }
        }

        public string TooltipContent
        {
            get { return _tooltipContent; }
            private set { SetProperty<string>(ref _tooltipContent, value); }
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

        #region Public methods

        #endregion

        #region Private methods

        void _messageWindow_Closed(object sender, EventArgs e)
        {
            if (_messageWindow != null) {
                _logger.logEndElement();
                _messageWindow.Close();
                _messageWindow = null;
                CurrentMessage = null;
            }
        }

        private void MarkMessagesContainingWord(string word)
        {
            // Null strings crash our Regex
            if (word == null) {
                word = "";
            }

            bool isEmpty = string.IsNullOrWhiteSpace(word);
            string wordPattern = Regex.Replace(word.Trim(), @"\s+", @"\s(\r?\n)?");
            Regex containsWord = new Regex(@"\b(" + wordPattern + @")\b", RegexOptions.IgnoreCase);

            Stopwatch timer = new Stopwatch();
            timer.Start();

            using (_heatMapView.DeferRefresh()) {
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

        private void UpdateTooltipContent(string word)
        {
            Dictionary<Label, int> countPerLabel = new Dictionary<Label, int>();

            foreach (NewsItem item in _messages) {
                int count;
                Label label = item.UserLabel;
                if (label == null) {
                    label = _unknownLabel;
                }
                countPerLabel.TryGetValue(label, out count);
                if (item.IsHighlighted) {
                    count++;
                }
                countPerLabel[label] = count;
            }

            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<Label, int> pair in countPerLabel) {
                sb.AppendFormat("{0} messages in '{1}' contain '{2}'.\n\n", pair.Value, pair.Key, word);
            }

            TooltipContent = sb.ToString();
        }

        private CollectionViewSource BuildCollectionViewSourceForCollection(NewsCollection collection)
        {
            CollectionViewSource cvs = new CollectionViewSource();
            cvs.Source = collection;
            ListCollectionView view = cvs.View as ListCollectionView;
            //view.CustomSort = new SortByHighlight();
            view.GroupDescriptions.Clear();
            view.GroupDescriptions.Add(new PropertyGroupDescription("UserLabel", new LabelToStringConverter()));
            view.IsLiveGrouping = true;
            //cvs.IsLiveGroupingRequested = true;

            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription("UserLabel", ListSortDirection.Descending));
            view.SortDescriptions.Add(new SortDescription("IsHighlighted", ListSortDirection.Descending));
            view.IsLiveSorting = true;
            //cvs.IsLiveSortingRequested = true;

            return cvs;
        }

        //private class SortByHighlight : IComparer<NewsItem>, IComparer
        //{
        //    public int Compare(object a, object b)
        //    {
        //        NewsItem na = a as NewsItem;
        //        NewsItem nb = b as NewsItem;
        //        if (na == null || nb == null)
        //            throw new ArgumentException("SortByHighlight can only sort NewsItem objects");
        //        else
        //            return Compare(na, nb);
        //    }

        //    public int Compare(NewsItem a, NewsItem b)
        //    {
        //        if (a.IsHighlighted && !b.IsHighlighted)
        //            return -1;
        //        else if (!a.IsHighlighted && b.IsHighlighted)
        //            return 1;
        //        else
        //            return 0;
        //    }
        //}

        #endregion

    }
}
