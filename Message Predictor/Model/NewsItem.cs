﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibIML;
using System.IO;
using System.Windows.Documents;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Diagnostics;

namespace MessagePredictor.Model
{
    public class NewsItem : ViewModelBase, IInstance, IEditableObject
    {
        private readonly double MIN_CONFIDENCE_CHANGE = .01;
        private static Regex _newlineRegex = new Regex(@"\r?\n");
        private static Regex _whitespaceRegex = new Regex(@"\s\s+");

        private int _id;
        private int _order;
        private string _originalGroup;
        private string _subject;
        private string _body;
        private string _bodySingleLine;
        private string _author;
        private Dictionary<string, int> _tokenCounts;
        private Prediction _prediction;
        private Prediction _previousPrediction;
        private Direction _predictionConfidenceDirection;
        private double _predictionConfidenceDifference;
        private bool _recentlyChanged;
        private bool? _isPredictionCorrect;
        private SparseVector _featureCounts;
        private Label _userLabel;
        private Label _groundTruthLabel;
        private bool _isHighlighted; // Should this message be highlighted in the heatmap?
        private string _document; // The message in XML for easier displaying (including highlighting of features)
        private Vocabulary _vocab; // Keep a reference to the most recent Vocabulary we've computed features for; needed for displaying _document with features highlighted
        private bool _isInEditMode;

        public NewsItem()
            : base()
        {
            _document = null;
            _vocab = null;
            _isHighlighted = false;
        }

        #region Properties

        public int Id
        {
            get { return _id; }
            private set { SetProperty<int>(ref _id, value); }
        }

        public int Order
        {
            get { return _order; }
            private set { SetProperty<int>(ref _order, value); }
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
            private set
            {
                if (SetProperty<string>(ref _body, value)) {
                    string body = _newlineRegex.Replace(_body, " ");
                    body = _whitespaceRegex.Replace(body, " ");
                    BodySingleLine = body;
                }
            }
        }

        public string BodySingleLine
        {
            get { return _bodySingleLine; }
            private set { SetProperty<string>(ref _bodySingleLine, value); }
        }

        public string Author
        {
            get { return _author; }
            private set { SetProperty<string>(ref _author, value); }
        }

        public Label UserLabel
        {
            get { return _userLabel; }
            set { SetProperty<Label>(ref _userLabel, value); }
        }

        public Label GroundTruthLabel
        {
            get { return _groundTruthLabel; }
            set { SetProperty<Label>(ref _groundTruthLabel, value); }
        }

        public bool IsHighlighted
        {
            get { return _isHighlighted; }
            set { SetProperty<bool>(ref _isHighlighted, value); }
        }

        public string AllText
        {
            get { return string.Format("{0}\n{1}\n{2}", Subject, Author, Body); }
        }

        public string AllTextSingleLine
        {
            get { return string.Format("{0}\n{1}\n{2}", Subject, Author, BodySingleLine); }
        }

        public string Document
        {
            get
            {
                // If our display document has been invalidated by feature changes, rebuild it.
                if (_document == null)
                    UpdateDocument(_vocab);

                return _document;
            }
            private set
            {
                if (SetProperty<string>(ref _document, value))
                    this.OnPropertyChanged("DocumentReadOnly");
            }
        }

        public string DocumentReadOnly
        {
            get { return Document; }
            set { /* This needs to be public because of a bug in RichTextBox, but don't do anything here. */ }
        }

        public Dictionary<string, int> TokenCounts
        {
            get { return _tokenCounts; }
            set { SetProperty<Dictionary<string, int>>(ref _tokenCounts, value); }
        }

        public Prediction Prediction
        {
            get { return _prediction; }
            set
            {
                PreviousPrediction = Prediction;
                
                SetProperty<Prediction>(ref _prediction, value);                

                if (Prediction.Label == GroundTruthLabel)
                    IsPredictionCorrect = true;
                else
                    IsPredictionCorrect = false;

                if (PreviousPrediction != null) {
                    if (PreviousPrediction.Label == Prediction.Label) {
                        PredictionConfidenceDifference = Math.Abs(Prediction.Confidence - PreviousPrediction.Confidence);
                        PredictionConfidenceDirection = Direction.None; // Always refresh this arrow
                        RecentlyChanged = false;
                        // If our label hasn't changed, check the direction of the confidence change.
                        // (Only show a confidence change if the difference is more than 1%)
                        if (Prediction.Confidence - PreviousPrediction.Confidence >= MIN_CONFIDENCE_CHANGE) {
                            PredictionConfidenceDirection = Direction.Up;
                        } else if (PreviousPrediction.Confidence - Prediction.Confidence >= MIN_CONFIDENCE_CHANGE) {
                            PredictionConfidenceDirection = Direction.Down;
                        } else {
                            PredictionConfidenceDirection = Direction.None;
                        }
                    } else {
                        // If the label changed, the prior confidence doesn't really matter.
                        RecentlyChanged = true;
                        PredictionConfidenceDirection = Direction.None;
                        PredictionConfidenceDifference = 0;
                    }
                } else {
                    RecentlyChanged = false;
                    PredictionConfidenceDirection = Direction.None;
                    PredictionConfidenceDifference = 0;
                }
            }
        }

