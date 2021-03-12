using Crestron.SimplSharp.CrestronIO;                   // For FileReadWrite
using System;

namespace MastersHelperLibrary
{
    /// <summary>
    /// Separate Log file writing to emulate what is taught in 301 class
    /// this is useful for writing your own log entries separate from the 
    /// system log.  This allows non critical items to not clutter the system log file.
    /// 
    /// This can also be used to create a log of system use, source use, etc.
    /// </summary>
    public class LogFileWriter
    {
        private FileStream myStream;

        //private StreamReader myReader;
        private StreamWriter myWriter;

        /// <summary>
        ///  Logpath is set for where you want your log file to be written to.
        ///  NOTE: if this is a new program it is strongly recommended to use the UserPath.
        ///  using NVRAM is discouraged as it is only there for legacy backward compatibility.
        ///  Best Practice is now to put files in the User folder for that program.
        /// </summary>
        public string LogPath { get; set; }

        /// <summary>
        /// Returns the path to user folder with a trailing separator
        /// </summary>
        public string UserPath
        {
            get
            {
                string progPath = Directory.GetApplicationRootDirectory();  // Where are we?

                if (progPath.Length < 2)  // Cheater way of  detecting if we are on VC or Hardware
                {
                    progPath = "";
                }

                return string.Format("{0}/user/", progPath);
            }
        }

        /// <summary>
        /// Returns the Path to the program path with a trailing separator for 4 series and VC-4
        /// </summary>
        public string ProgramPath
        {
            get
            {
                string progPath = Directory.GetApplicationRootDirectory();  // Where are we?

                return string.Format("{0}/", progPath);
            }
        }

        /// <summary>
        ///  Writes single lines to a separate log file with a date and time stamp ends the line with \x0D\x0A
        ///  Creates the file if does not exist,  appends if it does exist.
        ///  Every line written will be prepended with YYYY.MM.DD-HH-MM-SS
        /// </summary>
        /// <param name="strPath">Path with filename to write to</param>
        /// <param name="s">Content you want written as a single line in the file</param>
        public void WriteLog(string s)
        {
            //Format the string to have a time and date stamp
            string payload = String.Format("{0}.{1:00}.{2:00}-{3:00}:{4:00}:{5:00} : {6}",
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