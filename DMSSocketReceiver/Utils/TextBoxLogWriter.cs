using System;
using System.Windows.Controls.Primitives;

namespace DMSSocketReceiver.Utils
{
    /// <summary>
    /// writer which outputs the messages to a textbox.
    /// since it inherits ConsoleLogWriter, it will also output to console.
    /// </summary>
    public class TextBoxLogWriter : ConsoleLogWriter
    {
        private TextBoxBase outputTextBox;

        /// <summary>
        /// creates a new TextBoxLogWriter, which will output to the TextBox.
        /// </summary>
        /// <param name="outputTextBox">the TextBox to output to</param>
        public TextBoxLogWriter(TextBoxBase outputTextBox)
        {
            this.outputTextBox = outputTextBox ?? throw new NullReferenceException();
        }

        /// <summary>
        /// writes the message to the box (if available)
        /// </summary>
        /// <param name="message">the message to output</param>
        public new void WriteMessage(string message)
        {
            base.WriteMessage(message);
            outputTextBox.Dispatcher.Invoke(new Action(() =>
            {
                outputTextBox.AppendText(message + "\n");
                outputTextBox.ScrollToEnd();
            })
            );
        }

    }
}