using GalaSoft.MvvmLight.Command;
using LibIML;
using MessagePredictor.Model;
using MessagePredictor.View;
using MessagePredictor.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MessagePredictor
{
    class MessagePredictorViewModel : ViewModelBase
    {
        FeatureSetViewModel _featureSetVM;
        HeatMapViewModel _heatMapVM;
        List<NewsCollection> _folders; // Collection of all our folders
        NewsCollection _unknownFolder;
        NewsCollection _topic1Folder;
        NewsCollection _topic2Folder;
        NewsCollection _currentFolder;
        NewsItem _currentMessage;
        List<Label> _labels;
        Vocabulary _vocab;
        int _desiredVocabSize;
        MultinomialNaiveBayesFeedbackClassifier _classifier;
        bool _autoUpdatePredictions;
        bool _onlyShowRecentChanges;
        int _topic1Predictions;
        int _topic2Predictions;
        int _recentlyChangedPredictions;
        List<string> _textToHighlight;

        public MessagePredictorViewModel()
        {
            Console.WriteLine("MessagePredictorViewModel() start");
            Stopwatch timer = new Stopwatch();

            timer.Start();
            List<NewsCollection> folders = new List<NewsCollection>();
            _unknownFolder = LoadDataset();
            timer.Stop();
            Console.WriteLine("Time to load data set: {0}", timer.Elapsed);

            timer.Restart();
            _topic1Folder = BuildTopicTrainingSet(_unknownFolder, _labels[0], (int)App.Current.Properties[App.PropertyKey.Topic1TrainSize]);
            _topic2Folder = BuildTopicTrainingSet(_unknownFolder, _labels[1], (int)App.Current.Properties[App.PropertyKey.Topic2TrainSize]);
            timer.Stop();
            Console.WriteLine("Time to build topic training sets: {0}", timer.Elapsed);

            folders.Add(_unknownFolder);
            folders.Add(_topic1Folder);
            folders.Add(_topic2Folder);

            // Build a vocab from our training set
            timer.Restart();
            List<NewsItem> forVocab = new List<NewsItem>();
            forVocab.AddRange(_topic1Folder.ToList());
            forVocab.AddRange(_topic2Folder.ToList());
            _desiredVocabSize = (int)App.Current.Properties[App.PropertyKey.Topic1VocabSize] + (int)App.Current.Properties[App.PropertyKey.Topic2VocabSize];
            _vocab = Vocabulary.CreateVocabulary(forVocab, _labels, Vocabulary.Restriction.HighIG, _desiredVocabSize);
            timer.Stop();
            Console.WriteLine("Time to build vocab: {0}", timer.Elapsed);

            // Update our test set in light of our new vocabulary
            timer.Restart();
            Parallel.ForEach(_unknownFolder, (instance, state, index) =>
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

            _featureSetVM = new FeatureSetViewModel(_classifier, _vocab, _labels);
            _featureSetVM.FeatureAdded += featureSetVM_FeatureAdded;
            _featureSetVM.FeatureRemoved += featureSetVM_FeatureRemoved;
            _featureSetVM.FeatureTextEdited += featureSetVM_FeatureTextEdited;

            _heatMapVM = new HeatMapViewModel(_unknownFolder, _topic1Folder, _topic2Folder);
            _heatMapVM.HighlightTextChanged += _heatMapVM_HighlightTextChanged;

            // Setup our Commands
            UpdatePredictions = new RelayCommand(PerformUpdatePredictions, CanPerformUpdatePredictions);
            FileToUnknown = new RelayCommand(PerformFileToUnknown, CanPerformFileToUnknown);
            FileToTopic1 = new RelayCommand(PerformFileToTopic1, CanPerformFileToTopic1);
            FileToTopic2 = new RelayCommand(PerformFileToTopic2, CanPerformFileToTopic2);

            // Start with our current folder pointing at the collection of unlabeled items.
            Folders = folders;
            CurrentFolder = Folders[0];
            CurrentMessage = CurrentFolder[0];
            AutoUpdatePredictions = (bool)App.Current.Properties[MessagePredictor.App.PropertyKey.AutoUpdatePredictions];

            // Evaluate the classifier (so we can show predictions to the user)
            PerformUpdatePredictions();

            Console.WriteLine("MessagePredictorViewModel() end");
        }

        #region Properties

        public List<NewsCollection> Folders
        {
            get { return _folders; }
            private set { SetProperty<List<NewsCollection>>(ref _folders, value); }
        }

        public NewsCollection CurrentFolder
        {
            get { return _currentFolder; }
            set
            {
                if (SetProperty<NewsCollection>(ref _currentFolder, value))
                {
                    CurrentMessage = CurrentFolder[0]; // Go to the first message in this folder
                    FileToUnknown.RaiseCanExecuteChanged();
                    FileToTopic1.RaiseCanExecuteChanged();
                    FileToTopic2.RaiseCanExecuteChanged();
                }
            }
        }

        public NewsItem CurrentMessage
        {
            get { return _currentMessage; }
            set { SetProperty<NewsItem>(ref _currentMessage, value); }
        }
        
        public string Topic1UserLabel
        {
            get
            {
                if (_labels != null && _labels.Count > 0)
                    return _labels[0].UserLabel;
                else
                    return "[None]";
            }
        }

        public string Topic2UserLabel
        {
            get
            {
                if (_labels != null && _labels.Count > 1)
                    return _labels[1].UserLabel;
                else
                    return "[None]";
            }
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
            set { SetProperty<bool>(ref _onlyShowRecentChanges, value); }
        }

        public int Topic1Predictions
        {
            get { return _topic1Predictions; }
            private set { SetProperty<int>(ref _topic1Predictions, value); }
        }

        public int Topic2Predictions
        {
            get { return _topic2Predictions; }
            private set { SetProperty<int>(ref _topic2Predictions, value); }
        }

        public int RecentlyChangedPredictions
        {
            get { return _recentlyChangedPredictions; }
            private set { SetProperty<int>(ref _recentlyChangedPredictions, value); }
        }

        public List<string> TextToHighlight
        {
            get { return _textToHighlight; }
            private set { SetProperty<List<string>>(ref _textToHighlight, value); }
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

        #endregion

        #region Commands

        public RelayCommand UpdatePredictions { get; private set; }
        public RelayCommand FileToUnknown { get; private set; }
        public RelayCommand FileToTopic1 { get; private set; }
        public RelayCommand FileToTopic2 { get; private set; }

        private bool CanPerformUpdatePredictions()
        {
            if (AutoUpdatePredictions)
                return false;
            else
            {
                // TODO also make sure something has changed
                return true;
            }
        }

        private void PerformUpdatePredictions()
        {
            Console.WriteLine("Update predictions");
            if (_vocab.HasUpdatedTokens)
            {
                UpdateVocab();
            }

            TrainClassifier(_classifier, _topic1Folder, _topic2Folder);
            PredictMessages(_classifier, _folders);
            EvaluateClassifier(_classifier);
        }

        private bool CanPerformFileToUnknown()
        {
            if (CurrentMessage != null &&
                GetFolderContainingMessage(CurrentMessage) != _unknownFolder)
            {
                return true;
            }
            else
                return false;
        }

        private void PerformFileToUnknown()
        {
            FileCurrentMessageToFolder(_unknownFolder);
        }

        private bool CanPerformFileToTopic1()
        {
            if (CurrentMessage != null &&
                GetFolderContainingMessage(CurrentMessage) != _topic1Folder)
            {
                return true;
            }
            else
                return false;
        }

        private void PerformFileToTopic1()
        {
            FileCurrentMessageToFolder(_topic1Folder);
        }

        private bool CanPerformFileToTopic2()
        {
            if (CurrentMessage != null &&
                GetFolderContainingMessage(CurrentMessage) != _topic2Folder)
            {
                return true;
            }
            else
                return false;
        }

        private void PerformFileToTopic2()
        {

            FileCurrentMessageToFolder(_topic2Folder);
        }

        #endregion

        /// <summary>
        /// Update the vocabulary in response to the user adding or removing tokens, or adjusting the training set
        /// </summary>
        private void UpdateVocab()
        {
            UpdateInstanceFeatures(false);
            _vocab.RestrictVocab(_topic1Folder.Concat(_topic2Folder), _labels,
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
        private void TrainClassifier(IClassifier classifier, IEnumerable<IInstance> topic1, IEnumerable<IInstance> topic2)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

            classifier.ClearInstances();
            classifier.AddInstances(topic1.Concat(topic2));
            classifier.AddPriors(FeatureSetVM.UserAddedFeatures);

            timer.Stop();
            Console.WriteLine("Time to train classifier: {0}", timer.Elapsed);
        }

        /// <summary>
        /// Update predictions for every message in the provided folders.
        /// </summary>
        /// <param name="classifier">The classifier to use</param>
        /// <param name="folders">The folders to run predictions for.</param>
        private void PredictMessages(IClassifier classifier, IEnumerable<IEnumerable<IInstance>> folders)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

            foreach (NewsCollection folder in folders)
            {
                foreach (IInstance instance in folder)
                {
                    Prediction prediction = classifier.PredictInstance(instance);
                    instance.Prediction = prediction;
                }
            }

            timer.Stop();
            Console.WriteLine("Time to predict instances: {0}", timer.Elapsed);
        }

        private void EvaluateClassifier(IClassifier classifier)
        {
            // Evaluate the classifier
            Stopwatch timer = new Stopwatch();
            timer.Start();
            int pRight = 0;
            int topic1Predictions = 0;
            int topic2Predictions = 0;
            int recentlyChangedPredictions = 0;
            foreach (IInstance instance in _topic1Folder)
            {
                if (instance.IsPredictionCorrect == true)
                {
                    topic1Predictions++;
                    pRight++;
                }
                else
                {
                    topic2Predictions++;
                }
                if (instance.RecentlyChanged)
                    recentlyChangedPredictions++;
            }
            _topic1Folder.CorrectPredictions = pRight;
            pRight = 0;
            foreach (IInstance instance in _topic2Folder)
            {
                if (instance.IsPredictionCorrect == true)
                {
                    topic2Predictions++;
                    pRight++;
                }
                else
                {
                    topic1Predictions++;
                }
                if (instance.RecentlyChanged)
                    recentlyChangedPredictions++;
            }
            _topic2Folder.CorrectPredictions = pRight;
            foreach (IInstance instance in _unknownFolder)
            {
                if (instance.Prediction.Label == _topic1Folder.Label)
                    topic1Predictions++;
                else if (instance.Prediction.Label == _topic2Folder.Label)
                    topic2Predictions++;
                if (instance.RecentlyChanged)
                    recentlyChangedPredictions++;
            }
            Topic1Predictions = topic1Predictions;
            Topic2Predictions = topic2Predictions;
            RecentlyChangedPredictions = recentlyChangedPredictions;
            timer.Stop();
            Console.WriteLine("Time to evaluate classifier: {0}", timer.Elapsed);
        }

        private void FileCurrentMessageToFolder(NewsCollection folder)
        {
            NewsItem item = CurrentMessage;

            if (MoveMessageToFolder(item, folder))
            {
                // We successfully moved this message
                if (folder == _unknownFolder)
                {
                    // If we moved something to Unknown, remove it from our vocabulary
                    _vocab.RemoveInstanceTokens(item);
                }
                else
                {
                    // If we moved something from unknown to a different folder, add it to our vocabulary
                    // (This is safe because we can't move items between the two topic folders--they can only be filed
                    // to the correct folder.)
                    _vocab.AddInstanceTokens(item);
                }

                // If autoupdate is on, retrain the classifier
                if (AutoUpdatePredictions)
                {
                    PerformUpdatePredictions();
                }
            }
        }

        /// <summary>
        /// Move a message to a different folder.
        /// </summary>
        /// <param name="item">The message to move.</param>
        /// <param name="collection">The folder to move the item into.</param>
        /// <returns>True on success, false on failure.</returns>
        private bool MoveMessageToFolder(NewsItem item, NewsCollection collection)
        {
            bool retval = false;
            NewsCollection container = GetFolderContainingMessage(item);
            if (container != null)
            {
                if (container == collection)
                {
                    Console.Error.WriteLine("Error: Cannot move item {0} into container {0} because it's already there.", item, container);
                }
                else if (!container.Remove(item))
                {
                    Console.Error.WriteLine("Error removing item {0} from container {0}", item, container);
                }
                else
                {
                    collection.Add(item);
                    CurrentMessage = container.First();
                    retval = true;
                }
            }

            return retval;
        }

        /// <summary>
        /// Find the collection containing a given item.
        /// </summary>
        /// <param name="item">The NewsItem to search for.</param>
        /// <returns>The collection containing 'item', or null if not found.</returns>
        private NewsCollection GetFolderContainingMessage(NewsItem item)
        {
            NewsCollection container = null;

            foreach (NewsCollection collection in Folders)
            {
                if (collection.Contains(item))
                {
                    container = collection;
                    break;
                }
            }

            return container;
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
            foreach (IEnumerable<IInstance> folder in _folders)
            {
                foreach (IInstance instance in folder)
                {
                    instance.ComputeFeatureVector(_vocab, isRestricted);
                }
            }
        }

        private void featureSetVM_FeatureAdded(object sender, FeatureSetViewModel.FeatureAddedEventArgs e)
        {
            Console.WriteLine("added feature {0}", e.Tokens);
            // Ensure the vocabulary includes the token for this feature
            if (_vocab.AddToken(e.Tokens, -1)) // Use -1 for doc frequency for now, we'll fix it below
            {
                // If we created a new vocab element, include it in our documents' tokenizations
                //int id = _vocab.GetWordId(e.Tokens, false);
                int df = 0;
                foreach (IEnumerable<IInstance> folder in _folders)
                {
                    foreach (IInstance instance in folder)
                    {
                        if (instance.TokenizeForString(e.Tokens))
                            df++;
                    }
                }
                // Update our vocab with the correct document frequency
                _vocab.AddToken(e.Tokens, df);
            }

            UpdateVocab();
            _classifier.UpdateCountsForNewFeature(_vocab.GetWordId(e.Tokens, true));
            if (AutoUpdatePredictions)
            {
                PerformUpdatePredictions();
            }
        }

        private void featureSetVM_FeatureRemoved(object sender, EventArgs e)
        {
            UpdateVocab();
            if (AutoUpdatePredictions)
            {
                PerformUpdatePredictions();
            }
        }

        // Tell the heatmap to show messages containing the feature the user is currently editing
        void featureSetVM_FeatureTextEdited(object sender, FeatureSetViewModel.FeatureAddedEventArgs e)
        {
            HeatMapVM.ToHighlight = e.Tokens;
        }

        void _heatMapVM_HighlightTextChanged(object sender, HeatMapViewModel.HighlightTextChangedEventArgs e)
        {
            if (CurrentMessage != null)
                CurrentMessage.HighlightWithWord(e.Text);
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
        /// Build a training set of a desired size by taking the first 'size' elements from 'source'.
        /// </summary>
        /// <param name="source">The collection to pull items from.</param>
        /// <param name="label">The label to restrict our training set to.</param>
        /// <param name="size">The number of items to add to the training set.</param>
        /// <returns></returns>
        private NewsCollection BuildTopicTrainingSet(NewsCollection source, Label label, int size)
        {
            NewsCollection nc = new NewsCollection();
            nc.Label = label;

            for (int i = 0; i < size; i++)
            {
                NewsItem item = source.First(newsItem => newsItem.Label == label);
                if (item != null)
                {
                    nc.Add(item);
                    source.Remove(item);
                }
                else
                {
                    Console.Error.WriteLine("Warning: unable to find {0} items for {1} training set.", size, label);
                }
            }

            return nc;
        }
    }
}
