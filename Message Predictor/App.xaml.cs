using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace MessagePredictor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public enum Condition
        {
            Control,
            Treatment
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LoadPropertiesFile();
        }

        /// <summary>
        /// Figure out if we're running as "control" or a treatment.
        /// </summary>
        /// <param name="element">The XML element containing the desired condition.</param>
        private void LoadConditionProperty(XElement element)
        {
            string conditionString = element.Value.ToString();
            Condition condition = Condition.Control;
            if (conditionString.Equals("treatment", StringComparison.InvariantCultureIgnoreCase))
                condition = Condition.Treatment;

            this.Properties["condition"] = condition;
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

            this.Properties["timelimit"] = (minutes * 60) + seconds;
        }

        /// <summary>
        /// Figure out which train and test data to load.
        /// </summary>
        /// <param name="element">The XML element containing information about the desired dataset.</param>
        /// <param name="propertyPrefix"></param>
        private void LoadTopicProperty(XElement element, string propertyPrefix)
        {
            int trainSize = 0;
            int testSize = 0;
            int vocabSize = 0;
            string systemLabel = null;
            string userLabel = null;
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

            this.Properties[propertyPrefix + "TrainSize"] = trainSize;
            this.Properties[propertyPrefix + "TestSize"] = testSize;
            this.Properties[propertyPrefix + "VocabSize"] = vocabSize;
            this.Properties[propertyPrefix + "SystemLabel"] = systemLabel;
            this.Properties[propertyPrefix + "UserLabel"] = userLabel;
        }

        /// <summary>
        /// Load an XML file containing various runtime properties.
        /// </summary>
        private void LoadPropertiesFile()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "properties.xml");
            try
            {
                XDocument xdoc = XDocument.Load(path);
                foreach (XElement element in xdoc.Root.Elements())
                {
                    if (element.Name == "Condition") // Are we running control or treatment?
                    {
                        LoadConditionProperty(element);
                    }
                    else if (element.Name == "TimeLimit") // How long should we let the program run?
                    {
                        LoadTimeLimitProperty(element);
                    }
                    else if (element.Name == "DataSet")
                    {
                        foreach (XElement childElement in element.Elements())
                        {
                            if (childElement.Name == "Topic1")
                            {
                                LoadTopicProperty(childElement, "Topic1");
                            }
                            else if (childElement.Name == "Topic2")
                            {
                                LoadTopicProperty(childElement, "Topic2");
                            }
                        }
                    }
                }
            }
            catch (FileNotFoundException e)
            {
                Console.Error.WriteLine("Could not load properties file: {0}", e.Message);
            }
        }
    }
}
