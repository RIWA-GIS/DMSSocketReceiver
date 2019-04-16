using DMSSocketReceiver.dmshandler;
using DMSSocketReceiver.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DMSSocketReceiver.network.tcp
{

    public class AsynchronousTCPListener : AbstractDMSListener
    {
        private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(AsynchronousTCPListener));

        private static Encoding DEFAULT_ENCODING = Encoding.UTF8;
        private Thread lThread2;
        // Thread signal.  
        private Boolean runTread;

        private TcpListener tcpListener;

        public AsynchronousTCPListener(ILogWriter writerParam, IDMSHandler handler) : base(writerParam, handler)
        {

        }



        public override void StartListeningInternal(int port)
        {
            if (runTread)
                return;

            IPEndPoint localEndPointL = new IPEndPoint(IPAddress.Any, port);
            tcpListener = new TcpListener(localEndPointL);
            tcpListener.Start();


            lThread2 = new Thread(run2);
            runTread = true;
            lThread2.Start();
        }

        public override void StopListeningInternal()
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
                    Writer.WriteMessage("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also user server.AcceptSocket() here.
                    TcpClient client;
                    try
                    {
                        client = tcpListener.AcceptTcpClient();
                    }
                    catch (SocketException sockex)
                    {
                        Writer.WriteMessage(String.Format("sockex: {0}", sockex.Message));
                        client = null;
                    }
                    if (client != null)
                    {
                        using (client)
                        {
                            Writer.WriteMessage(string.Format("Connected from '{0}'.", ((IPEndPoint) client.Client.RemoteEndPoint).Address));

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
                                    content = DEFAULT_ENCODING.GetString(byteContent);
                                    LOG.Debug(String.Format("received: '{0}'", content));
                                }
                                catch (IOException ioex)
                                {
                                    LOG.Error(String.Format("error reading stream: {0}", ioex.Message), ioex);
                                    Writer.WriteMessage(String.Format("unable to receive message: {0}", ioex.Message));
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
                                    ret[KEY_STATUS] = "FAIL";
                                    ret[KEY_ERRORTEXT] = "empty message received";
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
                    Writer.WriteMessage(e.ToString());
                    Writer.WriteMessage("Waiting for a connection... Err!!");
                }
            }
            Writer.WriteMessage("Waiting for a connection... DONE!");
            if (tcpListener != null)
                tcpListener.Stop();
        }

    }
}
