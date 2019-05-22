using System;

namespace DMSSocketReceiver.network
{
    public class BaseCommandEventArgs : EventArgs
    {
        public DateTime EventTime { get; private set; }

        protected BaseCommandEventArgs()
        {
            this.EventTime = DateTime.Now;
        }

    }

    public class ErrorCommandEventArgs : BaseCommandEventArgs
    {
        public Exception Error { get; set; }

    }

    public class CommandEventArgs : BaseCommandEventArgs
    {
        public CommandEventType Type { get; private set; }
        public CommandEventArgs(CommandEventType type)
        {
            this.Type = type;
        }
    }

    public enum CommandEventType
    {
        START, FINISH
    }
}