using IML_Playground.Learning;
using IML_Playground.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace IML_Playground
{
    // TODO Add interface for extracting per-class feature counts and weights from a classifier
    // TODO Add interface for updating per-class feature weights in a classifier
    // TODO Build UI to display feature counts for each class
    // TODO UI should display classifier's F1 score
    // TODO Allow user to update feature weights from UI

    class Program
    {
        public const string DataDir = "Datasets";

        [STAThread]
        public static void Main()
        {
            Application app = new Application();
            MainWindow window = new MainWindow();
            window.Show();
            app.Run(window);
            //TestSerializedModel();
            //Test20Newsgroups();
            //TestSimple();
        }

        private static void TestSerializedModel()
        {
            IFormatter formatter = new BinaryFormatter();
            MultinomialNaiveBayesClassifier classifier;
            NewsCollection testSet;
            using (FileStream s = File.OpenRead("model.bin"))
                classifier = (MultinomialNaiveBayesClassifier)formatter.Deserialize(s);
            using (FileStream s = File.OpenRead("testSet.bin"))
                testSet = (NewsCollection)formatter.Deserialize(s);
            
        }

        private static void Test20Newsgroups(int trainingSize = Int32.MaxValue)
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

            NewsCollection trainHockeyBaseball = trainAll.Subset(trainingSize, labels.ToArray());
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
            List<Instance> instances = new List<Instance>();
            foreach (NewsItem item in trainHockeyBaseball)
            {
                Instance instance = new Instance { Label = item.Label, Features = item.FeatureCounts };
                instances.Add(instance);
            }
            classifier.AddInstances(instances);
            watch.Stop();
            TimeSpan tsClassifier = watch.Elapsed;

            // Test our classifier
            watch.Restart();
            Evaluator eval = new Evaluator { Classifier = classifier };
            IEnumerable<Instance> testInstances = testHockeyBaseball.ToInstances();
            eval.EvaluateOnTestSet(testInstances);
            Console.WriteLine("Weighted F1: {0:0.000}", eval.WeightedF1);

            watch.Stop();
            TimeSpan tsTest = watch.Elapsed;

            Console.WriteLine("Saving ARFF files...");
            trainHockeyBaseball.SaveArffFile("trainingSet.arff", vocab, labels.ToArray());
            testHockeyBaseball.SaveArffFile("testingSet.arff", vocab, labels.ToArray());

            Console.WriteLine("Serializing data objects...");
            IFormatter formatter = new BinaryFormatter();
            using (FileStream s = File.Create("trainingSet.bin"))
                formatter.Serialize(s, trainHockeyBaseball);
            using (FileStream s = File.Create("testSet.bin"))
                formatter.Serialize(s, testHockeyBaseball);
            using (FileStream s = File.Create("vocabulary.bin"))
                formatter.Serialize(s, vocab);
            using (FileStream s = File.Create("labels.bin"))
                formatter.Serialize(s, labels);
            using (FileStream s = File.Create("model.bin"))
                formatter.Serialize(s, classifier);
            

            // Print some diagnostics
            Console.WriteLine("Vocabulary size: {0}", vocab.Count);
            Console.WriteLine("Training set size: {0}", trainHockeyBaseball.Count);
            Console.WriteLine("Elapsed time to build vocab: {0:00}:{1:00}:{2:00}.{3:00}", tsVocab.Hours, tsVocab.Minutes, tsVocab.Seconds, tsVocab.Milliseconds);
            Console.WriteLine("Elapsed time to build data sets: {0:00}:{1:00}:{2:00}.{3:00}", tsDatasets.Hours, tsDatasets.Minutes, tsDatasets.Seconds, tsDatasets.Milliseconds);
            Console.WriteLine("Elapsed time to train classifier: {0:00}:{1:00}:{2:00}.{3:00}", tsClassifier.Hours, tsClassifier.Minutes, tsClassifier.Seconds, tsClassifier.Milliseconds);
            Console.WriteLine("Elapsed time to test classifier: {0:00}:{1:00}:{2:00}.{3:00}", tsTest.Hours, tsTest.Minutes, tsTest.Seconds, tsTest.Milliseconds);
            Console.WriteLine("Memory usage: {0:.00} MB", GC.GetTotalMemory(true) / 1024.0 / 1024.0);
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void TestSimple()
        {
            Stopwatch watch = new Stopwatch();

            // Build our vocabulary
            watch.Restart();

            NewsCollection trainAll = NewsCollection.CreateFromZip(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DataDir, "simple-train.zip"));
            foreach (NewsItem item in trainAll)
            {
                Console.WriteLine("Item {0}: {1}", item.Id, item.AllText);
            }
            Console.WriteLine();
            Vocabulary vocab = trainAll.BuildVocabulary();
            Console.WriteLine("Vocab: {0}\n", vocab);

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
            NewsCollection testHockeyBaseball = NewsCollection.CreateFromZip(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DataDir, "simple-test.zip"), labels.ToArray());
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
                Instance instance = new Instance { Label = item.Label, Features = item.FeatureCounts };
                Console.Write("Adding instance of {0}: ", item.Label.UserLabel);
                foreach (KeyValuePair<int, double> pair in item.FeatureCounts.Data)
                {
                    Console.Write("{0}={1:0.0} ", vocab.GetWord(pair.Key), pair.Value);
                }
                Console.WriteLine();
                classifier.AddInstance(instance);
            }
            Console.WriteLine();
            watch.Stop();
            TimeSpan tsClassifier = watch.Elapsed;

            // Test our classifier
            watch.Restart();
            Evaluator eval = new Evaluator { Classifier = classifier };
            IEnumerable<Instance> testInstances = testHockeyBaseball.ToInstances();
            eval.EvaluateOnTestSet(testInstances);

            Console.WriteLine("Saving ARFF file...");
            trainHockeyBaseball.SaveArffFile("test.arff", vocab, labels.ToArray());

            Console.WriteLine();
            //Console.WriteLine("Hockey true-positives: {0}\nBaseball true-positives: {1}\nHockey false-positives: {2}\nBaseball false-positives: {3}\nAccuracy: {4:0.0000}", rightHockey, rightBaseball, wrongHockey, wrongBaseball, (rightHockey + rightBaseball) / (double)(rightHockey + rightBaseball + wrongHockey + wrongBaseball));
            watch.Stop();
            TimeSpan tsTest = watch.Elapsed;

            // Print some diagnostics
            Console.WriteLine("Vocabulary size: {0}", vocab.Count);
            Console.WriteLine("Training set size: {0}", trainHockeyBaseball.Count);
            Console.WriteLine("Elapsed time to build vocab: {0:00}:{1:00}:{2:00}.{3:00}", tsVocab.Hours, tsVocab.Minutes, tsVocab.Seconds, tsVocab.Milliseconds);
            Console.WriteLine("Elapsed time to build data sets: {0:00}:{1:00}:{2:00}.{3:00}", tsDatasets.Hours, tsDatasets.Minutes, tsDatasets.Seconds, tsDatasets.Milliseconds);
//            Console.WriteLine("Elapsed time to train classifier: {0:00}:{1:00}:{2:00}.{3:00}", tsClassifier.Hours, tsClassifier.Minutes, tsClassifier.Seconds, tsClassifier.Milliseconds);
//            Console.WriteLine("Elapsed time to test classifier: {0:00}:{1:00}:{2:00}.{3:00}", tsTest.Hours, tsTest.Minutes, tsTest.Seconds, tsTest.Milliseconds);
            Console.WriteLine("Memory usage: {0:.00} MB", GC.GetTotalMemory(true) / 1024.0 / 1024.0);
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();

        }

    }
}
