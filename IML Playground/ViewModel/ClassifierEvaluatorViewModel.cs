using GalaSoft.MvvmLight.Command;
using IML_Playground.Learning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IML_Playground.ViewModel
{
    class ClassifierEvaluatorViewModel : ViewModelBase
    {
        private IEnumerable<IClassifier> _classifiers;
        private IClassifier _currentClassifier;
        private IEnumerable<Label> _labels;
        private Evaluator _evaluator;
        private Vocabulary _fullVocab; // The complete vocabulary (allows us to build vocabulary subsets at will).
        private IInstances _testSet;
        private IInstances _trainSet;
        private IInstances _fullTrainSet; // The complete training set (allows us to resample smaller training sets at will).
        private ClassifierFeaturesViewModel _classifierViewModel;
        private Label _positiveLabel;
        private Label _negativeLabel;
        private int _truePositives;
        private int _trueNegatives;
        private int _falsePositives;
        private int _falseNegatives;
        private double _weightedF1;
        private int _vocabSize;
        private int _resampleSize;
        private string _statusMessage;
        private SerializableModel _serializableModel; // Used to serialize our classifier, test set, and complete training set.

        public ClassifierEvaluatorViewModel(List<IClassifier> classifiers, IEnumerable<Label> labels, Vocabulary fullVocab, IInstances trainSet, IInstances testSet, IInstances fullTrainSet)
        {
            _classifiers = classifiers;
            _labels = labels;
            //_evaluator = new Evaluator() { Classifier = CurrentClassifier };
            _trainSet = trainSet;
            _testSet = testSet;
            _fullVocab = fullVocab;
            _fullTrainSet = fullTrainSet;

            _serializableModel = new SerializableModel();
            _serializableModel.Classifiers = classifiers;
            _serializableModel.FullTrainingSet = _fullTrainSet;
            _serializableModel.TestSet = _testSet;

            VocabSize = _fullVocab.Count;
            CurrentClassifier = AvailableClassifiers.First(); // Take the first available classifier
            
            Retrain = new RelayCommand(PerformRetrain);
            Resample = new RelayCommand(PerformResample);
            ResizeVocab = new RelayCommand(PerformResizeVocab);
            SaveModel = new RelayCommand(PerformSaveModel);
            LoadModel = new RelayCommand(PerformLoadModel);
            ExportModelAsArff = new RelayCommand(PerformExportModelAsArff);
            ExportTestSetAsArff = new RelayCommand(PerformExportTestSetAsArff);

            //Initialize();
        }

        private void Initialize()
        {
            Console.WriteLine("Initialize()");
            if (CurrentClassifier.Labels.Count() >= 2)
            {
                PositiveLabel = CurrentClassifier.PositiveLabel;
                NegativeLabel = CurrentClassifier.NegativeLabel;
            }

            CurrentClassifier.ClearInstances(); // Ensure the classifier has no training data
            CurrentClassifier.AddInstances(_trainSet.ToInstances()); // Add our current training set
            UpdateVocab();
            UpdateFeatureVectors();
            VocabSize = CurrentClassifier.Vocab.Count;
            ClassifierViewModel = new ClassifierFeaturesViewModel(CurrentClassifier);
            AddTestSetFeatureCounts();

            PerformRetrain(); // Ensure our values are up-to-date
        }

        #region Properties

        public ClassifierFeaturesViewModel ClassifierViewModel
        {
            get { return _classifierViewModel; }
            private set { SetProperty<ClassifierFeaturesViewModel>(ref _classifierViewModel, value); }
        }

        public Label PositiveLabel
        {
            get { return _positiveLabel; }
            private set { SetProperty<Label>(ref _positiveLabel, value); }
        }

        public Label NegativeLabel
        {
            get { return _negativeLabel; }
            private set { SetProperty<Label>(ref _negativeLabel, value); }
        }

        public int TruePositives
        {
            get { return _truePositives; }
            private set { SetProperty<int>(ref _truePositives, value); }
        }

        public int TrueNegatives
        {
            get { return _trueNegatives; }
            private set { SetProperty<int>(ref _trueNegatives, value); }
        }

        public int FalsePositives
        {
            get { return _falsePositives; }
            private set { SetProperty<int>(ref _falsePositives, value); }
        }

        public int FalseNegatives
        {
            get { return _falseNegatives; }
            private set { SetProperty<int>(ref _falseNegatives, value); }
        }

        public double WeightedF1
        {
            get { return _weightedF1; }
            private set { SetProperty<double>(ref _weightedF1, value); }
        }

        public int VocabSize
        {
            get { return _vocabSize; }
            private set { SetProperty<int>(ref _vocabSize, value); }
        }

        public int FullVocabSize
        {
            get { return _fullVocab.Count; }
        }

        public int FullTrainSize
        {
            get { return _fullTrainSet.Count; }
        }

        public int ResampleSize
        {
            get { return _resampleSize; }
            set { SetProperty<int>(ref _resampleSize, value); }
        }

        public string StatusMessage
        {
            get { return _statusMessage; }
            private set { SetProperty<string>(ref _statusMessage, value); }
        }

        public IEnumerable<IClassifier> AvailableClassifiers
        {
            get { return _classifiers; }
            private set { SetProperty<IEnumerable<IClassifier>>(ref _classifiers, value); }
        }

        public IClassifier CurrentClassifier
        {
            get { return _currentClassifier; }
            set 
            { 
                if (SetProperty<IClassifier>(ref _currentClassifier, value))
                {
                    _evaluator = new Evaluator() { Classifier = _currentClassifier };
                    Initialize();
                }
            }
        }

        public ICommand Retrain { get; private set; }
        public ICommand Resample { get; private set; }
        public ICommand ResizeVocab { get; private set; }
        public ICommand SaveModel { get; private set; }
        public ICommand LoadModel { get; private set; }
        public ICommand ExportModelAsArff { get; private set; }
        public ICommand ExportTestSetAsArff { get; private set; }

        #endregion

        private void AddTestSetFeatureCounts()
        {
            StatusMessage = "Updating feature counts...";

            foreach (Feature feature in ClassifierViewModel.FeaturesPositive)
            {
                foreach (Instance instance in _testSet.ToInstances())
                {
                    if (instance.Label == PositiveLabel)
                    {
                        int featureId = CurrentClassifier.Vocab.GetWordId(feature.Characters);
                        double count;
                        instance.Features.TryGet(featureId, out count);
                        feature.CountTesting += (int)count;
                    }
                }
            }
            foreach (Feature feature in ClassifierViewModel.FeaturesNegative)
            {
                foreach (Instance instance in _testSet.ToInstances())
                {
                    if (instance.Label == NegativeLabel)
                    {
                        int featureId = CurrentClassifier.Vocab.GetWordId(feature.Characters);
                        double count;
                        instance.Features.TryGet(featureId, out count);
                        feature.CountTesting += (int)count;
                    }
                }
            }

            StatusMessage = "";
        }

        private void PerformRetrain()
        {
            Console.WriteLine("PerformRetrain()");
            StatusMessage = "Retraining...";

            // Update priors
            foreach (Feature feature in ClassifierViewModel.FeaturesPositive)
            {
                int featureId = CurrentClassifier.Vocab.GetWordId(feature.Characters);
                CurrentClassifier.UpdatePrior(CurrentClassifier.PositiveLabel, featureId, feature.Weight);
            }
            foreach (Feature feature in ClassifierViewModel.FeaturesNegative)
            {
                int featureId = CurrentClassifier.Vocab.GetWordId(feature.Characters);
                CurrentClassifier.UpdatePrior(CurrentClassifier.NegativeLabel, featureId, feature.Weight);
            }

            // Retrain our model
            CurrentClassifier.Train();

            // Evaluate new classifier
            _evaluator.EvaluateOnTestSet(_testSet.ToInstances());
            WeightedF1 = _evaluator.WeightedF1;
            int[,] cm = _evaluator.ConfusionMatrix;
            if (cm.GetLength(0) == 2 && cm.GetLength(1) == 2)
            {
                TruePositives = cm[0, 0];
                FalsePositives = cm[0, 1];
                FalseNegatives = cm[1, 0];
                TrueNegatives = cm[1, 1];
            }

            StatusMessage = "";
        }

        private async void PerformSaveModel()
        {
            // Display the Save File Dialog
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.DefaultExt = ".model";
            dialog.Filter = "MODEL files (.model)|*.model";
            dialog.FileName = "model";
            dialog.Title = "Save model as";

            Nullable<bool> result = dialog.ShowDialog();

            if (result == true)
            {
                string filename = dialog.FileName;
                StatusMessage = "Saving model...";
                await SerializeModelAsync(filename);
                StatusMessage = "";
            }
        }

        private async void PerformLoadModel()
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".model";
            dialog.Filter = "MODEL files (.model)|*.model";
            dialog.Title = "Load model";

            Nullable<bool> result = dialog.ShowDialog();
            if (result == true)
            {
                string filename = dialog.FileName;
                StatusMessage = "Loading model...";
                await DeserializeModelAsync(filename);
                StatusMessage = "";
            }
        }

        private async void PerformExportModelAsArff()
        {
            // Display the Save File Dialog
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.DefaultExt = ".arff";
            dialog.Filter = "ARFF files (.arff)|*.arff";
            dialog.FileName = "model";
            dialog.Title = "Export model as";

            Nullable<bool> result = dialog.ShowDialog();

            if (result == true)
            {
                string filename = dialog.FileName;
                StatusMessage = "Exporting model...";
                await _evaluator.Classifier.SaveArffFile(filename); // Save the ARFF file
                StatusMessage = "";
            }
        }

        private async void PerformExportTestSetAsArff()
        {
            // Display the Save File Dialog
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.DefaultExt = ".arff";
            dialog.Filter = "ARFF files (.arff)|*.arff";
            dialog.FileName = "testSet";
            dialog.Title = "Export test set as";

            Nullable<bool> result = dialog.ShowDialog();

            if (result == true)
            {
                string filename = dialog.FileName;
                StatusMessage = "Exporting test set...";
                await _testSet.SaveArffFile(filename, CurrentClassifier.Vocab, CurrentClassifier.Labels.ToArray()); // Save the ARFF file
                StatusMessage = "";
            }
        }

        private void UpdateVocab()
        {
            Console.WriteLine("UpdateVocab()");
            Vocabulary newVocab = _fullVocab.GetSubset(_trainSet);
            _trainSet.ComputeFeatureVectors(newVocab);
            _testSet.ComputeFeatureVectors(newVocab);
            IEnumerable<int> highIGFeatures = _trainSet.GetHighIGFeatures(newVocab, _labels, VocabSize);
            newVocab.RestrictToSubset(highIGFeatures);
            //Vocabulary newVocab = _fullVocab.GetSubset(highIGFeatures);
            //newVocab.RestrictToInstances(_trainSet);
            CurrentClassifier.Vocab = newVocab;
            CurrentClassifier.AddInstances(_trainSet.ToInstances());
        }

        private void UpdateFeatureVectors()
        {
            //Console.WriteLine("UpdateFeatureVectors()");
            // FIXME
            //_trainSet.ComputeFeatureVectors(CurrentClassifier.Vocab);
            //_testSet.ComputeFeatureVectors(CurrentClassifier.Vocab);

            //vocab.RestrictToInstances(trainHockeyBaseballSmall);
            //trainHockeyBaseball.ComputeFeatureVectors(fullVocab);
            //IEnumerable<int> highIGFeatures = trainHockeyBaseballSmall.GetHighIGFeatures(fullVocab, labels, vocabSize);
            //fullVocab.RestrictToSubset(highIGFeatures);
        }

        private async void PerformResample()
        {
            Console.WriteLine("PerformResample()");
            StatusMessage = "Resampling...";
            await Task.Run(() =>
                {
                    _trainSet = _fullTrainSet.ItemsSubset(ResampleSize, CurrentClassifier.Labels.ToArray());
                    //IEnumerable<Instance> instances = _fullTrainSet.Subset(ResampleSize, CurrentClassifier.Labels.ToArray()); // FIXME This needs to be stored in _trainSet
                    UpdateVocab(); // New training set means our vocab needs to be updated too.
                    //UpdateFeatureVectors();
                    //CurrentClassifier.ClearInstances(); // Remove the existing training set
                    //CurrentClassifier.AddInstances(_trainSet.ToInstances()); // Add the new training set

                    PerformRetrain(); // Evaluate the new model
                });
            // Update our classifier viewmodel
            ClassifierViewModel.UpdateFeatures();
            AddTestSetFeatureCounts();
            StatusMessage = "";
        }

        private async void PerformResizeVocab()
        {
            Console.WriteLine("PerformResizeVocab()");
            StatusMessage = "Resizing vocabulary...";
            await Task.Run(() =>
                {
                    UpdateVocab();
                    PerformRetrain();
                });
            // Update our classifier viewmodel
            ClassifierViewModel.UpdateFeatures();
            AddTestSetFeatureCounts();
            StatusMessage = "";
        }

        private Task SerializeModelAsync(string filename)
        {
            return Task.Run(() =>
                {
                    IFormatter formatter = new BinaryFormatter();
                    using (FileStream s = File.Create(filename))
                        formatter.Serialize(s, _serializableModel);
                });
        }

        private Task DeserializeModelAsync(string filename)
        {
            return Task.Run(() =>
                {
                    IFormatter formatter = new BinaryFormatter();
                    using (FileStream s = File.OpenRead(filename))
                    {
                        _serializableModel = (SerializableModel)formatter.Deserialize(s);
                        AvailableClassifiers = _serializableModel.Classifiers;
                        _fullTrainSet = _serializableModel.FullTrainingSet;
                        _testSet = _serializableModel.TestSet;

                        Initialize();
                    }
                });
        }
    }
}
