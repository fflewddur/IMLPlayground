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
    public class Logger
    {
        XmlWriter _writer;

        public Logger(string userId, string mode, bool overwrite, bool crash)
        {
            // Is this a crash log or a regular user action log?
            string prefix = "log";
            if (crash) {
                prefix = "crash";
            }

            // If our log directory doesn't exist, create it.
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, App.LogDir);
            if (!Directory.Exists(logDir)) {
                Directory.CreateDirectory(logDir);
            }

            // Create a new log file
            if (string.IsNullOrWhiteSpace(userId)) {
                userId = "dev";
            }
            string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, App.LogDir, string.Format("{2}-{0}-{1}.xml", userId, mode, prefix));
            string newFile = logFile;

            // If the file already exists, move it out of the way
            int i = 1;
            while (File.Exists(newFile) && !overwrite) {
                newFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, App.LogDir, string.Format("{3}-{0}-{1}-{2}.xml", userId, mode, i, prefix));
                i++;
            }
            if (!overwrite && i > 1) {
                File.Move(logFile, newFile);
            }

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "\t";

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
