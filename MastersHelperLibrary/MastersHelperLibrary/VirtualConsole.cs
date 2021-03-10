using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Crestron.SimplSharp.WebScripting;
using Crestron.SimplSharpPro;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Crestron.SimplSharp.CrestronIO;

namespace MastersHelperLibrary
{
    public static class VirtualConsole
    {
        private static TcpListener TcpServer;
        private static int TcpPort;
        private static bool Active;

        private static ConcurrentDictionary<int, VirtualConsoleClient> TcpClients = new ConcurrentDictionary<int, VirtualConsoleClient>();
        private static Thread ServerThread;
        private static string RoomId = InitialParametersClass.RoomId;
        //private static string ProcType = InitialParametersClass.ControllerPromptName;
        private static HttpCwsServer CwsServer;

        private static Dictionary<string, VirtualConsoleCommand> CommandDictionary = new Dictionary<string, VirtualConsoleCommand>();

        static VirtualConsole()
        {
            CrestronEnvironment.ProgramStatusEventHandler += CrestronEnvironment_ProgramStatusEventHandler;



            Thread T = new Thread(() =>
            {
                Thread.Sleep(5000);
                try
                {
                    AddNewConsoleCommand(HelpHandler, "Help", "Shows help menu");
                    AddNewConsoleCommand(HelpHandler, "?", "Shows help menu");

                    CwsServer = new HttpCwsServer("/VirtualConsole");
                    CwsServer.HttpRequestHandler = new CwsUnknownRequestProcessor();

                    HttpCwsRoute EnabledRoute = new HttpCwsRoute("Active");
                    EnabledRoute.RouteHandler = new CwsRequestProcessor();
                    EnabledRoute.Name = "Active";

                    CwsServer.Routes.Add(EnabledRoute);

                    CwsServer.ReceivedRequestEvent += CwsServer_ReceivedRequestEvent;

                    CwsServer.Register();

                }
                catch (Exception ex)
                {
                    ErrorLog.Exception("Exception starting Virtual Console CWS", ex);
                }
            });
            T.Start();
        }

        private static void CwsServer_ReceivedRequestEvent(object sender, HttpCwsRequestEventArgs args)
        {
            VirtualConsole.Send("Received Request");
        }

        private static int GetNextClientId()
        {
            int k = 0;
            while (TcpClients.ContainsKey(k))
            {
                k++;
            }
            return k;
        }

        /// <summary>
        /// Starts the VirtualConsole server
        /// </summary>
        /// <param name="Port">TCP port to listen on</param>
        /// <returns>Returns true is successful</returns>
        public static bool Start(int Port)
        {
            bool Success = false;



            TcpPort = Port;
            if (TcpServer != null)
            {
                Active = false;
                Stop();
            }

            ErrorLog.Notice("Starting Virtual Console");
            try
            {
                TcpServer = new TcpListener(System.Net.IPAddress.Any, TcpPort);
                TcpServer.Start();
                Success = true;
                Active = true;
            }
            catch (Exception ex)
            {
                ErrorLog.Exception("Error Starting Virtual Console", ex);
            }

            if (Success)
            {
                ServerThread = new Thread(() =>
                {
                    while (true)
                    {
                        try
                        {
                            TcpClient Client = TcpServer.AcceptTcpClient();
                            VirtualConsoleClient VcClient = new VirtualConsoleClient(Client, RoomId);
                            VcClient.OnConnection += VcClient_OnConnection;
                            VcClient.OnDataReceived += VcClient_OnDataReceived;
                            VcClient.Send(String.Format("{0}>", RoomId));
                            TcpClients.TryAdd(GetNextClientId(), VcClient);
                        }
                        catch
                        {
                            break;
                        }

                    }
                });
                ServerThread.Start();
            }
            return Success;
        }

        /// <summary>
        /// Stops the VirtualConsole server and disconects all clients
        /// </summary>
        public static void Stop()
        {
            ErrorLog.Notice("Stopping Virtual Console");
            try
            {
                Active = false;
                TcpServer.Stop();
                foreach (VirtualConsoleClient C in TcpClients.Values)
                {
                    if (C.Connected) { C.Close(); }
                }
            }
            catch { }
        }

        /// <summary>
        /// Sends a message to all connected VirtualConsole clients and adds the console prompt
        /// </summary>
        /// <param name="Message">Message to send</param>
        public static void Send(string Message)
        {
            Send(Message, true);
        }

        /// <summary>
        /// Sends a message to all connected VirtualConsole clients and optionally adds the console prompt
        /// </summary>
        /// <param name="Message">Messsage to send</param>
        /// <param name="Final">If true, adds the console prompt</param>
        public static void Send(string Message, bool Final)
        {
            foreach (VirtualConsoleClient C in TcpClients.Values)
            {
                if (C.Connected)
                {
                    C.Send(Message, Final);
                }
            }
        }

