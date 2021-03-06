﻿using LibIML;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace IML_Playground.ViewModel
{
    class ClassifierFeaturesViewModel : ViewModelBase
    {
        IClassifier _classifier;

        public ClassifierFeaturesViewModel(IClassifier classifier)
        {
            _classifier = classifier;

            FeaturesPositive = new ObservableCollection<Feature>();
            FeaturesNegative = new ObservableCollection<Feature>();
            LabelPositive = _classifier.Labels.ToList()[0];
            LabelNegative = _classifier.Labels.ToList()[1];

            UpdateFeatures(_classifier);
        }

        #region Properties

        public Label LabelPositive { get; private set; }
        public Label LabelNegative { get; private set; }
        public ObservableCollection<Feature> FeaturesPositive { get; private set; }
        public ObservableCollection<Feature> FeaturesNegative { get; private set; }

        #endregion

        public void UpdateFeatures()
        {
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateFeatures(_classifier);
                });
            }
            else
            {
                UpdateFeatures(_classifier);
            }
        }

        private void UpdateFeatures(IClassifier classifier)
        {

            FeaturesPositive.Clear();
            FeaturesNegative.Clear();

            foreach (Feature feature in classifier.FeaturesPerClass[classifier.PositiveLabel])
            {
                FeaturesPositive.Add(feature);
            }

            foreach (Feature feature in classifier.FeaturesPerClass[classifier.NegativeLabel])
            {
                FeaturesNegative.Add(feature);
            }

        }
    }
}
