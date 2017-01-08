using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuperSocket.ClientEngine;
using WebSocket4Net;
using DataReceivedEventArgs = WebSocket4Net.DataReceivedEventArgs;


namespace Sample
{
    public class Socket
    {
        public WebSocket _socket;
        private string URL;
        private string id;
        private long counter;
        private string AuthToken;
        private BasicListener _listener;

        public Socket(string URL)
        {

            _socket = new WebSocket(URL);
            counter = 0;

            // hook in all the event handling
            _socket.Opened += new EventHandler(OnSocketOpened);
            _socket.Error += new EventHandler<ErrorEventArgs>(websocket_Error);
            _socket.Closed += new EventHandler(websocket_Closed);
            _socket.MessageReceived += new EventHandler<MessageReceivedEventArgs>(websocket_MessageReceived);
            _socket.DataReceived +=new EventHandler<DataReceivedEventArgs>(websocket_DataReceived);

        }

        public void setListerner(BasicListener listener)
        {
            _listener = listener;
        }

        private void OnSocketOpened(object sender, EventArgs e)
        {

            counter = 0;
            var authobject=new Dictionary<string, object> {{"event", "#handshake"},{"data",new Dictionary<string,object>{{"authToken",AuthToken}}},{"cid",Interlocked.Increment(ref counter)}};
            var json = JsonConvert.SerializeObject(authobject, Formatting.Indented);

            ((WebSocket)sender).Send(json);

            _listener.onConnected(this);

        }
        private void websocket_Error(object sender, ErrorEventArgs e)
        {

            _listener.onConnectError(this,e);

        }
        private void websocket_Closed(object sender, EventArgs e)
        {

            _listener.onDisconnected(this);

        }
        private void websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            // send the message
            Console.WriteLine("Message received"+e.Message);
            if (e.Message == "#1")
            {
                _socket.Send("#2");
            }
            else
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(e.Message);

                var dataobject = dict.GetValue<JObject>("data",null);
                var rid = dict.GetValue<long?>("rid", null);
                var cid = dict.GetValue<long?>("cid",null);
                var Event =  dict.GetValue<string>("event",null);

//                Console.WriteLine("data is "+e.Message);
//                Console.WriteLine("data is "+dataobject +" rid is "+rid+" cid is "+cid+" event is "+Event);

                switch (Parser.parse(dataobject,rid,cid,Event))
                {
                    case Parser.ParseResult.ISAUTHENTICATED:
                        Console.WriteLine("IS authenticated got called");
                        break;
                    case Parser.ParseResult.PUBLISH:
                        Console.WriteLine("Publish got called");
                        break;
                    case Parser.ParseResult.REMOVETOKEN:
                        Console.WriteLine("Removetoken got called");
                        break;
                    case Parser.ParseResult.SETTOKEN:
                        Console.WriteLine("Set token got called");
                        break;
                    case Parser.ParseResult.EVENT:
                        Console.WriteLine("Evemt got called");
                        break;
                    case Parser.ParseResult.ACKRECEIVE:
                        Console.WriteLine("Ack receive got called");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            }

//            _socket.Send("Hello World!");
        }

        private void websocket_DataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine("Data received ");
        }

        public void connect()
        {
            _socket.Open();
        }
    }
}