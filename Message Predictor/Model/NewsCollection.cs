using LibIML;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePredictor.Model
{
    class NewsCollection : ViewModelCollectionBase<NewsItem>
    {
        //string _title;
        //Label _label;
        //int _correctPredictions;

        public NewsCollection()
            : base()
        {
            //Title = title;
            //Label = null;
            //CorrectPredictions = 0;
        }

        #region Properties

        //public string Title
        //{
        //    get { return _title; }
        //    set { SetProperty<string>(ref _title, value); }
        //}

        //public Label Label
        //{
        //    get { return _label; }
        //    set { SetProperty<Label>(ref _label, value); }
        //}

        //public int CorrectPredictions
        //{
        //    get { return _correctPredictions; }
        //    set { SetProperty<int>(ref _correctPredictions, value); }
        //}

        #endregion

        /// <summary>
        /// Read in a .zip file of the 20 Newsgroups dataset.
        /// </summary>
        /// <param name="path">Path to ZIP archive.</param>
        /// <param name="labels">List of newsgroups to include, or null to include all groups.</param>
        /// <returns>A NewsCollection representing the newsgroup messages in the ZIP archive.</returns>
        public static NewsCollection CreateFromZip(string path, IEnumerable<Label> labels = null)
        {
            NewsCollection nc = new NewsCollection();

            if (!File.Exists(path)) {
                Console.Error.WriteLine("Error: file '{0}' not found.", path);
                return null;
            }

            using (ZipArchive zip = ZipFile.OpenRead(path)) {
                foreach (ZipArchiveEntry entry in zip.Entries) {
                    // Don't bother with directory entries
                    if (entry.FullName != null && !entry.FullName.EndsWith("/")) {
                        NewsItem item = null;
                        if (labels != null && labels.Count() > 0) {
                            foreach (Label label in labels) // Did we ask to include this group?
                            {
                                if (entry.FullName.StartsWith(label.SystemLabel, StringComparison.InvariantCultureIgnoreCase)) {
                                    item = NewsItem.CreateFromStream(entry.Open(), entry.FullName);
                                    if (item != null)
                                        item.GroundTruthLabel = label;
                                    break;
                                }
                            }
                        } else
                            item = NewsItem.CreateFromStream(entry.Open(), entry.FullName);

                        if (item != null)
                            nc.Add(item);
                    }
                }
            }

            return nc;
        }
    }
}
