using IML_Playground.Learning;
using IML_Playground.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IML_Playground
{
    class Program
    {
        public const string DataDir = "Datasets";

        public static void Main()
        {
            Stopwatch watch = new Stopwatch();

            // Build our vocabulary
            watch.Restart();

            NewsCollection trainAll = NewsCollection.CreateFromZip(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DataDir, "20news-bydate-train.zip"));
            Vocabulary vocab = trainAll.BuildVocabulary();
            trainAll.ComputeTFIDFVectors(vocab);
            List<Label> labels = new List<Label>();
            labels.Add(new Label("Baseball", "rec.sport.baseball"));
            labels.Add(new Label("Hockey", "rec.sport.hockey"));

            watch.Stop();
            TimeSpan tsVocab = watch.Elapsed;

            // Build a training set
            watch.Restart();

            NewsCollection trainHockeyBaseball = trainAll.Subset(labels.ToArray());
            trainHockeyBaseball.ComputeFeatureVectors(vocab);

            // Build a test set
            NewsCollection testHockeyBaseball = NewsCollection.CreateFromZip(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DataDir, "20news-bydate-test.zip"),
                labels.ToArray());
            testHockeyBaseball.TokenizeItems();
            testHockeyBaseball.ComputeTFIDFVectors(vocab, trainAll.Count); // Use the full training set size for computing TF-IDF weights (do we even need this?)
            testHockeyBaseball.ComputeFeatureVectors(vocab);

            watch.Stop();
            TimeSpan tsDatasets = watch.Elapsed;

            // Build a classifier and train it
            watch.Restart();
            MultinomialNaiveBayesClassifier classifier = new MultinomialNaiveBayesClassifier(labels, vocab);
            foreach (NewsItem item in trainHockeyBaseball)
            {
                classifier.AddInstance(item.Label, item.FeatureCounts);
            }
            watch.Stop();
            TimeSpan tsClassifier = watch.Elapsed;

            // Test our classifier
            int rightHockey = 0;
            int rightBaseball = 0;
            int wrongHockey = 0;
            int wrongBaseball = 0;
            int count = 0;
            foreach (NewsItem item in testHockeyBaseball)
            {
                Label prediction = classifier.PredictInstance(item.FeatureCounts);
                if (prediction != null)
                {
                    if (prediction == item.Label)
                    {
                        switch (prediction.UserLabel)
                        {
                            case "Hockey":
                                rightHockey++;
                                break;
                            case "Baseball":
                                rightBaseball++;
                                break;
                        }

                    }
                    else
                    {
                        switch (prediction.UserLabel)
                        {
                            case "Hockey":
                                wrongHockey++;
                                break;
                            case "Baseball":
                                wrongBaseball++;
                                break;
                        }
                    }
                    Console.WriteLine("Item {0}:\nIs about {1}, predicated as {2}.\n", item.Id, item.Label.UserLabel, prediction.UserLabel);
                }
                else
                {
                    Console.WriteLine("No prediction.");
                }
                count++;
                //if (count >= 2)
                //    break;
            }
            Console.WriteLine("Hockey true-positives: {0}\nBaseball true-positives: {1}\nHockey false-positives: {2}\nBaseball false-positives: {3}\nAccuracy: {4:0.0000}", rightHockey, rightBaseball, wrongHockey, wrongBaseball, (rightHockey + rightBaseball) / (double)(rightHockey + rightBaseball + wrongHockey + wrongBaseball));

            // Print some diagnostics
            Console.WriteLine("Vocabulary size: {0}", vocab.Count);
            Console.WriteLine("Training set size: {0}", trainHockeyBaseball.Count);
            Console.WriteLine("Elapsed time to build vocab: {0:00}:{1:00}:{2:00}.{3:00}", tsVocab.Hours, tsVocab.Minutes, tsVocab.Seconds, tsVocab.Milliseconds);
            Console.WriteLine("Elapsed time to build data sets: {0:00}:{1:00}:{2:00}.{3:00}", tsDatasets.Hours, tsDatasets.Minutes, tsDatasets.Seconds, tsDatasets.Milliseconds);
            Console.WriteLine("Elapsed time to train classifier: {0:00}:{1:00}:{2:00}.{3:00}", tsClassifier.Hours, tsClassifier.Minutes, tsClassifier.Seconds, tsClassifier.Milliseconds);
            Console.WriteLine("Memory usage: {0:.00} MB", GC.GetTotalMemory(true) / 1024.0 / 1024.0);
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }


    }
}
