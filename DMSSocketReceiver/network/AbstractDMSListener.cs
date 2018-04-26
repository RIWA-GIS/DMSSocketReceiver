using DMSSocketReceiver.dmshandler;
using DMSSocketReceiver.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DMSSocketReceiver.network
{
    public abstract class AbstractDMSListener : IDMSListener
    {
        private const string KEY_DMSID = "dmsid";
        private const string KEY_ORIGINKEY = "originkey";
        private const string KEY_INSERTABLE = "insertable";
        private const string KEY_ATTACHABLE = "attachable";
        private const string KEY_DOCUMENTNAME = "documentname";
        private const string KEY_FILEPATH = "filepath";
        private const string KEY_METADATA = "metadata";

        public LogWriter writer { get; private set; }
        public IDMSHandler handler { get; private set; }

        protected AbstractDMSListener(LogWriter writerParam, IDMSHandler handler)
        {
            this.handler = handler;
            this.writer = writerParam == null ? new LogWriter() { } : writerParam;
        }

        public abstract void StartListening(int port);

        public abstract void StopListening();

        protected IDictionary<string, dynamic> readPayload(dynamic jsonpayload)
        {
            IDictionary<string, dynamic> payload = new Dictionary<string, dynamic>();
            if (jsonpayload != null)
            {
                foreach (dynamic currKVPair in jsonpayload)
                {
                    payload.Add(currKVPair.Name, currKVPair.Value);
                }
            }
            writer.WriteMessage(String.Format("received payload; entries: {0}", payload.Count));
            return payload;

        }

        protected IDictionary<string, string> readMetadata(IDictionary<string, dynamic> payload)
        {
            IDictionary<string, string> metadata = new Dictionary<string, string>();
            if (payload!=null && payload[KEY_METADATA] != null)
            {
                foreach (dynamic currMetadata in payload[KEY_METADATA])
                {
                    metadata.Add(currMetadata.Name, currMetadata.Value.ToString());
                }
            }
            writer.WriteMessage(String.Format("received metadata {0}", metadata));
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
                writer.WriteMessage(String.Format("Read {0} bytes from socket. \n Data : {1}",
                    strippedContent.Length, strippedContent));
                dynamic jsonContent = JsonConvert.DeserializeObject(strippedContent);
                string command = jsonContent.command;
                writer.WriteMessage(String.Format("Command: {0}", command));
                ret["command"] = command;
                IDictionary<string, dynamic> input_payload = readPayload(jsonContent.payload);
                switch (command.ToLower())
                {
                    case "readcapabilities":
                        {
                            DMSCapabilities capa = handler.DetermineCapabilities();
                            ret["status"] = "OK";
                            payload[KEY_INSERTABLE] = capa.insertable;
                            payload[KEY_ATTACHABLE] = capa.attachable;
                        }
                        break;
                    case "insert":
                        {
                            string filepath = input_payload[KEY_FILEPATH];
                            string originKey = input_payload[KEY_ORIGINKEY];
                            IDictionary<String, String> metadata = readMetadata(input_payload);

                            writer.WriteMessage(String.Format("inserting; file '{0}', originkey: '{1}'", filepath, originKey));
                            DMSDocument selDocument = handler.InsertDocument(originKey, filepath, metadata);
                            if (selDocument != null)
                            {
                                writer.WriteMessage(String.Format("returned document '{0}'", selDocument));
                                payload[KEY_DMSID] = selDocument.id;
                                payload[KEY_DOCUMENTNAME] = selDocument.name;
                                payload[KEY_ORIGINKEY] = originKey;
                                ret["status"] = "OK";
                            }
                            else
                            {
                                ret["status"] = "CANCEL";
                            }
                        }
                        break;
                    case "attach":
                        {
                            string originKey = input_payload[KEY_ORIGINKEY];
                            IDictionary<String, String> metadata = readMetadata(jsonContent.metadata);

                            writer.WriteMessage(String.Format("attaching; originkey: '{0}'", originKey));
                            DMSDocument selDocument = handler.AttachDocument(originKey, metadata);
                            if (selDocument != null)
                            {
                                writer.WriteMessage(String.Format("returned document '{0}'", selDocument));
                                payload[KEY_DMSID] = selDocument.id;
                                payload[KEY_DOCUMENTNAME] = selDocument.name;
                                payload[KEY_ORIGINKEY] = originKey;
                                ret["status"] = "OK";
                            }
                            else
                            {
                                ret["status"] = "CANCEL";
                            }
                        }
                        break;
                    case "show":
                        {
                            string idOfFileInDMS_show = input_payload[KEY_DMSID];
                            string nameOfDMSDocument = input_payload[KEY_DOCUMENTNAME];
                            string originKey = input_payload[KEY_ORIGINKEY];
                            DMSDocument documentToShow = new DMSDocument(idOfFileInDMS_show, nameOfDMSDocument);
                            writer.WriteMessage(String.Format("show document '{0}'", documentToShow));
                            handler.ShowDocument(documentToShow);
                            ret["status"] = "OK";
                        }
                        break;
                    default:
                        {
                            // Echo the data back to the client.  
                            ret["status"] = "FAIL";
                            ret["errortext"] = "Unknown command";
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                writer.WriteMessage("processing error: " + ex.Message);
                ret["status"] = "FAIL";
                ret["errortext"] = ex.Message;
            }

            return ret;
        }

    }


}
