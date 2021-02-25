using System;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace AemulusModManager
{
    public class ConsoleWriterEventArgs : EventArgs
    {
        public string Value { get; private set; }
        public ConsoleWriterEventArgs(string value)
        {
            Value = value;
        }
    }

    public static class RichTextBoxExtensions
    {
        public static void AppendText(this RichTextBox box, string text, string color)
        {
            BrushConverter bc = new BrushConverter();
            TextRange tr = new TextRange(box.Document.ContentEnd, box.Document.ContentEnd);
            tr.Text = text;
            try
            {
                tr.ApplyPropertyValue(TextElement.ForegroundProperty,
                    bc.ConvertFromString(color));
            }
            catch (FormatException) { }
        }
    }

    public class TextBoxOutputter : TextWriter
    {
        public StreamWriter sw;
        public TextBoxOutputter(StreamWriter streamWriter)
        {
            sw = streamWriter;
        }
        public override Encoding Encoding { get { return Encoding.UTF8; } }

        public override void Write(string value)
        {
            WriteEvent?.Invoke(this, new ConsoleWriterEventArgs(value));
            base.Write(value);
            sw.Write(value);
        }

        public override void WriteLine(string value)
        {
            WriteLineEvent?.Invoke(this, new ConsoleWriterEventArgs(value));
            base.WriteLine(value);
            if (sw != null)
            {
                sw.WriteLine($"{DateTime.Now} {value}");
            }
        }

        // Make sure you call this before you end
        public override void Close()
        {
            if (sw != null)
            {
                sw.Dispose();
                sw = null;
            }
        }

        public event EventHandler<ConsoleWriterEventArgs> WriteEvent;
        public event EventHandler<ConsoleWriterEventArgs> WriteLineEvent;
    }
}