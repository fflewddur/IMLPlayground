﻿using LibIML;
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
    public class NewsCollection : ViewModelCollectionBase<NewsItem>
    {
        public NewsCollection()
            : base()
        {
        }

        #region Properties

        #endregion

        /// <summary>
        /// Read in a .zip file of the 20 Newsgroups dataset.
        /// </summary>
        /// <param name="path">Path to ZIP archive.</param>
        /// <param name="labels">List of newsgroups to include, or null to include all groups.</param>
        /// <returns>A NewsCollection representing the newsgroup messages in the ZIP archive.</returns>
        public static NewsCollection CreateFromZip(string path, IEnumerable<Label> labels = null)
        {
            //NewsCollection nc = new NewsCollection();
            List<NewsItem> items = new List<NewsItem>();

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
                            items.Add(item);
                    }
                }
            }

            NewsCollection nc = new NewsCollection();
            foreach (NewsItem item in items.OrderBy(o => o.Order)) {
                nc.Add(item);
            }

            return nc;
        }

        /// <summary>
        /// Create a new NewsCollection by making a deep-copy of items in an existing collection.
        /// Only item attributes that are loaded from the source data (Subject, Body, Author, etc.) will be copied.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static NewsCollection CreateFromExisting(NewsCollection e)
        {
            NewsCollection nc = new NewsCollection();
            
            foreach (NewsItem item in e.OrderBy(o => o.Order)) {
                nc.Add(NewsItem.CreateFromExisting(item));
            }

            return nc;
        }
    }
}
