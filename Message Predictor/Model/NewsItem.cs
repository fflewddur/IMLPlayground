using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibIML;
using System.IO;

namespace MessagePredictor
{
    class NewsItem : ViewModelBase, IInstance
    {
        private int _id;
        private string _originalGroup;
        private string _subject;
        private string _body;
        private string _author;
        private Dictionary<string, int> _tokenCounts;
        private SparseVector _featureWeights;
        private SparseVector _featureCounts;
        private Label _label;

        public NewsItem() : base()
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

        public Label Label
        {
            get { return _label; }
            set { SetProperty<Label>(ref _label, value); }
        }

        public string AllText
        {
            get { return Subject + "\n" + Body; }
        }

        #endregion

        /// <summary>
        /// Create a new NewsItem from an IO Stream.
        /// </summary>
        /// <param name="stream">The IO Stream to read from.</param>
        /// <param name="filePath">The file path, based on where the file is in the 20 Newsgroup archive.</param>
        /// <returns></returns>
        public static NewsItem CreateFromStream(Stream stream, string filePath)
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

                if (!GetGroupAndId(filePath, out originalGroup, out id))
                    Console.Error.WriteLine("Warning: could not get group and ID from file '{0}'.", filePath);

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

        /// <summary>
        /// Parse a filename to get the message's group and ID.
        /// </summary>
        /// <param name="filePath">The full path of the message (from within the 20 Newsgroup archive)</param>
        /// <param name="group">Returns the newsgroup this message came from</param>
        /// <param name="id">Return the message ID</param>
        /// <returns>True on success, false otherwise.</returns>
        private static bool GetGroupAndId(string filePath, out string group, out int id)
        {
            int splitter = filePath.LastIndexOf('/');
            group = filePath.Substring(0, splitter);
            string strId = filePath.Substring(splitter + 1);
            int.TryParse(strId, out id);

            if (group != null && id > 0)
                return true;
            else
                return false;
        }
    }
}
