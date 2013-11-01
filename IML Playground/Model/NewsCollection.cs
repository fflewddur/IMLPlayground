using IML_Playground.ViewModel;
using IML_Playground.Learning;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IML_Playground.Model
{
    [Serializable]
    class NewsCollection : Collection<NewsItem>, IInstances
    {
        IEnumerable<Instance> _instances;

        public NewsCollection()
        {
            _instances = null;
        }

        #region Properties

        public new int Count
        {
            get { return base.Count; }
        }

        #endregion

        #region Override methods

        protected override void ClearItems()
        {
            base.ClearItems();
            _instances = null;
        }

        protected override void InsertItem(int index, NewsItem item)
        {
            base.InsertItem(index, item);
            _instances = null;
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
            _instances = null;
        }

        protected override void SetItem(int index, NewsItem item)
        {
            base.SetItem(index, item);
            _instances = null;
        }

        #endregion

        /// <summary>
        /// Return an enumerable collection of Instances representing the items in this collection.
        /// </summary>
        /// <returns>Enumerable collection of Instances.</returns>
        public IEnumerable<Instance> ToInstances()
        {
            if (_instances == null)
            {
                List<Instance> instances = new List<Instance>();

                foreach (NewsItem item in this)
                {
                    Instance instance = new Instance { Label = item.Label, Features = item.FeatureCounts };
                    instances.Add(instance);
                }

                _instances = instances;
            }

            return _instances;
        }

        /// <summary>
        /// Compute the feature vectors for each item.
        /// </summary>
        /// <param name="vocab">The feature vocabulary to use.</param>
        public void ComputeFeatureVectors(Vocabulary vocab)
        {
            Parallel.ForEach(this, (item) =>
            {
                item.ComputeCountVector(vocab);
            });

            _instances = null; // This operation invalidates our existing instance collection.
        }

        /// <summary>
        /// Compute TF-IDF vectors for each item in the collection.
        /// </summary>
        /// <param name="vocab">Vocabulary to use.</param>
        /// <param name="nDocs">Number of documents to use in IDF computation.</param>
        public void ComputeTFIDFVectors(Vocabulary vocab, int nDocs)
        {
            Parallel.ForEach(this, (item) =>
            {
                item.ComputeTFIDFVector(vocab, nDocs);
            });
        }

        public void ComputeTFIDFVectors(Vocabulary vocab)
        {
            ComputeTFIDFVectors(vocab, this.Count);
        }

        /// <summary>
        /// Create a Vocabulary object representing all of the tokens identified in this NewsCollection.
        /// </summary>
        /// <returns>A new Vocabulary object.</returns>
        public Vocabulary BuildVocabulary(double min_df_percent = Vocabulary.MIN_DF_PERCENT, double max_df_percent = Vocabulary.MAX_DF_PERCENT)
        {
            ConcurrentDictionary<string, int> tokenDocCounts = new ConcurrentDictionary<string, int>();

            // Tokenize and stem each document, returning a collection of tokens and corresponding counts.
            Parallel.ForEach(this, (item, state, index) =>
            {
                PorterStemmer stemmer = new PorterStemmer(); // PorterStemmer isn't threadsafe, so we need one for each operation.
                Dictionary<string, int> tokens = Tokenizer.TokenizeAndStem(item.AllText, stemmer) as Dictionary<string, int>;
                item.TokenCounts = tokens;
                foreach (KeyValuePair<string, int> pair in tokens)
                {
                    // If this key doesn't exist yet, add it with a value of 1. Otherwise, increment its value by 1.
                    tokenDocCounts.AddOrUpdate(pair.Key, 1, (key, value) => value + 1);
                }
            });

            Vocabulary vocab = new Vocabulary();
            vocab.AddTokens(tokenDocCounts, this.Count, min_df_percent, max_df_percent);

            return vocab;
        }

        /// <summary>
        /// Tokenize each item in the collection.
        /// </summary>
        public void TokenizeItems()
        {
            Parallel.ForEach(this, (item) =>
            {
                PorterStemmer stemmer = new PorterStemmer();
                Dictionary<string, int> tokens = Tokenizer.TokenizeAndStem(item.AllText, stemmer) as Dictionary<string, int>;
                item.TokenCounts = tokens;
            });
        }

        /// <summary>
        /// Read in a .zip file of the 20 Newsgroups dataset.
        /// </summary>
        /// <param name="path">Path to ZIP archive.</param>
        /// <param name="labels">List of newsgroups to include, or null to include all groups.</param>
        /// <returns>A NewsCollection representing the newsgroup messages in the ZIP archive.</returns>
        public static NewsCollection CreateFromZip(string path, params Label[] labels)
        {
            NewsCollection nc = new NewsCollection();

            if (!File.Exists(path))
            {
                Console.Error.WriteLine("Error: file '{0}' not found.", path);
                return null;
            }

            using (ZipArchive zip = ZipFile.OpenRead(path))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    // Don't bother with directory entries
                    if (entry.FullName != null && !entry.FullName.EndsWith("/"))
                    {
                        NewsItem item = null;
                        if (labels.Length > 0)
                        {
                            foreach (Label label in labels) // Did we ask to include this group?
                            {
                                if (entry.FullName.StartsWith(label.SystemLabel, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    item = NewsItem.CreateFromStream(entry.Open(), entry.FullName);
                                    if (item != null)
                                        item.Label = label;
                                    break;
                                }
                            }
                        }
                        else
                            item = NewsItem.CreateFromStream(entry.Open(), entry.FullName);

                        if (item != null)
                            nc.Add(item);
                    }
                }
            }

            return nc;
        }

        /// <summary>
        /// Create a new NewsCollection, using this one as a starting point, that only includes specified groups.
        /// </summary>
        /// <param name="size">The max size of each group.</param>
        /// <param name="labels">The news groups to include in the new collection.</param>
        /// <returns>A new NewsCollection.</returns>
        public NewsCollection ItemsSubset(int size, params Label[] labels)
        {
            NewsCollection nc = new NewsCollection();
            Random rand = new Random();

            foreach (Label label in labels)
            {
                NewsCollection thisLabel = new NewsCollection();

                // Get all of the items with a matching label
                foreach (NewsItem item in this)
                {
                    if (item.OriginalGroup.Equals(label.SystemLabel, StringComparison.InvariantCultureIgnoreCase))
                    {
                        item.Label = label;
                        thisLabel.Add(item);
                    }
                }

                // Randomly remove items until the list's length is less than 'size'
                while (thisLabel.Count > size)
                {
                    thisLabel.RemoveAt(rand.Next(thisLabel.Count));
                }

                // Add the items (no more than 'size') to our collection
                foreach (NewsItem item in thisLabel)
                    nc.Add(item);
            }

            return nc;
        }

        // Call Subset, but don't restrict group size
        public NewsCollection ItemsSubset(params Label[] labels)
        {
            return this.ItemsSubset(Int32.MaxValue, labels);
        }

        /// <summary>
        /// Save a weka-compatible ARFF file of this collection.
        /// </summary>
        /// <param name="filePath">The location of the output file.</param>
        /// <param name="vocab">The Vocabulary of features.</param>
        /// <param name="labels">The collection of potential output labels.</param>
        public async Task<bool> SaveArffFile(string filePath, Vocabulary vocab, params Label[] labels)
        {
            return await MultinomialNaiveBayesClassifier.SaveArffFile(ToInstances(), vocab, labels, filePath);
        }

        /// <summary>
        /// Return a collection of instances from this collection, with a limited number of items in each class.
        /// </summary>
        /// <param name="size">The max number of items to include from each class.</param>
        /// <param name="labels">List of classes.</param>
        /// <returns></returns>
        public IEnumerable<Instance> Subset(int size, params Label[] labels)
        {
            Random rand = new Random();
            List<Instance> subset = new List<Instance>();

            foreach (Label label in labels)
            {
                List<Instance> thisLabel = new List<Instance>();

                // Get all of the items with a matching label
                foreach (Instance instance in this.ToInstances())
                {
                    if (instance.Label.Equals(label))
                    {
                        thisLabel.Add(instance);
                    }
                }

                // Randomly remove items until the list's length is less than 'size'
                while (thisLabel.Count > size)
                {
                    thisLabel.RemoveAt(rand.Next(thisLabel.Count));
                }

                // Add the items (no more than 'size') to our collection
                foreach (Instance instance in thisLabel)
                    subset.Add(instance);
            }

            return subset;
        }
    }
}
