using LibIML;
using MessagePredictor.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Offline_tests
{
    class Program
    {
        internal class Evaluation
        {
            public int TrainingSize;
            public int VocabSizeRequested;
            public int VocabSizeActual;
            public double F1Positive;
            public double F1Negative;
            public double F1Weighted;
            public double F1WeightedValidation;
        }

        public Program()
        {
        }

        /// <summary>
        /// Load the dataset from the specified filename.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="labels"></param>
        /// <returns></returns>
        internal NewsCollection LoadDataset(string filename, IEnumerable<Label> labels)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, MessagePredictor.App.DataDir, filename);

            NewsCollection messages = NewsCollection.CreateFromZip(path, labels);

            return messages;
        }

        /// <summary>
        /// Reset all of the items in 'messages' to be unlabeled, then randomly label 'nToLabelTotal' of them.
        /// </summary>
        /// <param name="instances"></param>
        /// <param name="nToLabelTotal"></param>
        internal void LabelDataset(IEnumerable<IInstance> instances, IEnumerable<Label> labels, int nToLabelTotal, Random rand)
        {
            Dictionary<Label, int> labelCounts = new Dictionary<Label, int>();
            int nInstances = instances.Count();

            foreach (Label label in labels) {
                labelCounts[label] = 0;
            }

            foreach (NewsItem item in instances) {
                item.UserLabel = null;
                labelCounts[item.GroundTruthLabel] += 1;
            }

            foreach (Label label in labels) {
                int nToLabel = (int)Math.Round((labelCounts[label] / (double)nInstances) * nToLabelTotal);
                int nLabeled = 0;
                while (nLabeled < nToLabel) {
                    int index;
                    lock (rand) {
                        index = rand.Next(0, nInstances);
                    }
                    IInstance instance = instances.ElementAt(index);
                    if (instance.GroundTruthLabel == label) {
                        instance.UserLabel = instance.GroundTruthLabel;
                        nLabeled++;
                    }
                }
            }
        }

        /// <summary>
        /// Perform a traditional evaluation of the classifier by computing its weighted F1 score.
        /// </summary>
        /// <param name="labels"></param>
        /// <param name="classifier"></param>
        /// <param name="testSet"></param>
        /// <returns></returns>
        internal Evaluation EvalClassifier(IEnumerable<Label> labels, IClassifier classifier, IEnumerable<IInstance> testSet)
        {
            int truePositives = 0;
            int trueNegatives = 0;
            int falsePositives = 0;
            int falseNegatives = 0;
            int positiveGroundTruthCount, negativeGroundTruthCount;
            double precisionPositive, precisionNegative;
            double recallPositive, recallNegative;
            Label positiveLabel = labels.ElementAt(0);
            Label negativeLabel = labels.ElementAt(1);

            foreach (IInstance instance in testSet) {
                if (instance.GroundTruthLabel == positiveLabel) {
                    if (instance.Prediction.Label == positiveLabel) {
                        truePositives++;
                    } else if (instance.Prediction.Label == negativeLabel) {
                        falseNegatives++;
                    } else {
                        Debug.Assert(false, "An evaluated instance has a predicted label that is neither positive nor negative.");
                    }

                } else if (instance.GroundTruthLabel == negativeLabel) {
                    if (instance.Prediction.Label == positiveLabel) {
                        falsePositives++;
                    } else if (instance.Prediction.Label == negativeLabel) {
                        trueNegatives++;
                    } else {
                        Debug.Assert(false, "An evaluated instance has a predicted label that is neither positive nor negative.");
                    }
                } else {
                    Debug.Assert(false, "An evaluated instance has a ground-truth label that is neither positive nor negative.");
                }
            }

            positiveGroundTruthCount = truePositives + falseNegatives;
            negativeGroundTruthCount = trueNegatives + falsePositives;
            precisionPositive = truePositives / (double)(truePositives + falsePositives);
            recallPositive = truePositives / (double)(truePositives + falseNegatives);
            precisionNegative = trueNegatives / (double)(trueNegatives + falseNegatives);
            recallNegative = trueNegatives / (double)(trueNegatives + falsePositives);

            Evaluation eval = new Evaluation();
            eval.F1Positive = (2.0 * precisionPositive * recallPositive) / (precisionPositive + recallPositive);
            eval.F1Negative = (2.0 * precisionNegative * recallNegative) / (precisionNegative + recallNegative);
            eval.F1Weighted = (eval.F1Positive * positiveGroundTruthCount / (positiveGroundTruthCount + negativeGroundTruthCount)) +
                              (eval.F1Negative * negativeGroundTruthCount / (positiveGroundTruthCount + negativeGroundTruthCount));
            return eval;
        }

        internal void UpdateInstanceFeatures(IEnumerable<IInstance> instances, Vocabulary vocab)
        {
            foreach (IInstance instance in instances) {
                Dictionary<string, int> tokens = Tokenizer.Tokenize(instance.AllText) as Dictionary<string, int>;
                instance.TokenCounts = tokens;
                instance.ComputeFeatureVector(vocab, true);
            }
        }

        /// <summary>
        /// Build the specified number of classifiers on random samples (of size 'trainingSize') on 'collection'. 
        /// Return an Evaluation object that averages the results of 'nFolds' evaluations.
        /// </summary>
        /// <param name="labels"></param>
        /// <param name="collection"></param>
        /// <param name="trainingSize"></param>
        /// <param name="vocabSize"></param>
        /// <param name="nFolds"></param>
        internal Evaluation BuildAndEvalClassifier(IEnumerable<Label> labels, NewsCollection collection, NewsCollection testCollection, 
            int trainingSize, int vocabSize, int nFolds, int nThreads)
        {
            Evaluation evalSum = new Evaluation();
            ParallelOptions options = new ParallelOptions();
            options.MaxDegreeOfParallelism = nThreads;
            Random rand = new Random();
            int count = 0, countValidation = 0;

            Parallel.For(0, nFolds, options, i => {
                NewsCollection localCollection = NewsCollection.CreateFromExisting(collection);
                NewsCollection localTestCollection = NewsCollection.CreateFromExisting(testCollection);
                this.LabelDataset(localCollection, labels, trainingSize, rand);
                Vocabulary v = Vocabulary.CreateVocabulary(FilterToTrainingSet(localCollection, labels), labels, 
                    Vocabulary.Restriction.HighIG, vocabSize, false);
                UpdateInstanceFeatures(localCollection, v);
                UpdateInstanceFeatures(localTestCollection, v);
                MultinomialNaiveBayesFeedbackClassifier classifier = new MultinomialNaiveBayesFeedbackClassifier(labels, v);
                classifier.AddInstances(FilterToTrainingSet(localCollection, labels));
                classifier.Train();
                foreach (IInstance instance in localCollection) {
                    instance.Prediction = classifier.PredictInstance(instance);
                }
                foreach (IInstance instance in localTestCollection) {
                    instance.Prediction = classifier.PredictInstance(instance);
                }
                Evaluation eval = EvalClassifier(labels, classifier, FilterToTestSet(localCollection, labels));
                Evaluation evalValidation = EvalClassifier(labels, classifier, localTestCollection);
                lock (evalSum) {
                    if (double.IsNaN(eval.F1Weighted)) {
                        Console.Error.WriteLine("*** Error: F1 is NaN for trainingSize={0}, vocabSize={1}, i={2} ***", trainingSize, vocabSize, i);
                    } else {
                        evalSum.F1Weighted += eval.F1Weighted;
                        evalSum.VocabSizeActual += v.Count;
                        count++;
                    }
                    if (double.IsNaN(evalValidation.F1Weighted)) {
                        Console.Error.WriteLine("*** Error: Validation F1 is NaN for trainingSize={0}, vocabSize={1}, i={2} ***", trainingSize, vocabSize, i);
                    } else {
                        evalSum.F1WeightedValidation += evalValidation.F1Weighted;
                        countValidation++;
                    }
                }
                
                Console.WriteLine("F1 = {0:F3} for iteration {1}", eval.F1Weighted, i);
            });

            Evaluation evalAvg = new Evaluation();
            evalAvg.TrainingSize = trainingSize;
            evalAvg.VocabSizeRequested = vocabSize;
            evalAvg.VocabSizeActual = evalSum.VocabSizeActual / count;
            evalAvg.F1Weighted = evalSum.F1Weighted / count;
            evalAvg.F1WeightedValidation = evalSum.F1WeightedValidation / countValidation;

            return evalAvg;
        }

        private IEnumerable<IInstance> FilterToTrainingSet(IEnumerable<IInstance> fullSet, IEnumerable<Label> labels)
        {
            Debug.Assert(fullSet != null);

            return fullSet.Where(item => labels.Contains(item.UserLabel));
        }

        private IEnumerable<IInstance> FilterToTestSet(IEnumerable<IInstance> fullSet, IEnumerable<Label> labels)
        {
            Debug.Assert(fullSet != null);

            return fullSet.Where(item => !labels.Contains(item.UserLabel));
        }

        static void Main(string[] args)
        {
            Stopwatch watch = new Stopwatch();
            // Limit the number of threads to 4; more than that and we start hitting .NET's 2GB memory limit
            int nThreads = (Environment.ProcessorCount > 4) ? 4 : Environment.ProcessorCount;
            int nFolds = 100;

            List<Label> labels = new List<Label>();
            labels.Add(new Label("Hockey", "rec.sport.hockey", null, null));
            labels.Add(new Label("Baseball", "rec.sport.baseball", null, null));

            Program prog = new Program();
            NewsCollection messages = prog.LoadDataset("20news-sports-bydate-train.zip", labels);
            NewsCollection testMessages = prog.LoadDataset("20news-sports-bydate-test.zip", labels);
            List<Evaluation> evaluations = new List<Evaluation>();

            watch.Start();
            // Loop for different training set sizes
            for (int i = 10; i <= 1500; i *= 2) {
                // Loop for different vocabulary sizes
                for (int j = 10; j <= 20480; j *= 2) {
                    Console.WriteLine("=== Running evaluations for training set size = {0} and vocab size = {1} ===", i, j);
                    Evaluation eval = prog.BuildAndEvalClassifier(labels, messages, testMessages, i, j, nFolds, nThreads);
                    evaluations.Add(eval);
                    Console.WriteLine("Average Weighted F1: {0:F3} on test, {1:F3} on validation ({2} features used)", 
                        eval.F1Weighted, eval.F1WeightedValidation, eval.VocabSizeActual);
                }
            }
            watch.Stop();

            Console.WriteLine("============= RESULTS =============");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"evaluation.csv")) {
                file.WriteLine("TrainingSize, VocabSizeRequested, VocabSizeActual, F1Weighted, F1WeightedValidation");
                foreach (Evaluation eval in evaluations.OrderBy(x => x.TrainingSize).ThenBy(x => x.VocabSizeRequested)) {
                    Console.WriteLine("\nTraining set size: {0}\nVocab size (requested): {1}\nVocab size (actual): {2}, F1Weighted Average: {3:F3}, F1Weighted Validation Average: {4:F3}", 
                        eval.TrainingSize, eval.VocabSizeRequested, eval.VocabSizeActual, eval.F1Weighted, eval.F1WeightedValidation);
                    file.WriteLine("{0}, {1}, {2}, {3}, {4}", 
                        eval.TrainingSize, eval.VocabSizeRequested, eval.VocabSizeActual, eval.F1Weighted, eval.F1WeightedValidation);
                }
            }
            Console.WriteLine("\nEvaluation took {0}", watch.Elapsed.ToString());
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
