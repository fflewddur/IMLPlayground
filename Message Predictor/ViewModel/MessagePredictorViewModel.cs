using GalaSoft.MvvmLight.Command;
using LibIML;
using LibIML.Features;
using MessagePredictor.Model;
using MessagePredictor.View;
using MessagePredictor.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;

namespace MessagePredictor
{
    public class MessagePredictorViewModel : ViewModelBase
    {
        // Models
        private NewsCollection _messages;
        private NewsCollection _testMessages;
        private List<Label> _labels;
        private Vocabulary _vocab;
        private MultinomialNaiveBayesFeedbackClassifier _classifier;
        private Logger _logger;
        private LinkedList<UserAction> _actions;
        private int _nMessagesLabeled;
        private int _nFeaturesAdded;

        // Viewmodels (and data for this viewmodel)
        private FolderListViewModel _folderListVM;
        private MessageViewModel _messageVM;
        private FeatureSetViewModel _featureSetVM;
        private HeatMapViewModel _heatMapVM;
        private EvaluatorViewModel _evaluatorVM;
        private CollectionViewSource _messageListViewSource;
        private CollectionViewSource _folderListViewSource;
        private List<string> _textToHighlight;
        private DispatcherTimer _logTimer;
        private int _logTimerCount;
        private DispatcherTimer _runtimeTimer;

        // Application settings (set at startup)
        private int _desiredVocabSize;
        private App.Condition _condition;
        private bool _showExplanations;

        // UI preferences (user-modifiable)
        private bool _autoUpdatePredictions;
        private bool _onlyShowRecentChanges;
        private int _logEachNLabels = 5;
        private int _logEachNFeatures = 5;

        public MessagePredictorViewModel(Logger logger)
        {
            Console.WriteLine("MessagePredictorViewModel() start");
            Stopwatch timer = new Stopwatch();

            _nMessagesLabeled = 0;
            _nFeaturesAdded = 0;

            _logger = logger;
            _actions = new LinkedList<UserAction>(); // Store undo-able user actions

            timer.Start();
            _messages = LoadDataset();
            _testMessages = LoadTestDataset(); // Also load the test set, but use the existing labels
            timer.Stop();
            Console.WriteLine("Time to load data set: {0}", timer.Elapsed);

            timer.Restart();
            LabelTrainingSet(_messages, _labels[0], (int)App.Current.Properties[App.PropertyKey.Topic1TrainSize]);
            LabelTrainingSet(_messages, _labels[1], (int)App.Current.Properties[App.PropertyKey.Topic2TrainSize]);
            timer.Stop();
            Console.WriteLine("Time to build topic training sets: {0}", timer.Elapsed);

            // Build a vocab from our training set
            timer.Restart();
            _desiredVocabSize = (int)App.Current.Properties[App.PropertyKey.Topic1VocabSize] + (int)App.Current.Properties[App.PropertyKey.Topic2VocabSize];
            _vocab = Vocabulary.CreateVocabulary(FilterToTrainingSet(_messages), _labels, Vocabulary.Restriction.HighIG, _desiredVocabSize);
            timer.Stop();
            Console.WriteLine("Time to build vocab: {0}", timer.Elapsed);

            // Convenient accessor for our treatment condition
            _condition = (App.Condition)App.Current.Properties[App.PropertyKey.Condition];
            if (_condition == App.Condition.Treatment) {
                ShowExplanations = true;
            } else {
                ShowExplanations = false;
            }

            // Update our test set in light of our new vocabulary
            timer.Restart();
            Parallel.ForEach(FilterToTestSet(_messages), (instance, state, index) =>
            {
                PorterStemmer stemmer = new PorterStemmer(); // PorterStemmer isn't threadsafe, so we need one for each operation.
                //Dictionary<string, int> tokens = Tokenizer.TokenizeAndStem(instance.AllText, stemmer) as Dictionary<string, int>;
                Dictionary<string, int> tokens = Tokenizer.Tokenize(instance.AllText) as Dictionary<string, int>;
                instance.TokenCounts = tokens;
                instance.ComputeFeatureVector(_vocab, true);
            });
            timer.Stop();
            Console.WriteLine("Time to update data for new vocab: {0}", timer.Elapsed);

            // Build a classifier
            _classifier = new MultinomialNaiveBayesFeedbackClassifier(_labels, _vocab);

            _evaluatorVM = new EvaluatorViewModel(_labels);

            _folderListVM = new FolderListViewModel(_evaluatorVM.Evaluators);
            _folderListVM.SelectedFolderChanged += _folderListVM_SelectedFolderChanged;
            _folderListVM.SelectedMessageChanged += _folderListVM_SelectedMessageChanged;

            _messageVM = new MessageViewModel(_labels);

            _featureSetVM = new FeatureSetViewModel(_classifier, _vocab, _labels, _logger);
            _featureSetVM.FeatureAdded += _featureSetVM_FeatureAdded;
            _featureSetVM.FeatureRemoved += _featureSetVM_FeatureRemoved;
            _featureSetVM.FeatureTextEdited += _featureSetVM_FeatureTextEdited;
            _featureSetVM.FeaturePriorEdited += _featureSetVM_FeaturePriorsEdited;

            _heatMapVM = new HeatMapViewModel(this, _messages, _folderListVM.UnknownLabel, _logger);
            _heatMapVM.HighlightTextChanged += _heatMapVM_HighlightTextChanged;

            _messageListViewSource = new CollectionViewSource();
            _messageListViewSource.Source = _messages;
            _messageListViewSource.View.CurrentChanged += View_CurrentChanged;

            // Setup our Commands
            ManuallyUpdatePredictions = new RelayCommand(PerformUpdatePredictions, CanPerformUpdatePredictions);
            LabelMessage = new RelayCommand<Label>(PerformLabelMessage, CanPerformLabelMessage);
            FindText = new RelayCommand<string>(PerformFindText, CanPerformFindText);
            ClearFindText = new RelayCommand(PerformClearFindText, CanPerformClearFindText);

            // Start with our current folder pointing at the collection of unlabeled items.
            AutoUpdatePredictions = (bool)App.Current.Properties[MessagePredictor.App.PropertyKey.AutoUpdatePredictions];

            // Evaluate the classifier (so we can show predictions to the user)
            UpdateVocabForce();
            UpdatePredictions();
            UpdatePredictions(); // Do this twice to avoid showing any change indicators at the start

            // Regularly log classifier accuracy
            _logTimerCount = 0;
            _logTimer = new DispatcherTimer();
            _logTimer.Tick += _logTimer_Tick;
            _logTimer.Interval = new TimeSpan(0, 0, (int)App.Current.Properties[MessagePredictor.App.PropertyKey.EvalInterval]);
            if ((int)App.Current.Properties[MessagePredictor.App.PropertyKey.EvalInterval] > 0) {
                _logTimer.Start();
            }

            // Exit the application when time is up
            _runtimeTimer = new DispatcherTimer();
            _runtimeTimer.Tick += _runtimeTimer_Tick;
            _runtimeTimer.Interval = new TimeSpan(0, 0, (int)App.Current.Properties[MessagePredictor.App.PropertyKey.TimeLimit]);
            if ((int)App.Current.Properties[MessagePredictor.App.PropertyKey.TimeLimit] > 0) {
                _runtimeTimer.Start();
            }

            Console.WriteLine("MessagePredictorViewModel() end");
        }

