using System;
using System.Collections.Generic;
using System.Threading;

namespace DMSSocketReceiver.network
{
    /// <summary>
    /// Description of a session. Contains session key, workload with payload and results.
    /// </summary>
    public class SessionDesc
    {
        public enum SessionStatus { DONE, WAIT };
        public string Command { get; private set; }
        public string Key { get; private set; }
        public IDictionary<string, object> Payload { get; private set; }
        public SessionStatus Status { get; set; }
        public Dictionary<string, object> ReturnMap { get; private set; }
        public DateTime StartTime { get; private set; }
        private readonly TimeSpan OLD_MILLIS = new TimeSpan(1, 0, 0); // 1 hour, 0 min, 0 sec

        /// <summary>
        /// creates a new instance of SessionDesc, a SessionKey is created automatically.
        /// </summary>
        /// <param name="command">the command to execute within this session.</param>
        /// <param name="payload">the payload (parameters) for the command.</param>
        public SessionDesc(string command, IDictionary<string, object> payload)
        {
            this.Command = command;
            this.Key = Guid.NewGuid().ToString();
            this.Payload = payload;
            this.Status = SessionStatus.WAIT;
            this.ReturnMap = new Dictionary<string, object>();
            this.StartTime = DateTime.Now;
        }

        /// <summary>
        /// checks if the session is old (older than one hour).
        /// </summary>
        /// <returns>true, if session is old.</returns>
        public bool IsOld()
        {
            return (DateTime.Now - StartTime) > OLD_MILLIS;
        }

    }

    /// <summary>
    /// thread safe collection of SessionDesc objects.
    /// </summary>
    public class SessionDescCollection
    {
        private IList<SessionDesc> dicSessions
                = new List<SessionDesc>();

        /// <summary>
        /// retrieves the next unworked session object from the list of sessions.
        /// </summary>
        /// <returns>next SessionDesc if available, null otherwise.</returns>
        public SessionDesc RetrieveNextUnworkedSession()
        {
            lock (dicSessions)
            {
                if (dicSessions.Count == 0)
                {
                    // wait for pulse or go on in 2 secs.
                    Monitor.Wait(dicSessions, 2000);
                }

                if (dicSessions.Count > 0)
                {
                    foreach (SessionDesc currSession in dicSessions)
                    {
                        if (currSession.Status == SessionDesc.SessionStatus.WAIT)
                        {
                            return currSession;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// clears the session queue.
        /// </summary>
        public void Clear()
        {
            lock (dicSessions)
            {
                dicSessions.Clear();
            }
        }

        /// <summary>
        /// adds a SessionDesc to the list of sessions.
        /// </summary>
        /// <param name="sessionDesc">the SessionDesc to add.</param>
        internal void Add(SessionDesc sessionDesc)
        {
            if (sessionDesc != null)
            {
                lock (dicSessions)
                {
                    this.dicSessions.Add(sessionDesc);
                    Monitor.Pulse(dicSessions);
                }
            }
        }

        public void RemoveSessionByKey(string sessionKey)
        {
            if (sessionKey != null)
            {
                lock (dicSessions)
                {
                    ICollection<SessionDesc> toDelete = new List<SessionDesc>();
                    foreach (SessionDesc currSesssionDesc in dicSessions)
                    {
                        if (sessionKey.Equals(currSesssionDesc.Key))
                        {
                            toDelete.Add(currSesssionDesc);
                        }
                    }
                    foreach (SessionDesc currSessionDescDelete in toDelete)
                    {
                        dicSessions.Remove(currSessionDescDelete);
                    }
                }
            }
        }

        public SessionDesc FindSessionByKey(string sessionKey)
        {
            if (sessionKey != null)
            {
                lock (dicSessions)
                {
                    foreach (SessionDesc currSesssionDesc in dicSessions)
                    {
                        if (sessionKey.Equals(currSesssionDesc.Key))
                        {
                            return currSesssionDesc;
                        }
                    }
                }
            }
            return null;
        }

    }

}