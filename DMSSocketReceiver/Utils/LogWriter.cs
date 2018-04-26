using System;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace DMSSocketReceiver.Utils
{
    public class LogWriter
    {
        public delegate void UpdateTextCallback(string message);
        private TextBoxBase textBlock1;

        public LogWriter() : this(null)
        {
        }

        public LogWriter(TextBoxBase textBlock1)
        {
            this.textBlock1 = textBlock1;
        }

        public void WriteMessage(string message)
        {
            Console.WriteLine(message);
            if (textBlock1 != null)
                textBlock1.Dispatcher.Invoke(new UpdateTextCallback(this.UpdateText), new Object[] { message });
        }

        private void UpdateText(string message)
        {
            textBlock1.AppendText(message + "\n");
        }
    }
}