        /// <summary>
        /// Registers a user command with VirtualConsole.  Callback function must accept a string and return a string.
        /// </summary>
        /// <param name="UserFunction">Callback method to be invoked when the</param>
        /// <param name="UserCmdName">Name of the UserCommand to be registered as a string. NO SPACES</param>
        /// <param name="UserHelp">Short help description as a string</param>
        /// <returns>Returns true if command was able to be added</returns>
        /// <exception cref="System.ArgumentException">Thrown when UserCmdName contains spaces or is null, or callback method is null</exception>
        public static bool AddNewConsoleCommand(VirtualConsoleCmdFunction UserFunction, string UserCmdName, string UserHelp)
        {
            if (UserCmdName.Contains(" ") || String.IsNullOrWhiteSpace(UserCmdName))
            {
                throw new ArgumentException("UserCmdName may not contain spaces", "UserCmdName");
            }
            else if (UserFunction == null)
            {
                throw new ArgumentException("UserFunction may not be null", "UserFunction");
            }
            else if (CommandDictionary.ContainsKey(UserCmdName.ToUpper()))
            {
                return false;
            }
            else
            {
                VirtualConsoleCommand V = new VirtualConsoleCommand(UserFunction, UserCmdName.ToUpper(), UserHelp);
                CommandDictionary.Add(UserCmdName.ToUpper(), V);
                return true;
            }
        }

        private static void VcClient_OnDataReceived(object sender, DataEventArgs e)
        {
            string Command, Parameters;

            VirtualConsoleClient VcClient = (VirtualConsoleClient)sender;
            string Data = e.Data.Trim();

            if (Data.IndexOf(" ") == -1)
            {
                Command = Data.ToUpper();
                Parameters = String.Empty;
            }
            else
            {
                Command = Data.Substring(0, Data.IndexOf(" ")).ToUpper();
                Parameters = Data.Substring(Data.IndexOf(" ") + 1);
            }

            if (String.IsNullOrWhiteSpace(Command))
            {
                VcClient.Send("", true);
            }
            else if (CommandDictionary.ContainsKey(Command))
            {
                string Response = CommandDictionary[Command].UserFunction(Parameters);
                VcClient.Send(Response, true);
            }
            else
            {
                VcClient.Send("Bad or incomplete command", true);
            }
        }

        private static string HelpHandler(string Params)
        {
            string R = String.Empty;

            int LongestCommand = CommandDictionary.Keys.Aggregate("", (Max, Current) => Max.Length > Current.Length ? Max : Current).Length;
            IEnumerable<string> Ordered = CommandDictionary.Keys.OrderBy(a => a);

            R += "Virtual Console for " + RoomId + " Help\x0A\x0D";
            R += "------------------------------------------\x0A\x0D\x0A\x0D";
            foreach (string C in Ordered)
            {
                R += String.Format("{0}{1}\x0A\x0D", C.PadRight(LongestCommand + 10), CommandDictionary[C].UserHelp);
            }

            return R;
        }

        private static void VcClient_OnConnection(object sender, ConnectedEventArgs e)
        {
            if (e.Connected == false)
            {
                int Id = TcpClients.Where(x => x.Value == sender).Select(x => x.Key).FirstOrDefault();
                TcpClients.TryRemove(Id, out VirtualConsoleClient C);
            }
        }

        private static void CrestronEnvironment_ProgramStatusEventHandler(eProgramStatusEventType programEventType)
        {
            if (programEventType == eProgramStatusEventType.Stopping)
            {
                Stop();
            }
        }

        private class CwsUnknownRequestProcessor : IHttpCwsHandler
        {
            public void ProcessRequest(HttpCwsContext Context)
            {
                Context.Response.Write("Unknown Request", true);
            }
        }

        private class CwsRequestProcessor : IHttpCwsHandler
        {
            public void ProcessRequest(HttpCwsContext Context)
            {
                try
                {
                    dynamic JRoot;
                    if (Context.Request.HttpMethod == "GET")
                    {
                        JRoot = new JObject();
                        JRoot.active = Active;
                        Context.Response.Write(JsonConvert.SerializeObject(JRoot), true);
                    }
                    else if (Context.Request.HttpMethod == "PUT")
                    {
                        Stream S = Context.Request.InputStream;
                        string Rx = String.Empty;
                        byte[] StreamBuf = new byte[1024];
                        while (S.Read(StreamBuf, 0, 1024) > 0)
                        {
                            Rx += Encoding.ASCII.GetString(StreamBuf);
                        }

                        JRoot = JObject.Parse(Rx);
                        if (JRoot["active"] != null)
                        {
                            if (bool.TryParse(JRoot["active"].ToString(), out bool NewVal))
                            {
                                if (NewVal != Active)
                                {
                                    if (NewVal) { Start(TcpPort); }
                                    else { Stop(); }
                                }
                            }
                        }

                        JRoot = new JObject();
                        JRoot.active = Active;
                        Context.Response.Write(JsonConvert.SerializeObject(JRoot), true);
                    }
                }
                catch (Exception ex)
                {
                    ErrorLog.Exception("Error with VirtualConsole CWS", ex);
                }
            }
        }
    }
}