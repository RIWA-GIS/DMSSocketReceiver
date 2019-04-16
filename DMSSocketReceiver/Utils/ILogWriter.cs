namespace DMSSocketReceiver.Utils
{
    /// <summary>
    /// writer which outputs the messages to a destination.
    /// </summary>
    public interface ILogWriter
    {
        /// <summary>
        /// writes the message to the destination.
        /// </summary>
        /// <param name="message">the message to output</param>
        void WriteMessage(string message);
    }
}