        #region Properties

        public string Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        public bool ShowExplanations
        {
            get { return _showExplanations; }
            private set { SetProperty<bool>(ref _showExplanations, value); }
        }

        public CollectionViewSource MessageListViewSource
        {
            get { return _messageListViewSource; }
            private set { SetProperty<CollectionViewSource>(ref _messageListViewSource, value); }
        }

        public CollectionViewSource FolderListViewSource
        {
            get { return _folderListViewSource; }
            private set { SetProperty<CollectionViewSource>(ref _folderListViewSource, value); }
        }

        public IReadOnlyList<Label> Labels
        {
            get { return _labels; }
        }

        public bool AutoUpdatePredictions
        {
            get { return _autoUpdatePredictions; }
            set
            {
                if (SetProperty<bool>(ref _autoUpdatePredictions, value)) {
                    _logger.Writer.WriteStartElement("ChangedAutoUpdatePredictions");
                    _logger.Writer.WriteAttributeString("value", value.ToString());
                    _logger.logTime();

                    ManuallyUpdatePredictions.RaiseCanExecuteChanged();

                    _logger.logEndElement();
                }
            }
        }

        public bool OnlyShowRecentChanges
        {
            get { return _onlyShowRecentChanges; }
            set
            {
                if (SetProperty<bool>(ref _onlyShowRecentChanges, value)) {
                    _logger.Writer.WriteStartElement("ChangedOnlyShowRecentChanges");
                    _logger.Writer.WriteAttributeString("value", value.ToString());
                    _logger.logTime();

                    UpdateFilters(_onlyShowRecentChanges);

                    _logger.logEndElement();
                }
            }
        }

        public List<string> TextToHighlight
        {
            get { return _textToHighlight; }
            private set
            {
                SetProperty<List<string>>(ref _textToHighlight, value);
            }
        }

        public MessageViewModel MessageVM
        {
            get { return _messageVM; }
            private set { SetProperty<MessageViewModel>(ref _messageVM, value); }
        }

        public FolderListViewModel FolderListVM
        {
            get { return _folderListVM; }
            private set { SetProperty<FolderListViewModel>(ref _folderListVM, value); }
        }

        public FeatureSetViewModel FeatureSetVM
        {
            get { return _featureSetVM; }
            private set { SetProperty<FeatureSetViewModel>(ref _featureSetVM, value); }
        }

        public HeatMapViewModel HeatMapVM
        {
            get { return _heatMapVM; }
            private set { SetProperty<HeatMapViewModel>(ref _heatMapVM, value); }
        }

        public EvaluatorViewModel EvaluatorVM
        {
            get { return _evaluatorVM; }
            private set { SetProperty<EvaluatorViewModel>(ref _evaluatorVM, value); }
        }

        #endregion

        #region Events

        public class SelectedMessageProgrammaticallyChangedEventArgs : EventArgs
        {
            public readonly NewsItem Message;

            public SelectedMessageProgrammaticallyChangedEventArgs(NewsItem message)
            {
                Message = message;
            }
        }

        public event EventHandler<SelectedMessageProgrammaticallyChangedEventArgs> SelectedMessageProgrammaticallyChanged;

        protected virtual void OnSelectedMessageProgrammaticallyChanged(SelectedMessageProgrammaticallyChangedEventArgs e)
        {
            if (SelectedMessageProgrammaticallyChanged != null)
                SelectedMessageProgrammaticallyChanged(this, e);
        }

        #endregion

        #region Commands

        public RelayCommand ManuallyUpdatePredictions { get; private set; }
        public RelayCommand<Label> LabelMessage { get; private set; }
        public RelayCommand<string> FindText { get; private set; }
        public RelayCommand ClearFindText { get; private set; }

        private bool CanPerformUpdatePredictions()
        {
            if (AutoUpdatePredictions)
                return false;
            else {
                // TODO also make sure something has changed
                return true;
            }
        }

        private void PerformUpdatePredictions()
        {
            Console.WriteLine("Update predictions");
            _logger.Writer.WriteStartElement("ManuallyUpdatePredictions");
            _logger.logTime();

            UpdatePredictions();

            _logger.logEndElement();
        }

        private bool CanPerformLabelMessage(Label label)
        {
            return true;
        }

