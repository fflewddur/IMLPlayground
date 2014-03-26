using MessagePredictor.Model;
using MessagePredictor.View;
using System;
using System.IO;
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
            Control,
            Treatment
        }

        // Use these to access Application.Properties items.
        public enum PropertyKey
        {
            Unknown,
            Condition,
            TimeLimit,
            DatasetFile,
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
            AutoUpdatePredictions
        }

        private Logger _logger;
        MessagePredictorViewModel _vm;

        // Called on Application startup. Handle any command line arguments, load our configuration properties, 
        // and then build the ViewModel and View.
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _logger = new Logger();
            _logger.Writer.WriteStartDocument();
            _logger.Writer.WriteStartElement("MessagePredictorLog");

            LoadPropertiesFile();
            _logger.Writer.WriteAttributeString("condition", this.Properties[PropertyKey.Condition].ToString());
            _logger.Writer.WriteAttributeString("dataset", this.Properties[PropertyKey.DatasetFile].ToString());
            _logger.Writer.WriteAttributeString("autoupdate", this.Properties[PropertyKey.AutoUpdatePredictions].ToString());
            _logger.Writer.WriteAttributeString("timelimit", this.Properties[PropertyKey.TimeLimit].ToString());
            _logger.Writer.WriteAttributeString("system", Environment.OSVersion.ToString());
            _logger.Writer.WriteAttributeString("cpus", Environment.ProcessorCount.ToString());
            _logger.Writer.WriteAttributeString("runtime", Environment.Version.ToString());

            // Use a longer tooltip timeout (20 seconds)
            ToolTipService.ShowDurationProperty.OverrideMetadata(
                typeof(DependencyObject), new FrameworkPropertyMetadata(20000));

            _vm = new MessagePredictorViewModel(_logger);
            var window = new MessagePredictorWindow();
            window.DataContext = _vm;
            window.Loaded += window_Loaded;
            window.Show();
            _logger.Writer.WriteStartElement("WindowOpen");
            _logger.Writer.WriteAttributeString("time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            _logger.Writer.WriteEndElement();
            _logger.Writer.WriteStartElement("UserActions");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            _logger.Writer.WriteEndElement(); // End <actions/> element
            _logger.Writer.WriteStartElement("WindowClose");
            _logger.Writer.WriteAttributeString("time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            _logger.Writer.WriteEndElement();

            // Log the feature set and training set
            _vm.LogFeatureSet();
            _vm.LogTrainingSet();

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
        private void LoadPropertiesFile()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DataDir, "Properties.xml");
            try {
                XDocument xdoc = XDocument.Load(path);
                foreach (XElement element in xdoc.Root.Elements()) {
                    if (element.Name == "Condition") // Are we running control or treatment?
                    {
                        LoadConditionProperty(element);
                    } else if (element.Name == "TimeLimit") // How long should we let the program run?
                    {
                        LoadTimeLimitProperty(element);
                    } else if (element.Name == "DataSet") {
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
                    }
                }
            } catch (FileNotFoundException e) {
                Console.Error.WriteLine("Could not load properties file: {0}", e.Message);
            }
        }

        #region Event handlers

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            Window window = sender as Window;
            MessagePredictorViewModel vm = window.DataContext as MessagePredictorViewModel;
            vm.SelectDefaultMessage();
        }

        #endregion
    }
}
