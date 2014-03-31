﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Xml.Linq;
using Xceed.Wpf.Toolkit;

namespace MessagePredictor.View
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

        private void RichTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            Xceed.Wpf.Toolkit.RichTextBox tb = sender as Xceed.Wpf.Toolkit.RichTextBox;
            MessagePredictorViewModel vm = tb.DataContext as MessagePredictorViewModel;
            vm.FeatureSetVM.HighlightFeature.Execute(tb.Selection.Text.Trim());
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            MessagePredictorViewModel vm = this.DataContext as MessagePredictorViewModel;
            if (vm != null && e.VerticalChange != 0) {
                vm.LogMessageScrolled(e.VerticalChange, e.VerticalOffset);
            }
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
            document.Blocks.Clear();

            if (string.IsNullOrWhiteSpace(text))
                return;

            XDocument doc = XDocument.Parse(text);

            Paragraph p = new Paragraph();
            foreach (XElement line in doc.Root.Elements()) {
                if (line.Name == "sender") {
                    p.Inlines.Add("From: ");
                }

                foreach (XElement element in line.Elements()) {
                    if (element.Name == "normal") {
                        if (line.Name == "subject") {
                            Bold b = new Bold(new Run(element.Value.ToString()));
                            p.Inlines.Add(b);
                        } else if (line.Name == "sender") {
                            Italic em = new Italic(new Run(element.Value.ToString()));
                            p.Inlines.Add(em);
                        } else {
                            p.Inlines.Add(element.Value.ToString());
                        }
                    } else if (element.Name == "feature") {
                        if (line.Name == "subject") {
                            Bold b = new Bold(new Run(element.Value.ToString()));
                            b.Background = SystemColors.ActiveCaptionBrush;
                            p.Inlines.Add(b);
                        } else if (line.Name == "sender") {
                            Italic em = new Italic(new Run(element.Value.ToString()));
                            em.Background = SystemColors.ActiveCaptionBrush;
                            p.Inlines.Add(em);
                        } else {
                            Run r = new Run(element.Value.ToString());
                            r.Background = SystemColors.ActiveCaptionBrush;
                            p.Inlines.Add(r);
                        }
                    } else if (element.Name == "highlightedFeature") {
                        if (line.Name == "subject") {
                            Bold b = new Bold(new Run(element.Value.ToString()));
                            b.Background = SystemColors.HighlightBrush;
                            b.Foreground = SystemColors.HighlightTextBrush;
                            p.Inlines.Add(b);
                        } else if (line.Name == "sender") {
                            Italic em = new Italic(new Run(element.Value.ToString()));
                            em.Background = SystemColors.HighlightBrush;
                            em.Foreground = SystemColors.HighlightTextBrush;
                            p.Inlines.Add(em);
                        } else {
                            Run r = new Run(element.Value.ToString());
                            r.Background = SystemColors.HighlightBrush;
                            r.Foreground = SystemColors.HighlightTextBrush;
                            p.Inlines.Add(r);
                        }
                    }
                }

                p.Inlines.Add(new LineBreak());
                if (line.Name == "sender") {
                    p.Inlines.Add(new LineBreak());
                }
            }

            document.Blocks.Add(p);
        }
    }
}
