using System;
using System.IO;
using System.Text;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using System.Windows.Controls;
using QZ.UserControl;

namespace Microsoft.Windows.Controls
{
    public class HtmlTextBlock : TextBlock
    {
        public static DependencyProperty HtmlProperty = DependencyProperty.Register("Html", typeof(string),
                typeof(HtmlTextBlock), new System.Windows.PropertyMetadata("Html", new PropertyChangedCallback(OnHtmlChanged)));

        public string Html { get { return (string)GetValue(HtmlProperty); } set { SetValue(HtmlProperty, value); } }

        private ITextFormatter _textFormatter;
        /// <summary>
        /// The ITextFormatter the is used to format the text of the RichTextBox.
        /// Deafult formatter is the RtfFormatter
        /// </summary>
        public ITextFormatter TextFormatter
        {
            get
            {
                if (_textFormatter == null)
                    _textFormatter = new HtmlFormatter(); //default is rtf

                return _textFormatter;
            }
            set
            {
                _textFormatter = value;
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            Parse(Html);
        }

        private void Parse(string html)
        {
            Inlines.Clear();
            //html = HtmlToXamlConverter.ConvertHtmlToXaml(html, false);
            //try
            //{
            //    TextRange tr = new TextRange(ContentStart, ContentEnd);
            //    using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(html)))
            //    {
            //        tr.Load(ms, DataFormats.Xaml);
            //    }
            //}
            //catch
            //{
            //    throw new InvalidDataException("data provided is not in the correct Html format.");
            //}
            HtmlTagTree tree = new HtmlTagTree();
            HtmlParser1 parser = new HtmlParser1(tree); //output
            parser.Parse(new StringReader(html));     //input

            HtmlUpdater updater = new HtmlUpdater(this); //output
            updater.Update(tree);
        }

        public static void OnHtmlChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            HtmlTextBlock sender = (HtmlTextBlock)s;
            sender.Parse((string)e.NewValue);
        }

        public HtmlTextBlock()
        {
            Text = "Assign Html Property";
        }

    }

    public class HtmlHighlightTextBlock : TextBlock
    {
        public string Highlight
        {
            get { return (string)GetValue(HighlightProperty); }
            set { SetValue(HighlightProperty, value); }
        }


        public static readonly DependencyProperty HighlightProperty =
        DependencyProperty.Register("Highlight", typeof(string), typeof(HtmlHighlightTextBlock), new UIPropertyMetadata(""));


        public static DependencyProperty HtmlProperty = DependencyProperty.Register("Html", typeof(string),
                typeof(HtmlHighlightTextBlock), new UIPropertyMetadata("Html", new PropertyChangedCallback(OnHtmlChanged)));

        public string Html { get { return (string)GetValue(HtmlProperty); } set { SetValue(HtmlProperty, value); } }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            Parse(Html);
        }

        private void Parse(string html)
        {
            if (!String.IsNullOrEmpty(Highlight))
            {
                int idx = html.IndexOf(Highlight, StringComparison.InvariantCultureIgnoreCase);
                while (idx != -1)
                {
                    html = String.Format("{0}[b]{1}[/b]{2}",
                        html.Substring(0, idx), html.Substring(idx, Highlight.Length), html.Substring(idx + Highlight.Length));
                    idx = html.IndexOf(Highlight, idx + 7 + Highlight.Length, StringComparison.InvariantCultureIgnoreCase);
                }
            }

            Inlines.Clear();
            HtmlTagTree tree = new HtmlTagTree();
            HtmlParser1 parser = new HtmlParser1(tree); //output
            parser.Parse(new StringReader(html));     //input

            HtmlUpdater updater = new HtmlUpdater(this); //output
            updater.Update(tree);
        }

        public static void OnHtmlChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            HtmlHighlightTextBlock sender = (HtmlHighlightTextBlock)s;
            sender.Parse((string)e.NewValue);
        }

        public HtmlHighlightTextBlock()
        {

        }

    }
}
