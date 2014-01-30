using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;

namespace MessagePredictor
{
    /// <summary>
    /// Interaction logic for MessageView.xaml
    /// </summary>
    public partial class MessageView : UserControl
    {
        public MessageView()
        {
            InitializeComponent();
        }
    }

    public class MyFormatter : ITextFormatter
    {
        public string GetText(System.Windows.Documents.FlowDocument document)
        {
            return new TextRange(document.ContentStart, document.ContentEnd).Text;
        }

        public void SetText(System.Windows.Documents.FlowDocument document, string text)
        {
            string[] lines = text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

            document.Blocks.Clear();

            if (lines.Length > 1)
            {
                // Get the Subject (1st line)
                Paragraph p = new Paragraph();
                Bold bTxt = new Bold();
                bTxt.Inlines.Add(lines[0]);
                p.Inlines.Add(bTxt);
                p.Inlines.Add(new LineBreak());

                // Get the Sender (2nd line)
                Italic iTxt = new Italic();
                p.Inlines.Add("From ");
                iTxt.Inlines.Add(lines[1]);
                p.Inlines.Add(iTxt);

                document.Blocks.Add(p);

                // Get everything else
                p = new Paragraph();
                for (int i = 2; i < lines.Length; i++)
                {
                    p.Inlines.Add(lines[i]);
                    p.Inlines.Add(new LineBreak());
                }

                document.Blocks.Add(p);
            }
        }
    }
}
