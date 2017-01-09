using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuperSocket.ClientEngine;
using WebSocket4Net;
using DataReceivedEventArgs = WebSocket4Net.DataReceivedEventArgs;


namespace Sample
{
    public class Socket :Emitter
    {
        public WebSocket _socket;
        private string URL;
        private string id;
        private long counter;
        private string AuthToken;
        List<Channel> channels;
        private Dictionary<long, object[]> acks;
        private BasicListener _listener;

        public Socket(string URL)
        {

            _socket = new WebSocket(URL);
            counter = 0;
            channels = new List<Channel>();

            // hook in all the event handling
            _socket.Opened += new EventHandler(OnSocketOpened);
            _socket.Error += new EventHandler<ErrorEventArgs>(websocket_Error);
            _socket.Closed += new EventHandler(websocket_Closed);
            _socket.MessageReceived += new EventHandler<MessageReceivedEventArgs>(websocket_MessageReceived);
            _socket.DataReceived +=new EventHandler<DataReceivedEventArgs>(websocket_DataReceived);

        }

        public Channel createChannel(string name)
        {
            var channel=new Channel(this,name);
            channels.Add(channel);
            return channel;
        }

        public List<Channel> getChannels()
        {
            return channels;
        }

        public Channel getChannelByName(string name)
        {
            return channels.FirstOrDefault(channel => channel.getChannelName().Equals(name));
        }

