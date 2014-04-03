using LibIML.Features;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MessagePredictor.Model
{
    class Logger
    {
        XmlWriter _writer;

        public Logger()
        {
            // If our log directory doesn't exist, create it.
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, App.LogDir);
            if (!Directory.Exists(logDir)) {
                Directory.CreateDirectory(logDir);
            }

            // Create a new log file
            string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, App.LogDir, "log.xml");
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "\t";
            //XmlWriter logger = XmlWriter.Create(logFile, settings);


            _writer = XmlWriter.Create(logFile, settings);
        }

        public XmlWriter Writer
        {
            get { return _writer; }
            private set { _writer = value; }
        }

        public void logTime()
        {
            _writer.WriteAttributeString("time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        public void logFeature(Feature f)
        {
            _writer.WriteStartElement("Feature");
            _writer.WriteAttributeString("characters", f.Characters);
            _writer.WriteAttributeString("userAdded", f.UserAdded.ToString());
            if (f.MostImportantLabel != null) {
                _writer.WriteAttributeString("mostImportantLabel", f.MostImportantLabel.ToString());
            } else {
                _writer.WriteAttributeString("mostImportantLabel", "");
            }
            logTime();
            logFeatureImportance(f.Topic1Importance);
            logFeatureImportance(f.Topic2Importance);
            
            _writer.WriteEndElement();
        }

        public void logFeatureImportance(FeatureImportance fi)
        {
            _writer.WriteStartElement("FeatureImportance");
            _writer.WriteAttributeString("label", fi.Label.ToString());
            _writer.WriteAttributeString("userHeight", fi.UserHeight.ToString());
            _writer.WriteAttributeString("systemHeight", fi.SystemHeight.ToString());
            _writer.WriteAttributeString("userWeight", fi.UserWeight.ToString());
            _writer.WriteAttributeString("userPrior", fi.UserPrior.ToString());
            _writer.WriteAttributeString("systemWeight", fi.SystemWeight.ToString());
            _writer.WriteEndElement();
        }

        /// <summary>
        /// Close the current XML element and flush the stream to disk. 
        /// </summary>
        public void logEndElement()
        {
            _writer.WriteEndElement();
            _writer.Flush();
        }
    }
}
