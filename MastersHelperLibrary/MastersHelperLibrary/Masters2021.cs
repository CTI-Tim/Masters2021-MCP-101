using Crestron.SimplSharp.CrestronIO;                   // For FileReadWrite
using System;

namespace MastersHelperLibrary
{
    public class Masters2021
    {
        public Masters2021()
        {
            //string FileName = @"\nvram\names.txt";  // DO NOT USE NVRAM It's for backwards compatibility only.
            string FileName = Directory.GetApplicationRootDirectory() + "/user/names.txt";

            if(!File.Exists(FileName))  // If file does not exist

            {
                string[] names = new string[10]; // Create an array
                names[0] = "Fred";
                names[1] = "Thomas";
                names[2] = "Annabelle";
                names[3] = "Jose'";
                names[4] = "Eliza";
                names[5] = "Richard";
                names[6] = "Tim";
                names[7] = "Dan";
                names[8] = "Beyhan";
                names[9] = "Carl";

                var myStream = new FileStream(FileName, FileMode.Create);  // Create a new file
                var myWriter = new StreamWriter(myStream);
                myWriter.NewLine = "\x0D\x0A"; // set the end of line terminator

                for (var i = 0; i < 10; i++)  // Iterate through and write each line
                {
                    myWriter.WriteLine(names[i]);
                }
                // Clean up after ourselves
                myWriter.Close();
                myWriter.Dispose();
                myStream.Close();
                myStream.Dispose();
            }
        }

    }
}
