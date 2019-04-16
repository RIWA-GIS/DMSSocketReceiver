using DMSSocketReceiver.dmshandler;
using DMSSocketReceiver.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DMSSocketReceiver.network
{

    public class AsynchronousSocketListener : AbstractDMSListener
    {
        private static Encoding DEFAULT_ENCODING = Encoding.UTF8;
        private Thread lThread;
        // Thread signal.  
        public ManualResetEvent allDone = new ManualResetEvent(false);
        private Boolean runTread;

        private Socket listener;

        public AsynchronousSocketListener(ILogWriter writerParam, IDMSHandler handler) : base(writerParam, handler)
        {

        }



        public override void StartListeningInternal(int port)
        {
            if (runTread)
            {
                return;
            }

            // Establish the local endpoint for the socket.  
            // The DNS name of the computer  
            // running the listener is "host.contoso.com".  
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);

            // Create a TCP/IP socket.  
            listener = new Socket(IPAddress.Any.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);
            // Bind the socket to the local endpoint and listen for incoming connections.  
            listener.Bind(localEndPoint);
            listener.Listen(100);

            Writer.WriteMessage("Waiting for a connection on port " + port);
            runTread = true;
            lThread = new Thread(run);
            lThread.Start();

        }

        internal void run()
        {
            try
            {
                while (runTread)
                {
                    // Set the event to nonsignaled state.  
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.  
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // Wait until a connection is made before continuing.  
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Writer.WriteMessage(e.ToString());
            }
        }

        public override void StopListeningInternal()
        {
            runTread = false;
            if (listener != null)
            {
                listener.Close();
                listener = null;
                allDone.Set();
            }
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();
            if (runTread)
            {

                // Get the socket that handles the client request.  
                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);

                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = handler;
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
            }
        }

        public void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket.   
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.  
                state.sb.Append(DEFAULT_ENCODING.GetString(
                    state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read   
                // more data.  
                content = state.sb.ToString();
                if (content.IndexOf(Consts.EOF_MARK) > -1)
                {
                    // All the data has been read from the   
                    // client. Display it on the console.  
                    String strippedContent = content.Replace(Consts.EOF_MARK, "");
                    IDictionary<string, object> ret = HandleContent(strippedContent);
                    Send(handler, JsonConvert.SerializeObject(ret));
                }
                else
                {
                    // Not all data received. Get more.  
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
                }
            }
        }


        private void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using encoding.  
            byte[] byteData = DEFAULT_ENCODING.GetBytes(data);

            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
                Writer.WriteMessage(String.Format("Sent {0} bytes to client.", bytesSent));

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception e)
            {
                Writer.WriteMessage(e.ToString());
            }
        }

    }
}