        private void subscribeChannels()
        {
            foreach (var channel in channels)
            {
                channel.subscribe();
            }
        }
        public void setAuthToken(string token){
            AuthToken=token;
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

                switch (Parser.parse(dataobject, rid, cid, Event))
                {
                    case Parser.ParseResult.ISAUTHENTICATED:
                        Console.WriteLine("IS authenticated got called");
                        id = (string) dataobject.GetValue("id");
                        _listener.onAuthentication(this, (bool) dataobject.GetValue("isAuthenticated"));
                        subscribeChannels();
                        break;
                    case Parser.ParseResult.PUBLISH:
                        handlePublish((string) dataobject.GetValue("channel"), dataobject.GetValue("data"));
                        Console.WriteLine("Publish got called");
                        break;
                    case Parser.ParseResult.REMOVETOKEN:
                        setAuthToken(null);
                        Console.WriteLine("Removetoken got called");
                        break;
                    case Parser.ParseResult.SETTOKEN:
                        _listener.onSetAuthToken((string) dataobject.GetValue("token"), this);
                        Console.WriteLine("Set token got called");
                        break;
                    case Parser.ParseResult.EVENT:
                        Console.WriteLine("Evemt got called");
                        if (hasEventAck(Event))
                        {
                            handleEmitAck(Event, dataobject, ack(cid));
                        }
                        else
                        {
                            handleEmit(Event, dataobject);
                        }
                        break;
                    case Parser.ParseResult.ACKRECEIVE:

                        Console.WriteLine("Ack receive got called");
                        if (acks.ContainsKey(rid.Value))
                        {
                            object[] Object = acks[rid.Value];
                            acks.Remove(rid.Value);
                            if (Object != null)
                            {
                                Ack fn = (Ack) Object[1];
                                if (fn != null)
                                {
                                    fn.call((string) Object[0], dict.GetValue<object>("error", null),
                                        dict.GetValue<object>("data", null));
                                }
                                else
                                {
                                   Console.WriteLine("Ack function is null");
                                }
                            }

                        }
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

        private Ack ack(long? cid)
        {
            return new InternalAck(cid,_socket);
        }

        private class InternalAck:Ack
        {
            private long? rid;
            private WebSocket socket;

            public InternalAck(long? cid,WebSocket socket)
            {
                rid = cid;
                this.socket = socket;
            }
            public void call(string name, object error, object data)
            {
                Dictionary<string,object> dataObject=new Dictionary<string, object>{{"error",error},{"data",data},{"rid",rid}};
                var json = JsonConvert.SerializeObject(dataObject, Formatting.Indented);
                socket.Send(json);

            }
        }

        public Socket emit(string Event, object Object)
        {
            Dictionary<string,object> eventObject=new Dictionary<string, object>{{"event",Event},{"data",Object}};
            var json = JsonConvert.SerializeObject(eventObject, Formatting.Indented);
            _socket.Send(json);
            return this;
        }

        public Socket emit(string Event, object Object, Ack ack)
        {
            acks.Add(counter,getAckObject(Event,ack));
            Dictionary<string,object> eventObject=new Dictionary<string, object>{{"event",Event},{"data",Object},{"cid",Interlocked.Increment(ref counter)}};
            var json = JsonConvert.SerializeObject(eventObject, Formatting.Indented);
            _socket.Send(json);
            return this;
        }

        public Socket subscribe(string Channel)
        {

            Dictionary<string,object> subscribeObject=new Dictionary<string, object>{{"event","#subscribe"},{"data",new Dictionary<string,string>(){{"channel",Channel}}},{"cid",Interlocked.Increment(ref counter)}};
            var json = JsonConvert.SerializeObject(subscribeObject, Formatting.Indented);
            _socket.Send(json);
            return this;
        }

        public Socket subscribe(string Channel,Ack ack)
        {
            acks.Add(counter, getAckObject(Channel, ack));
            Dictionary<string,object> subscribeObject=new Dictionary<string, object>{{"event","#subscribe"},{"data",new Dictionary<string,string>(){{"channel",Channel}}},{"cid",Interlocked.Increment(ref counter)}};
            var json = JsonConvert.SerializeObject(subscribeObject, Formatting.Indented);
            _socket.Send(json);
            return this;
        }

        public Socket unsubscribe(string Channel)
        {
            Dictionary<string,object> subscribeObject=new Dictionary<string, object>{{"event","#unsubscribe"},{"data",Channel},{"cid",Interlocked.Increment(ref counter)}};
            var json = JsonConvert.SerializeObject(subscribeObject, Formatting.Indented);
            _socket.Send(json);
            return this;
        }

        public Socket unsubscribe(string Channel,Ack ack)
        {
            acks.Add(counter, getAckObject(Channel, ack));
            Dictionary<string,object> subscribeObject=new Dictionary<string, object>{{"event","#unsubscribe"},{"data",Channel},{"cid",Interlocked.Increment(ref counter)}};
            var json = JsonConvert.SerializeObject(subscribeObject, Formatting.Indented);
            _socket.Send(json);
           return this;

        }

        public Socket publish(string Channel, object data)
        {
            Dictionary<string,object> publishObject=new Dictionary<string, object>{{"event","#publish"},{"data",new Dictionary<string,object>{{"channel",Channel},{"data",data}}},{"cid",Interlocked.Increment(ref counter)}};
            var json = JsonConvert.SerializeObject(publishObject, Formatting.Indented);
            _socket.Send(json);
            return this;
        }

        public Socket publish(string Channel, object data, Ack ack)
        {
            acks.Add(counter, getAckObject(Channel, ack));
            Dictionary<string,object> publishObject=new Dictionary<string, object>{{"event","#publish"},{"data",new Dictionary<string,object>{{"channel",Channel},{"data",data}}},{"cid",Interlocked.Increment(ref counter)}};
            var json = JsonConvert.SerializeObject(publishObject, Formatting.Indented);
            _socket.Send(json);
            return this;
        }


        private object[] getAckObject(string Event,Ack ack){
            object [] Object ={Event,ack};
            return Object;
        }

        public class Channel
        {
            private string Channelname;
            private Socket socket;

            public Channel(Socket socket,string channelName)
            {
                this.socket = socket;
                Channelname = channelName;
            }
            public void subscribe()
            {
                socket.subscribe(Channelname);
            }

            public void subscribe(Ack ack)
            {
                socket.subscribe(Channelname, ack);
            }

            public void onMessage(Listener listener)
            {
                socket.onSubscribe(Channelname, listener);
            }

            public void publish(object data)
            {
                socket.publish(Channelname, data);
            }

            public void publish(object data, Ack ack)
            {
                socket.publish(Channelname, data, ack);
            }

            public void unsubscribe()
            {
                socket.unsubscribe(Channelname);
                socket.channels.Remove(this);
            }

            public void unsubscribe(Ack ack)
            {
                socket.unsubscribe(Channelname, ack);
                socket.channels.Remove(this);
            }

            public string getChannelName()
            {
                return Channelname;
            }
        }

    }

}