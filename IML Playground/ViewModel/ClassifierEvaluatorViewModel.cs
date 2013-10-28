﻿using GalaSoft.MvvmLight.Command;
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
        private string _positiveLabel;
        private string _negativeLabel;
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
                PositiveLabel = _evaluator.Classifier.Labels[0].UserLabel;
                NegativeLabel = _evaluator.Classifier.Labels[1].UserLabel;
            }

            ClassifierViewModel = new ClassifierFeaturesViewModel(_evaluator.Classifier);
            
            Retrain = new RelayCommand(PerformRetrain);
            PerformRetrain(); // Ensure our values are up-to-date
        }

        public ClassifierFeaturesViewModel ClassifierViewModel { get; private set; }

        public string PositiveLabel
        {
            get { return _positiveLabel; }
            private set { SetProperty<string>(ref _positiveLabel, value); }
        }

        public string NegativeLabel
        {
            get { return _negativeLabel; }
            private set { SetProperty<string>(ref _negativeLabel, value); }
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

        private void PerformRetrain()
        {
            Console.WriteLine("PerformRetrain()");
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
    }
}
