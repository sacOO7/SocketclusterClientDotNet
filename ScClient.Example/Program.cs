using SuperSocket.ClientEngine;
using System;
using System.Threading;

namespace ScClient.Examples
{
    internal class Program : IBasicListener
    {
        public void OnConnected(Socket socket)
        {
            Console.WriteLine("connected got called");
            new Thread(() =>
            {
                Thread.Sleep(2000);
                socket.Emit("chat", "Hi sachin", (evemtname, error, data) => { });
                socket.GetChannelByName("yell").Publish("Hi there,How are you");
            }).Start();
        }

        public void OnDisconnected(Socket socket)
        {
            Console.WriteLine("disconnected got called");
        }

        public void OnConnectError(Socket socket, ErrorEventArgs e)
        {
            Console.WriteLine("on connect error got called");

        }

        public void OnAuthentication(Socket socket, bool status)
        {
            Console.WriteLine(status ? "Socket is authenticated" : "Socket is not authenticated");
        }

        public void OnSetAuthToken(string token, Socket socket)
        {
            socket.SetAuthToken(token);
            Console.WriteLine("on set auth token got called");
        }

        public static void Main(string[] args)
        {
            var socket = new Socket("ws://localhost:8000/socketcluster/");
            socket.SetListerner(new Program());
            socket.SetReconnectStrategy(new ReconnectStrategy().SetMaxAttempts(10));
            socket.Connect();

            socket.On("chat", (name, data, ack) =>
            {
                Console.WriteLine("got message " + data + " from event " + name);
                ack(name, "No error", "Hi there buddy");
            });

            new Thread(() =>
            {
                Thread.Sleep(1000);
                socket.CreateChannel("yell").Subscribe();
            }).Start();

            socket.On("yell", (name, data, ack) =>
            {
                Console.WriteLine("event :" + name + " data:" + data);
                ack(name, " yell error ", " This is sample data ");
            });


            socket.OnSubscribe("yell",
                (name, data) => { Console.WriteLine("Got data for channel:: " + name + " data :: " + data); });


            Console.ReadKey();
        }
    }
}