        private void PerformLabelMessage(Label label)
        {
            NewsItem item = MessageVM.Message;
            Mouse.OverrideCursor = Cursors.Wait;

            _logger.Writer.WriteStartElement("LabelMessage");
            _logger.Writer.WriteAttributeString("id", item.Id.ToString());
            _logger.Writer.WriteAttributeString("label", label.ToString());
            if (item.UserLabel != null) {
                _logger.Writer.WriteAttributeString("priorLabel", item.UserLabel.ToString());
            } else {
                _logger.Writer.WriteAttributeString("priorLabel", "Unknown");
            }
            _logger.Writer.WriteAttributeString("time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            // Don't do anything if the message already has the desired label
            if (item.UserLabel == label || (item.UserLabel == null && label == FolderListVM.UnknownLabel)) {
                Mouse.OverrideCursor = null;
                _logger.Writer.WriteAttributeString("wrongFolder", "False");
                _logger.Writer.WriteAttributeString("sameFolder", "True");
                _logger.logEndElement();

                Dialog d = new Dialog();
                d.DialogTitle = string.Format("Already in {0}", label);
                d.DialogMessage = string.Format("This message is already in the {0} folder.", label);
                d.Owner = App.Current.MainWindow;
                d.ShowDialog();
                return;
            }

            // Make sure the user is moving the item to the correct folder
            bool wrongFolder = false;
            if (label != FolderListVM.UnknownLabel && label != item.GroundTruthLabel) {
                //Mouse.OverrideCursor = null;
                wrongFolder = true;

                //_logger.Writer.WriteAttributeString("wrongFolder", "True");
                //_logger.Writer.WriteAttributeString("sameFolder", "False");
                //_logger.logEndElement();

                //Dialog d = new Dialog();
                //d.DialogTitle = string.Format("Not about {0}", label);
                //d.DialogMessage = string.Format("For this experiment, we can't let you move this message to the {0} folder.\n\nThe person who wrote this message says it's about {1}.", label, item.GroundTruthLabel);
                //d.Owner = App.Current.MainWindow;
                //d.ShowDialog();
                //return;
            }

            _logger.Writer.WriteAttributeString("sameFolder", "False");
            _logger.Writer.WriteAttributeString("wrongFolder", wrongFolder.ToString());

            // If we're moving the item to Unknown, it's label should be null
            if (label == FolderListVM.UnknownLabel) {
                label = null;
            }
            item.UserLabel = label;

            // Let the view know we updated this data
            IEditableCollectionView view = _messageListViewSource.View as IEditableCollectionView;
            view.EditItem(item);
            view.CommitEdit();

            // Try to select the next message. If there is no next message, select the previous message.
            //if (!_messageListViewSource.View.MoveCurrentToNext()) {
            //    _messageListViewSource.View.MoveCurrentToPrevious();
            //}

            if (label == null) {
                // If we moved something to Unknown, remove it from our vocabulary
                _vocab.RemoveInstanceTokens(item);
            } else {
                // If we moved something from unknown to a different folder, add it to our vocabulary
                // (This is safe because we can't move items between the two topic folders--they can only be filed
                // to the correct folder.)
                _vocab.AddInstanceTokens(item);
            }

            // If autoupdate is on, retrain the classifier
            if (AutoUpdatePredictions) {
                UpdatePredictions();
            } else {
                // Still need to update our view
                _folderListVM.UpdateFolderCounts(_messages);
                //_messageListViewSource.View.Refresh(); // FIXME super slow
            }

            _logger.logEndElement();

            _nMessagesLabeled++;
            if (_nMessagesLabeled % _logEachNLabels == 0) {
                Mouse.OverrideCursor = Cursors.Wait;
                LogInterval("messagesLabeled", _nMessagesLabeled);
            }
            Mouse.OverrideCursor = null;
        }

        private bool CanPerformFindText(string text)
        {
            if (text != null && text.Length > 0) {
                return true;
            } else {
                return false;
            }
        }

        private void PerformFindText(string text)
        {
            HeatMapVM.ToHighlight = text;
        }

        private bool CanPerformClearFindText()
        {
            return (HeatMapVM.ToHighlight != null && HeatMapVM.ToHighlight.Length > 0);
        }

        private void PerformClearFindText()
        {
            HeatMapVM.ToHighlight = "";
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Select the first message in the first folder.
        /// </summary>
        public void SelectDefaultMessage()
        {
            _folderListVM.SelectFolderByIndex(0);
        }

        /// <summary>
        /// Select the given message.
        /// </summary>
        /// <param name="message"></param>
        public void SelectMessage(NewsItem message)
        {
            _folderListVM.SelectMessage(message);
            OnSelectedMessageProgrammaticallyChanged(new SelectedMessageProgrammaticallyChangedEventArgs(message));
        }

        public void LogMessageScrolled(double change, double offset)
        {
            _logger.Writer.WriteStartElement("MessageScrolled");
            _logger.Writer.WriteAttributeString("change", change.ToString());
            _logger.Writer.WriteAttributeString("offset", offset.ToString());
            _logger.logTime();
            _logger.logEndElement();
        }

        public void LogMessageListScrolled(double change, double offset)
        {
            _logger.Writer.WriteStartElement("MessageListScrolled");
            _logger.Writer.WriteAttributeString("change", change.ToString());
            _logger.Writer.WriteAttributeString("offset", offset.ToString());
            _logger.logTime();
            _logger.logEndElement();
        }

        public void LogMessageListSorted(string sortField, string direction)
        {
            _logger.Writer.WriteStartElement("MessageListSorted");
            _logger.Writer.WriteAttributeString("field", sortField);
            _logger.Writer.WriteAttributeString("direction", direction);
            _logger.logTime();
            _logger.logEndElement();
        }

        public void LogFeatureSet()
        {
            _classifier.LogFeatureSet(_logger.Writer);
        }

        public void LogTrainingSet()
        {
            _classifier.LogTrainingSet(_logger.Writer);
        }

        public void LogClassifierEvaluation(EvaluatorViewModel evaluatorVM, Vocabulary vocab, string elementName, 
                                            string datasetName, IEnumerable<IInstance> messages, int order)
        {
            Label positive = Labels[0];
            Label negative = Labels[1];

            evaluatorVM.EvaluateClassifier(messages, positive, negative);
            IEnumerable<IInstance> trainingSet = FilterToTrainingSet(messages);
            int trainingSetSize = trainingSet.Count();
            int positiveLabelCount = FilterToLabel(trainingSet, positive).Count();
            int negativeLabelCount = FilterToLabel(trainingSet, negative).Count();
            _logger.Writer.WriteStartElement(elementName);
            _logger.Writer.WriteAttributeString("dataset", datasetName);
            _logger.Writer.WriteAttributeString("order", order.ToString());
            _logger.Writer.WriteAttributeString("vocabSize", vocab.Count.ToString());
            _logger.Writer.WriteAttributeString("trainingSetSize", trainingSetSize.ToString());
            _logger.logTime();
            _logger.Writer.WriteStartElement("PositiveLabel");
            _logger.Writer.WriteAttributeString("count", positiveLabelCount.ToString());
            _logger.Writer.WriteString(positive.ToString());
            _logger.Writer.WriteEndElement();
            _logger.Writer.WriteStartElement("NegativeLabel");
            _logger.Writer.WriteAttributeString("count", negativeLabelCount.ToString());
            _logger.Writer.WriteString(negative.ToString());
            _logger.Writer.WriteEndElement();

            _logger.Writer.WriteElementString("TruePositives", evaluatorVM.TruePositives.ToString());
            _logger.Writer.WriteElementString("TrueNegatives", evaluatorVM.TrueNegatives.ToString());
            _logger.Writer.WriteElementString("FalsePositives", evaluatorVM.FalsePositives.ToString());
            _logger.Writer.WriteElementString("FalseNegatives", evaluatorVM.FalseNegatives.ToString());
            _logger.Writer.WriteElementString("F1Positive", evaluatorVM.F1Positive.ToString());
            _logger.Writer.WriteElementString("F1Negative", evaluatorVM.F1Negative.ToString());
            _logger.Writer.WriteElementString("F1Weighted", evaluatorVM.F1Weighted.ToString());
            _logger.logEndElement();
        }

        public void LogClassifierEvaluationTraining(int order = -1)
        {
            LogClassifierEvaluation(_evaluatorVM, _vocab, "Evaluation", "training", _messages, order);
        }

        public void LogClassifierEvaluationTest(int order = -1)
        {
            LogClassifierEvaluation(_evaluatorVM, _vocab, "Evaluation", "test", _testMessages, order);
        }

        /// <summary>
        /// Build a classifier/vocabulary w/ all available features based on the training set, then log its accuracy.
        /// </summary>
        public void LogClassifierBoW()
        {
            Console.WriteLine("LogClassifierBow() begin");
            NewsCollection messages = NewsCollection.CreateFromExisting(_messages);
            NewsCollection testMessages = NewsCollection.CreateFromExisting(_testMessages);
            IEnumerable<IInstance> trainingMessages = FilterToTrainingSet(messages);
            Vocabulary v = Vocabulary.CreateVocabulary(trainingMessages, _labels, Vocabulary.Restriction.None, -1, false);

            // Training set
            Parallel.ForEach(messages, (instance, state, index) =>
            {
                PorterStemmer stemmer = new PorterStemmer(); // PorterStemmer isn't threadsafe, so we need one for each operation.
                Dictionary<string, int> tokens = Tokenizer.Tokenize(instance.AllText) as Dictionary<string, int>;
                instance.TokenCounts = tokens;
                instance.ComputeFeatureVector(v, true);
            });

            // Test set
            Parallel.ForEach(testMessages, (instance, state, index) =>
            {
                PorterStemmer stemmer = new PorterStemmer(); // PorterStemmer isn't threadsafe, so we need one for each operation.
                Dictionary<string, int> tokens = Tokenizer.Tokenize(instance.AllText) as Dictionary<string, int>;
                instance.TokenCounts = tokens;
                instance.ComputeFeatureVector(v, true);
            });

            MultinomialNaiveBayesFeedbackClassifier classifier = new MultinomialNaiveBayesFeedbackClassifier(_labels, v);
            classifier.ClearInstances();
            classifier.AddInstances(trainingMessages);
            classifier.Train();

            PredictMessages(classifier, messages);
            PredictMessages(classifier, testMessages);

            EvaluatorViewModel evaluatorVM = new EvaluatorViewModel(_labels);

            LogClassifierEvaluation(evaluatorVM, v, "Evaluation", "trainingBoW", messages, -1);
            LogClassifierEvaluation(evaluatorVM, v, "Evaluation", "testBoW", testMessages, -1);

            Console.WriteLine("LogClassifierBow() end");
        }

        /// <summary>
        /// Build a classifier w/ all of the user's features, but none of their prior weights.
        /// </summary>
        public void LogClassifierOnlySysWeight()
        {
            Console.WriteLine("LogClassifierOnlySysWeight() begin");
            NewsCollection messages = NewsCollection.CreateFromExisting(_messages);
            NewsCollection testMessages = NewsCollection.CreateFromExisting(_testMessages);
            IEnumerable<IInstance> trainingMessages = FilterToTrainingSet(messages);

            // Training set
            Parallel.ForEach(messages, (instance, state, index) =>
            {
                PorterStemmer stemmer = new PorterStemmer(); // PorterStemmer isn't threadsafe, so we need one for each operation.
                Dictionary<string, int> tokens = Tokenizer.Tokenize(instance.AllText) as Dictionary<string, int>;
                instance.TokenCounts = tokens;
                instance.ComputeFeatureVector(_vocab, true);
            });

            // Test set
            Parallel.ForEach(testMessages, (instance, state, index) =>
            {
                PorterStemmer stemmer = new PorterStemmer(); // PorterStemmer isn't threadsafe, so we need one for each operation.
                Dictionary<string, int> tokens = Tokenizer.Tokenize(instance.AllText) as Dictionary<string, int>;
                instance.TokenCounts = tokens;
                instance.ComputeFeatureVector(_vocab, true);
            });

            MultinomialNaiveBayesFeedbackClassifier classifier = new MultinomialNaiveBayesFeedbackClassifier(_labels, _vocab);
            classifier.ClearInstances();
            classifier.AddInstances(trainingMessages);
            classifier.Train();

            PredictMessages(classifier, messages);
            PredictMessages(classifier, testMessages);

            EvaluatorViewModel evaluatorVM = new EvaluatorViewModel(_labels);

            LogClassifierEvaluation(evaluatorVM, _vocab, "Evaluation", "trainingOnlySysWeight", messages, -1);
            LogClassifierEvaluation(evaluatorVM, _vocab, "Evaluation", "testOnlySysWeight", testMessages, -1);

            Console.WriteLine("LogClassifierOnlySysWeight() end");
        }

        public void LogClassifierOnlyHighIGFeatures()
        {
            Console.WriteLine("LogClassifierOnlyHighIGFeatures() begin");
            NewsCollection messages = NewsCollection.CreateFromExisting(_messages);
            NewsCollection testMessages = NewsCollection.CreateFromExisting(_testMessages);
            IEnumerable<IInstance> trainingMessages = FilterToTrainingSet(messages);
            Vocabulary v = Vocabulary.CreateVocabulary(trainingMessages, _labels, Vocabulary.Restriction.HighIG, 250);

            // Training set
            Parallel.ForEach(messages, (instance, state, index) =>
            {
                PorterStemmer stemmer = new PorterStemmer(); // PorterStemmer isn't threadsafe, so we need one for each operation.
                //Dictionary<string, int> tokens = Tokenizer.TokenizeAndStem(instance.AllText, stemmer) as Dictionary<string, int>;
                Dictionary<string, int> tokens = Tokenizer.Tokenize(instance.AllText) as Dictionary<string, int>;
                instance.TokenCounts = tokens;
                instance.ComputeFeatureVector(v, true);
            });

            // Test set
            Parallel.ForEach(testMessages, (instance, state, index) =>
            {
                PorterStemmer stemmer = new PorterStemmer(); // PorterStemmer isn't threadsafe, so we need one for each operation.
                Dictionary<string, int> tokens = Tokenizer.Tokenize(instance.AllText) as Dictionary<string, int>;
                instance.TokenCounts = tokens;
                instance.ComputeFeatureVector(v, true);
            });

            MultinomialNaiveBayesFeedbackClassifier classifier = new MultinomialNaiveBayesFeedbackClassifier(_labels, v);
            classifier.ClearInstances();
            classifier.AddInstances(trainingMessages);
            classifier.Train();

            PredictMessages(classifier, messages);
            PredictMessages(classifier, testMessages);

            EvaluatorViewModel evaluatorVM = new EvaluatorViewModel(_labels);

            LogClassifierEvaluation(evaluatorVM, v, "Evaluation", "trainingOnlyHighIGFeatures", messages, -1);
            LogClassifierEvaluation(evaluatorVM, v, "Evaluation", "testOnlyHighIGFeatures", testMessages, -1);

            Console.WriteLine("LogClassifierOnlyHighIGFeatures() end");
        }

        public void UpdateDatasetForLogging()
        {
            if (_vocab.HasUpdatedTokens) {
                UpdateVocab();
            }
            TrainClassifier(_classifier, FilterToTrainingSet(_messages));

            // Training set
            Parallel.ForEach(_messages, (instance, state, index) =>
            {
                //PorterStemmer stemmer = new PorterStemmer(); // PorterStemmer isn't threadsafe, so we need one for each operation.
                //Dictionary<string, int> tokens = Tokenizer.TokenizeAndStem(instance.AllText, stemmer) as Dictionary<string, int>;
                Dictionary<string, int> tokens = Tokenizer.Tokenize(instance.AllText) as Dictionary<string, int>;
                instance.TokenCounts = tokens;
                instance.ComputeFeatureVector(_vocab, true);
            });
            PredictMessages(_classifier, _messages);

            // Test set
            Parallel.ForEach(_testMessages, (instance, state, index) =>
            {
                PorterStemmer stemmer = new PorterStemmer(); // PorterStemmer isn't threadsafe, so we need one for each operation.
                //Dictionary<string, int> tokens = Tokenizer.TokenizeAndStem(instance.AllText, stemmer) as Dictionary<string, int>;
                Dictionary<string, int> tokens = Tokenizer.Tokenize(instance.AllText) as Dictionary<string, int>;
                instance.TokenCounts = tokens;
                instance.ComputeFeatureVector(_vocab, true);
            });
            PredictMessages(_classifier, _testMessages);
        }

        public bool SelectedMessageIsVisible(NewsItem item)
        {
            // TODO return false if the currently selected message isn't visible in the messageListView.
            return _messageListViewSource.View.Contains(item);
        }

        #endregion

        #region Private methods

        private void LogInterval(string type, int interval)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            LogClassifierEvaluation(_evaluatorVM, _vocab, type, "training", _messages, interval);
            //watch.Restart();
            NewsCollection messages = NewsCollection.CreateFromExisting(_messages);
            IEnumerable<IInstance> trainingMessages = FilterToTrainingSet(messages);
            Vocabulary v = Vocabulary.CreateVocabulary(trainingMessages, _labels, Vocabulary.Restriction.None, -1, false);

            // Training set
            //foreach (IInstance instance in messages) { 
            Parallel.ForEach(messages, (instance, state, index) => {
                Dictionary<string, int> tokens = Tokenizer.Tokenize(instance.AllText) as Dictionary<string, int>;
                instance.TokenCounts = tokens;
                instance.ComputeFeatureVector(v, true);
            });

            MultinomialNaiveBayesFeedbackClassifier classifier = new MultinomialNaiveBayesFeedbackClassifier(_labels, v);
            classifier.ClearInstances();
            classifier.AddInstances(trainingMessages);
            classifier.Train();

            PredictMessages(classifier, messages, false);

            EvaluatorViewModel evaluatorVM = new EvaluatorViewModel(_labels);
            LogClassifierEvaluation(evaluatorVM, v, type + "BoW", "training", messages, interval);
            watch.Stop();
            Console.WriteLine("Logging BoW interval took {0}", watch.Elapsed);
        }

        private void UpdatePredictions()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            Stopwatch watch = new Stopwatch();
            watch.Start();
            if (_vocab.HasUpdatedTokens) {
                UpdateVocab();
                watch.Stop();
                Console.WriteLine("Time to update vocab: {0}", watch.Elapsed);
                watch.Restart();
            }
            TrainClassifier(_classifier, FilterToTrainingSet(_messages));
            watch.Stop();
            Console.WriteLine("Time to train classifier: {0}", watch.Elapsed);
            watch.Restart();
            PredictMessages(_classifier, _messages);
            watch.Stop();
            Console.WriteLine("Time to predict messages: {0}", watch.Elapsed);
            watch.Restart();
            _evaluatorVM.EvaluatePredictions(_messages);
            watch.Stop();
            Console.WriteLine("Time to evaluate predictions: {0}", watch.Elapsed);
            watch.Restart();
            _folderListVM.UpdateFolderCounts(_messages);
            watch.Stop();
            Console.WriteLine("Time to update folder counts: {0}", watch.Elapsed);
            //watch.Restart();
            //_messageListViewSource.View.Refresh();
            //watch.Stop();
            //Console.WriteLine("Time to refresh view: {0}", watch.Elapsed);

            Mouse.OverrideCursor = null;
        }

