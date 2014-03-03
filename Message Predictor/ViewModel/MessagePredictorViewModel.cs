using GalaSoft.MvvmLight.Command;
using LibIML;
using MessagePredictor.Model;
using MessagePredictor.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace MessagePredictor
{
    class MessagePredictorViewModel : ViewModelBase
    {
        // Models
        private NewsCollection _messages;
        List<Label> _labels;
        Vocabulary _vocab;
        MultinomialNaiveBayesFeedbackClassifier _classifier;

        // Viewmodels (and data for this viewmodel)
        private FolderListViewModel _folderListVM;
        private FeatureSetViewModel _featureSetVM;
        private HeatMapViewModel _heatMapVM;
        private EvaluatorViewModel _evaluatorVM;
        private CollectionViewSource _messageListViewSource;
        private CollectionViewSource _folderListViewSource;
        List<string> _textToHighlight;

        // Application settings (set at startup)
        int _desiredVocabSize;

        // UI preferences (user-modifiable)
        bool _autoUpdatePredictions;
        bool _onlyShowRecentChanges;

        public MessagePredictorViewModel()
        {
            Console.WriteLine("MessagePredictorViewModel() start");
            Stopwatch timer = new Stopwatch();

            timer.Start();
            List<NewsCollection> folders = new List<NewsCollection>();
            _messages = LoadDataset();
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

            _featureSetVM = new FeatureSetViewModel(_classifier, _vocab, _labels);
            _featureSetVM.FeatureAdded += _featureSetVM_FeatureAdded;
            _featureSetVM.FeatureRemoved += _featureSetVM_FeatureRemoved;
            _featureSetVM.FeatureTextEdited += _featureSetVM_FeatureTextEdited;
            _featureSetVM.FeatureWeightEdited += _featureSetVM_FeatureWeightEdited;

            _heatMapVM = new HeatMapViewModel(_messages);
            _heatMapVM.HighlightTextChanged += _heatMapVM_HighlightTextChanged;

            _messageListViewSource = new CollectionViewSource();
            _messageListViewSource.Source = _messages;

            // Setup our Commands
            UpdatePredictions = new RelayCommand(PerformUpdatePredictions, CanPerformUpdatePredictions);
            LabelMessage = new RelayCommand<Label>(PerformLabelMessage, CanPerformLabelMessage);

            // Start with our current folder pointing at the collection of unlabeled items.
            AutoUpdatePredictions = (bool)App.Current.Properties[MessagePredictor.App.PropertyKey.AutoUpdatePredictions];

            // Evaluate the classifier (so we can show predictions to the user)
            PerformUpdatePredictions();
            PerformUpdatePredictions(); // Do this twice to avoid showing any change indicators at the start

            Console.WriteLine("MessagePredictorViewModel() end");
        }

        #region Properties

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
                if (SetProperty<bool>(ref _autoUpdatePredictions, value))
                    UpdatePredictions.RaiseCanExecuteChanged();
            }
        }

        public bool OnlyShowRecentChanges
        {
            get { return _onlyShowRecentChanges; }
            set
            {
                if (SetProperty<bool>(ref _onlyShowRecentChanges, value)) {
                    UpdateFilters(_onlyShowRecentChanges);
                }
            }
        }

        public List<string> TextToHighlight
        {
            get { return _textToHighlight; }
            private set { SetProperty<List<string>>(ref _textToHighlight, value); }
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

        #region Commands

        public RelayCommand UpdatePredictions { get; private set; }
        public RelayCommand<Label> LabelMessage { get; private set; }

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
            Mouse.OverrideCursor = Cursors.Wait;

            Console.WriteLine("Update predictions");
            if (_vocab.HasUpdatedTokens) {
                UpdateVocab();
            }

            TrainClassifier(_classifier, FilterToTrainingSet(_messages));
            PredictMessages(_classifier, _messages);
            _evaluatorVM.EvaluatePredictions(_messages);
            _folderListVM.UpdateFolderCounts(_messages);
            _messageListViewSource.View.Refresh();

            Mouse.OverrideCursor = null;
        }

        private bool CanPerformLabelMessage(Label label)
        {
            return true;
        }

        private void PerformLabelMessage(Label label)
        {
            NewsItem item = FolderListVM.SelectedFolder.SelectedMessage;
            Mouse.OverrideCursor = Cursors.Wait;

            // Don't do anything if the message already has the desired label
            if (item.UserLabel == label ||
                (item.UserLabel == null && label == FolderListVM.UnknownLabel)) {
                return;
            }

            item.UserLabel = label;

            // Try to select the next message. If there is no next message, select the previous message.
            if (!_messageListViewSource.View.MoveCurrentToNext()) {
                _messageListViewSource.View.MoveCurrentToPrevious();
            }

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
                PerformUpdatePredictions();
            } else {
                // Still need to update our view
                _folderListVM.UpdateFolderCounts(_messages);
                _messageListViewSource.View.Refresh();
            }

            Mouse.OverrideCursor = null;
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

        #endregion

        #region Private methods

        /// <summary>
        /// Update the vocabulary in response to the user adding or removing tokens, or adjusting the training set
        /// </summary>
        private void UpdateVocab()
        {
            UpdateInstanceFeatures(false);
            _vocab.RestrictVocab(FilterToTrainingSet(_messages), _labels,
                _featureSetVM.UserAddedFeatures, _featureSetVM.UserRemovedFeatures, _desiredVocabSize);
            UpdateInstanceFeatures(true);
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
            //classifier.AddPriors(FeatureSetVM.UserAddedFeatures);
            classifier.AddPriors(FeatureSetVM.FeatureSet);

            timer.Stop();
            Console.WriteLine("Time to train classifier: {0}", timer.Elapsed);
        }

        /// <summary>
        /// Update predictions for every message in the provided folders.
        /// </summary>
        /// <param name="classifier">The classifier to use</param>
        /// <param name="folders">The folders to run predictions for.</param>
        private void PredictMessages(IClassifier classifier, IEnumerable<IInstance> testSet)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

            foreach (IInstance instance in testSet) {
                Prediction prediction = classifier.PredictInstance(instance);
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
        private void UpdateInstanceFeatures(bool isRestricted)
        {
            foreach (IInstance instance in _messages) {
                instance.ComputeFeatureVector(_vocab, isRestricted);
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
            _labels.Add(new Label(App.Current.Properties[App.PropertyKey.Topic1UserLabel].ToString(), App.Current.Properties[App.PropertyKey.Topic1SystemLabel].ToString()));
            _labels.Add(new Label(App.Current.Properties[App.PropertyKey.Topic2UserLabel].ToString(), App.Current.Properties[App.PropertyKey.Topic2SystemLabel].ToString()));

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

        private IEnumerable<NewsItem> FilterToTrainingSet(IEnumerable<NewsItem> fullSet)
        {
            Debug.Assert(fullSet != null);

            return fullSet.Where(item => _labels.Contains(item.UserLabel));
        }

        private IEnumerable<NewsItem> FilterToTestSet(IEnumerable<NewsItem> fullSet)
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
                //int id = _vocab.GetWordId(e.Tokens, false);
                int df = 0;
                foreach (IInstance instance in _messages) {
                    if (instance.TokenizeForString(e.Feature.Characters))
                        df++;
                }
                // Update our vocab with the correct document frequency
                _vocab.AddToken(e.Feature.Characters, df);
            }

            UpdateVocab();
            int id = _vocab.GetWordId(e.Feature.Characters, true);
            _classifier.UpdateCountsForNewFeature(id);
            _classifier.Train();
            e.Feature.SystemWeight = _classifier.GetFeatureSystemWeight(id, e.Feature.Label);
            if (AutoUpdatePredictions) {
                PerformUpdatePredictions();
            }
        }

        private void _featureSetVM_FeatureRemoved(object sender, EventArgs e)
        {
            UpdateVocab();
            if (AutoUpdatePredictions) {
                PerformUpdatePredictions();
            }
        }

        // Tell the heatmap to show messages containing the feature the user is currently editing
        private void _featureSetVM_FeatureTextEdited(object sender, FeatureSetViewModel.FeatureTextEditedEventArgs e)
        {
            HeatMapVM.ToHighlight = e.Tokens;
        }

        void _featureSetVM_FeatureWeightEdited(object sender, FeatureSetViewModel.FeatureWeightEditedEventArgs e)
        {
            Console.WriteLine("weight edited");
            if (AutoUpdatePredictions) {
                PerformUpdatePredictions();
            }
        }

        private void _heatMapVM_HighlightTextChanged(object sender, HeatMapViewModel.HighlightTextChangedEventArgs e)
        {
            if (FolderListVM.SelectedFolder != null && FolderListVM.SelectedFolder.SelectedMessage != null) {
                FolderListVM.SelectedFolder.SelectedMessage.HighlightWithWord(e.Text);
            }
        }

        private void _folderListVM_SelectedFolderChanged(object sender, FolderListViewModel.SelectedFolderChangedEventArgs e)
        {
            FolderListViewModel vm = sender as FolderListViewModel;
            UpdateFilters(_onlyShowRecentChanges);
        }

        #endregion
    }
}
