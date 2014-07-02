using MessagePredictor.Model;
using MessagePredictor.View;
using MessagePredictor.ViewModel;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;

namespace MessagePredictor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public const string DataDir = "Data";
        public const string LogDir = "Logs";

        public enum Condition
        {
            None,
            Control,
            Treatment
        }

        public enum Mode
        {
            None,
            Tutorial,
            Study
        }

        // Use these to access Application.Properties items.
        public enum PropertyKey
        {
            Unknown,
            Condition,
            TimeLimit,
            EvalInterval,
            DatasetFile,
            TestDatasetFile,
            TutorialDatasetFile,
            Topic1TrainSize,
            Topic1TestSize,
            Topic1VocabSize,
            Topic1SystemLabel,
            Topic1UserLabel,
            Topic1Color,
            Topic1ColorDesc,
            Topic2TrainSize,
            Topic2TestSize,
            Topic2VocabSize,
            Topic2SystemLabel,
            Topic2UserLabel,
            Topic2Color,
            Topic2ColorDesc,
            AutoUpdatePredictions,
            UserId,
            Mode,
            FullScreen,
            DevMode
        }

        private class Options
        {
            public Mode Mode;
            public Condition Condition;
            public bool FullScreen;
            public bool ShowHelp;
            public bool ShowVersion;
            public string UserId;
            public bool OverWriteLog;
            public int TimeLimit;
            public int EvalInterval;
            public bool DevMode;

            public Options()
            {
                Mode = Mode.None;
                Condition = Condition.None;
            }
        }

        private Logger _logger;
        private Logger _backgroundLogger;
        private MessagePredictorViewModel _vm;
        private bool _crashed = false;

        // Called on Application startup. Handle any command line arguments, load our configuration properties, 
        // and then build the ViewModel and View.
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Options options = ParseCommandOptions(e.Args);
            if (options.ShowHelp) {
                Console.WriteLine("Options:");
                Console.WriteLine("\t-c [condition]\tStart either the 'control' or 'treatment' condition");
                Console.WriteLine("\t-d\t\tDeveloper mode");
                Console.WriteLine("\t-f\t\tExpand window to full size of screen");
                Console.WriteLine("\t-h\t\tDisplay this message and exit");
                Console.WriteLine("\t-i [interval]\tEvaluate classifier every [interval] seconds");
                Console.WriteLine("\t-m [mode]\tStart in either 'tutorial' or 'study' mode");
                Console.WriteLine("\t-ow\t\tOverwrite the existing log file");
                Console.WriteLine("\t-p [id]\t\tSet the participant ID to [id]");
                Console.WriteLine("\t-t [time]\tAutomatically close the application after [time] minutes");
                Console.WriteLine("\t-v\t\tDisplay the version number and exit");
                Environment.Exit(0);
            } else if (options.ShowVersion) {
                Console.WriteLine("Message Predictor {0}", Assembly.GetExecutingAssembly().GetName().Version.ToString());
                Environment.Exit(0);
            }

            LoadPropertiesFile(options.Mode == Mode.Tutorial);

            // If we specified command line arguments to over-ride the properties file, make sure that happens
            if (options.Condition != Condition.None) {
                this.Properties[PropertyKey.Condition] = options.Condition;
            }
            if (options.Mode != Mode.None) {
                this.Properties[PropertyKey.Mode] = options.Mode;
            }
            if (!string.IsNullOrWhiteSpace(options.UserId)) {
                this.Properties[PropertyKey.UserId] = options.UserId;
            } else {
                this.Properties[PropertyKey.UserId] = "dev"; // development identifier
            }
            this.Properties[PropertyKey.FullScreen] = options.FullScreen;
            this.Properties[PropertyKey.DevMode] = options.DevMode;
            if (options.TimeLimit > 0) {
                this.Properties[PropertyKey.TimeLimit] = options.TimeLimit;
            }
            if (options.EvalInterval > 0) {
                this.Properties[PropertyKey.EvalInterval] = options.EvalInterval;
            }

            _logger = new Logger(this.Properties[PropertyKey.UserId].ToString(), this.Properties[PropertyKey.Mode].ToString(), 
                                 options.OverWriteLog, false, false);
            _logger.Writer.WriteStartDocument();
            _logger.Writer.WriteStartElement("MessagePredictorLog");

            _logger.Writer.WriteAttributeString("condition", this.Properties[PropertyKey.Condition].ToString());
            _logger.Writer.WriteAttributeString("mode", this.Properties[PropertyKey.Mode].ToString());
            _logger.Writer.WriteAttributeString("dataset", this.Properties[PropertyKey.DatasetFile].ToString());
            _logger.Writer.WriteAttributeString("testDataset", this.Properties[PropertyKey.TestDatasetFile].ToString());
            _logger.Writer.WriteAttributeString("autoupdate", this.Properties[PropertyKey.AutoUpdatePredictions].ToString());
            _logger.Writer.WriteAttributeString("timelimit", this.Properties[PropertyKey.TimeLimit].ToString());
            _logger.Writer.WriteAttributeString("evalInterval", this.Properties[PropertyKey.EvalInterval].ToString());
            _logger.Writer.WriteAttributeString("userid", this.Properties[PropertyKey.UserId].ToString());
            _logger.Writer.WriteAttributeString("fullScreen", this.Properties[PropertyKey.FullScreen].ToString());
            _logger.Writer.WriteAttributeString("system", Environment.OSVersion.ToString());
            _logger.Writer.WriteAttributeString("cpus", Environment.ProcessorCount.ToString());
            _logger.Writer.WriteAttributeString("runtime", Environment.Version.ToString());
            _logger.Writer.WriteAttributeString("machine", Environment.MachineName.ToString());
            _logger.Writer.WriteAttributeString("version", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            _logger.Writer.WriteAttributeString("startTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            _backgroundLogger = new Logger(this.Properties[PropertyKey.UserId].ToString(), this.Properties[PropertyKey.Mode].ToString(),
                                           options.OverWriteLog, false, true);
            _backgroundLogger.Writer.WriteStartDocument();
            _backgroundLogger.Writer.WriteStartElement("MessagePredictorBackgroundLog");
            _backgroundLogger.Writer.WriteAttributeString("condition", this.Properties[PropertyKey.Condition].ToString());
            _backgroundLogger.Writer.WriteAttributeString("mode", this.Properties[PropertyKey.Mode].ToString());
            _backgroundLogger.Writer.WriteAttributeString("dataset", this.Properties[PropertyKey.DatasetFile].ToString());
            _backgroundLogger.Writer.WriteAttributeString("testDataset", this.Properties[PropertyKey.TestDatasetFile].ToString());
            _backgroundLogger.Writer.WriteAttributeString("autoupdate", this.Properties[PropertyKey.AutoUpdatePredictions].ToString());
            _backgroundLogger.Writer.WriteAttributeString("timelimit", this.Properties[PropertyKey.TimeLimit].ToString());
            _backgroundLogger.Writer.WriteAttributeString("evalInterval", this.Properties[PropertyKey.EvalInterval].ToString());
            _backgroundLogger.Writer.WriteAttributeString("userid", this.Properties[PropertyKey.UserId].ToString());
            _backgroundLogger.Writer.WriteAttributeString("fullScreen", this.Properties[PropertyKey.FullScreen].ToString());
            _backgroundLogger.Writer.WriteAttributeString("system", Environment.OSVersion.ToString());
            _backgroundLogger.Writer.WriteAttributeString("cpus", Environment.ProcessorCount.ToString());
            _backgroundLogger.Writer.WriteAttributeString("runtime", Environment.Version.ToString());
            _backgroundLogger.Writer.WriteAttributeString("machine", Environment.MachineName.ToString());
            _backgroundLogger.Writer.WriteAttributeString("version", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            _backgroundLogger.Writer.WriteAttributeString("startTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            // Use a longer tooltip timeout (20 seconds)
            ToolTipService.ShowDurationProperty.OverrideMetadata(
                typeof(DependencyObject), new FrameworkPropertyMetadata(20000));

            _vm = new MessagePredictorViewModel(_logger, _backgroundLogger);
            var window = new MessagePredictorWindow();
            window.DataContext = _vm;
            window.Loaded += window_Loaded;
            if (options.FullScreen) {
                window.WindowState = WindowState.Maximized;
            }
            if ((int)this.Properties[PropertyKey.TimeLimit] > 0 && !(bool)this.Properties[PropertyKey.DevMode]) {
                window.WindowStyle = WindowStyle.None;
            }
            window.Show();
            _logger.Writer.WriteStartElement("WindowOpen");
            _logger.Writer.WriteAttributeString("time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            _logger.Writer.WriteEndElement();
            _logger.Writer.WriteStartElement("UserActions");
            _backgroundLogger.Writer.WriteStartElement("Evaluations");

            this.DispatcherUnhandledException += Application_DispatcherUnhandledException;
            // We don't want to intercept FirstChanceExceptions unless they turn into UnhandledExceptions
            //AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            _vm.StopBackgroundEvaluation();
            _backgroundLogger.Writer.WriteEndElement(); // End <evaluations/> element
            _backgroundLogger.Writer.WriteEndElement(); // End root element
            _backgroundLogger.Writer.WriteEndDocument();
            _backgroundLogger.Writer.Close();

            _logger.Writer.WriteEndElement(); // End <actions/> element
            _logger.Writer.WriteStartElement("WindowClose");
            _logger.Writer.WriteAttributeString("time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            _logger.Writer.WriteEndElement();

            // Log the accuracy the user saw for each folder
            _logger.Writer.WriteStartElement("FolderResults");
            foreach (FolderViewModel fvm in _vm.FolderListVM.Folders) {
                _logger.Writer.WriteStartElement("Folder");
                _logger.Writer.WriteAttributeString("label", fvm.Label.ToString());
                if (fvm.Evaluator != null) {
                    _logger.Writer.WriteAttributeString("correctPredictions", fvm.Evaluator.CorrectPredictionCount.ToString());
                    _logger.Writer.WriteAttributeString("averageConfidence", fvm.Evaluator.TrainingSetAverageConfidence.ToString());
                }
                _logger.Writer.WriteAttributeString("totalMessages", fvm.MessageCount.ToString());
                _logger.Writer.WriteEndElement();
            }
            _logger.Writer.WriteEndElement();

            // Log the average confidence of each topic
            _logger.Writer.WriteStartElement("AverageConfidence");
            foreach (Evaluator evaluator in _vm.EvaluatorVM.Evaluators) {
                _logger.Writer.WriteStartElement("TopicAverageConfidence");
                _logger.Writer.WriteAttributeString("label", evaluator.Label.ToString());
                _logger.Writer.WriteAttributeString("averageConfidence", evaluator.AverageConfidence.ToString());
                _logger.Writer.WriteEndElement();
            }
            _logger.Writer.WriteEndElement();

            // Log the feature set and training set
            _vm.UpdateDatasetForLogging();
            _vm.LogFeatureSet();
            _vm.LogTrainingSet();
            _vm.LogClassifierEvaluationTraining(_logger);
            _vm.LogClassifierEvaluationTest(_logger);

            // Log variations of the feature set/training set
            _vm.LogClassifierBoW();
            _vm.LogClassifierOnlySysWeight();
            //_vm.LogClassifierOnlyHighIGFeatures();

            _logger.Writer.WriteEndElement(); // End root element
            _logger.Writer.WriteEndDocument();
            _logger.Writer.Close();
        }

        /// <summary>
        /// Figure out if we're running as "control" or a treatment.
        /// </summary>
        /// <param name="element">The XML element containing the desired condition.</param>
        private void LoadConditionProperty(XElement element)
        {
            string conditionString = element.Value.ToString();
            Condition condition = Condition.Control;
            if (conditionString.Equals("Treatment", StringComparison.InvariantCultureIgnoreCase))
                condition = Condition.Treatment;

            this.Properties[PropertyKey.Condition] = condition;
        }

        private void LoadModeProperty(XElement element)
        {
            string modeString = element.Value.ToString();
            Mode mode = Mode.Study;
            if (modeString.Equals("Tutorial", StringComparison.InvariantCultureIgnoreCase))
                mode = Mode.Tutorial;

            this.Properties[PropertyKey.Mode] = mode;
        }

        /// <summary>
        /// Figure out how long to run the prototype for.
        /// </summary>
        /// <param name="element">The XML element containing the desired time limit.</param>
        private void LoadTimeLimitProperty(XElement element)
        {
            int minutes = 0;
            int seconds = 0;
            if (element.Attribute("minutes") != null)
                minutes = Int32.Parse(element.Attribute("minutes").Value.ToString());
            if (element.Attribute("seconds") != null)
                seconds = Int32.Parse(element.Attribute("seconds").Value.ToString());

            this.Properties[PropertyKey.TimeLimit] = (minutes * 60) + seconds;
        }

        private void LoadEvalIntervalProperty(XElement element)
        {
            int minutes = 0;
            int seconds = 0;
            if (element.Attribute("minutes") != null)
                minutes = Int32.Parse(element.Attribute("minutes").Value.ToString());
            if (element.Attribute("seconds") != null)
                seconds = Int32.Parse(element.Attribute("seconds").Value.ToString());

            this.Properties[PropertyKey.EvalInterval] = (minutes * 60) + seconds;
        }

        /// <summary>
        /// Figure out which train and test data to load.
        /// </summary>
        /// <param name="element">The XML element containing information about the desired dataset.</param>
        /// <param name="topic">The topic number (e.g., 1 or 2)</param>
        private void LoadTopicProperty(XElement element, int topic)
        {
            int trainSize = 0;
            int testSize = 0;
            int vocabSize = 0;
            string systemLabel = null;
            string userLabel = null;
            Brush color = null;
            string colorDesc = null;

            if (element.Attribute("trainSize") != null)
                trainSize = Int32.Parse(element.Attribute("trainSize").Value.ToString());
            if (element.Attribute("testSize") != null)
                testSize = Int32.Parse(element.Attribute("testSize").Value.ToString());
            if (element.Attribute("vocabSize") != null)
                vocabSize = Int32.Parse(element.Attribute("vocabSize").Value.ToString());
            if (element.Attribute("systemLabel") != null)
                systemLabel = element.Attribute("systemLabel").Value.ToString();
            if (element.Attribute("userLabel") != null)
                userLabel = element.Attribute("userLabel").Value.ToString();
            if (element.Attribute("color") != null)
                color = new SolidColorBrush((Color)ColorConverter.ConvertFromString(element.Attribute("color").Value.ToString()));
            if (element.Attribute("colorDesc") != null)
                colorDesc = element.Attribute("colorDesc").Value.ToString();

            PropertyKey keyTrainSize = PropertyKey.Unknown;
            PropertyKey keyTestSize = PropertyKey.Unknown;
            PropertyKey keyVocabSize = PropertyKey.Unknown;
            PropertyKey keySystemLabel = PropertyKey.Unknown;
            PropertyKey keyUserLabel = PropertyKey.Unknown;
            PropertyKey keyColor = PropertyKey.Unknown;
            PropertyKey keyColorDesc = PropertyKey.Unknown;

            switch (topic) {
                case 1:
                    keyTrainSize = PropertyKey.Topic1TrainSize;
                    keyTestSize = PropertyKey.Topic1TestSize;
                    keyVocabSize = PropertyKey.Topic1VocabSize;
                    keySystemLabel = PropertyKey.Topic1SystemLabel;
                    keyUserLabel = PropertyKey.Topic1UserLabel;
                    keyColor = PropertyKey.Topic1Color;
                    keyColorDesc = PropertyKey.Topic1ColorDesc;
                    break;
                case 2:
                    keyTrainSize = PropertyKey.Topic2TrainSize;
                    keyTestSize = PropertyKey.Topic2TestSize;
                    keyVocabSize = PropertyKey.Topic2VocabSize;
                    keySystemLabel = PropertyKey.Topic2SystemLabel;
                    keyUserLabel = PropertyKey.Topic2UserLabel;
                    keyColor = PropertyKey.Topic2Color;
                    keyColorDesc = PropertyKey.Topic2ColorDesc;
                    break;
                default:
                    Console.Error.WriteLine("Unknown topic: {0}", topic);
                    break;
            }

            this.Properties[keyTrainSize] = trainSize;
            this.Properties[keyTestSize] = testSize;
            this.Properties[keyVocabSize] = vocabSize;
            this.Properties[keySystemLabel] = systemLabel;
            this.Properties[keyUserLabel] = userLabel;
            this.Properties[keyColor] = color;
            this.Properties[keyColorDesc] = colorDesc;
        }

        private void LoadAutoUpdatePredictionsProperty(XElement element)
        {
            bool autoupdate = true;
            if (!bool.TryParse(element.Value, out autoupdate)) {
                Console.Error.WriteLine("Error parsing AutoUpdatePredictions property '{0}'", element.Value.ToString());
            }

            this.Properties[PropertyKey.AutoUpdatePredictions] = autoupdate;
        }

        /// <summary>
        /// Load an XML file containing various runtime properties.
        /// </summary>
        private void LoadPropertiesFile(bool isTutorial)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DataDir, "Properties.xml");
            try {
                XDocument xdoc = XDocument.Load(path);
                foreach (XElement element in xdoc.Root.Elements()) {
                    if (element.Name == "Condition") // Are we running control or treatment?
                    {
                        LoadConditionProperty(element);
                    } else if (element.Name == "Mode") {
                        LoadModeProperty(element);
                    } else if (element.Name == "TimeLimit") // How long should we let the program run?
                    {
                        LoadTimeLimitProperty(element);
                    } else if (element.Name == "EvalInterval") // When should we log evaluations of the classifier?
                    {
                        LoadEvalIntervalProperty(element);
                    } else if (element.Name == "DataSet" && !isTutorial) {
                        if (element.Attribute("file") != null)
                            this.Properties[PropertyKey.DatasetFile] = element.Attribute("file").Value.ToString();

                        foreach (XElement childElement in element.Elements()) {
                            if (childElement.Name == "Topic1") {
                                LoadTopicProperty(childElement, 1);
                            } else if (childElement.Name == "Topic2") {
                                LoadTopicProperty(childElement, 2);
                            }
                        }
                    } else if (element.Name == "AutoUpdatePredictions") {
                        LoadAutoUpdatePredictionsProperty(element);
                    } else if (element.Name == "TestDataSet") {
                        if (element.Attribute("file") != null) {
                            this.Properties[PropertyKey.TestDatasetFile] = element.Attribute("file").Value.ToString();
                        }
                    } else if (element.Name == "TutorialDataSet" && isTutorial) {
                        if (element.Attribute("file") != null)
                            this.Properties[PropertyKey.DatasetFile] = element.Attribute("file").Value.ToString();

                        foreach (XElement childElement in element.Elements()) {
                            if (childElement.Name == "Topic1") {
                                LoadTopicProperty(childElement, 1);
                            } else if (childElement.Name == "Topic2") {
                                LoadTopicProperty(childElement, 2);
                            }
                        }
                    }
                }
            } catch (FileNotFoundException e) {
                Console.Error.WriteLine("Could not load properties file: {0}", e.Message);
            }
        }

        private Options ParseCommandOptions(string[] args)
        {
            Options options = new Options();

            for (int i = 0; i < args.Length; i++) {
                switch (args[i]) {
                    case "-c":
                        i++;
                        if (i < args.Length) {
                            string c = args[i];
                            if (c.Equals("treatment", StringComparison.CurrentCultureIgnoreCase)) {
                                options.Condition = Condition.Treatment;
                            } else if (c.Equals("control", StringComparison.CurrentCultureIgnoreCase)) {
                                options.Condition = Condition.Control;
                            } else {
                                Console.Error.WriteLine("Error: {0} is not a valid condition. Use the -h option to view valid conditions.", c);
                                Environment.Exit(1);
                            }
                        } else {
                            Console.Error.WriteLine("Error: No condition specified");
                            Environment.Exit(1);
                        }
                        break;
                    case "-d":
                        options.DevMode = true;
                        break;
                    case "-f":
                        options.FullScreen = true;
                        break;
                    case "-h":
                        options.ShowHelp = true;
                        break;
                    case "-i":
                        i++;
                        if (i < args.Length) {
                            options.EvalInterval = int.Parse(args[i]);
                        } else {
                            Console.Error.WriteLine("Error: No evaluation interval specified");
                            Environment.Exit(1);
                        }
                        break;
                    case "-m":
                        i++;
                        if (i < args.Length) {
                            string m = args[i];
                            if (m.Equals("study", StringComparison.CurrentCultureIgnoreCase)) {
                                options.Mode = Mode.Study;
                            } else if (m.Equals("tutorial", StringComparison.CurrentCultureIgnoreCase)) {
                                options.Mode = Mode.Tutorial;
                            } else {
                                Console.Error.WriteLine("Error: {0} is not a valid mode. Use the -h option to view valid mode.", m);
                                Environment.Exit(1);
                            }
                        } else {
                            Console.Error.WriteLine("Error: No mode specified");
                            Environment.Exit(1);
                        }
                        break;
                    case "-ow":
                        options.OverWriteLog = true;
                        break;
                    case "-p":
                        i++;
                        if (i < args.Length) {
                            string id = args[i];
                            options.UserId = id;
                        } else {
                            Console.Error.WriteLine("Error: No user ID specified");
                            Environment.Exit(1);
                        }
                        break;
                    case "-t":
                        i++;
                        if (i < args.Length) {
                            options.TimeLimit = int.Parse(args[i]) * 60; // Convert minutes to seconds
                        } else {
                            Console.Error.WriteLine("Error: No time limit specified");
                            Environment.Exit(1);
                        }
                        break;
                    case "-v":
                        options.ShowVersion = true;
                        break;
                    default:
                        Console.Error.WriteLine("Error: Unknown argument '{0}'.", args[i]);
                        options.ShowHelp = true;
                        break;
                }
            }

            return options;
        }

        private void LogExceptionAndExit(Exception e, string source)
        {
            if (!(bool)this.Properties[PropertyKey.DevMode]) {
                if (_crashed) {
                    // If we've already crashed, don't do anything
                    return;
                } else {
                    _crashed = true;
                }
            }

            //Console.WriteLine("runtimeTimer elapsed");
            Logger l = new Logger(this.Properties[PropertyKey.UserId].ToString(), this.Properties[PropertyKey.Mode].ToString(), false, true, false);
            l.Writer.WriteStartDocument();
            l.Writer.WriteStartElement("MessagePredictorCrashLog");

            l.Writer.WriteStartElement("Source");
            l.Writer.WriteValue(source);
            l.Writer.WriteEndElement();

            l.Writer.WriteStartElement("Exception");
            l.Writer.WriteValue(e.Message);
            l.Writer.WriteEndElement();

            l.Writer.WriteStartElement("InnerException");
            if (e.InnerException != null) {
                l.Writer.WriteValue(e.InnerException.Message);
            }
            l.Writer.WriteEndElement();

            l.Writer.WriteStartElement("StackTrace");
            l.Writer.WriteValue(e.StackTrace);
            l.Writer.WriteEndElement();

            l.Writer.WriteStartElement("TargetSite");
            l.Writer.WriteValue(e.TargetSite.ToString());
            l.Writer.WriteEndElement();

            l.Writer.WriteEndElement(); // End root element
            l.Writer.WriteEndDocument();
            l.Writer.Close();

            Dialog d = new Dialog();
            d.DialogTitle = "Oh noes!";
            string msg = "Unknown error :(";
            if (e != null) {
                msg = e.Message;
            }
            d.DialogMessage = string.Format("Something went wrong horribly wrong. Please flag down a helper and show them this error:\n\n{0}\n\nSource: {1}\n\nTargetSite: {2}", 
                msg, source, e.TargetSite.ToString());
            d.Owner = App.Current.MainWindow;
            d.ShowDialog();
            if (!(bool)this.Properties[PropertyKey.DevMode]) {
                App.Current.MainWindow.Close();
            }
        }

        #region Event handlers

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            Window window = sender as Window;
            MessagePredictorViewModel vm = window.DataContext as MessagePredictorViewModel;
            vm.SelectDefaultMessage();
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogExceptionAndExit(e.Exception, "DispatcherUnhandledException");
        }

        private void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            LogExceptionAndExit(e.Exception, "FirstChanceException");
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogExceptionAndExit(e.ExceptionObject as Exception, "UnhandledException");
        }

        #endregion
    }
}
