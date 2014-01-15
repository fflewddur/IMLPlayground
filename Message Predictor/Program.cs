using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Message_Predictor
{
    class Program
    {
        [STAThread]
        public static void Main()
        {
            Console.WriteLine("hello");
            Application app = new Application();
            MessagePredictorWindow window = new MessagePredictorWindow();
            //window.DataContext = vm;
            window.Show();
            window.Activate();
            app.Run(window);
        }
    }
}
