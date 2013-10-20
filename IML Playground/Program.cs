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
            watch.Start();
            NewsCollection train = NewsCollection.CreateFromZip(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DataDir, "20news-bydate-train.zip"));
            Vocabulary vocab = train.BuildVocabulary();
            train.ComputeTFIDFVectors(vocab);
            watch.Stop();
            TimeSpan ts = watch.Elapsed;
            Console.WriteLine("Vocabulary size: {0}", vocab.Count);
            Console.WriteLine("Elapsed time: {0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
            Console.WriteLine("Memory usage: {0:.00} MB", GC.GetTotalMemory(true) / 1024.0 / 1024.0);
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }


    }
}
