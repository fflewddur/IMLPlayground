using LibIML;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePredictor
{
    class MessagePredictorViewModel: ViewModelBase
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

        public MessagePredictorViewModel()
        {
            List<NewsCollection> folders = new List<NewsCollection>();
            _unknownFolder = LoadDataset();
            _topic1Folder = BuildTopicTrainingSet(_unknownFolder, _labels[0], (int)App.Current.Properties[App.PropertyKey.Topic1TrainSize]);
            _topic2Folder = BuildTopicTrainingSet(_unknownFolder, _labels[1], (int)App.Current.Properties[App.PropertyKey.Topic2TrainSize]);

            folders.Add(_unknownFolder);
            folders.Add(_topic1Folder);
            folders.Add(_topic2Folder);

            // Build a vocab from our training set
            List<NewsItem> forVocab = new List<NewsItem>();
            forVocab.AddRange(_topic1Folder.ToList());
            forVocab.AddRange(_topic2Folder.ToList());
            _vocab = Vocabulary.CreateVocabulary(forVocab, _labels, (int)App.Current.Properties[App.PropertyKey.Topic1VocabSize] + (int)App.Current.Properties[App.PropertyKey.Topic2VocabSize]);

            // Build a classifier
            _classifier = new MultinomialNaiveBayesFeedbackClassifier(_labels, _vocab);
            _classifier.AddInstances(forVocab);

            // Evaluate the classifier
            int pRight = 0;
            foreach (IInstance instance in _topic1Folder)
            {
                Prediction pred = _classifier.PredictInstance(instance);
                if (pred.Label == instance.Label)
                    pRight++;
            }
            _topic1Folder.CorrectPredictions = pRight;
            pRight = 0;
            foreach (IInstance instance in _topic2Folder)
            {
                Prediction pred = _classifier.PredictInstance(instance);
                if (pred.Label == instance.Label)
                    pRight++;
            }
            _topic2Folder.CorrectPredictions = pRight;

            // Start with our current folder pointing at the collection of unlabeled items.
            Folders = folders;
            CurrentFolder = Folders[0];
            CurrentMessage = CurrentFolder[0];
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
                    CurrentMessage = CurrentFolder[0]; // Go to the first message in this folder
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
            get
            {
                return _vocab.ToString();
            }
        }

        public string Topic2VocabList
        {
            get
            {
                return _vocab.ToString();
            }
        }

        #endregion

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
