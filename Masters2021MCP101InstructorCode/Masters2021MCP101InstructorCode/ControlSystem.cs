using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharp.CrestronIO;                   // Add this for File IO
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.DeviceSupport;         	// For Generic Device Support
using Crestron.SimplSharpPro.UI;                        // Bring in Touchpanels in Session 1
using MastersHelperLibrary;                             // Bring in our custom Library in Session 2
using System;


namespace Masters2021MCP101InstructorCode   //DO NOT name the solution Masters2021 Avoid namespace collisions.
{
    public partial class ControlSystem : CrestronControlSystem  // note the "partial" keyword... see ControlSystemEvents.cs for more info
    {
        // ***** Session 1
        private XpanelForSmartGraphics myXpanel;    // Create a container for the Xpanel
        // ***** Session 2
        private TCPClientHelper myClient;           // Create our container:  Question: Why put this here?
        // ***** Session 3
        private string[] Names = new string[10]; // array for the file loading exercise

        // Why here?  This location makes sure it's available to everything below it in the class   (Context matters for access)

        /// <summary>
        /// ControlSystem Constructor. Starting point for the SIMPL#Pro program.
        /// Use the constructor to:
        /// * Initialize the maximum number of threads (max = 400)
        /// * Register devices
        /// * Register event handlers
        /// * Add Console Commands
        /// NOTE:
        /// * You do NOT have access to Hardware here
        /// * you CAN NOT start threads here
        ///
        /// Please be aware that the constructor needs to exit quickly; if it doesn't
        /// exit in time, the SIMPL#Pro program will exit.
        ///
        /// You cannot send / receive data in the constructor
        /// </summary>
        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;

                // Subscribe to the controller events (System, Program, and Ethernet)
                // These are Optional.
                // Only keep these if you are going to use them.  This example moves them to a 
                // seperate cs file called ControlsystemEvents.cs by leveraging the partial keyword

