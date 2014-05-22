using GalaSoft.MvvmLight.Command;
using LibIML;
using LibIML.Features;
using MessagePredictor.Model;
using MessagePredictor.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;
using System.Xml;

namespace MessagePredictor.ViewModel
{
    public class FeatureSetViewModel : ViewModelBase
    {
        public static readonly double PERCENT_HEIGHT_OF_MAX_BAR = 0.55;

        private IClassifier _classifier;
        private Vocabulary _vocab;
        private IReadOnlyList<Label> _labels;
        private List<Feature> _userAdded;
        private List<Feature> _userRemoved;
        private ObservableCollection<Feature> _featureSet;
        private LinkedList<UserAction> _userActions;
        private CollectionViewSource _collectionViewSourceGraph;
        private string _featureText; // The feature the user is currently typing in
        private string _selectedText; // If the user has highlighted text in a message, use it as the default for a new feature
        private Feature _selectedFeature; // The highlighted feature in the UI
        private DispatcherTimer _featureTextEditedTimer;
        private DispatcherTimer _featurePriorsEditedTimer;
        private DispatcherTimer _featureGraphHeightChangedTimer;
        private double _featureGraphHeight;
        private double _pixelsToWeight; // How many pixels to use to display each unit of feature weight (changes based on display size)
        //private bool _featureImportanceAdjusted;
        private Logger _logger;
        private AddFeatureDialog _addFeatureDialog;
        private string _undoButtonText;
        private string _undoButtonTooltip;

        public FeatureSetViewModel(IClassifier classifier, Vocabulary vocab, IReadOnlyList<Label> labels, Logger logger)
            : base()
        {
            _logger = logger;
            _classifier = classifier;
            _vocab = vocab;
            _labels = labels;
            _featureSet = new ObservableCollection<Feature>();
            _userAdded = new List<Feature>();
            _userRemoved = new List<Feature>();
            _userActions = new LinkedList<UserAction>();
            _collectionViewSourceGraph = BuildCollectionViewSourceGraph();
            _featureText = null;
            _featureTextEditedTimer = new DispatcherTimer();
            _featurePriorsEditedTimer = new DispatcherTimer();
            _featureGraphHeightChangedTimer = new DispatcherTimer();
            //_featureImportanceAdjusted = false;
            _undoButtonText = "Undo";

            HighlightFeature = new RelayCommand<string>(PerformHighlightFeature);
            AddFeature = new RelayCommand(PerformAddFeature);
            FeatureRemove = new RelayCommand<Feature>(PerformRemoveFeature, CanPerformRemoveFeature);
            FeatureVeryImportant = new RelayCommand<Feature>(PerformFeatureVeryImportant);
            FeatureSomewhatImportant = new RelayCommand<Feature>(PerformFeatureSomewhatImportant);
            ApplyFeatureAdjustments = new RelayCommand<Tuple<double, Label>>(PerformApplyFeatureAdjustments, CanPerformApplyFeatureAdjustments);
            UndoUserAction = new RelayCommand(PerformUndoUserAction, CanPerformUndoUserAction);

            _classifier.Retrained += classifier_Retrained;
            _vocab.Updated += vocab_Updated;
            _featureTextEditedTimer.Interval = new TimeSpan(2000000); // 200 milliseconds
            _featureTextEditedTimer.Tick += _featureTextEditedTimer_Tick;
            _featurePriorsEditedTimer.Interval = new TimeSpan(2000000); // 200 milliseconds
            _featurePriorsEditedTimer.Tick += _featurePriorsEditedTimer_Tick;
            _featureGraphHeightChangedTimer.Interval = new TimeSpan(2000000); // 200 milliseconds
            _featureGraphHeightChangedTimer.Tick += _featureGraphHeightChangedTimer_Tick;
            FeatureSet.CollectionChanged += FeatureSet_CollectionChanged;
        }

        #region Properties

        public IReadOnlyList<Label> Labels
        {
            get { return _labels; }
        }

        public IReadOnlyList<Feature> UserAddedFeatures
        {
            get { return _userAdded; }
        }

        public IReadOnlyList<Feature> UserRemovedFeatures
        {
            get { return _userRemoved; }
        }

        public ObservableCollection<Feature> FeatureSet
        {
            get { return _featureSet; }
            set { SetProperty<ObservableCollection<Feature>>(ref _featureSet, value); }
        }

