﻿using GalaSoft.MvvmLight.Command;
using LibIML;
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
        List<NewsCollection> _folders; // Collection of all our folders
        NewsCollection _unknownFolder;
        NewsCollection _topic1Folder;
        NewsCollection _topic2Folder;
        NewsCollection _currentFolder;
        NewsItem _currentMessage;
        List<Label> _labels;
        Vocabulary _vocab;
        MultinomialNaiveBayesFeedbackClassifier _classifier;
        bool _autoUpdatePredictions;
        bool _onlyShowRecentChanges;
        int _topic1Predictions;
        int _topic2Predictions;
        int _recentlyChangedPredictions;

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
            _vocab = Vocabulary.CreateVocabulary(forVocab, _labels, (int)App.Current.Properties[App.PropertyKey.Topic1VocabSize] + (int)App.Current.Properties[App.PropertyKey.Topic2VocabSize]);
            timer.Stop();
            Console.WriteLine("Time to build vocab: {0}", timer.Elapsed);

            // Update our test set in light of our new vocabulary
            timer.Restart();
            Parallel.ForEach(_unknownFolder, (instance, state, index) =>
            {
                PorterStemmer stemmer = new PorterStemmer(); // PorterStemmer isn't threadsafe, so we need one for each operation.
                Dictionary<string, int> tokens = Tokenizer.TokenizeAndStem(instance.AllText, stemmer) as Dictionary<string, int>;
                instance.TokenCounts = tokens;
                instance.ComputeFeatureVector(_vocab);
            });
            timer.Stop();
            Console.WriteLine("Time to update data for new vocab: {0}", timer.Elapsed);

            // Build a classifier
            timer.Restart();
            _classifier = new MultinomialNaiveBayesFeedbackClassifier(_labels, _vocab);
            _classifier.AddInstances(forVocab);
            timer.Stop();
            Console.WriteLine("Time to build classifier: {0}", timer.Elapsed);

            // Evaluate the classifier
            timer.Restart();
            int pRight = 0;
            Topic1Predictions = 0;
            Topic2Predictions = 0;
            RecentlyChangedPredictions = 0;
            foreach (IInstance instance in _topic1Folder)
            {
                Prediction pred = _classifier.PredictInstance(instance);
                if (pred.Label == instance.Label)
                {
                    Topic1Predictions++;
                    pRight++;
                }
                else
                    Topic2Predictions++;
            }
            _topic1Folder.CorrectPredictions = pRight;
            pRight = 0;
            foreach (IInstance instance in _topic2Folder)
            {
                Prediction pred = _classifier.PredictInstance(instance);
                if (pred.Label == instance.Label)
                {
                    Topic2Predictions++;
                    pRight++;
                }
                else
                    Topic1Predictions++;
            }
            _topic2Folder.CorrectPredictions = pRight;
            foreach (IInstance instance in _unknownFolder)
            {
                Prediction pred = _classifier.PredictInstance(instance);
                if (pred.Label == _labels[0])
                    Topic1Predictions++;
                else
                    Topic2Predictions++;
            }
            timer.Stop();
            Console.WriteLine("Time to evaluate classifier: {0}", timer.Elapsed);

            UpdatePredictions = new RelayCommand(PerformUpdatePredictions, CanPerformUpdatePredictions);
            FileToUnknown = new RelayCommand(PerformFileToUnknown, CanPerformFileToUnknown);
            FileToTopic1 = new RelayCommand(PerformFileToTopic1, CanPerformFileToTopic1);
            FileToTopic2 = new RelayCommand(PerformFileToTopic2, CanPerformFileToTopic2);

            // Start with our current folder pointing at the collection of unlabeled items.
            Folders = folders;
            CurrentFolder = Folders[0];
            CurrentMessage = CurrentFolder[0];
            AutoUpdatePredictions = true;

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

        public string Topic1VocabList
        {
            get { return _vocab.ToString(); }
        }

        public string Topic2VocabList
        {
            get { return _vocab.ToString(); }
        }

        public bool AutoUpdatePredictions
        {
            get { return _autoUpdatePredictions; }
            set {
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
            NewsItem item = CurrentMessage;
            MoveMessageToFolder(item, _unknownFolder);
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
            NewsItem item = CurrentMessage;
            MoveMessageToFolder(item, _topic1Folder);
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
            NewsItem item = CurrentMessage;
            MoveMessageToFolder(item, _topic2Folder);
        }

        #endregion

        public void MoveMessageToFolder(NewsItem item, NewsCollection collection)
        {
            NewsCollection container = GetFolderContainingMessage(item);
            if (container != null)
            {
                if (!container.Remove(item))
                {
                    Console.Error.WriteLine("Error removing item {0} from container {0}", item, container);
                }
                else
                {
                    collection.Add(item);
                }
            }
        }

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
            NewsCollection nc = new NewsCollection(label.UserLabel);

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
