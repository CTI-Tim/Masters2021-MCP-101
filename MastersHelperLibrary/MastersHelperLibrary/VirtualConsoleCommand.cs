namespace MastersHelperLibrary
{
    public delegate string VirtualConsoleCmdFunction(string CmdParameters);

    internal class VirtualConsoleCommand
    {
        public string UserCmdName { get; set; }
        public string UserHelp { get; set; }
        public VirtualConsoleCmdFunction UserFunction { get; set; }

        public VirtualConsoleCommand(VirtualConsoleCmdFunction UserFunction, string UserCmdName, string UserHelp)
        {
            this.UserFunction = UserFunction;
            this.UserCmdName = UserCmdName;
            this.UserHelp = UserHelp;
        }
    }
}