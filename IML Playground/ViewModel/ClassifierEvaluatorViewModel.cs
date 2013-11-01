using GalaSoft.MvvmLight.Command;
using IML_Playground.Learning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IML_Playground.ViewModel
{
    class ClassifierEvaluatorViewModel : ViewModelBase
    {
        private Evaluator _evaluator;
        private IInstances _testSet;
        private Label _positiveLabel;
        private Label _negativeLabel;
        private int _truePositives;
        private int _trueNegatives;
        private int _falsePositives;
        private int _falseNegatives;
        private double _weightedF1;

        public ClassifierEvaluatorViewModel(Evaluator evaluator, IInstances instances)
        {
            _evaluator = evaluator;
            _testSet = instances;

            if (_evaluator.Classifier.Labels.Count >= 2)
            {
                PositiveLabel = _evaluator.Classifier.Labels[0];
                NegativeLabel = _evaluator.Classifier.Labels[1];
            }

            ClassifierViewModel = new ClassifierFeaturesViewModel(_evaluator.Classifier);
            AddTestSetFeatureCounts();

            Retrain = new RelayCommand(PerformRetrain);
            Resample = new RelayCommand(PerformResample, () => false);
            SaveModel = new RelayCommand(PerformSaveModel, () => (false));
            LoadModel = new RelayCommand(PerformLoadModel, () => (false));
            ExportModelAsArff = new RelayCommand(PerformExportModelAsArff);
            ExportTestSetAsArff = new RelayCommand(PerformExportTestSetAsArff);

            PerformRetrain(); // Ensure our values are up-to-date
        }

        #region Properties

        public ClassifierFeaturesViewModel ClassifierViewModel { get; private set; }

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

        public ICommand Retrain { get; private set; }
        public ICommand Resample { get; private set; }
        public ICommand SaveModel { get; private set; }
        public ICommand LoadModel { get; private set; }
        public ICommand ExportModelAsArff { get; private set; }
        public ICommand ExportTestSetAsArff { get; private set; }

        #endregion

        private void AddTestSetFeatureCounts()
        {
            foreach (Feature feature in ClassifierViewModel.FeaturesPositive)
            {
                foreach (Instance instance in _testSet.ToInstances())
                {
                    if (instance.Label == PositiveLabel)
                    {
                        int featureId = _evaluator.Classifier.Vocab.GetWordId(feature.Characters);
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
                        int featureId = _evaluator.Classifier.Vocab.GetWordId(feature.Characters);
                        double count;
                        instance.Features.TryGet(featureId, out count);
                        feature.CountTesting += (int)count;
                    }
                }
            }

        }

        private void PerformRetrain()
        {
            Console.WriteLine("PerformRetrain()");

            // Update priors
            foreach (Feature feature in ClassifierViewModel.FeaturesPositive)
            {
                int featureId = _evaluator.Classifier.Vocab.GetWordId(feature.Characters);
                _evaluator.Classifier.UpdatePrior(_evaluator.Classifier.Labels[0], featureId, feature.Weight);
            }
            foreach (Feature feature in ClassifierViewModel.FeaturesNegative)
            {
                int featureId = _evaluator.Classifier.Vocab.GetWordId(feature.Characters);
                _evaluator.Classifier.UpdatePrior(_evaluator.Classifier.Labels[1], featureId, feature.Weight);
            }

            // Retrain our model
            _evaluator.Classifier.Retrain();

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
        }

        private void PerformSaveModel()
        { }

        private void PerformLoadModel()
        { }

        private async void PerformExportModelAsArff()
        {
            // Display the Save File Dialog
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.DefaultExt = ".arff";
            dialog.Filter = "ARFF files|.arff";

            Nullable<bool> result = dialog.ShowDialog();

            if (result == true)
            {
                string filename = dialog.FileName;
                Console.WriteLine("Export ARFF file to {0}.", filename);
                await _evaluator.Classifier.SaveArffFile(filename); // Save the ARFF file
            }
        }

        private async void PerformExportTestSetAsArff()
        {
            // Display the Save File Dialog
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.DefaultExt = ".arff";
            dialog.Filter = "ARFF files|.arff";

            Nullable<bool> result = dialog.ShowDialog();

            if (result == true)
            {
                string filename = dialog.FileName;
                Console.WriteLine("Export ARFF file to {0}.", filename);
                await _testSet.SaveArffFile(filename, _evaluator.Classifier.Vocab, _evaluator.Classifier.Labels.ToArray()); // Save the ARFF file
            }
        }

        private void PerformResample()
        {

        }
    }
}
