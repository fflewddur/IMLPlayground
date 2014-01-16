using LibIML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePredictor
{
    class MessagePredictorViewModel: ViewModelBase
    {
        NewsCollection _unlabeled;
        NewsCollection _labeled;
        NewsCollection _currentFolder;
        NewsItem _currentMessage;

        public MessagePredictorViewModel()
        {
            _unlabeled = LoadDataset();
            
            // Start with our current folder pointing at the collection of unlabeled items.
            CurrentFolder = _unlabeled;
        }

        #region Properties

        public NewsCollection CurrentFolder
        {
            get { return _currentFolder; }
            private set { SetProperty<NewsCollection>(ref _currentFolder, value); }
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

            List<Label> labels = new List<Label>();
            labels.Add(new Label(App.Current.Properties[App.PropertyKey.Topic1UserLabel].ToString(), App.Current.Properties[App.PropertyKey.Topic1SystemLabel].ToString()));
            labels.Add(new Label(App.Current.Properties[App.PropertyKey.Topic2UserLabel].ToString(), App.Current.Properties[App.PropertyKey.Topic2SystemLabel].ToString()));

            NewsCollection dataset = NewsCollection.CreateFromZip(path, labels);

            return dataset;
        }
    }
}
