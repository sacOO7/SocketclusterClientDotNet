.Net Socketcluster Client
=====================================

Overview
--------
This client provides following functionality

- Support for emitting and listening to remote events
- Automatic reconnection
- Pub/sub
- Authentication (JWT)

Client supports following platforms

- .Net 2.0
- .Net 3.5
- .Net 4.0
- Mono
- Silverlight
- WindowsPhone
- Xamarin.Android
- Xamarin.iOS
- Unity

License
-------
Apache License, Version 2.0

Nuget
------
Yet to be uploaded. Library is built on top of Websocket4Net and Newtonsoft.Json. Install those packages via nuget and add source
files into project

Description
-----------
Create instance of `Socket` class by passing url of socketcluster-server end-point

```C#
    //Create a socket instance
    string url="ws://localhost:8000/socketcluster/";
    var socket=new Socket(url);

```
**Important Note** : Default url to socketcluster end-point is always *ws://somedomainname.com/socketcluster/*.


#### Registering basic listeners

Create a class implementing `BasicListener` interface and pass it's instance to socket setListener method

```C#

        internal class MyListener:BasicListener
        {
            public void onConnected(Socket socket)
            {
                Console.WriteLine("connected got called");
            }

            public void onDisconnected(Socket socket)
            {
                Console.WriteLine("disconnected got called");
            }

            public void onConnectError(Socket socket, ErrorEventArgs e)
            {
                Console.WriteLine("on connect error got called");
            }

            public void onAuthentication(Socket socket, bool status)
            {
                Console.WriteLine(status ? "Socket is authenticated" : "Socket is not authenticated");
            }

            public void onSetAuthToken(string token, Socket socket)
            {
                socket.setAuthToken(token);
                Console.WriteLine("on set auth token got called");
            }

        }

        internal class Program
        {
            public static void Main(string[] args)
            {
                var socket=new Socket("ws://localhost:8000/socketcluster/");
                socket.setListerner(new MyListener());
            }
        }


```

#### Connecting to server

- For connecting to server:

```C#
    //This will send websocket handshake request to socketcluster-server
    socket.connect();
```

- By default reconnection to server is not enabled , to enable it :

```C#
    //This will set automatic-reconnection to server with delay of 3 seconds and repeating it for 30 times
    socket.setReconnectStrategy(new ReconnectStrategy().setMaxAttempts(30));
    socket.connect()
```

- To disable reconnection :

```
   socket.setReconnectStrategy(null);
```

Emitting and listening to events
--------------------------------
#### Event emitter

- eventname is name of event and message can be String, boolean, Long or JSON-object

```C#
    socket.emit(eventname,message);

    //socket.emit("chat","Hi");
```

- To send event with acknowledgement

```C#

    socket.emit("chat", "Hi", (eventName, error, data) =>
    {
       //If error and data is String
       Console.WriteLine("Got message for :"+eventName+" error is :"+error+" data is :"+data);
    });

```

#### Event Listener

- For listening to events :

The object received can be String, Boolean, Long or JSONObject.

```C#

    socket.on("chat", (eventName, data) =>
    {
        Console.WriteLine("got message "+ data+ " from event "+eventName);
    });

```

- To send acknowledgement back to server

```C#

    socket.on("chat", (eventName, data, ack) =>
    {
        Console.WriteLine("got message "+ data+ " from event "+eventName);
        ack(name, "No error", "Hi there buddy");
    });

```

Implementing Pub-Sub via channels
---------------------------------

#### Creating channel

- For creating and subscribing to channels:

```C#
    var channel=socket.createChannel(channelName);
   //var channel=socket.createChannel("yolo");


    /**
     * without acknowledgement
     */
     channel.subscribe();

    /**
     * with acknowledgement
     */

    channel.subscribe((channelName, error, data) =>
    {
       if (error == null)
       {
             Console.WriteLine("Subscribed to channel "+channelName+" successfully");
       }
    });
```

- For getting list of created channels :

```C#
    List<Socket.Channel> channels=socket.getChannels();
```

- To get channel by name :

```C#
    var channel=socket.getChannelByName("yell");
    //Returns null if channel of given name is not present

```

#### Publishing event on channel

- For publishing event :

```C#
       // message can have any data type
    /**
     * without acknowledgement
     */
     channel.publish(message);

    /**
     * with acknowledgement
     */
       channel.publish(message, (channelName, error, data) =>
       {
            if (error == null) {
               Console.WriteLine("Published message to channel "+channelName+" successfully");
            }
       });

```

#### Listening to channel

- For listening to channel event :

```C#

    //If instance of channel exists
    channel.onMessage((channelName, data) =>
    {
         Console.WriteLine("Got message for channel "+channelName+" data is "+data);
    });

    //or
    socket.onSubscribe(channelName, (channelName, data) =>
    {
        Console.WriteLine("Got message for channel "+channelName+" data is "+data);
    });

```

<!--###### Pub-sub without creating channel-->
#### Un-subscribing to channel

```C#

    /**
     * without acknowledgement
     */

     channel.unsubscribe();

    /**
     * with acknowledgement
     */

    channel.unsubscribe((name, error, data) =>
    {
        if (error == null) {
            Console.WriteLine("channel unsubscribed successfully");
        }
    });

```

#### Handling SSL connection with server

To enable or disable SSL certficate verification use
```C#

socket.setSSLCertVerification(true/false);

```

#### Setting HTTP proxy with server



```C#

//args, string : host , int : port
socket.setProxy(host,port);

```
