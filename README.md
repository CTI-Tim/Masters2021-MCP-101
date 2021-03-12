# Masters 2021 MCP-101 Helper Library 
<img src="https://img.shields.io/badge/Language-C Sharp-blue">
<img src="https://img.shields.io/badge/Platform-Crestron-blue">
<img src="https://img.shields.io/badge/Masters- 2021-blue">


 The Library is released for the Masters 2021  C# for Crestron MCP-101, 201, 301 class.  Assorted files for the class and to help the student are also included.    A compiled version of the library is included as a dll as well for ease of use by the students in class.

**TCPClientHelper**

This class simplifies making a TCP/IP connection to a server or device. It automatically creates a worker thread and utilizes a serial queue to emulate a serial port to the programmer.  You send your command to  the TX property and if connected to the device or service it will send that string.

When a string comes in from the remote device it will raise an event with that information as well as update the RX property with the string that was received.  Connect and Disconnect methods allow the programmer to have control over the connection.  Limited status information is available as well as limited errors as to why a connection failed or disconnected.  The Class is more of an example on how to use the TCP Client method and leveraging a thread to make the code non blocking.  The programmer is encouraged to expand its capabilities and add in the proper error checking to make it more robust.

**LogFileWriter**

This class will write time and date stamped  lines that end in [0D][0A] to a file specified by the programmer.  A Property for the path and properties to return the program path as well as the user folder path are included with a single method that will open, write, and close the file.  It also creates the file if it does not exist.  
  
This is a simplified example method provided as a starting point for the programmer to expand.  You are encouraged to expand and add in more error checking and safety features to ensure operation across platforms.

**VirtualConsole**

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
<details><summary><b>Event Arguments</b></summary>

The event has arguments that will contain information.   RX is the last packet recieved by the client, Message contains what kind of event was thrown as well as Connected boolean and a status integer.

to Send text to the connected device simply send a string to the myClient.TX property.