        private void UpdateVocabForce()
        {
            UpdateVocab(true);
        }

        /// <summary>
        /// Update the vocabulary in response to the user adding or removing tokens, or adjusting the training set
        /// </summary>
        private void UpdateVocab(bool force = false)
        {
            bool onlyUserFeatures = false;

            if (!force && _condition == App.Condition.Treatment) {
                // Unless we're forcing the vocab update, only use user-provided features for our vocab in the
                // feature-adjustment treatment.
                onlyUserFeatures = true;
            }

            UpdateInstanceFeatures(_messages, _vocab, false);
            _vocab.RestrictVocab(FilterToTrainingSet(_messages), _labels,
                _featureSetVM.UserAddedFeatures, _featureSetVM.UserRemovedFeatures, _desiredVocabSize, onlyUserFeatures);
            UpdateInstanceFeatures(_messages, _vocab, true);
            TextToHighlight = _vocab.GetFeatureWords();
        }

        /// <summary>
        /// Clear the classifier's current training data and re-train it using instances in 'topic1' and 'topic2'.
        /// </summary>
        /// <param name="classifier">The classifier to train.</param>
        /// <param name="topic1">The first classification of training data.</param>
        /// <param name="topic2">The second classification of training data.</param>
        private void TrainClassifier(IClassifier classifier, IEnumerable<IInstance> trainingSet)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

