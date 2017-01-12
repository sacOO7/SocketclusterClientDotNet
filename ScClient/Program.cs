using System;
using System.Collections.Generic;
using System.Threading;
using SuperSocket.ClientEngine;


namespace ScClient
{
    internal class MyListener:BasicListener
    {
        public void onConnected(Socket socket)
        {
            Console.WriteLine("connected got called");
            new Thread(() =>
            {
                Thread.Sleep(2000);
                socket.emit("chat", "Hi sachin", (evemtname, error, data) =>
                {

                });
                socket.getChannelByName("yell").publish("Hi there,How are you")

            }).Start();

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
            socket.setReconnectStrategy(new ReconnectStrategy().setMaxAttempts(10));
            socket.connect();

            socket.on("chat", (name, data, ack) =>
            {
                Console.WriteLine("got message "+ data+ " from event "+name);
                ack(name, "No error", "Hi there buddy");
            });

            new Thread(() =>
            {
                Thread.Sleep(1000);
                socket.createChannel("yell").subscribe();

            }).Start();

            socket.on("yell",(name, data, ack) =>
            {
                Console.WriteLine("event :"+name+" data:"+data);
                ack(name, " yell error ", " This is sample data ");
            });


            socket.onSubscribe("yell", (name, data) =>
            {
                Console.WriteLine("Got data for channel:: "+name+ " data :: "+data);
            });



            Console.ReadKey();

        }
    }
}