using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SuperSocket.ClientEngine;
using WebSocket4Net;

namespace Sample
{
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
            Console.WriteLine("on authentication got called");
        }

        public void onSetAuthToken(string token, Socket socket)
        {
            Console.WriteLine("on set auth token got called");
        }
    }

    internal class Program
    {

        public static void Main(string[] args)
        {
           var socket=new Socket("ws://localhost:8000/socketcluster/");
           socket.setListerner(new MyListener());
           socket.connect();
           Console.ReadKey();

//            var points = new Dictionary<string, object> {{"Hi", 1234}};
//            points["hello"] = "asdff";
//            points["data"]=new Dictionary<string, object> {{"Hi", 23543},{"total","dwd"}};
//            var Json = JsonConvert.SerializeObject(points, Formatting.Indented);
//            Console.WriteLine(Json);




        }

    }
}