            classifier.ClearInstances();
            classifier.AddInstances(trainingSet);
            classifier.AddPriors(FeatureSetVM.FeatureSet);
            classifier.Train();

            timer.Stop();
            Console.WriteLine("Time to train classifier: {0}", timer.Elapsed);
        }

        /// <summary>
        /// Update predictions for every message in the provided folders.
        /// </summary>
        /// <param name="classifier">The classifier to use</param>
        /// <param name="folders">The folders to run predictions for.</param>
        private void PredictMessages(IClassifier classifier, IEnumerable<IInstance> testSet, bool withEvidence = true)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

            foreach (IInstance instance in testSet) {
                Prediction prediction = classifier.PredictInstance(instance, withEvidence);
                instance.Prediction = prediction;
            }

            timer.Stop();
            Console.WriteLine("Time to predict instances: {0}", timer.Elapsed);
        }

        private void EvaluateClassifier(IClassifier classifier, IEnumerable<IInstance> instances)
        {
            // Evaluate the classifier
            Stopwatch timer = new Stopwatch();
            timer.Start();
            //int pRight = 0;
            //int topic1Predictions = 0;
            //int topic2Predictions = 0;
            //int recentlyChangedPredictions = 0;
            //foreach (IInstance instance in _topic1Folder)
            //{
            //    if (instance.IsPredictionCorrect == true)
            //    {
            //        topic1Predictions++;
            //        pRight++;
            //    }
            //    else
            //    {
            //        topic2Predictions++;
            //    }
            //    if (instance.RecentlyChanged)
            //        recentlyChangedPredictions++;
            //}
            //_topic1Folder.CorrectPredictions = pRight;
            //pRight = 0;
            //foreach (IInstance instance in _topic2Folder)
            //{
            //    if (instance.IsPredictionCorrect == true)
            //    {
            //        topic2Predictions++;
            //        pRight++;
            //    }
            //    else
            //    {
            //        topic1Predictions++;
            //    }
            //    if (instance.RecentlyChanged)
            //        recentlyChangedPredictions++;
            //}
            //_topic2Folder.CorrectPredictions = pRight;
            //foreach (IInstance instance in _unknownFolder)
            //{
            //    if (instance.Prediction.Label == _topic1Folder.Label)
            //        topic1Predictions++;
            //    else if (instance.Prediction.Label == _topic2Folder.Label)
            //        topic2Predictions++;
            //    if (instance.RecentlyChanged)
            //        recentlyChangedPredictions++;
            //}
            //Topic1Predictions = topic1Predictions;
            //Topic2Predictions = topic2Predictions;
            //RecentlyChangedPredictions = recentlyChangedPredictions;
            timer.Stop();
            Console.WriteLine("Time to evaluate classifier: {0}", timer.Elapsed);
        }

        /// <summary>
        /// Recompute the feature vector for every item in every folder.
        /// This is a brute-force way to ensure that our feature vectors are always in sync
        /// with a user's feature adjustments.
        /// </summary>
        /// <param name="isRestricted">If true, restrict feature vectors to the "restricted" 
        /// set of features in our vocabular. Otherwise, use all available features.</param>
        private void UpdateInstanceFeatures(IEnumerable<IInstance> messages, Vocabulary vocab, bool isRestricted)
        {
            foreach (IInstance instance in messages) {
                instance.ComputeFeatureVector(vocab, isRestricted);
            }
        }

        /// <summary>
        /// Load the dataset specified in our configuration properties.
        /// </summary>
        /// <returns>A NewsCollection representing the requested dataset.</returns>
        private NewsCollection LoadDataset()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, App.DataDir, App.Current.Properties[App.PropertyKey.DatasetFile].ToString());

            _labels = new List<Label>();
            _labels.Add(new Label(App.Current.Properties[App.PropertyKey.Topic1UserLabel].ToString(), App.Current.Properties[App.PropertyKey.Topic1SystemLabel].ToString(),
                (Brush)App.Current.Properties[App.PropertyKey.Topic1Color], App.Current.Properties[App.PropertyKey.Topic1ColorDesc].ToString()));
            _labels.Add(new Label(App.Current.Properties[App.PropertyKey.Topic2UserLabel].ToString(), App.Current.Properties[App.PropertyKey.Topic2SystemLabel].ToString(),
                (Brush)App.Current.Properties[App.PropertyKey.Topic2Color], App.Current.Properties[App.PropertyKey.Topic2ColorDesc].ToString()));

            NewsCollection dataset = NewsCollection.CreateFromZip(path, _labels);

            return dataset;
        }

        private NewsCollection LoadTestDataset()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, App.DataDir, App.Current.Properties[App.PropertyKey.TestDatasetFile].ToString());
            NewsCollection dataset = NewsCollection.CreateFromZip(path, _labels);

            return dataset;
        }

        /// <summary>
        /// Build a training set of a desired size by labeling the first 'size' elements in 'source' with the correct label.
        /// </summary>
        /// <param name="source">The collection of items.</param>
        /// <param name="label">The label to restrict our training set to.</param>
        /// <param name="size">The number of items to label.</param>
        private void LabelTrainingSet(NewsCollection source, Label label, int size)
        {
            foreach (NewsItem item in source.Where(newsItem => newsItem.GroundTruthLabel == label).Take(size)) {
                if (item != null) {
                    item.UserLabel = label;
                } else {
                    Console.Error.WriteLine("Warning: unable to find {0} items for {1} training set.", size, label);
                }
            }

            return;
        }

        private IEnumerable<IInstance> FilterToLabel(IEnumerable<IInstance> fullSet, Label label)
        {
            Debug.Assert(fullSet != null);

            return fullSet.Where(item => item.UserLabel == label);
        }

        private IEnumerable<IInstance> FilterToTrainingSet(IEnumerable<IInstance> fullSet)
        {
            Debug.Assert(fullSet != null);

            return fullSet.Where(item => _labels.Contains(item.UserLabel));
        }

        private IEnumerable<IInstance> FilterToTestSet(IEnumerable<IInstance> fullSet)
        {
            Debug.Assert(fullSet != null);

            return fullSet.Where(item => !_labels.Contains(item.UserLabel));
        }

        private void UpdateFilters(bool onlyShowRecentChanges)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            using (_messageListViewSource.View.DeferRefresh()) {
                // If this is the Unknown folder, look for items without a user label.
                if (FolderListVM.SelectedFolder.Label == FolderListVM.UnknownLabel) {
                    if (onlyShowRecentChanges)
                        _messageListViewSource.View.Filter = (item => (item as NewsItem).UserLabel == null &&
                            ((item as NewsItem).RecentlyChanged || (item as NewsItem).PredictionConfidenceDirection != Direction.None));
                    else
                        _messageListViewSource.View.Filter = (item => (item as NewsItem).UserLabel == null);
                } else {
                    // Otherwise, look for items with the same label as the selected folder
                    if (onlyShowRecentChanges)
                        _messageListViewSource.View.Filter = (item => (item as NewsItem).UserLabel == FolderListVM.SelectedFolder.Label &&
                            ((item as NewsItem).RecentlyChanged || (item as NewsItem).PredictionConfidenceDirection != Direction.None));
                    else
                        _messageListViewSource.View.Filter = (item => (item as NewsItem).UserLabel == FolderListVM.SelectedFolder.Label);
                }
            }

            if (_messageListViewSource.View.CurrentItem == null ||
                _messageListViewSource.View.IsCurrentAfterLast ||
                _messageListViewSource.View.IsCurrentBeforeFirst) {
                _messageListViewSource.View.MoveCurrentToFirst();
            }

            Mouse.OverrideCursor = null;
        }

        #endregion

        #region Event handlers

        private void _featureSetVM_FeatureAdded(object sender, FeatureSetViewModel.FeatureAddedEventArgs e)
        {
            Console.WriteLine("added feature {0}", e.Feature);
            Debug.Assert(e.Feature != null);
            // Ensure the vocabulary includes the token for this feature
            if (_vocab.AddToken(e.Feature.Characters, -1)) // Use -1 for doc frequency for now, we'll fix it below
            {
                // If we created a new vocab element, include it in our documents' tokenizations
                int df = 0;
                Regex featureRegex = new Regex(@"\b" + Regex.Escape(e.Feature.Characters) + @"\b", RegexOptions.IgnoreCase);

                // FIXME Do we only want this from our training set, or the entire data set?
                foreach (IInstance instance in _messages) {
                    if (instance.TokenizeForPattern(featureRegex, e.Feature.Characters)) {
                        df++;
                    }
                }

                //// Build a frequency from our training set
                //foreach (IInstance instance in this.FilterToTrainingSet(_messages)) {
                //    if (instance.TokenizeForPattern(featureRegex, e.Feature.Characters)) {
                //        df++;
                //    }
                //}
                //// And update the tokenization for our test set
                //foreach (IInstance instance in this.FilterToTestSet(_messages)) {
                //    instance.TokenizeForPattern(featureRegex, e.Feature.Characters);
                //}
                // Update our vocab with the correct document frequency
                _vocab.AddToken(e.Feature.Characters, df);
            }

            UpdateVocab();
            //int id = _vocab.GetWordId(e.Feature.Characters, true);
            //_classifier.UpdateCountsForNewFeature(id);
            //_classifier.Train();
            //double weight;
            //_classifier.TryGetFeatureSystemWeight(id, e.Feature.Topic1Importance.Label, out weight);
            //e.Feature.Topic1Importance.SystemWeight = weight;
            //_classifier.TryGetFeatureSystemWeight(id, e.Feature.Topic2Importance.Label, out weight);
            //e.Feature.Topic2Importance.SystemWeight = weight;
            //_classifier.TryGetFeatureUserWeight(id, e.Feature.Topic1Importance.Label, out weight);
            //e.Feature.Topic1Importance.UserWeight = weight;
            //_classifier.TryGetFeatureUserWeight(id, e.Feature.Topic2Importance.Label, out weight);
            //e.Feature.Topic2Importance.UserWeight = weight;
            if (AutoUpdatePredictions) {
                UpdatePredictions();
                //FeatureSetVM.UpdateFeaturePriors(); // FIXME this may cause two retrains?
            } else {
                TrainClassifier(_classifier, FilterToTrainingSet(_messages));
            }

            _nFeaturesAdded++;
            if (_nFeaturesAdded % _logEachNFeatures == 0) {
                Mouse.OverrideCursor = Cursors.Wait;
                LogInterval("featuresAdded", _nFeaturesAdded);
                Mouse.OverrideCursor = null;
            }
        }

        private void _featureSetVM_FeatureRemoved(object sender, FeatureSetViewModel.FeatureRemovedEventArgs e)
        {
            UpdateVocab();
            if (AutoUpdatePredictions) {
                UpdatePredictions();
            }

            // If the removed feature was highlighted, reset the highlight.
            if (HeatMapVM.ToHighlight == e.Tokens) {
                HeatMapVM.ToHighlight = "";
            }
        }

        // Tell the heatmap to show messages containing the feature the user is currently editing
        private void _featureSetVM_FeatureTextEdited(object sender, FeatureSetViewModel.FeatureTextEditedEventArgs e)
        {
            HeatMapVM.ToHighlight = e.Tokens;
        }

        void _featureSetVM_FeaturePriorsEdited(object sender, FeatureSetViewModel.FeaturePriorEditedEventArgs e)
        {
            //Console.WriteLine("weight edited. Adjust feature weights to correspond to the proportion of user-adjust heights");
            UpdatePredictions();
            //if (AutoUpdatePredictions) {
            //    PerformUpdatePredictions();
            //}
        }

        private void _heatMapVM_HighlightTextChanged(object sender, HeatMapViewModel.HighlightTextChangedEventArgs e)
        {
            _logger.Writer.WriteStartElement("HighlightTextChanged");
            _logger.Writer.WriteAttributeString("text", e.Text);
            _logger.logTime();

            FeatureSetVM.SelectFeature(e.Text);

            if (MessageVM.Message != null) {
                MessageVM.Message.HighlightWithWord(e.Text);
            }

            _logger.logEndElement();
        }

        private void _folderListVM_SelectedFolderChanged(object sender, FolderListViewModel.SelectedFolderChangedEventArgs e)
        {
            _logger.Writer.WriteStartElement("SelectedFolder");
            _logger.Writer.WriteAttributeString("label", e.Folder.Label.ToString());
            _logger.logTime();

            UpdateFilters(_onlyShowRecentChanges);
            MessageVM.Message = e.Folder.SelectedMessage;

            _logger.logEndElement();
        }

        private void _folderListVM_SelectedMessageChanged(object sender, FolderViewModel.SelectedMessageChangedEventArgs e)
        {
            if (e.Message != null) {
                _logger.Writer.WriteStartElement("SelectedMessage");
                _logger.Writer.WriteAttributeString("id", e.Message.Id.ToString());
                if (e.Message.UserLabel != null) {
                    _logger.Writer.WriteAttributeString("userLabel", e.Message.UserLabel.ToString());
                } else {
                    _logger.Writer.WriteAttributeString("userLabel", "none");
                }
                _logger.Writer.WriteAttributeString("groundTruthLabel", e.Message.GroundTruthLabel.ToString());
                _logger.Writer.WriteAttributeString("isHighlighted", e.Message.IsHighlighted.ToString());
                _logger.Writer.WriteAttributeString("isPredictionCorrect", e.Message.IsPredictionCorrect.ToString());
                _logger.Writer.WriteAttributeString("predictionLabel", e.Message.Prediction.Label.ToString());
                _logger.Writer.WriteAttributeString("predictionConfidence", e.Message.Prediction.Confidence.ToString());
                _logger.Writer.WriteAttributeString("predictionConfidenceDirection", e.Message.PredictionConfidenceDirection.ToString());
                //_logger.Writer.WriteAttributeString("messagePosition", _messageListViewSource.View.CurrentPosition.ToString()); // Doesn't work because the view hasn't updated yet
                _logger.logTime();

                MessageVM.Message = e.Message;
                MessageVM.Message.HighlightWithWord(HeatMapVM.ToHighlight);

                _logger.logEndElement();
            }
        }

        void View_CurrentChanged(object sender, EventArgs e)
        {
            int order = _messageListViewSource.View.CurrentPosition;
            NewsItem item = _messageListViewSource.View.CurrentItem as NewsItem;
            if (order >= 0) {
                _logger.Writer.WriteStartElement("SelectedMessageOrder");
                _logger.Writer.WriteAttributeString("order", order.ToString());
                if (item != null) {
                    _logger.Writer.WriteAttributeString("id", item.Id.ToString());
                } else {
                    _logger.Writer.WriteAttributeString("id", "-1");
                }
                _logger.logTime();
                _logger.Writer.WriteValue(order);
                _logger.logEndElement();
            }
        }

        void _logTimer_Tick(object sender, EventArgs e)
        {
            //Console.WriteLine("logTimer elapsed");
            LogClassifierEvaluationTraining(_logTimerCount);
            _logTimerCount++;
        }

        void _runtimeTimer_Tick(object sender, EventArgs e)
        {
            //Console.WriteLine("runtimeTimer elapsed");
            Dialog d = new Dialog();
            d.DialogTitle = "That's all, folks!";
            d.DialogMessage = "Time's up! Press 'Close' to close this application.";
            d.Owner = App.Current.MainWindow;
            d.ShowDialog();
            App.Current.MainWindow.Close();
        }

        #endregion
    }
}
