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
        List<NewsCollection> _folders;
        NewsCollection _currentFolder;
        NewsItem _currentMessage;
        List<Label> _labels;

        public MessagePredictorViewModel()
        {
            List<NewsCollection> folders = new List<NewsCollection>();
            NewsCollection unknown = LoadDataset();
            NewsCollection topic1 = BuildTopicTrainingSet(unknown, _labels[0], (int)App.Current.Properties[App.PropertyKey.Topic1TrainSize]);
            NewsCollection topic2 = BuildTopicTrainingSet(unknown, _labels[1], (int)App.Current.Properties[App.PropertyKey.Topic2TrainSize]);

            folders.Add(unknown);
            folders.Add(topic1);
            folders.Add(topic2);

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