        public CollectionViewSource FeatureSetViewSourceGraph
        {
            get { return _collectionViewSourceGraph; }
            private set { SetProperty<CollectionViewSource>(ref _collectionViewSourceGraph, value); }
        }

        public double FeatureGraphHeight
        {
            get { return _featureGraphHeight; }
            set
            {
                if (SetProperty<double>(ref _featureGraphHeight, value)) {
                    // Wait until this stops changing before updating the layout
                    _featureGraphHeightChangedTimer.Stop();
                    _featureGraphHeightChangedTimer.Start();
                }
            }
        }

        public string UndoButtonText
        {
            get { return _undoButtonText; }
            private set { SetProperty<string>(ref _undoButtonText, value); }
        }

        public string UndoButtonTooltip
        {
            get { return _undoButtonTooltip; }
            private set { SetProperty<string>(ref _undoButtonTooltip, value); }
        }

        public Feature SelectedFeature
        {
            get { return _selectedFeature; }
            set { SetProperty<Feature>(ref _selectedFeature, value); }
        }

        public string SelectedText
        {
            get { return _selectedText; }
            set { SetProperty<string>(ref _selectedText, value); }
        }

        #endregion

        #region Commands

        public RelayCommand<string> HighlightFeature { get; private set; }
        public RelayCommand AddFeature { get; private set; }
        public RelayCommand<Feature> FeatureRemove { get; private set; }
        public RelayCommand<Feature> FeatureVeryImportant { get; private set; }
        public RelayCommand<Feature> FeatureSomewhatImportant { get; private set; }
        public RelayCommand<Tuple<double, Label>> ApplyFeatureAdjustments { get; private set; }
        public RelayCommand UndoUserAction { get; private set; }

        #endregion

        #region Events

        public class FeatureAddedEventArgs : EventArgs
        {
            public readonly Feature Feature;
            public FeatureAddedEventArgs(Feature feature)
            {
                Feature = feature;
            }
        }

        public class FeatureTextEditedEventArgs : EventArgs
        {
            public readonly string Tokens;
            public FeatureTextEditedEventArgs(string tokens)
            {
                Tokens = tokens;
            }
        }

        public class FeaturePriorEditedEventArgs : EventArgs
        {
            public readonly Feature Feature;
            public FeaturePriorEditedEventArgs(Feature feature)
            {
                Feature = feature;
            }
        }

        public class FeatureRemovedEventArgs : EventArgs
        {
            public readonly string Tokens;
            public FeatureRemovedEventArgs(string tokens)
            {
                Tokens = tokens;
            }
        }

        public event EventHandler<FeatureAddedEventArgs> FeatureAdded;
        public event EventHandler<FeatureRemovedEventArgs> FeatureRemoved;
        public event EventHandler<FeatureTextEditedEventArgs> FeatureTextEdited;
        public event EventHandler<FeaturePriorEditedEventArgs> FeaturePriorEdited;

        protected virtual void OnFeatureAdded(FeatureAddedEventArgs e)
        {
            if (FeatureAdded != null)
                FeatureAdded(this, e);
        }

        protected virtual void OnFeatureRemoved(FeatureRemovedEventArgs e)
        {
            if (FeatureRemoved != null)
                FeatureRemoved(this, e);
        }

        protected virtual void OnFeatureTextEdited(FeatureTextEditedEventArgs e)
        {
            if (FeatureTextEdited != null)
                FeatureTextEdited(this, e);
        }

        protected virtual void OnFeaturePriorEdited(FeaturePriorEditedEventArgs e)
        {
            if (FeaturePriorEdited != null)
                FeaturePriorEdited(this, e);
        }

        #endregion

        #region Public methods

        public void AddUserAction(UserAction action)
        {
            _userActions.AddFirst(action);
            UpdateUndoButtonText();
        }

        /// <summary>
        /// If there is a feature that matches 'word', select it.
        /// </summary>
        /// <param name="word"></param>
        public void SelectFeature(string word)
        {
            Feature toFind = new Feature(word);
            bool found = false;
            foreach (Feature f in _featureSet) {
                if (f.Equals(toFind)) {
                    f.IsSelected = true;
                    found = true;
                    SelectedFeature = f;
                } else {
                    f.IsSelected = false;
                }
            }
            if (!found) {
                SelectedFeature = null;
            }
            _featureText = word;
            SelectedText = null;
        }

