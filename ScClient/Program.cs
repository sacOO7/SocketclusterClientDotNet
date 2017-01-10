using System;
using SuperSocket.ClientEngine;


namespace ScClient
{
    internal class MyListener:BasicListener
    {
        public void onConnected(Socket socket)
        {
            Console.WriteLine("connected got called");

            socket.emit("chat", "Hi", (name, error, data) =>
            {
                Console.WriteLine("data :: "+data+" error ::"+error);
            });

            socket.createChannel("yell").subscribe((name, error, data) =>
            {
                Console.WriteLine("Successfully scubscribed to channel "+name);
            });
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
            Console.WriteLine(status ? "Socket is authenticated" : "SOcket is not authenticated");
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

            socket.on("chat", (name, data, ack) =>
            {
                Console.WriteLine("got message "+ data+ " from event "+name);
                ack(name, "No error", "Hi there buddy");
            });

            Console.ReadKey();
        }
    }
}