using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IML_Playground.Learning
{
    [Serializable]
    class Tokenizer
    {
        private static readonly Regex _regexWordBoundaries;
        private static readonly Regex _regexWhitespace;
        private static readonly Regex _regexAlphaNumeric;
        private static readonly Regex _regexNumericOnly;
        private static readonly HashSet<string> _stopWords;
        private static readonly char[] WordSeparators = { ' ', '\t', ',', '.', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']', '<', '>', '\r', '\n' };

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

        public static IEnumerable<string> Tokenize(string text)
        {
            return Tokenize(text, MIN_TOKEN_LENGTH);
        }

        /// <summary>
        /// Break input into tokens on word boundaries. Eliminate whitespace and only include words of minLength or greater.
        /// </summary>
        /// <param name="text">The input text to tokenize.</param>
        /// <param name="minLength">The minimum token length.</param>
        /// <returns>An enumerable collection of string tokens.</returns>
        public static IEnumerable<string> Tokenize(string text, int minLength)
        {
            HashSet<string> tokens = new HashSet<string>();

            string[] tempTokens = text.Split(Tokenizer.WordSeparators, StringSplitOptions.RemoveEmptyEntries);

            foreach (string token in tempTokens)
            {
                string t = token.ToLowerInvariant();

                if (t.Length >= minLength && t.Length <= MAX_TOKEN_LENGTH &&
                    !_stopWords.Contains(t) && StringIsAlphaNumeric(t))
                {

                    tokens.Add(t);
                }
            }

            return tokens;
        }

        /// <summary>
        /// Returns true if a string contains at least one letter and no other characters except digits, '-', and '''.
        /// </summary>
        /// <param name="str">The string to check.</param>
        /// <returns></returns>
        private static bool StringIsAlphaNumeric(string str)
        {
            bool retval = true;
            bool numericOnly = true;

            foreach (char c in str)
            {
                if (Char.IsLetter(c))
                {
                    numericOnly = false;
                }
                else if (!Char.IsDigit(c) && !Char.IsPunctuation(c))
                {
                    retval = false;
                    break;
                }
            }

            return retval && !numericOnly;
        }
    }
}