        public void LogFeatureTabChanged(string tabName)
        {
            _logger.Writer.WriteStartElement("FeatureTabChanged");
            _logger.Writer.WriteAttributeString("tabName", tabName);
            _logger.logTime();
            _logger.logEndElement();
        }

        public void LogFeatureAdjustBegin(Feature feature, FeatureImportance fi)
        {
            _logger.Writer.WriteStartElement("FeatureAdjustmentBegin");
            _logger.Writer.WriteAttributeString("adjustingLabel", fi.Label.ToString());
            _logger.logTime();
            _logger.logFeature(feature);
        }

        public void LogFeatureAdjustEnd(Feature feature, FeatureImportance fi)
        {
            _logger.Writer.WriteStartElement("FeatureAdjustmentEnd");
            _logger.Writer.WriteAttributeString("feature", feature.Characters);
            _logger.Writer.WriteAttributeString("label", fi.Label.ToString());
            _logger.Writer.WriteAttributeString("userHeight", fi.UserHeight.ToString());
            _logger.Writer.WriteAttributeString("systemHeight", fi.SystemHeight.ToString());
            _logger.Writer.WriteAttributeString("userWeight", fi.UserWeight.ToString());
            _logger.Writer.WriteAttributeString("userPrior", fi.UserPrior.ToString());
            _logger.Writer.WriteAttributeString("systemWeight", fi.SystemWeight.ToString());
            _logger.logTime();
            _logger.logEndElement();
            _logger.logEndElement(); // Also end the FeatureAdjustBegin element
        }

        public void LogOverviewScrolled(double change, double offset)
        {
            _logger.Writer.WriteStartElement("FeatureOverviewScrolled");
            _logger.Writer.WriteAttributeString("change", change.ToString());
            _logger.Writer.WriteAttributeString("offset", offset.ToString());
            _logger.logTime();
            _logger.logEndElement();
        }

        public void LogControlScrolled(string control, double change, double offset)
        {
            _logger.Writer.WriteStartElement(control + "Scrolled");
            _logger.Writer.WriteAttributeString("change", change.ToString());
            _logger.Writer.WriteAttributeString("offset", offset.ToString());
            _logger.logTime();
            _logger.logEndElement();
        }

        public void LogShowImportantWordsExplanationStart()
        {
            _logger.Writer.WriteStartElement("ShowImportantWordsExplanation");
            _logger.logTime();
        }

        public void LogShowImportantWordsExplanationEnd()
        {
            _logger.logEndElement();
        }

        public void AddUserFeature(Feature feature)
        {
            _logger.Writer.WriteStartElement("AddFeature");
            _logger.logTime();
            _logger.logFeature(feature);
            _logger.logEndElement();

            // Allow the user to undo this action
            UserAction action = new UserAction(UserAction.ActionType.AddFeature, feature, null);
            _userActions.AddFirst(action);
            UpdateUndoButtonText();

            // If the user previously removed this feature, clear it from the list of removed features
            _userRemoved.Remove(feature);
            // If the user already added this feature, overwrite the previous version
            _userAdded.Remove(feature);
            _userAdded.Add(feature);

            // Update the UI right away, even if we don't retrain
            FeatureSet.Add(feature);

            OnFeatureAdded(new FeatureAddedEventArgs(feature));
        }

        public void RemoveUserFeature(Feature feature)
        {
            _logger.Writer.WriteStartElement("RemoveFeature");
            _logger.logTime();
            _logger.logFeature(feature);
            _logger.logEndElement();

            // Allow the user to undo this action
            UserAction action = new UserAction(UserAction.ActionType.RemoveFeature, feature, null);
            _userActions.AddFirst(action);
            UpdateUndoButtonText();

            // If the user previously added this feature manually, remove it from our list
            _userAdded.Remove(feature);

            // Ensure we don't add this feature to the list multiple times
            _userRemoved.Add(feature);

            // Update the UI right away, even if we don't retrain
            FeatureSet.Remove(feature);

            OnFeatureRemoved(new FeatureRemovedEventArgs(feature.Characters));
        }

