using IML_Playground.Framework;
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
    class NewsCollection : Collection<NewsItem>
    {
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
        public Vocabulary BuildVocabulary()
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
            vocab.AddTokens(tokenDocCounts);

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
        /// <param name="groups">List of newsgroups to include, or null to include all groups.</param>
        /// <returns>A NewsCollection representing the newsgroup messages in the ZIP archive.</returns>
        public static NewsCollection CreateFromZip(string path, params string[] groups)
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
                        if (groups.Length > 0)
                        {
                            foreach (string group in groups) // Did we ask to include this group?
                            {
                                if (entry.FullName.StartsWith(group))
                                {
                                    item = NewsItem.CreateFromStream(entry.Open(), entry.FullName);
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
        /// <param name="groups">The news groups to include in the new collection.</param>
        /// <returns>A new NewsCollection.</returns>
        public NewsCollection Subset(params string[] groups)
        {
            NewsCollection nc = new NewsCollection();

            foreach (NewsItem item in this)
            {
                foreach (string group in groups)
                {
                    if (item.OriginalGroup.Equals(group))
                    {
                        nc.Add(item);
                        break;
                    }
                }
            }

            return nc;
        }
    }
}
