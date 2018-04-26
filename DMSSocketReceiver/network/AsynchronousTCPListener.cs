using DMSSocketReceiver.dmshandler;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DMSSocketReceiver.network
{

    public class AsynchronousTCPListener : AbstractDMSListener
    {
        private static Encoding DEFAULT_ENCODING = Encoding.UTF8;
        private Thread lThread2;
        // Thread signal.  
        private Boolean runTread;

        private TcpListener tcpListener;

        public AsynchronousTCPListener(LogWriter writerParam, IDMSHandler handler) : base(writerParam, handler)
        {

        }



        public override void StartListening(int port)
        {
            if (runTread)
                return;

            IPEndPoint localEndPointL = new IPEndPoint(IPAddress.Any, port);
            tcpListener = new TcpListener(localEndPointL);
            tcpListener.Start();


            lThread2 = new Thread(run2);
            lThread2.Start();

            runTread = true;
        }

        public override void StopListening()
        {
            runTread = false;
            if (tcpListener != null)
            {
                tcpListener.Stop();
                tcpListener = null;
            }
        }


        internal void run2()
        {
            while (runTread)
            {
                try
                {
                    writer.WriteMessage("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also user server.AcceptSocket() here.
                    TcpClient client;
                    try
                    {
                        client = tcpListener.AcceptTcpClient();
                    }
                    catch (SocketException sockex)
                    {
                        writer.WriteMessage(String.Format("sockex: {0}", sockex.Message));
                        client = null;
                    }
                    if (client != null)
                    {
                        using (client)
                        {
                            writer.WriteMessage("Connected!");

                            // Get a stream object for reading and writing
                            using (NetworkStream networkStream = client.GetStream())
                            using (BinaryReader stream = new BinaryReader(networkStream, DEFAULT_ENCODING))
                            {
                                IDictionary<string, object> ret = new Dictionary<string, object>();
                                String content;
                                try
                                {
                                    int l = stream.ReadInt32();
                                    byte[] byteContent = stream.ReadBytes(l);
                                    ret["receivedcontent.base64"] = byteContent;
                                    content = DEFAULT_ENCODING.GetString(byteContent);
                                    ret["receivedcontent.string"] = content;
                                    writer.WriteMessage(String.Format("received: '{0}'", content));
                                }
                                catch (IOException ioex)
                                {
                                    writer.WriteMessage(String.Format("unable to receive message: {0}", ioex.Message));
                                    content = "";
                                }
                                if (!String.IsNullOrEmpty(content))
                                {
                                    IDictionary<String, object> tmpDir = HandleContent(content);
                                    foreach (var currEntry in tmpDir)
                                        ret.Add(currEntry);
                                }
                                else
                                {
                                    ret["status"] = "FAIL";
                                    ret["errortext"] = "empty message received";
                                }
                                byte[] v = DEFAULT_ENCODING.GetBytes(JsonConvert.SerializeObject(ret));
                                networkStream.Write(v, 0, v.Length);

                                stream.Close();
                                networkStream.Close();
                            }
                            client.Close();
                        }
                        tcpListener.Start();
                    }
                }
                catch (Exception e)
                {
                    writer.WriteMessage(e.ToString());
                    writer.WriteMessage("Waiting for a connection... Err!!");
                }
            }
            writer.WriteMessage("Waiting for a connection... DONE!");
            if (tcpListener != null)
                tcpListener.Stop();
        }

    }
}