        public Prediction PreviousPrediction
        {
            get { return _previousPrediction; }
            private set { SetProperty<Prediction>(ref _previousPrediction, value); }
        }

        public Direction PredictionConfidenceDirection
        {
            get { return _predictionConfidenceDirection; }
            private set { SetProperty<Direction>(ref _predictionConfidenceDirection, value); }
        }

        public double PredictionConfidenceDifference
        {
            get { return _predictionConfidenceDifference; }
            private set { SetProperty<double>(ref _predictionConfidenceDifference, value); }
        }

        public bool? IsPredictionCorrect
        {
            get { return _isPredictionCorrect; }
            private set { SetProperty<bool?>(ref _isPredictionCorrect, value); }
        }

        public bool RecentlyChanged
        {
            get { return _recentlyChanged; }
            private set { SetProperty<bool>(ref _recentlyChanged, value); }
        }

        public SparseVector Features
        {
            get { return _featureCounts; }
            private set { SetProperty<SparseVector>(ref _featureCounts, value); }
        }

        public bool IsInEditMode
        {
            get { return _isInEditMode; }
            private set { SetProperty<bool>(ref _isInEditMode, value); }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Given a vocabulary, compute the number of instances of each vocabulary member in this document.
        /// The values are stored in this item's FeatureCounts property.
        /// </summary>
        /// <param name="vocab">The vocabulary to restrict ourselves to.</param>
        public void ComputeFeatureVector(Vocabulary vocab, bool isRestricted)
        {
            SparseVector features = new SparseVector();

            foreach (KeyValuePair<string, int> pair in TokenCounts) {
                int wordId = vocab.GetWordId(pair.Key, isRestricted);
                if (wordId > 0) // Make sure this token was included in our vocabulary
                    features.Set(wordId, pair.Value);
            }

            Features = features;
            _vocab = vocab; // Track our most-recently used vocabulary (for highlighting features)
            Document = null; // force our document to re-parse the next time it's requested for display
        }

        public bool TokenizeForPattern(Regex pattern, string token)
        {
            int count = pattern.Matches(AllTextSingleLine).Count;
            if (count > 0)
                TokenCounts[token] = count;

            return (count > 0);
        }

        public void HighlightWithWord(string word)
        {
            UpdateDocument(_vocab, word);
        }

        #endregion

        #region IEditableObject support
        
        public void BeginEdit()
        {
            IsInEditMode = true;
        }

        public void CancelEdit()
        {
            IsInEditMode = false;
        }
        
        public void EndEdit()
        {
            IsInEditMode = false;
        }

        #endregion

        /// <summary>
        /// Create an XML document that includes tags describing which features to highlight.
        /// </summary>
        /// <param name="vocab"></param>
        private void UpdateDocument(Vocabulary vocab, string highlightedFeature = null)
        {
            // Use the largest first, so that sub-features don't get broken up (e.g., if "team" and "teams" are features, don't split "teams" into "team" and "s")
            List<string> featureWords = vocab.GetFeatureWords().OrderByDescending(s => s.Length).ToList();

            // Ensure the list of features to highlight includes the specifically requested one
            if (!string.IsNullOrWhiteSpace(highlightedFeature) && !featureWords.Contains(highlightedFeature)) {
                featureWords.Add(highlightedFeature);
            }

            // Ensure linebreaks won't break the highlighting
            List<string> featureWordPatterns = new List<string>();
            for (int i = 0; i < featureWords.Count; i++) {
                string escaped = Regex.Replace(Regex.Escape(featureWords[i].Trim()), @"(\\ )+", @" (\r?\n)?");
                featureWordPatterns.Add(escaped);
            }

            // Split the text on the features, then join it back together with <feature> tags surrounding each feature
            string[] lines = AllText.Split(new string[] { "\r\n", "\n" }, 3, StringSplitOptions.None);
            string featurePattern = @"\b(" + string.Join("|", featureWordPatterns) + @")\b";
            Regex featureRegex = new Regex(featurePattern, RegexOptions.IgnoreCase);
            
            for (int i = 0; i < 3 && featureWords.Count > 0; i++) {
                lines[i] = featureRegex.Replace(lines[i], "<feature>$1<feature>");
            }

            XElement root = new XElement("message");

            if (lines.Length > 1) {
                for (int i = 0; i < lines.Length; i++) {

                    // First line is the subject, second line is the sender, everything else is the message body
                    XElement line;
                    if (i == 0) {
                        line = new XElement("subject");
                    } else if (i == 1) {
                        line = new XElement("sender");
                    } else {
                        line = new XElement("line");
                    }

                    string[] phrases = lines[i].Split(new string[] { "<feature>" }, StringSplitOptions.None); // Split on features/non-features
                    Regex linebreakRegex = new Regex(@"\r?\n");
                    foreach (string phrase in phrases) {
                        string phraseWithoutLinebreak = linebreakRegex.Replace(phrase, " ");
                        XElement phraseElement;

                        // If this is our highlighted feature, tag it appropriately
                        if (string.Equals(phraseWithoutLinebreak, highlightedFeature, StringComparison.InvariantCultureIgnoreCase)) {
                            phraseElement = new XElement("highlightedFeature", phrase);
                        } else if (featureWords.Contains(phraseWithoutLinebreak, StringComparer.InvariantCultureIgnoreCase)) {
                            // Otherwise, see if this is a feature
                            phraseElement = new XElement("feature", phrase);
                        } else {
                            // Or just normal text
                            phraseElement = new XElement("normal", phrase);
                        }
                        
                        line.Add(phraseElement);
                    }

                    root.Add(line);
                }
            }

            XDocument doc = new XDocument(root);

            Document = doc.ToString();
        }

        #region Static methods

        /// <summary>
        /// Create a new NewsItem from an IO Stream.
        /// </summary>
        /// <param name="stream">The IO Stream to read from.</param>
        /// <param name="filePath">The file path, based on where the file is in the 20 Newsgroup archive.</param>
        /// <returns></returns>
        public static NewsItem CreateFromStream(Stream stream, string filePath)
        {
            NewsItem item = null;

            using (TextReader reader = new StreamReader(stream)) {
                string line;
                StringBuilder body = new StringBuilder();
                string subject = null;
                string author = null;
                bool prevLineBlank = false;
                int id;
                int order = 0;
                string originalGroup;

                if (!GetGroupAndId(filePath, out originalGroup, out id))
                    Console.Error.WriteLine("Warning: could not get group and ID from file '{0}'.", filePath);

                while ((line = reader.ReadLine()) != null) {
                    if (body.Length > 0 || // We've already found the body of the message, keep adding to it.
                        (prevLineBlank && !line.Contains(':')) || // This isn't a header line, and we found the empty line denoting the start of the message.
                        (prevLineBlank && line.Contains("writes"))) // This line references the re: message, and we found the empty line denoting the start of the message.
                        body.AppendLine(line);
                    else if (line.StartsWith("From: ", StringComparison.InvariantCultureIgnoreCase))
                        author = line.Substring("From: ".Length);
                    else if (line.StartsWith("Subject: ", StringComparison.InvariantCultureIgnoreCase))
                        subject = line.Substring("Subject: ".Length);
                    else if (line.StartsWith("Order: ", StringComparison.InvariantCultureIgnoreCase))
                        order = int.Parse(line.Substring("Order: ".Length));
                    else if (string.IsNullOrWhiteSpace(line))
                        prevLineBlank = true;
                }

                if (body.Length > 0) // Ensure we have some content in this item
                    item = new NewsItem { Id = id, Order = order, OriginalGroup = originalGroup, Author = author, Subject = subject, Body = body.ToString() };
            }

            return item;
        }

        /// <summary>
        /// Create a new NewsItem from an existing NewsItem. Only copy the contents that would be loaded if we created this via the CreateFromStream method.
        /// </summary>
        /// <param name="existing">Item to deep-copy.</param>
        /// <returns>A new deep-copy of the NewsItem.</returns>
        public static NewsItem CreateFromExisting(NewsItem e)
        {
            Debug.Assert(e != null);

            NewsItem item = new NewsItem { Id = e.Id, Order = e.Order, OriginalGroup = e.OriginalGroup, Author = e.Author, 
                Subject = e.Subject, Body = e.Body, UserLabel = e.UserLabel, GroundTruthLabel = e.GroundTruthLabel };

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

        #endregion
    }
}
