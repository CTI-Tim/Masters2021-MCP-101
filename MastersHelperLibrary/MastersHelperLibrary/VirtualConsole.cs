using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharp.CrestronSockets;              // For Sockets
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading

namespace MastersHelperLibrary
{
    public class VirtualConsole
    {
        // Private Containers and Variables
        private TCPServer myServer;

        private Thread myThread;
        private bool RunThread = false;
        private string[] lastMessage = new string[10];

        private ushort port;
        private EthernetAdapterType adapter;

        //Constructor
        /// <summary>
        /// Default Constructor to build and start the Virtual Console Server at the port
        /// Specified in the parameter.  The server will start and accept up to 5 connections
        /// this class creates a worker thread that will stay running as long as the class is used
        /// the deconstructor will tell the thread to end if the program is stopped or the class is no longer in use
        /// If a conflicting port number is used it will fail to open the port.
        /// </summary>
        /// <param name="Port">network port number to listen on from 0 to 65535 is valid</param>
        public VirtualConsole(ushort Port)
        {
            port = Port;  // save it for later use

            adapter = EthernetAdapterType.EthernetLANAdapter;

            // Setup our server
            myServer = new TCPServer("0.0.0.0", port, 512, adapter, 5);
            myServer.SocketStatusChange += MyServer_SocketStatusChange;

            //Get the thread running
            myThread = new Thread(ThreadCode, null);
            RunThread = true;
            myThread.Start();   // Start our thread
        }

        //Deconstructor
        ~VirtualConsole()
        {
            RunThread = false;  // Sutdown the thread on deconstruct
        }

        private void MyServer_SocketStatusChange(TCPServer myTCPServer, uint clientIndex, SocketStatus serverSocketStatus)
        {
            if (serverSocketStatus == SocketStatus.SOCKET_STATUS_CONNECTED)  // Did we get a connect event? if so send the header
            {
                this.PrintLine("---------------------\x0d\x0a");
                this.PrintLine("Connected\x0d\x0a");
                this.PrintLine("---------------------\x0d\x0a");
            }
        }

        /// <summary>
        /// Send a string to a telnet console connected to our virtual console at the port specified in the default constructor
        /// if you need to send variables then use a string.format() inside the method call
        /// </summary>
        /// <param name="s">string to send to the virtual console</param>
        public void PrintLine(string s)
        {
            CrestronConsole.PrintLine("  Printline : {0} : {1}", myServer.NumberOfClientsConnected, myServer.ServerSocketStatus);

            byte[] payload = System.Text.Encoding.ASCII.GetBytes(s + "\x0D\x0A"); // Convert the string to Bytes ASCII
            for (uint i = 1; i <= 5; i++)
            {
                //myServer.SendData(i, payload, payload.Length);                    // This will lose data on connect
                myServer.SendDataAsync(i, payload, payload.Length, SentDataMethod); // this is more robust
            }
        }

        private void SentDataMethod(TCPServer myTCPServer, uint clientIndex, int numberOfBytesSent) // empty method to make async happy
        {
        }

        // This is the code for the server running as a thread.   Wait for connection is blocking and will halt
        // Code execution until something connects.   it also needs to be restarted after it runs.
        // Putting it in a thread will stop it from blocking our program, and the loop makes sure it will
        // get re-triggered every time so that multiple clients can connect and reconnect as needed
        private object ThreadCode(object o)
        {
            bool clientConnected = false;
            while (RunThread)
            {
                if (myServer.NumberOfClientsConnected < 5)  // If we are full,  the stop allowing more connections
                {
                    clientConnected = false;
                }
                else
                {
                    clientConnected = true;
                }
                if (!clientConnected)
                {
                    myServer.WaitForConnection(); // This is a blocking method that will wait for a connection
                }
            }
            myServer.DisconnectAll();  // WE are shutting down kick them all off
            return false;
        }
    }
}