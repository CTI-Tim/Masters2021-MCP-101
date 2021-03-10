using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MastersHelperLibrary
{
    public class DataEventArgs : EventArgs
    {
        public string Data { get; set; }

        public DataEventArgs(string Data)
        {
            this.Data = Data;
        }
    }

    public class ConnectedEventArgs : EventArgs
    {
        public bool Connected { get; set; }

        public ConnectedEventArgs(bool Connected)
        {
            this.Connected = Connected;
        }
    }

    public class VirtualConsoleClient : IDisposable
    {
        private TcpClient Client;
        private Thread ClientThread;
        private string RoomId;

        private string RxBuffer;

        /// <summary>
        /// Raised when data is received from Client
        /// </summary>
        public event EventHandler<DataEventArgs> OnDataReceived;

        /// <summary>
        /// Raised when connection state changes
        /// </summary>
        public event EventHandler<ConnectedEventArgs> OnConnection;

        /// <summary>
        /// Returns client connection status
        /// </summary>
        public bool Connected => Client?.Connected == true;

        /// <summary>
        /// Constructor for VirtualConsole client
        /// </summary>
        /// <param name="Client">Underlying TcpClient</param>
        /// <param name="RoomId">RoomId for console prompt</param>
        public VirtualConsoleClient(TcpClient Client, string RoomId)
        {
            this.Client = Client;
            this.RoomId = RoomId;

            ClientThread = new Thread(Listen);
            ClientThread.Start();
        }

        /// <summary>
        /// Disposes VirtualConsoleClient, and underlying TcpClient.
        /// </summary>
        public void Dispose()
        {
            if (Client?.Connected == true)
            {
                Client.Close();
            }
        }

        /// <summary>
        /// Sends messsge to VirtualConsoleClient and adds prompt
        /// </summary>
        /// <param name="Message">Message to send</param>
        public void Send(string Message)
        {
            Send(Message, true);
        }

        /// <summary>
        /// Sends messsge to VirtualConsoleClient and optionally adds prompt
        /// </summary>
        /// <param name="Message">Message to send</param>
        /// <param name="Final">If true, adds console prompt</param>
        public void Send(string Message, bool Final)
        {
            if (Client?.Connected == true)
            {
                try
                {
                    byte[] MessageBytes = Encoding.ASCII.GetBytes(Message);
                    NetworkStream NS = Client.GetStream();

                    NS.Write(MessageBytes, 0, MessageBytes.Length);

                    if (Final)
                    {
                        byte[] FinalBytes = Encoding.ASCII.GetBytes(String.Format("\r\n\r\n{0}>", RoomId));
                        NS.Write(FinalBytes, 0, FinalBytes.Length);
                    }
                }
                catch (Exception)
                { }
            }
        }

        /// <summary>
        /// Disconnects the client
        /// </summary>
        public void Close()
        {
            if (Client?.Connected == true)
            {
                Client.Close();
            }
        }

        private void Listen()
        {
            try
            {
                NetworkStream NS = Client.GetStream();

                while (Client.Connected)
                {
                    try
                    {
                        byte[] Buffer = new byte[1024];
                        int BytesRead = NS.Read(Buffer, 0, 1024);
                        if (BytesRead > 0)
                        {
                            RxBuffer += Encoding.ASCII.GetString(Buffer, 0, BytesRead);
                            if (RxBuffer.IndexOf("\n") >= 0)
                            {
                                string Rx = RxBuffer.Substring(0, RxBuffer.IndexOf("\n") + 1);
                                RxBuffer = RxBuffer.Remove(0, RxBuffer.IndexOf("\n") + 1);
                                OnDataReceived?.Invoke(this, new DataEventArgs(Rx));
                            }
                        }
                        else
                        {
                            Client.Close();
                            break;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch { }

            if (Client.Connected == false)
            {
                OnConnection?.Invoke(this, new ConnectedEventArgs(Client.Connected));
            }
            else
            {
                OnConnection?.Invoke(this, new ConnectedEventArgs(Client.Connected));
            }
        }
    }
}