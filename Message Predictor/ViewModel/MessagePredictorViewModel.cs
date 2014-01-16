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
        public MessagePredictorViewModel()
        {

        }

        private void LoadDataset()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, App.DataDir, App.Current.Properties["datasetfile"].ToString());

            List<Label> labels = new List<Label>();
            labels.Add(new Label(App.Current.Properties["Topic1UserLabel"].ToString(), App.Current.Properties["Topic1SystemLabel"].ToString()));
            labels.Add(new Label(App.Current.Properties["Topic1UserLabe2"].ToString(), App.Current.Properties["Topic1SystemLabe2"].ToString()));

            NewsCollection test = NewsCollection.CreateFromZip(path, labels);
        }
    }
}
