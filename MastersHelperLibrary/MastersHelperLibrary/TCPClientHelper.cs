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

                            /* This comment block added after some great discussions about this learning point.
                             *
                             *  Codepages in C# can be a challenge.    In this code Codepage 1252 is used but is INCORRECT.    I used  it because if you search for help with
                             *  sending bytes in a string you will come across this answer a LOT out on the internet,  and it's the wrong answer because it has a hole in the middle that will not return
                             *  the correct information.   I am hesitant to just hand you the student the answer because the journey into understanding it is very important and is not covered
                             *  well in all C# training available.    Most programmers in C# will not use a string for data to and from a device or service because of the encoding,
                             *  instead the best practice for this is to use the byte array.   This is why the TCP client is using them.   
                             *  Byte arrays are the actual value and in a string because encoding is applied can change your data.   Many many programmers will use Encoding.ASCII and fail to get any
                             *  bytes above 127,  same for Encoding.Utf8 and this become confusing to a Crestron programmer as we are used to a string being just a string that holds a byte array.
                             *  
                             *  The answer is to use codepage 28591.   But how did I or others that have fought with this get that information?   It was a deep rabbit hole of learning into codepages.
                             *  start with  these links...
                             *  https://bizbrains.com/blog/encoding-101-part-1-what-is-encoding/
                             *  https://bizbrains.com/blog/encoding-101-part-2-windows-1252-vs-utf-8/
                             *  https://docs.microsoft.com/en-us/dotnet/api/system.text.encoding.ascii?redirectedfrom=MSDN&view=net-5.0#System_Text_Encoding_ASCII
                             *  https://docs.microsoft.com/en-us/dotnet/standard/base-types/character-encoding
                             *  
                             *  https://www.i18nqa.com/debug/table-iso8859-1-vs-windows-1252.html#:~:text=ISO%2D8859%2D1%20(also,assigned%20to%20these%20code%20points
                             *  
                             * 
                             */
                            //  Please see bug report comment thread on github for more detailed information on codepages at https://github.com/CTI-Tim/Masters2021-MCP-101/issues/1

                            byte[] payload = System.Text.Encoding.GetEncoding(1252).GetBytes(temp);  // This is the encoding and this will NOT WORK as there is a hole in the middle of that codepage

                            myClient.SendData(payload, payload.Length);                  // Send it out to the TCP connection that wants an array of bytes.
                        }

                        // Wait for Data incoming
                        if (myClient.DataAvailable)  // Do we have data to read?
                        {
                            myClient.ReceiveData(); // Extract the data into out IncomingDataBuffer

                            //string Buffer = System.Text.Encoding.UTF8.GetString(myClient.IncomingDataBuffer); // we get bytes, time to make it a string,  Encoding may change bytes so this will not work for beyond 127

                            string Buffer = System.Text.Encoding.GetEncoding(1252).GetString(myClient.IncomingDataBuffer);  // See the comment block above for details on codepage and what to pick
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


        //  The next 2 methods are manually moving a string to bytes and back without using encoding.   This will not work most of the time because encoding is usually required.
        //  They are included to give the student another example of how do the conversion.   But it can cause issues.   Encoding can be a difficult topic,  more information is 
        //  available at https://docs.microsoft.com/en-us/dotnet/standard/base-types/character-encoding


        /// <summary>
        /// Convert a string to raw bytes
        /// </summary>
        /// <param name="str">String to convert</param>
        /// <returns>raw Byte Array</returns>
        private byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        // Do NOT use on arbitrary bytes; only use on GetBytes's output on the SAME system
        private string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
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