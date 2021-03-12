# Masters 2021 MCP-101 Helper Library 
<center>
<img src="https://img.shields.io/badge/Language-C Sharp-blue">  <img src="https://img.shields.io/badge/Platform-Crestron-blue">  <img src="https://img.shields.io/badge/Masters- 2021-blue"></center>

 The Library is released for the Masters 2021  C# for Crestron MCP-101, 201, 301 class.  Assorted files for the class and to help the student are also included.    A compiled version of the library is included as a DLL as well for ease of use by the students in class. 

 **NOTE:** These are simplified Libraries for students to use to learn concepts from.  They are not 100% complete in regards to error testing and error recovery.  It is left up to the students to complete the work on their own when they progress to that level.  There are documents included for the student for the class as well as the class Xpanel used for the exercises.

Complete Simplified MCP101 C# class code as an example will be provided here after masters is complete.

#### **TCPClientHelper**

This class simplifies making a TCP/IP connection to a server or device. It automatically creates a worker thread and utilizes a serial queue to emulate a serial port to the programmer.  You send your command to  the TX property and if connected to the device or service it will send that string.

When a string comes in from the remote device it will raise an event with that information as well as update the RX property with the string that was received.  Connect and Disconnect methods allow the programmer to have control over the connection.  Limited status information is available as well as limited errors as to why a connection failed or disconnected.  The Class is more of an example on how to use the TCP Client method and leveraging a thread to make the code non blocking.  The programmer is encouraged to expand its capabilities and add in the proper error checking to make it more robust.

#### **LogFileWriter**

This class will write time and date stamped  lines that end in [0D][0A] to a file specified by the programmer.  A Property for the path and properties to return the program path as well as the user folder path are included with a single method that will open, write, and close the file.  It also creates the file if it does not exist.  

This is a simplified example method provided as a starting point for the programmer to expand.  You are encouraged to expand and add in more error checking and safety features to ensure operation across platforms.

#### **VirtualConsole**

This is the Virtual Console class that was provided in Masters 2019 with minor changes. It allows the user of a console for debugging and custom commands for a Crestron Virtual Control server room program.  NOTE: You MUST open the port used on the host operating system.  Failure to open the port in the OS firewall on Virtual Control will cause communication to not function.

There is no authentication, and it is a wide open telnet style connection.  There is no security at all programmed into this class.  It is recommended to the user to add such features or upgrade it to use AES encryption if security is needed.

This class will function on a 4 series processor if the programmer wanted a limited access console without authentication.




## How to Use it

 In your C# program you need to add the DLL file to your references.   In the solution Explorer select references, add reference and then the browse button to find where you placed the dll file.  Select it and make sure  it has a check mark next to it before hitting OK on the last dialog box.

Then at the top of your class files where you want to use the library add in the using statement for the namespace.
```C#
using MastersHelperLibrary;
```



### TCPClientHelper

This class is a public class and needs to be instantiated.  When you do you will supply the IP Address or hostname and the port number.   There is also an event that needs to be subscribed to, do that as well right away.
```C#
myClient = new TCPClientHelper("127.0.0.1", 55555);
myClient.tcpHelperEvent += MyClient_tcpHelperEvent;
```

The event has arguments that will contain information.   RX is the last packet recieved by the client, Message contains what kind of event was thrown as well as Connected boolean and a status integer.

to Send text to the connected device simply send a string to the myClient.TX property.



### LogFileWriter

This is a public class so you instantiate it like any other. it takes no arguments for the default constructor.
```C#
myLog = new LogFileWriter();
```
The path to the log file needs to be specified on the LogPath property.    The processors path to the program and the User Folder are available in the ProgramPath and UserPath properties.

```c#
myLog.LogPath = myLog.UserPath + "logfilename.txt";
```
The ProgramPath and UserPath read only properties will have the seperator already at the end and is not needed to be added by the programmer.

Writing a line to the log by calling the WriteLog() Method. It accepts a string and will prepend the date and time stamp to the line before it is written. 

```c#
myLog.WriteLog("Program has started");
myLog.WriteLog(string.Format(" Variable a={0}",a));
```



### VirtualConsole

This is a Static class and does not have to be instantiated.   The Virtual Console can be started easily by calling the start method.
**NOTE:** you MUST open the port you intend to use when using this class with Crestron Virtual Control.   Failure to open the port desired in the Linux host OS will cause the VirtualConstrol to not function.

```C#
VirtualConsole.Start(45545);
```

you can add in custom console commands to trigger methods in your program.   The method you call MUST accept a string and return a string.
```C#
VirtualConsole.AddNewConsoleCommand(TestFunc, "Test", "This should respond with a message");

        private string TestFunc(string s)
        {
            VirtualConsole.Send("RESPONSE MESSAGE");
            return "test";
        }
```

Sending a message to the virtual console has the method Send that as an overload.   One sends a single line the other can be used to send multiple lines.  Setting the second property to True will have a command prompt right after the line.  setting the property to false will not.

```c#
VirtualConsole.Send("RESPONSE MESSAGE");
VirtualConsole.Send("Welcome to Program Information", false);
VirtualConsole.Send("------------------------------",true);
```
