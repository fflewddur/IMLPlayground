﻿using IML_Playground.Framework;
using IML_Playground.Learning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IML_Playground.Model
{
    class NewsItem : IML_Playground.Framework.Model
    {
        private int _id;
        private string _originalGroup;
        private string _subject;
        private string _body;
        private string _author;
        private Dictionary<string, int> _tokenCounts;
        private SparseVector _features;

        public NewsItem()
        {
        }

        #region Properties

        public int Id
        {
            get { return _id; }
            private set { SetProperty<int>(ref _id, value); }
        }

        public string OriginalGroup
        {
            get { return _originalGroup; }
            private set { SetProperty<string>(ref _originalGroup, value); }
        }

        public string Subject
        {
            get { return _subject; }
            private set { SetProperty<string>(ref _subject, value); }
        }

        public string Body
        {
            get { return _body; }
            private set { SetProperty<string>(ref _body, value); }
        }

        public string Author
        {
            get { return _author; }
            private set { SetProperty<string>(ref _author, value); }
        }

        public string AllText
        {
            get { return Subject + ' ' + Body; }
        }

        public Dictionary<string, int> TokenCounts
        {
            get { return _tokenCounts; }
            set { SetProperty<Dictionary<string, int>>(ref _tokenCounts, value); }
        }

        public SparseVector Features
        {
            get { return _features; }
            private set { SetProperty<SparseVector>(ref _features, value); }
        }

        #endregion

        public void ComputeTFIDFVector(Vocabulary vocab, int nDocs)
        {
            SparseVector features = new SparseVector();

            foreach (KeyValuePair<string, int> pair in TokenCounts)
            {
                int wordId = vocab.GetWordId(pair.Key);
                if (wordId > 0) // Make sure this token was included in our vocabulary
                {
                    int df = vocab.GetDocFreq(wordId);
                    double tf = Math.Log10(pair.Value + 1); // Normalized term frequency
                    double idf = Math.Log10(nDocs / (df + 1)); // Normalized inverse document frequency
                    features.Set(wordId, tf * idf);
                }
            }

            Features = features;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(string.Format("Message ID: {0}", Id));
            sb.AppendLine(string.Format("Original group: {0}", OriginalGroup));
            sb.AppendLine(string.Format("Author: {0}", Author));
            sb.AppendLine(string.Format("Subject: {0}", Subject));
            sb.AppendLine(Body);

            return sb.ToString();
        }

        public static NewsItem CreateFromStream(Stream stream, string fullName)
        {
            NewsItem item = null;

            using (TextReader reader = new StreamReader(stream))
            {
                string line;
                StringBuilder body = new StringBuilder();
                string subject = null;
                string author = null;
                bool prevLineBlank = false;
                int id;
                string originalGroup;

                if (!GetGroupAndId(fullName, out originalGroup, out id))
                    Console.Error.WriteLine("Warning: could not get group and ID from file '{0}'.", fullName);

                while ((line = reader.ReadLine()) != null)
                {
                    if (body.Length > 0 || // We've already found the body of the message, keep adding to it.
                        (prevLineBlank && !line.Contains(':')) || // This isn't a header line, and we found the empty line denoting the start of the message.
                        (prevLineBlank && line.Contains("writes"))) // This line references the re: message, and we found the empty line denoting the start of the message.
                        body.AppendLine(line);
                    else if (line.StartsWith("From: ", StringComparison.InvariantCultureIgnoreCase))
                        author = line.Substring("From: ".Length);
                    else if (line.StartsWith("Subject: ", StringComparison.InvariantCultureIgnoreCase))
                        subject = line.Substring("Subject: ".Length);
                    else if (string.IsNullOrWhiteSpace(line))
                        prevLineBlank = true;
                }

                if (body.Length > 0) // Ensure we have some content in this item
                    item = new NewsItem { Id = id, OriginalGroup = originalGroup, Author = author, Subject = subject, Body = body.ToString() };
            }

            return item;
        }

        private static bool GetGroupAndId(string fullName, out string group, out int id)
        {
            int splitter = fullName.LastIndexOf('/');
            group = fullName.Substring(0, splitter);
            string strId = fullName.Substring(splitter + 1);
            int.TryParse(strId, out id);

            if (group != null && id > 0)
                return true;
            else
                return false;
        }
    }
}
