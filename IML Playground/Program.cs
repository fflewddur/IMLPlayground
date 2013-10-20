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

            watch.Stop();
            TimeSpan tsVocab = watch.Elapsed;

            // Build a training set
            watch.Restart();
            
            NewsCollection trainHockeyBaseball = trainAll.Subset("rec.sports.baseball", "rec.sports.hockey");
            trainHockeyBaseball.ComputeFeatureVectors(vocab);

            // Build a test set
            NewsCollection testHockeyBaseball = NewsCollection.CreateFromZip(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DataDir, "20news-bydate-test.zip"),
"rec.sports.baseball", "rec.sports.hockey");
            testHockeyBaseball.TokenizeItems();
            testHockeyBaseball.ComputeTFIDFVectors(vocab, trainAll.Count); // Use the full training set size for computing TF-IDF weights (do we even need this?)
            testHockeyBaseball.ComputeFeatureVectors(vocab);

            watch.Stop();
            TimeSpan tsDatasets = watch.Elapsed;

            // Build a classifier and train it
            watch.Restart();
            watch.Stop();
            TimeSpan tsClassifier = watch.Elapsed;

            // Print some diagnostics
            Console.WriteLine("Vocabulary size: {0}", vocab.Count);
            Console.WriteLine("Elapsed time to build vocab: {0:00}:{1:00}:{2:00}.{3:00}", tsVocab.Hours, tsVocab.Minutes, tsVocab.Seconds, tsVocab.Milliseconds);
            Console.WriteLine("Elapsed time to build data sets: {0:00}:{1:00}:{2:00}.{3:00}", tsDatasets.Hours, tsDatasets.Minutes, tsDatasets.Seconds, tsDatasets.Milliseconds);
            Console.WriteLine("Elapsed time to train classifier: {0:00}:{1:00}:{2:00}.{3:00}", tsClassifier.Hours, tsClassifier.Minutes, tsClassifier.Seconds, tsClassifier.Milliseconds);
            Console.WriteLine("Memory usage: {0:.00} MB", GC.GetTotalMemory(true) / 1024.0 / 1024.0);
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }


    }
}
