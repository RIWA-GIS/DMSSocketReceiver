using DMSSocketReceiver.dmshandler;
using DMSSocketReceiver.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DMSSocketReceiver.network
{
    public abstract class AbstractDMSListener : IDMSListener
    {

        private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(AbstractDMSListener));

        private const string KEY_DMSID = "dmsid";
        private const string KEY_ORIGINKEY = "originkey";
        private const string KEY_INSERTABLE = "insertable";
        private const string KEY_ATTACHABLE = "attachable";
        private const string KEY_DOCUMENTNAME = "documentname";
        private const string KEY_FILEPATH = "filepath";
        private const string KEY_METADATA = "metadata";
        public const string KEY_ACTIONSESSION = "session";
        public const string KEY_COMMAND = "command";
        public const string KEY_PAYLOAD = "payload";
        public const string KEY_STATUS = "status";
        public const string KEY_ERRORTEXT = "errortext";
        private const string KEY_STATUS_OK = "OK";
        private const string KEY_STATUS_CANCEL = "CANCEL";
        private const string KEY_STATUS_WAIT = "WAIT";
        private SessionDescCollection sessions = new SessionDescCollection();
        public ILogWriter Writer { get; private set; }
        public IDMSHandler Handler { get; private set; }

        private readonly Thread trWorker;
        private bool running = true;

        public event EventHandler CommandReceivedEvent;
        public event EventHandler CommandFinishedEvent;
        public event EventHandler CommandErrorEvent;

        protected AbstractDMSListener(ILogWriter writerParam, IDMSHandler handler)
        {
            this.Handler = handler;
            this.Writer = writerParam ?? new ConsoleLogWriter() { };
            this.trWorker = new Thread(() =>
          {
              while (running)
              {
                  SessionDesc nextSessionDesc = sessions.RetrieveNextUnworkedSession();
                  if (nextSessionDesc != null)
                  {
                      IDictionary<string, dynamic> input_payload = nextSessionDesc.Payload;
                      IDictionary<string, object> ret = nextSessionDesc.ReturnMap;
                      IDictionary<string, object> payload = new Dictionary<string, object>();
                      ret[KEY_PAYLOAD] = payload;
                      try
                      {
                          switch (nextSessionDesc.Command)
                          {
                              case "insert":
                                  {
                                      string filepath = input_payload[KEY_FILEPATH];
                                      string originKey = input_payload[KEY_ORIGINKEY];
                                      IDictionary<String, String> metadata = ReadMetadata(input_payload);

                                      Writer.WriteMessage(String.Format("inserting; file '{0}', originkey: '{1}'", filepath, originKey));
                                      DMSDocument selDocument = Handler.InsertDocument(originKey, filepath, metadata);
                                      if (selDocument != null)
                                      {
                                          Writer.WriteMessage(String.Format("returned document '{0}'", selDocument));
                                          payload[KEY_DMSID] = selDocument.Id;
                                          payload[KEY_DOCUMENTNAME] = selDocument.Name;
                                          payload[KEY_ORIGINKEY] = originKey;
                                          ret[KEY_STATUS] = KEY_STATUS_OK;
                                      }
                                      else
                                      {
                                          ret[KEY_STATUS] = KEY_STATUS_CANCEL;
                                      }
                                      nextSessionDesc.Status = SessionDesc.SessionStatus.DONE;
                                  }
                                  break;
                              case "attach":
                                  {
                                      string originKey = input_payload[KEY_ORIGINKEY];
                                      IDictionary<String, String> metadata = ReadMetadata(input_payload);

                                      Writer.WriteMessage(String.Format("attaching; originkey: '{0}'", originKey));
                                      DMSDocument selDocument = Handler.AttachDocument(originKey, metadata);
                                      if (selDocument != null)
                                      {
                                          Writer.WriteMessage(String.Format("returned document '{0}'", selDocument));
                                          payload[KEY_DMSID] = selDocument.Id;
                                          payload[KEY_DOCUMENTNAME] = selDocument.Name;
                                          payload[KEY_ORIGINKEY] = originKey;
                                          ret[KEY_STATUS] = KEY_STATUS_OK;
                                      }
                                      else
                                      {
                                          ret[KEY_STATUS] = KEY_STATUS_CANCEL;
                                      }
                                      nextSessionDesc.Status = SessionDesc.SessionStatus.DONE;
                                  }
                                  break;
                              case "show":
                                  {
                                      string idOfFileInDMS_show = input_payload[KEY_DMSID];
                                      string nameOfDMSDocument = input_payload[KEY_DOCUMENTNAME];
                                      string originKey = input_payload[KEY_ORIGINKEY];
                                      DMSDocument documentToShow = new DMSDocument(idOfFileInDMS_show, nameOfDMSDocument);
                                      Writer.WriteMessage(String.Format("show document '{0}'", documentToShow));
                                      Handler.ShowDocument(documentToShow);
                                      ret[KEY_STATUS] = KEY_STATUS_OK;
                                      nextSessionDesc.Status = SessionDesc.SessionStatus.DONE;
                                  }
                                  break;
                              default:
                                  {

                                  }
                                  break;
                          }

                      }
                      catch (Exception ex)
                      {
                          LOG.Error("processing error on session " + nextSessionDesc.Key + ": " + ex.Message, ex);
                          Writer.WriteMessage("processing error on session " + nextSessionDesc.Key + ": " + ex.Message);
                          ret[KEY_STATUS] = "FAIL";
                          ret[KEY_ERRORTEXT] = ex.Message;
                      }
                  }
              }
          }
        )
            {
                IsBackground = true
            };
            trWorker.Start();

        }

        public abstract void StartListeningInternal(int port);
        public void StartListening(int port)
        {
            StartListeningInternal(port);
        }

        public abstract void StopListeningInternal();
        public void StopListening()
        {
            running = false;
            StopListeningInternal();
            sessions.Clear();
        }

        protected IDictionary<string, dynamic> ReadPayload(dynamic jsonpayload)
        {
            IDictionary<string, dynamic> payload = new Dictionary<string, dynamic>();
            if (jsonpayload != null)
            {
                foreach (dynamic currKVPair in jsonpayload)
                {
                    payload.Add(currKVPair.Name, currKVPair.Value);
                }
            }
            LOG.DebugFormat("received payload; entries: {0}", payload.Count);
            return payload;

        }

        protected IDictionary<string, string> ReadMetadata(IDictionary<string, dynamic> payload)
        {
            IDictionary<string, string> metadata = new Dictionary<string, string>();
            if (payload != null && payload[KEY_METADATA] != null)
            {
                foreach (dynamic currMetadata in payload[KEY_METADATA])
                {
                    metadata.Add(currMetadata.Name, currMetadata.Value.ToString());
                }
            }
            LOG.DebugFormat("received metadata {0}", metadata);
            return metadata;
        }

        protected IDictionary<string, object> HandleContent(string strippedContent)
        {
            IDictionary<string, object> ret = new Dictionary<string, object>();
            IDictionary<string, object> payload = new Dictionary<string, object>();
            ret["payload"] = payload;

            try
            {    // All the data has been read from the   
                 // client. Display it on the console.  
                LOG.DebugFormat("Read {0} bytes from socket. \n Data : {1}",
                    strippedContent.Length, strippedContent);
                dynamic jsonContent = JsonConvert.DeserializeObject(strippedContent);
                string command = jsonContent.command;
                if (command == null)
                {
                    command = "unknown";
                }


                ret[KEY_COMMAND] = command;
                IDictionary<string, dynamic> input_payload = ReadPayload(jsonContent.payload);

                LOG.Info(String.Format("Command: {0}, Payload: {1}", command, input_payload));
                switch (command.ToLower())
                {
                    case "read":
                        {
                            string sessionKey = input_payload[KEY_ACTIONSESSION];
                            SessionDesc currSession = sessions.FindSessionByKey(sessionKey);
                            if (currSession == null)
                            {
                                Writer.WriteMessage("unknown session " + sessionKey);
                                ret[KEY_STATUS] = "FAIL";
                                ret[KEY_ERRORTEXT] = "Unknown session " + sessionKey;
                                payload[KEY_ACTIONSESSION] = sessionKey;
                            }
                            else
                            {
                                Writer.WriteMessage(string.Format("read session '{0}' (status: {1})", currSession.Key, currSession.Status));
                                switch (currSession.Status)
                                {
                                    case SessionDesc.SessionStatus.DONE:
                                        {
                                            ret.Clear();
                                            foreach (KeyValuePair<string, object> currKVP in currSession.ReturnMap)
                                            {
                                                ret.Add(currKVP);
                                            }
                                            sessions.RemoveSessionByKey(sessionKey);
                                        }
                                        break;
                                    case SessionDesc.SessionStatus.WAIT:
                                        {
                                            ret.Add(KEY_STATUS, KEY_STATUS_WAIT);
                                        }
                                        break;

                                }
                            }
                        }
                        break;
                    case "readcapabilities":
                        {
                            DMSCapabilities capa = Handler.DetermineCapabilities();
                            ret[KEY_STATUS] = KEY_STATUS_OK;
                            payload[KEY_INSERTABLE] = capa.insertable;
                            payload[KEY_ATTACHABLE] = capa.attachable;
                        }
                        break;
                    case "insert":
                    case "attach":
                    case "show":
                        {
                            // potential long runners must be worked on in background
                            SessionDesc currSession = new SessionDesc(command, input_payload);
                            sessions.Add(currSession);
                            ret[KEY_STATUS] = KEY_STATUS_WAIT;
                            payload[KEY_ACTIONSESSION] = currSession.Key;
                            Writer.WriteMessage(string.Format("added session: {0}", currSession.Key));
                        }
                        break;
                    default:
                        {
                            // Echo the data back to the client.  
                            ret[KEY_STATUS] = "FAIL";
                            ret[KEY_ERRORTEXT] = "Unknown command";
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Writer.WriteMessage("processing error: " + ex.Message);
                LOG.Error(string.Format("processing error: {0}", ex.Message), ex);
                ret[KEY_STATUS] = "FAIL";
                ret[KEY_ERRORTEXT] = ex.Message;
            }

            return ret;
        }

        protected virtual void OnCommandReceivedEvent()
        {
            CommandReceivedEvent?.Invoke(this, new CommandEventArgs(CommandEventType.START));
        }
        protected virtual void OnCommandFinishedEvent()
        {
            CommandFinishedEvent?.Invoke(this, new CommandEventArgs(CommandEventType.FINISH));
        }
        protected virtual void OnCommandErrorEvent(Exception e)
        {
            CommandErrorEvent?.Invoke(this, new ErrorCommandEventArgs() { Error = e });
        }

    }


}
