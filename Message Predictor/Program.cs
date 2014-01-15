using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace MessagePredictor
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Console.WriteLine("hello");
            LoadProperties();
            Application app = new Application();
            MessagePredictorWindow window = new MessagePredictorWindow();
            //window.DataContext = vm;
            window.Show();
            window.Activate();
            app.Run(window);
        }

        /// <summary>
        /// Load an XML file containing various runtime properties.
        /// </summary>
        public static void LoadProperties()
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
