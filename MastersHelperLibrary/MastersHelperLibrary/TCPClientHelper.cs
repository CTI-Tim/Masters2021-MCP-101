using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharp.CrestronSockets;              // For Sockets
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using System;

namespace MastersHelperLibrary
{
    /// <summary>
    /// This Class simplifies communication to a TCP/IP device or server as a client.
    /// It creates a worker thread for you and automates almost all features needed for communication
    /// to a device over  the network.
    /// </summary>
    public class TCPClientHelper
    {
        // Private Containers and Variables
        private TCPClient myClient;

        private Thread myThread;
        private bool RunThread = false;
        private string lastRX = "";
        private string ipAddress = "";
        private int port;
        private CrestronQueue txQueue;

        public event EventHandler<TCPClientHelperEventArgs> tcpHelperEvent; 

        // Public Properties

        /// <summary>
        /// True if connected False if not connected
        /// </summary>
        public bool isConnected
        {
            get
            {
                if (myClient.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Status of the connection in decimal from 0 to 10: 0=not connected 2=connected all others are documented in the API
        /// see Crestron.SimplSharp.CrestronSockets.SocketStatus for details
        /// </summary>
        public int Status
        {
            get
            {
                return (int)myClient.ClientStatus;
            }
        }

        /// <summary>
        /// Send a string to TX will cause that string to be sent to the TCP server or device if connected
        /// </summary>
        public string TX
        {
            set
            {
                txQueue.Enqueue(value);
            }
        }

        /// <summary>
        /// This will contain the last received string from the TCP server or device
        /// </summary>
        public string RX
        {
            get
            {
                return lastRX;
            }
        }

        /// <summary>
        /// Creates a TCPClient Object ready to connect to IP address at Port number.
        /// This will not automatically connect.  you need to use the methods for connection control.
        ///
        /// </summary>
        /// <param name="IpAddress">IP address of the Target server or device</param>
        /// <param name="Port">Port number between 0d and 65535d</param>
        public TCPClientHelper(string IpAddress, int Port)
        {
            txQueue = new CrestronQueue();

            ipAddress = IpAddress;

            if (Port > 65535)
                Port = 65535;

            port = Port;
        }

        //Public Methods
        /// <summary>
        /// Call this method to Connect to the TCP Server or Device
        /// </summary>
        public void Connect()
        {
            myClient = new TCPClient(ipAddress, port, 1024);  // Create the Client
            myClient.SocketStatusChange += MyClient_SocketStatusChange;
            myThread = new Thread(ThreadCode, null);
            RunThread = true;
            txQueue.Clear();    // Empty the queue
            myThread.Start();   // Start our thread
        }

        private void MyClient_SocketStatusChange(TCPClient myTCPClient, SocketStatus clientSocketStatus)
        {
            OnRaiseEvent(new TCPClientHelperEventArgs("STATUS")); // Call the Event Handler
        }

        /// <summary>
        /// Call this method to disconnect from the Server or device.
        /// All data will be cleared from the TX and RX
        /// </summary>
        public void Disconnect()
        {
            RunThread = false;
            myClient.DisconnectFromServer();
            txQueue.Clear();    // Empty the queue
            lastRX = "";
            myClient.Dispose();
        }

        // The code in this method is what will be run as our  worker thread watching for incoming data and when a message
        // is added to the Queue it is sent out to the TCP connection.
        // It also starts the connection in the thread
        private object ThreadCode(object o)
        {
            if (myClient.ConnectToServer() == 0) // Connect and look for a success
            {
                //TCP Client loop for TX and RX
                while (RunThread)
                {
                    if (myClient.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED) // Are we connected?
                    {
                        // Process outgoing data
                        if (!txQueue.IsEmpty)  // Do we have something in the Queue to send
                        {
                            string temp = txQueue.Dequeue().ToString();                  // Get our TX data out of the Queue

                            //byte[] payload = new byte[temp.Length];                      // Create a local array the size of our Payload 

                            //foreach (int i in payload)
                            //    payload[i] = Convert.ToByte(temp[i]);                    // Convert each character into it's byte representation and load it to our array.

                            byte[] payload = System.Text.Encoding.UTF8.GetBytes(temp); // This is another way of doign the above but in a single line, Convert the string to Byte array using UTF8... Why Not ASCII?   ASCII stops at 127 UTF8 stops at 255

                            myClient.SendData(payload, payload.Length);                  // Send it out to the TCP connection that wants an array of bytes.
                        }

                        // Wait for Data incoming
                        if (myClient.DataAvailable)  // Do we have data to read?
                        {
                            myClient.ReceiveData(); // Extract the data into out IncomingDataBuffer
                            string Buffer = System.Text.Encoding.UTF8.GetString(myClient.IncomingDataBuffer); // we get bytes, time to make it a string,  once again we need to convert to UTF8 and not ASCII so we get everything from 0 to 255
                            lastRX = Buffer.TrimEnd('\x00'); // make a copy in case the user wants to look at the last packet received, get rid of any trailing \x00's
                            OnRaiseEvent(new TCPClientHelperEventArgs("RX")); // Call the Event Handler
                        }
                    }
                    else
                    {
                        RunThread = false;  // we are not connected, kill the thread
                    }
                }
            }
            else
            {
                myClient.DisconnectFromServer(); // we had an error connecting. be safe and kill the connection
                RunThread = false;
                OnRaiseEvent(new TCPClientHelperEventArgs("FAIL")); // Call the Event Handler
            }
            return null;
        }

        protected virtual void OnRaiseEvent(TCPClientHelperEventArgs e)
        {
            EventHandler<TCPClientHelperEventArgs> raiseEvent = tcpHelperEvent;  // Make a copy of the event

            if (raiseEvent != null) // Verify we have subscribers
            {
                // Set all our event variables
                e.Connected = isConnected;
                e.Status = Status;
                e.RX = lastRX;

                raiseEvent(this, e); // trigger the event
            }
        }
    }

    // This is a separate class to define the data that will be sent to our program from the Event.
    public class TCPClientHelperEventArgs
    {
        public TCPClientHelperEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; set; }
        public bool Connected { get; set; }
        public int Status { get; set; }
        public string RX { get; set; }
    }
}