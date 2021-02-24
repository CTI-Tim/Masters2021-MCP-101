using Crestron.SimplSharp.CrestronIO;                   // For FileReadWrite
using System;

namespace MastersHelperLibrary
{
    public class LogFileWriter
    {
        private FileStream myStream;

        //private StreamReader myReader;
        private StreamWriter myWriter;

        public string LogPath { get; set; }

        /// <summary>
        /// Returns the path to nvram folder with a trailing separator
        /// </summary>
        public string NvramPath
        {
            get
            {
                string Seperator = "/";

                string progPath = Directory.GetApplicationRootDirectory();  // Where are we?
                if (progPath.Length < 2)  // Cheater way of  detecting if we are on VC or Hardware
                    progPath = "";
                return string.Format("{0}{1}nvram{2}", progPath, Seperator, Seperator);
            }
        }

        /// <summary>
        /// Returns the path to user folder with a trailing separator
        /// </summary>
        public string UserPath
        {
            get
            {
                string progPath = Directory.GetApplicationRootDirectory();  // Where are we?
                string Seperator = "/";
                if (progPath.Length < 2)  // Cheater way of  detecting if we are on VC or Hardware
                {
                    progPath = "";
                }

                return string.Format("{0}{1}user{2}", progPath, Seperator, Seperator);
            }
        }

        /// <summary>
        /// Returns the Path to the program path with a trailing seperator
        /// </summary>
        public string ProgramPath
        {
            get
            {
                string progPath = Directory.GetApplicationRootDirectory();  // Where are we?
                char Seperator = Path.PathSeparator;
                return string.Format("{0}{1}", progPath, Seperator);
            }
        }

        /// <summary>
        ///  Writes single lines to a separate log file with a date and time stamp ends the line with \x0D\x0A
        ///  Creates the file if does not exist,  appends if it does exist.
        /// </summary>
        /// <param name="strPath">Path with filename to write to</param>
        /// <param name="s">Content you want written as a single line in the file</param>
        public void WriteLog(string s)
        {
            //Format the string to have a time and date stamp
            string payload = String.Format("{0}.{1}.{2}-{3}:{4}:{5} : {6}",
                DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second,
                s);

            if (File.Exists(LogPath))
            {
                myStream = new FileStream(LogPath, FileMode.Append); // If it exists we add to it
            }
            else
            {
                myStream = new FileStream(LogPath, FileMode.Create); // if it doesn't exist we create it
            }

            myWriter = new StreamWriter(myStream);
            myWriter.NewLine = "\x0D\x0A"; // set the end of line terminator
            myWriter.WriteLine(payload);

            // Clean up after ourselves
            myWriter.Close();
            myWriter.Dispose();
            myStream.Close();
            myStream.Dispose();
        }
    }
}