                CrestronEnvironment.SystemEventHandler += new SystemEventHandler(_ControllerSystemEventHandler);
                CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(_ControllerProgramEventHandler);
                CrestronEnvironment.EthernetEventHandler += new EthernetEventHandler(_ControllerEthernetEventHandler);
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
                // Question:  What is this line for and what is the {0} represent?
            }
        }

        /// <summary>
        /// InitializeSystem - this method gets called after the constructor
        /// has finished.
        ///
        /// Use InitializeSystem to:
        /// * Start threads
        /// * Configure ports, such as serial and versiports
        /// * Start and initialize socket connections
        /// Send initial device configurations
        ///
        /// Please be aware that InitializeSystem needs to exit quickly also;
        /// if it doesn't exit in time, the SIMPL#Pro program will exit.
        /// </summary>
        public override void InitializeSystem()  // Remember  HERE we have access to IO, Hardware and Threads
        {
            try
            {
                // ***** Session 3
                // This is ONLY for masters Training session for creating a file to read
                var myMasters = new Masters2021();  // Instantiate our masters 2021 Class to create data for Session 3 file read

                // ***** Session 2
                //Question:  Why did we not have to instantiate the Virtual Console class?
                VirtualConsole.Start(40000); // Launch the virtual Console on port 40000  you can use telnet or even text console in Toolbox
                VirtualConsole.AddNewConsoleCommand(TestFunc, "Test", "This should respond with a message");

                // ***** Session 1
                myClient = new TCPClientHelper("127.0.0.1", 55555);  // Creates our client instance
                myClient.tcpHelperEvent += MyClient_tcpHelperEvent;  // Subscribe to its event handler and what method to pass it to

                myXpanel = new XpanelForSmartGraphics(0x03, this);

                //myXpanel.Register();  // How do we know we have actually registered the Touchpanel?  We should check.

                if (myXpanel.Register() == eDeviceRegistrationUnRegistrationResponse.Success)  // Did we actually get the Device?
                {
                    myXpanel.SigChange += MyXpanel_SigChange; //  Subscribe to our event handler
                    // Any other settings for the device
                }
                else
                {
                    // This will write to the error log why the Touch Panel was unable to be registered.
                    ErrorLog.Error("Unable To register Xpanel at IPID{0:X} for Reason {1}", myXpanel.ID, myXpanel.RegistrationFailureReason);

                }
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }

        /*****************************************************************************************************
        *  Session 1 Touchpanel Events setup, adding  page flips and basic logic
        *****************************************************************************************************/

        //  This is where our Touchpanel Signal changes are processed.   Digitals, Analogs, and Serials All end up here.
        private void MyXpanel_SigChange(BasicTriList currentDevice, SigEventArgs args)
        {
            VirtualConsole.Send(String.Format("TP Sig Change  Type={0} Number={1} State={2}",
                args.Sig.Type, args.Sig.Number, args.Sig.BoolValue), true);  // Question: What is all this? What does {0} mean?
                                                                             // and yes a line can be on 2 lines 
                                                                             // Answer: Look up String.Format and read up on the specifier string.  This is like the Trace and MakeString in Simpl Plus.

            // Important to split it like this for more flexibility.   Check if it's a digital, then anything below is for digitals
            if (args.Sig.Type == eSigType.Bool) // Is it a digital?
            {
                //SESSION 1: Create a Momentary Button  (This has to be outside the check if true below)
                if (args.Sig.Number == 11)
                {
                    // This passes the button events back to the feedback
                    myXpanel.BooleanInput[11].BoolValue = args.Sig.BoolValue;  // we want both the press and release here
                }

                //SESSION 1:  Check if it is pressed
                if (args.Sig.BoolValue == true) // Look for only pressed button events
                {
                    // Page Navigation                  Session 1: Instructor put in the first page nav code The rest are student lab
                    switch (args.Sig.Number)
                    {
                        case 1:
                            myXpanel.BooleanInput[1].BoolValue = true;  // Traditional way of setting it high then low
                            myXpanel.BooleanInput[1].BoolValue = false;
                            myXpanel.StringInput[1].StringValue = "Front Page";
                            break;

                        case 2:
                            myXpanel.BooleanInput[2].Pulse();  // using the built in method to pulse it
                            myXpanel.StringInput[1].StringValue = "Projector";
                            break;

                        case 3:
                            myXpanel.BooleanInput[3].Pulse();
                            myXpanel.StringInput[1].StringValue = "Phone Book";
                            break;
                    }

                    /******************************************************************************************
                     *   Session 1 Lab work below (Momentary is above outside the "true" check for boolvalue)
                     ******************************************************************************************/

                    // Create a Toggle Button  
                    if (args.Sig.Number == 10)
                    {
                        // We read the current feedback and use the compliment or NOT by using ! to toggle between states
                        myXpanel.BooleanInput[10].BoolValue = !myXpanel.BooleanInput[10].BoolValue; // Invert The Feedback
                    }

                    // Interlock with simple Logic. Pay attention to the join numbers used
                    /*
                    switch(args.Sig.Number)
                    {
                        case 12:
                            myXpanel.BooleanInput[13].BoolValue = false;
                            myXpanel.BooleanInput[14].BoolValue = false;
                            myXpanel.BooleanInput[12].BoolValue = true;
                            break;

                        case 13:
                            myXpanel.BooleanInput[12].BoolValue = false;
                            myXpanel.BooleanInput[14].BoolValue = false;
                            myXpanel.BooleanInput[13].BoolValue = true;
                            break;

                        case 14:
                            myXpanel.BooleanInput[12].BoolValue = false;
                            myXpanel.BooleanInput[13].BoolValue = false;
                            myXpanel.BooleanInput[14].BoolValue = true;
                            break;
                    }
                    */

                    //Interlock leveraging the power of a method
                    if (args.Sig.Number >= 12 && args.Sig.Number <= 14) // We only want joins 12,13,14
                        Interlock(myXpanel.BooleanInput, 12, 14, args.Sig.Number);  // Look for this custom method below

                    /**********************************************************************************************
                     *  Session 2
                     **********************************************************************************************/


                    // ####     Projector Button Press Logic
                    // ####     All of this SHOULD exist in a method or even another class to make the code more manageable and organized
                    //
                    //          We are still inside the pair of IF statements for "was this a boolean(digital) and was it true(pressed)
                    //          Nested If statements can get complex and hard to navigate quickly.

                    ProjectorButtonPress(args.Sig.Number);  // Call our method below

                    //  Hidden Challenge:   Make the projector Connect when it is on the projector Page,
                    //                      and Disconnect when the user leaves the projector page.

                    // Second hidden Challenge: Do not use join numbers but instead use an enum with names
                    //                          so you can easily remap to different buttons

                    /********************************************************************************************
                     *  Session 3
                     ********************************************************************************************/

                    // File Read and Sort button management  Methods start down near line 370
                    switch (args.Sig.Number)
                    {
                        case 30:
                            LoadFile();  // Call our method to load the file and display the contents
                            break;

                        case 31:
                            SortFile();  // Call our method to sort the strings and present them
                            break;
                    }
                }
            }
        }


        //  This is where we get responses from the TCP Client helper class.  This is where we handle the events that are raised
        //  This is a CUSTOM class.  how we interact with it was defined by the programmer.  This is not a Crestron Class!
        //  DO NOT assume this is how you use a Crestron class.   Look at the Sourcecode for that class to see how to interact with a 
        //  TCP Client directly.
        private void MyClient_tcpHelperEvent(object sender, TCPClientHelperEventArgs e)
        {
            // Send a string out our Virtual Console for display
            VirtualConsole.Send(String.Format("TCPIPClient Event Message={0}", e.Message), true);

            // e.Message will contain keywords telling us what event was raised
            // STATUS = Status change   RX = data was received  FAIL= connection failed
            if (e.Message == "STATUS")
            {
                // Connected is a boolean  True = connected, False = disconnected
                // We leverage the NOT ! operator here to make our feedback correct.
                myXpanel.BooleanInput[20].BoolValue = e.Connected;  //Connected Feedback join
                myXpanel.BooleanInput[21].BoolValue = !e.Connected; //Disconnected Feedback join
            }

            if (e.Message == "RX") // We have data waiting to be processed that is in e.RX
            {
                VirtualConsole.Send(String.Format("RX Text = {0}", e.RX));              // Print out what came in for debugging

                ProjProcessing(e.RX);  // Call our method we created below to process
                                       // the received data from the projector.
                                       // This method is at the bottom of this file
            }

            myXpanel.StringInput[2].StringValue = e.RX;   // display on our touch panel any received data
        }




        /***************************************************************************************************************
         *  Custom Methods for our program.    Best practice says these SHOULD be in their own class files for 
         *  orginization and readability
         ***************************************************************************************************************/

        // Create a Method to do our interlock for us
        // This method is passed the input collection so we can work on it directly.
        // we pass the Object specifically the Tri-list objects inputs,
        //then a number for the first and the last, and then finally the one we want selected
        // we pass the whole collection so this can be usable for any range and size.  Make your code reusable
        private void Interlock(DeviceBooleanInputCollection input, uint from, uint to, uint selected)
        {
            if (selected >= from && selected <= to) // Validate our data
            {
                for (uint i = from; i <= to; i++)    // Iterate through our items
                {
                    if (i == selected)
                        input[i].BoolValue = true;  // This is the one we want?  set it high
                    else
                        input[i].BoolValue = false; // Not what we want set it low

                    // NOTE: This is NOT a Break Before Make.   It has the potential of having two outputs high
                    //       at the same time for a very short amount of time 
                    // HIDDEN CHALLENGE:  Modify this code to become Break Before Make and properly act exactly like a Simpl Interlock

                }
            }
        }

        /*****************************************************************************************************
         *  Session 2  TCP helper and talking to the projector and getting data back
         *****************************************************************************************************/

        /// <summary>
        /// Code to control the Projector.  Send it the button number
        /// Would be a good idea to make sure it was a press first.
        /// </summary>
        /// <param name="num">uint button join number</param>
        private void ProjectorButtonPress(uint num)
        {
            switch (num)
            {
                case 20:        //Connect
                    myClient.Connect();
                    break;

                case 21:        //Disconnect
                    myClient.Disconnect();
                    break;

                case 22:        //Send ON command
                    myClient.TX = "\x02\x01\x00\x00PON\x00\x00\x00\x03";
                    break;

                case 23:        //Send Off command
                    myClient.TX = "\x02\x01\x00\x00POF\x00\x00\x00\x03";
                    break;

                case 24:        //Send Poll Hours Command
                    myClient.TX = "\x02\x01\x00\x00LH?\x00\x00\x00\x03";
                    break;
            }
        }

        /// <summary>
        /// Process the returned string from the Alexsonic projector emulator.
        /// this will set the digital feedback and serial feedback on the x panel
        /// </summary>
        /// <param name="rx">RX string from the projector</param>
        private void ProjProcessing(string rx)
        {
            if (rx.Contains("PON"))    // It reported back  we are on
            {
                myXpanel.BooleanInput[22].BoolValue = true;  // set feedback for Projector ON high
                myXpanel.BooleanInput[23].BoolValue = false; // set feedback for PRojector OFF low
            }
            else if (rx.Contains("POF"))  // Report we are off
            {
                myXpanel.BooleanInput[22].BoolValue = false;
                myXpanel.BooleanInput[23].BoolValue = true;
            }
            //  Students can create code here to detect warm and cool and report it in the header
            else if (rx.Contains("LH?"))    // Did we get a lamp hours response?
            {
                var hours = rx.TrimStart('\x02').TrimEnd('\x03');   // Clean the  0x02 and 0x03 from the front and end
                hours = hours.Substring(hours.IndexOf('?') + 1);    // Skip over the ? mark by adding 1
                myXpanel.StringInput[3].StringValue = hours;        // Send the data to the string field on the panel
            }
            //  Bonus!  If you send 3 different commands you get a strange response that wants you  name.
            else if (rx.Contains("Enter"))
            {
                myClient.TX = "\x02\x7F" + "Masters Instructor" + "\x03";  // Did you read the protocol instructions for the projector?
                // Escaped Hex codes are seperated to make sure the string parsing is not confused if the escaped code is
                // a 2 digit or 4 digit Hex number. \x7FCarl will try and  parse \x7FCA
            }
        }


        /***********************************************************************************************
         * Session 3  File loading and display and sorting  methods
         ***********************************************************************************************/

        private void LoadFile()
        {
            // Do not forget to add the Masters2021() class instantiation at the top to get the file created for students.
            // Challenge:  What if the file was not there? How can we check if it exists before we try to open it?
            //             Add the code to check if the file exists at that location only only  open and read it  if it does.
            var path = Directory.GetApplicationRootDirectory() + "/user/names.txt";

            VirtualConsole.Send(String.Format("Opening file {0}", path));

            // We should check to see if the file was actually able to be opened and read sucessfully and report back if it was unable to.
            // Add code here to make this fail gracefully if it was unable to open the file or read from it.

            var file = new FileStream(path, FileMode.Open);
            var stream = new StreamReader(file);

            VirtualConsole.Send("File Opened");  // If the above crashed by throwing an exception,  this will not execute.

            // C# arrays are zero based, we have index 0 to 9 available
            var i = 0;
            while (!stream.EndOfStream)             // Loop until we get to the end of the file
            {
                Names[i] = stream.ReadLine();       // Load the line from the file into the array
                i++;                                // Increment our index variable
            }
            stream.Close();     // Extremely important. Don not forget to close these
            file.Close();

            VirtualConsole.Send(String.Format("Read {0} entries", i));

            // We hardcoded the number of entries below,  this is NOT a best practice in coding.  we should know how many we read and 
            // all processing from that point on should be based on the amount of data that was read.  This also means using a hard array is 
            // not the right answer.    As an exercise, investigate what other storage method that can change based on data read in could be
            // used instead of an array.
            // Hint: it's not a dictionary.

            for (i = 0; i < 10; i++)                // Loop to load the array into the touch panel strings
                myXpanel.StringInput[(uint)(30 + i)].StringValue = Names[i];        // (uint) casts the integers to unsigned integers
        }

        // Student exercise to sort the array of names.  Showing off that you do not have to write it completely
        // on your own in C# as we can leverage the methods present in the Array Class
        private void SortFile()
        {

            // This is a Bubble Sort.  in Simpl Plus you had to write your own sorting functions.
            /*
            bool changed;
            do
            {
                changed = false;                                        // Reset our flag

                for(int i = 0 ; i < 9; i++)                             // Iterate through our array
                {
                    if( String.Compare(Names[i],Names[i+1]) > 0 )       // Is the Left higher than the right in alphabetic value?
                    {
                        string temp = "";                               // Create a  storage container

                        temp = Names[i];                                // Save the left in the container
                        Names[i] = Names[i + 1];                        // Copy the right over to the left
                        Names[i + 1] = temp;                            // Restore the Saved name in the Right side to finish the swap
                        changed = true;                                 // Set our flag that we changed something
                    }
                }


            } while (changed == true);     // Keep Looping if data was changed or swapped  This can also be expressed as while(changed)
            */


            Array.Sort(Names);   // Yes that is all there is to sorting an array in C#
                                 // C# Array.Sort uses quick sort in the background on strings.  This is a standard C# class and method
                                 // This is not a Crestron Specific Method.

            for (var i = 0; i < 10; i++)            // Loop to load the array into the touch panel strings
                myXpanel.StringInput[(uint)(30 + i)].StringValue = Names[i];
        }

        // This is how you create a Virtual Console Command Method: This is a freebie that is not a part of the Masters Session
        private string TestFunc(string s)
        {
            VirtualConsole.Send(string.Format("Test Command line Called and  after it was sent {0}", s));

            return "";  //return an empty string.
        }

    }
}