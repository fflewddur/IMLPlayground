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
        /// Create a Vocabulary object representing all of the tokens identified in this NewsCollection.
        /// </summary>
        /// <returns>A new Vocabulary object.</returns>
        public Vocabulary BuildVocabulary()
        {
            //HashSet<string>[] tokenSets = new HashSet<string>[this.Count];
            ConcurrentDictionary<string, int> tokenDocCounts = new ConcurrentDictionary<string, int>();

            // Tokenize and stem each document, returning a collection of tokens and corresponding counts.
            Parallel.ForEach(this, (item, state, index) =>
            {
                PorterStemmer stemmer = new PorterStemmer();
                //HashSet<string> tokens = (HashSet<string>)Tokenizer.TokenizeAndStem(item.Body, stemmer);
                //tokens.UnionWith(Tokenizer.Tokenize(item.Subject));
                Dictionary<string, int> tokens = Tokenizer.TokenizeAndStem(item.AllText, stemmer) as Dictionary<string, int>;
                item.TokenCounts = tokens;
                foreach (KeyValuePair<string, int> pair in tokens)
                {
                    tokenDocCounts.AddOrUpdate(pair.Key, 1, (key, value) => value + 1); // If this key doesn't exist yet, add it with a value of 1. Otherwise, increment its value by 1.
                }
//                tokenSets[index] = tokens;
            });
            //HashSet<string> tokens = new HashSet<string>();
            //int index = 0;
            //foreach (NewsItem item in this)
            //{
            //    HashSet<string> tokens = (HashSet<string>)Tokenizer.Tokenize(item.Body);
            //    tokens.UnionWith(Tokenizer.Tokenize(item.Subject));
            //    tokenSets[index] = tokens;
            //    index++;
            //}

            //foreach (string token in tokens)
            //{
            //    Console.WriteLine("'{0}'", token);
            //}

            //HashSet<string> allTokens = new HashSet<string>();
            //foreach (HashSet<string> tokens in tokenSets)
            //    allTokens.UnionWith(tokens);

            Vocabulary vocab = new Vocabulary();
            vocab.AddTokens(tokenDocCounts);

            return vocab;
        }

        /// <summary>
        /// Read in a .zip file of the 20 Newsgroups dataset.
        /// </summary>
        /// <param name="path">Path to ZIP archive.</param>
        /// <returns>A NewsCollection representing the newsgroup messages in the ZIP archive.</returns>
        public static NewsCollection CreateFromZip(string path)
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
                        NewsItem item = NewsItem.CreateFromStream(entry.Open(), entry.FullName);
                        if (item != null)
                            nc.Add(item);
                    }
                }
            }

            return nc;
        }
    }
}
