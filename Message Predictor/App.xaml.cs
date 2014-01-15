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
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LoadPropertiesFile();
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
                    if (element.Name == "Condition")
                    {
                        // TODO set condition
                    }
                    else if (element.Name == "TimeLimit")
                    {
                        // TODO set time limit
                    }
                    else if (element.Name == "DataSet")
                    {
                        foreach (XElement childElement in element.Elements())
                        {
                            if (childElement.Name == "Topic1")
                            {
                                // TODO set topic1
                            }
                            else if (childElement.Name == "Topic2")
                            {
                                // TODO set topic2
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