        public double AdjustUserFeatureHeight(FeatureImportance fi, double heightDelta)
        {
            if (fi.UserHeight + heightDelta < FeatureImportance.MINIMUM_HEIGHT) {
                heightDelta = -1 * (fi.UserHeight - FeatureImportance.MINIMUM_HEIGHT);
            }
            fi.UserHeight += heightDelta;
            //_featureImportanceAdjusted = true;

            return heightDelta;
        }

        public void UpdateFeaturePriors(Tuple<double, Label> diff)
        {
            Console.WriteLine("*** UpdateFeaturePriors() ***");

            // Don't fire a featureWeightEdited event while we're updating these values
            _featurePriorsEditedTimer.Stop();

            Console.WriteLine("UpdateFeaturePriors");

            // Get the sum of user heights for each feature
            double userHeightSumTopic1 = 0;
            double userHeightSumTopic2 = 0;
            foreach (Feature f in _featureSet) {
                userHeightSumTopic1 += f.Topic1Importance.UserHeight;
                userHeightSumTopic2 += f.Topic2Importance.UserHeight;
            }

            // Get the sum of system heights for each feature
            double systemHeightSumTopic1 = 0;
            double systemHeightSumTopic2 = 0;
            foreach (Feature f in _featureSet) {
                systemHeightSumTopic1 += f.Topic1Importance.SystemHeight;
                systemHeightSumTopic2 += f.Topic2Importance.SystemHeight;
            }

            // Get the sum of counts for each feature
            double systemFeatureCountSumTopic1 = 0;
            double systemFeatureCountSumTopic2 = 0;
            _classifier.TryGetSystemFeatureSum(_labels[0], out systemFeatureCountSumTopic1);
            _classifier.TryGetSystemFeatureSum(_labels[1], out systemFeatureCountSumTopic2);

            // Get the current sum of prior values
            double userPriorSumTopic1;
            double userPriorSumTopic2;
            _classifier.TryGetFeaturePriorSum(_labels[0], out userPriorSumTopic1);
            _classifier.TryGetFeaturePriorSum(_labels[1], out userPriorSumTopic2);

            // Get the ratio of area of user importance vs. area of system importance
            // (This tells us what the sum of user priors should be, relative to the sum of feature counts)
            double userToSystemRatioTopic1 = Math.Round((userHeightSumTopic1 / systemHeightSumTopic1), 3);
            double userToSystemRatioTopic2 = Math.Round((userHeightSumTopic2 / systemHeightSumTopic2), 3);
            if (Double.IsInfinity(userToSystemRatioTopic1)) {
                userToSystemRatioTopic1 = 1;
            }
            if (Double.IsInfinity(userToSystemRatioTopic2)) {
                userToSystemRatioTopic2 = 1;
            }
            double desiredPriorSumTopic1 = Math.Round((userToSystemRatioTopic1 * systemFeatureCountSumTopic1), 3);
            double desiredPriorSumTopic2 = Math.Round((userToSystemRatioTopic2 * systemFeatureCountSumTopic2), 3);

            // If there are no features in this topic, we might get 0 for the desiredPriorSum. Stick with the
            // last priorSum in that case.
            if (desiredPriorSumTopic1 <= 0) {
                desiredPriorSumTopic1 = userPriorSumTopic1;
                if (diff.Item2 == _labels[0]) {
                    double ratio = (userHeightSumTopic1 + diff.Item1) / userHeightSumTopic1;
                    desiredPriorSumTopic1 *= ratio;
                    if (desiredPriorSumTopic1 <= 0) {
                        desiredPriorSumTopic1 = .1;
                    }
                }
            }
            if (desiredPriorSumTopic2 <= 0) {
                desiredPriorSumTopic2 = userPriorSumTopic2;
                if (diff.Item2 == _labels[1]) {
                    double ratio = (userHeightSumTopic2 + diff.Item1) / userHeightSumTopic2;
                    desiredPriorSumTopic2 *= ratio;
                    if (desiredPriorSumTopic2 <= 0) {
                        desiredPriorSumTopic2 = .1;
                    }
                }
            }

            // Get the percentage of the total prior value that should assigned to each feature
            Console.WriteLine("userHeightSum1: {0} systemHeightSum1: {1} userToSystemRatio1: {2} systemFeatureCountSum1: {3} desiredPriorSum1: {4}",
                userHeightSumTopic1, systemHeightSumTopic1, userToSystemRatioTopic1, systemFeatureCountSumTopic1, desiredPriorSumTopic1);
            Console.WriteLine("userHeightSum2: {0} systemHeightSum2: {1} userToSystemRatio2: {2} systemFeatureCountSum2: {3} desiredPriorSum2: {4}",
                userHeightSumTopic2, systemHeightSumTopic2, userToSystemRatioTopic2, systemFeatureCountSumTopic2, desiredPriorSumTopic2);
            foreach (Feature f in _featureSet) {
                Console.Write("Old prior for {0} ({2}) = {1}, ", f.Characters, f.Topic1Importance.UserPrior, f.Topic1Importance.Label);
                f.Topic1Importance.UserPrior = Math.Round(((f.Topic1Importance.UserHeight / userHeightSumTopic1) * desiredPriorSumTopic1), 2);
                Console.WriteLine("new prior = {0}, sys weight = {1:N2}", f.Topic1Importance.UserPrior, f.Topic1Importance.SystemWeight);
                Console.Write("Old prior for {0} ({2}) = {1}, ", f.Characters, f.Topic2Importance.UserPrior, f.Topic2Importance.Label);
                f.Topic2Importance.UserPrior = Math.Round(((f.Topic2Importance.UserHeight / userHeightSumTopic2) * desiredPriorSumTopic2), 2);
                Console.WriteLine("new prior = {0}, sys weight = {1:N2}", f.Topic2Importance.UserPrior, f.Topic2Importance.SystemWeight);
            }

            _featurePriorsEditedTimer.Start();
        }

