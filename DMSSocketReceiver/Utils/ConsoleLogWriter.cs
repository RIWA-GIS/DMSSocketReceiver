using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DMSSocketReceiver.Utils
{
    public class ConsoleLogWriter : ILogWriter
    {
        public void WriteMessage(string message)
        {
            Console.WriteLine(message);
        }
    }
}
