using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SVMPlayground.TF_IDF
{
    [Serializable]
    class Tokenizer
    {
        private static Regex _regexWordBoundaries;
        private static Regex _regexWhitespace;
        private static Regex _regexAlphaNumeric;
        private static Regex _regexNumericOnly;
        private static HashSet<string> _stopWords;

        const int MAX_TOKEN_LENGTH = 128;
        const int MIN_TOKEN_LENGTH = 3;
        const string STOPWORDS_FILENAME = "stopwords.txt";

        static Tokenizer()
        {
            _regexWordBoundaries = new Regex(@"\b|_", RegexOptions.Compiled); // Match word bounary characters and underscores
            _regexWhitespace = new Regex(@"[\s_]+", RegexOptions.Compiled); // Match whitespace and underscores
            _regexAlphaNumeric = new Regex(@"^\w+$", RegexOptions.Compiled); // Match word characters
            _regexNumericOnly = new Regex(@"^\d+$", RegexOptions.Compiled); // Match strings containing only numbers

            // Read in our stopwords file
            _stopWords = new HashSet<string>();
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Datasets", STOPWORDS_FILENAME);
            string[] stopwords = File.ReadAllLines(path);
            foreach (string word in stopwords)
                _stopWords.Add(word);
        }

        public static HashSet<string> Tokenize(string text)
        {
            return Tokenize(text, MIN_TOKEN_LENGTH);
        }

        // Break @text into tokens on word boundaries. Eliminate whitespace and only include words of length @minLength or greater.
        public static HashSet<string> Tokenize(string text, int minLength)
        {
            HashSet<string> tokens = new HashSet<string>();

            string[] tempTokens = _regexWordBoundaries.Split(text);

            foreach (string token in tempTokens)
            {
                string t = token.ToLowerInvariant();
                t = _regexWhitespace.Replace(t, @"");

                if (t.Length >= minLength && t.Length <= MAX_TOKEN_LENGTH && 
                    !_stopWords.Contains(t) &&
                    _regexAlphaNumeric.IsMatch(t) &&
                    !_regexNumericOnly.IsMatch(t))
                {
                    //Console.WriteLine("Token: '{0}'", t);
                    tokens.Add(t);
                }
            }

            return tokens;
        }
    }
}