        #endregion

        #region Private methods

        private void UpdateUndoButtonText()
        {
            string desc = "Undo"; // Default text
            string tooltip = null;

            // Do we have any undo-able actions?
            UserAction action;
            if (_userActions.Count > 0) {
                action = _userActions.First.Value;
                switch (action.Type) {
                    case UserAction.ActionType.AdjustFeaturePrior:
                        desc = "Undo importance adjustment";
                        tooltip = action.Desc;
                        break;
                    case UserAction.ActionType.AddFeature:
                        desc = "Undo add word";
                        tooltip = action.Desc;
                        break;
                    case UserAction.ActionType.RemoveFeature:
                        desc = "Undo remove word";
                        tooltip = action.Desc;
                        break;
                }
            }
            
            UndoButtonText = desc;
            UndoButtonTooltip = tooltip;
        }

        private void FeatureSet_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null)
                return;

            foreach (Feature f in e.NewItems) {
                Console.WriteLine("new feature: {0} {1}", f, e.Action);
            }
        }

        private void _featureTextEditedTimer_Tick(object sender, EventArgs e)
        {
            _featureTextEditedTimer.Stop();
            OnFeatureTextEdited(new FeatureTextEditedEventArgs(_featureText));
        }

        private void _featurePriorsEditedTimer_Tick(object sender, EventArgs e)
        {
            _featurePriorsEditedTimer.Stop();
            // FIXME do we need to pass out the specific feature that changed? Might be enough just to let the main VM
            // know that it needs to update the priors and re-predict.
            OnFeaturePriorEdited(new FeaturePriorEditedEventArgs(null));
        }

        private void _featureGraphHeightChangedTimer_Tick(object sender, EventArgs e)
        {
            _featureGraphHeightChangedTimer.Stop();
            UpdateFeatureHeights();
        }

        private void classifier_Retrained(object sender, EventArgs e)
        {
            // The system's feature weights may have changed, we need to update the display
            UpdateFeatures();
        }

        private void vocab_Updated(object sender, EventArgs e)
        {
            // The system's vocab may have changed, we need to show the new vocab items
            UpdateFeatures();
        }

        private void UpdateFeatureHeights()
        {
            // Computer our new pixelsToWeight ratio
            double maxWeight = 0;
            foreach (Feature f in FeatureSet) {
                double weight = f.Topic1Importance.SystemWeight + f.Topic1Importance.UserWeight;
                if (maxWeight < weight) {
                    maxWeight = weight;
                }
                weight = f.Topic2Importance.SystemWeight + f.Topic2Importance.UserWeight;
                if (maxWeight < weight) {
                    maxWeight = weight;
                }
            }
            // Subtract ~50px because of the tabbed item headers
            _pixelsToWeight = (PERCENT_HEIGHT_OF_MAX_BAR * (_featureGraphHeight - 50)) / maxWeight;

            // Update the PixelsToWeight value for each Feature
            foreach (Feature f in FeatureSet) {
                f.Topic1Importance.PixelsToWeight = _pixelsToWeight;
                f.Topic2Importance.PixelsToWeight = _pixelsToWeight;
            }
        }

        private void UpdateFeatures()
        {
            Console.WriteLine("UpdateFeatures()");
            List<Feature> toRemove = new List<Feature>();

            // Add the features we don't already have in our set
            foreach (int id in _vocab.FeatureIds) {
                string word = _vocab.GetWord(id);
                Feature toFind = new Feature(word);
                if (!FeatureSet.Contains(toFind)) {
                    // These will be system-determined features. Include the default user weight and prior.
                    Feature f = new Feature(word, _labels[0], _labels[1]);
                    double prior, weight;
                    _classifier.TryGetFeatureUserPrior(id, _labels[0], out prior);
                    _classifier.TryGetFeatureUserWeight(id, _labels[0], out weight);
                    f.Topic1Importance.UserPrior = prior;
                    f.Topic1Importance.UserWeight = weight;
                    _classifier.TryGetFeatureUserPrior(id, _labels[1], out prior);
                    _classifier.TryGetFeatureUserWeight(id, _labels[1], out weight);
                    f.Topic2Importance.UserPrior = prior;
                    f.Topic2Importance.UserWeight = weight;
                    FeatureSet.Add(f);
                }
            }

            // Go through each feature and update it with the system's current weight, or mark it for removal
            foreach (Feature f in FeatureSet) {
                int id = _vocab.GetWordId(f.Characters, true);

                // Figure out if we need to remove this feature
                if (id < 0) {
                    toRemove.Add(f);
                }

                // Update this feature's system-determined weight
                double weight;
                if (_classifier.TryGetFeatureSystemWeight(id, f.Topic1Importance.Label, out weight)) {
                    f.Topic1Importance.SystemWeight = weight;
                }
                if (_classifier.TryGetFeatureUserWeight(id, f.Topic1Importance.Label, out weight)) {
                    f.Topic1Importance.UserWeight = weight;
                }
                if (_classifier.TryGetFeatureSystemWeight(id, f.Topic2Importance.Label, out weight)) {
                    f.Topic2Importance.SystemWeight = weight;
                }
                if (_classifier.TryGetFeatureUserWeight(id, f.Topic2Importance.Label, out weight)) {
                    f.Topic2Importance.UserWeight = weight;
                }

                Console.WriteLine("Feature {0} ({1}): userWeight={2}, sysWeight={3}",
                    f.Characters, f.Topic1Importance.Label, f.Topic1Importance.UserWeight, f.Topic1Importance.SystemWeight);
                Console.WriteLine("Feature {0} ({1}): userWeight={2}, sysWeight={3}",
                    f.Characters, f.Topic2Importance.Label, f.Topic2Importance.UserWeight, f.Topic2Importance.SystemWeight);
            }

            // Remove anything marked for removal
            foreach (Feature f in toRemove) {
                FeatureSet.Remove(f);
            }

            UpdateFeatureHeights();
            // Refresh our source views
            //foreach (CollectionViewSource cvs in _collectionViewSourcesOverview) {
            //    cvs.View.Refresh(); // FIXME too slow
            //}

            //_featureImportanceAdjusted = false;
        }

        private void PerformHighlightFeature(string text)
        {
            FeatureTextChanged(text);
        }

        private void PerformAddFeature()
        {
            _logger.Writer.WriteStartElement("AddFeatureDialog");
            _logger.logTime();

            if (_addFeatureDialog == null) {
                _addFeatureDialog = new AddFeatureDialog();
                _addFeatureDialog.Closed += _addFeatureDialog_Closed;
                _addFeatureDialog.Owner = App.Current.MainWindow;
                AddFeatureDialogViewModel vm = new AddFeatureDialogViewModel(_labels);
                // If the user has searched for text, use it as the default
                if (!string.IsNullOrWhiteSpace(SelectedText)) {
                    vm.Word = SelectedText;
                } else if (!string.IsNullOrWhiteSpace(_featureText) && !IsTextForSelectedFeature(_featureText)) {
                    // If the user has selected text in the message, use it as the second default choice
                    vm.Word = _featureText;
                }
                vm.PropertyChanged += AddFeatureVM_PropertyChanged;
                vm.AddFeature += AddFeatureVM_AddFeature;
                _addFeatureDialog.DataContext = vm;
                //bool? result = dialog.ShowDialog();
                _addFeatureDialog.Show();
            } else {
                if (_addFeatureDialog.WindowState == System.Windows.WindowState.Minimized) {
                    _addFeatureDialog.WindowState = System.Windows.WindowState.Normal; // restore a minimized window
                } else {
                    _addFeatureDialog.Activate(); // bring the window to the top of the desktop
                }
            }

            //_logger.logEndElement();
        }

        void _addFeatureDialog_Closed(object sender, EventArgs e)
        {
            _addFeatureDialog = null;
            //_logger.Writer.WriteStartElement("CloseAddFeatureDialog");
            //_logger.logTime();
            _logger.logEndElement(); // Close "AddFeatureDialog" element

            // Let anyone watching know that the user has closed the dialog, so _featureText is now empty.
            _featureText = "";
            OnFeatureTextEdited(new FeatureTextEditedEventArgs(_featureText));
        }

        private void AddFeatureVM_AddFeature(object sender, AddFeatureDialogViewModel.AddFeatureEventArgs e)
        {
            Feature f = e.Feature;
            f.Topic1Importance.PixelsToWeight = _pixelsToWeight;
            f.Topic2Importance.PixelsToWeight = _pixelsToWeight;
            AddUserFeature(f);
        }

        private void AddFeatureVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Word") {
                AddFeatureDialogViewModel vm = sender as AddFeatureDialogViewModel;
                if (vm != null)
                    FeatureTextChanged(vm.Word);
            }
        }

        private bool IsTextForSelectedFeature(string text)
        {
            bool retval = false;

            if (text != null && text.Length > 0) {
                Feature toFind = new Feature(text);
                foreach (Feature f in _featureSet) {
                    if (toFind.Equals(f) && f.IsSelected) {
                        retval = true;
                    }
                }
            }

            return retval;
        }

        private void FeatureTextChanged(string text)
        {
            _featureTextEditedTimer.Stop();
            _featureText = text;
            _featureTextEditedTimer.Start();
        }

        private bool CanPerformRemoveFeature(Feature feature)
        {
            return (feature != null);
        }

        private void PerformRemoveFeature(Feature feature)
        {
            RemoveUserFeature(feature);
        }

        private void PerformFeatureVeryImportant(Feature feature)
        {
            Console.WriteLine("PerformFeatureVeryImportant on {0}", feature);
        }

        private void PerformFeatureSomewhatImportant(Feature feature)
        {
            Console.WriteLine("PerformFeatureSomewhatImportant on {0}", feature);
        }

        private bool CanPerformApplyFeatureAdjustments(Tuple<double, Label> diff)
        {
            //return _featureImportanceAdjusted;
            return true;
        }

        private void PerformApplyFeatureAdjustments(Tuple<double, Label> diff)
        {
            Console.WriteLine("Apply feature adjustments");
            _logger.Writer.WriteStartElement("ApplyFeatureAdjustments");
            _logger.logTime();

            UpdateFeaturePriors(diff);

            //_featureImportanceAdjusted = false;

            _logger.logEndElement();
        }

        private bool CanPerformUndoUserAction()
        {
            return (_userActions.Count > 0);
        }

        private void PerformUndoUserAction()
        {
            UserAction action = _userActions.First.Value;
            _userActions.RemoveFirst();
            UpdateUndoButtonText();
            Console.WriteLine("Undo: {0}", action.Desc);
            _logger.Writer.WriteStartElement("Undo");
            _logger.Writer.WriteAttributeString("action", action.Type.ToString());
            _logger.logTime();
            switch (action.Type) {
                case UserAction.ActionType.AdjustFeaturePrior:
                    // Find the right feature and reset its user prior
                    foreach (Feature f in _featureSet) {
                        if (f.Equals(action.Feature)) {
                            if (f.Topic1Importance.Label.Equals(action.Label)) {
                                //Console.WriteLine("Resetting prior for {0} ({1}) from {2} to {3}",
                                //    action.Feature.Characters, action.Feature.Topic1Importance.Label, f.Topic1Importance.UserPrior, action.Feature.Topic1Importance.UserPrior);
                                f.Topic1Importance.UserPrior = action.Feature.Topic1Importance.UserPrior;
                                f.Topic1Importance.ForceHeightUpdate();
                            } else if (f.Topic2Importance.Label.Equals(action.Label)) {
                                //Console.WriteLine("Resetting prior for {0} ({1}) from {2} to {3}",
                                //    action.Feature.Characters, action.Feature.Topic2Importance.Label, f.Topic2Importance.UserPrior, action.Feature.Topic2Importance.UserPrior);
                                f.Topic2Importance.UserPrior = action.Feature.Topic2Importance.UserPrior;
                                f.Topic2Importance.ForceHeightUpdate();
                            }
                        }
                    }
                    // Let anyone watching know that the feature priors have changed
                    _featurePriorsEditedTimer.Stop();
                    _featurePriorsEditedTimer.Start();
                    break;
                case UserAction.ActionType.RemoveFeature:
                    // Ensure the feature isn't selected
                    action.Feature.IsSelected = false;
                    // Remove this feature from the list of removed features
                    _userRemoved.Remove(action.Feature);
                    _userAdded.Add(action.Feature);
                    // Update the UI right away, even if we don't retrain
                    FeatureSet.Add(action.Feature);
                    OnFeatureAdded(new FeatureAddedEventArgs(action.Feature));
                    break;
                case UserAction.ActionType.AddFeature:
                    // Remove this feature from the list of added features
                    _userAdded.Remove(action.Feature);
                    _userRemoved.Add(action.Feature);
                    // Update the UI right away, even if we don't retrain
                    FeatureSet.Remove(action.Feature);
                    OnFeatureRemoved(new FeatureRemovedEventArgs(action.Feature.Characters));
                    break;
                default:
                    Console.WriteLine("Error: Unknown user action '{0}'", action.Desc);
                    break;
            }
            _logger.logEndElement();
        }

        private IReadOnlyList<CollectionViewSource> BuildCollectionViewSourcesOverview(IReadOnlyList<Label> labels)
        {
            List<CollectionViewSource> collectionViewSources = new List<CollectionViewSource>();
            foreach (Label label in labels) {
                CollectionViewSource cvs = new CollectionViewSource();
                cvs.Source = FeatureSet;
                cvs.Filter += (o, e) =>
                {
                    Feature f = e.Item as Feature;
                    if (f != null) {
                        // Only display features that are more important for this label than other labels
                        if (f.MostImportantLabel == label)
                            e.Accepted = true;
                        else
                            e.Accepted = false;
                    }
                };
                cvs.SortDescriptions.Clear();
                cvs.SortDescriptions.Add(new System.ComponentModel.SortDescription("Characters", System.ComponentModel.ListSortDirection.Ascending));
                collectionViewSources.Add(cvs);
            }

            return collectionViewSources;
        }

        private IReadOnlyList<CollectionViewSource> BuildCollectionViewSourcesGraph(IReadOnlyList<Label> labels)
        {
            List<CollectionViewSource> collectionViewSources = new List<CollectionViewSource>();
            foreach (Label label in labels) {
                CollectionViewSource cvs = new CollectionViewSource();
                cvs.Source = FeatureSet;
                //cvs.Filter += (o, e) =>
                //{
                //    Feature f = e.Item as Feature;
                //    if (f != null) {
                //        // Only display features that are more important for this label than other labels
                //        if (f.Label == label)
                //            e.Accepted = true;
                //        else
                //            e.Accepted = false;
                //    }
                //};
                cvs.SortDescriptions.Clear();
                cvs.SortDescriptions.Add(new System.ComponentModel.SortDescription("Characters", System.ComponentModel.ListSortDirection.Ascending));
                collectionViewSources.Add(cvs);
            }

            return collectionViewSources;
        }

        private CollectionViewSource BuildCollectionViewSourceGraph()
        {
            CollectionViewSource cvs = new CollectionViewSource();
            cvs.Source = FeatureSet;
            cvs.SortDescriptions.Clear();
            cvs.SortDescriptions.Add(new System.ComponentModel.SortDescription("Characters", System.ComponentModel.ListSortDirection.Ascending));
            
            return cvs;
        }

        #endregion
    }
}
