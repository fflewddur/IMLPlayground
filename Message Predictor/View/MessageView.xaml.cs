﻿using LibIML;
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
using System.Xml.Linq;
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
            XDocument doc = XDocument.Parse(text);
            document.Blocks.Clear();

            Paragraph p = new Paragraph();
            foreach (XElement line in doc.Root.Elements())
            {
                if (line.Name == "subject")
                {
                    Bold b = new Bold(new Run(line.Value.ToString()));
                    p.Inlines.Add(b);
                    p.Inlines.Add(new LineBreak());
                }
                else if (line.Name == "sender") 
                {
                    p.Inlines.Add("From: ");
                    Italic em = new Italic(new Run(line.Value.ToString()));
                    p.Inlines.Add(em);
                    p.Inlines.Add(new LineBreak());
                }
                else
                {
                    p.Inlines.Add(line.Value.ToString());
                    p.Inlines.Add(new LineBreak());
                }
            }

            document.Blocks.Add(p);
        }
    }
}
