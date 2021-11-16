﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuperSocket.ClientEngine;
using SuperSocket.ClientEngine.Proxy;
using WebSocket4Net;

namespace ScClient
{
    public class Socket : Emitter
    {
        public WebSocket _socket;
        public string id;
        private long _counter;
        private string _authToken;
        private List<Channel> _channels;
        private IReconnectStrategy _strategy;
        private Dictionary<long?, object[]> acks;
        private IBasicListener _listener;
        private string _ping = "";
        private string _pong = "";

        public Socket(string url, int protocolVersion = 2)
        {
            _socket = new WebSocket(url);
            _counter = 0;
            _strategy = null;
            _channels = new List<Channel>();
            acks = new Dictionary<long?, object[]>();

            // hook in all the event handling
            _socket.Opened += new EventHandler(OnWebsocketConnected);
            _socket.Error += new EventHandler<ErrorEventArgs>(OnWebsocketError);
            _socket.Closed += new EventHandler(OnWebsocketClosed);
            _socket.MessageReceived += new EventHandler<MessageReceivedEventArgs>(OnWebsocketMessageReceived);
            _socket.DataReceived += new EventHandler<DataReceivedEventArgs>(OnWebsocketDataReceived);

            // set ping/pong strings based on protocol version
            if (protocolVersion == 1)
            {
                _ping = "#1";
                _pong = "#2";
            }
        }

        public void SetReconnectStrategy(IReconnectStrategy strategy)
        {
            _strategy = strategy;
        }

        public void SetProxy(string host, int port)
        {
            var proxy = new HttpConnectProxy(new IPEndPoint(IPAddress.Parse(host), port));
            _socket.Proxy = (SuperSocket.ClientEngine.IProxyConnector) proxy;
        }

        public void SetSslCertVerification(bool value)
        {
            _socket.Security.AllowUnstrustedCertificate = value;
        }


        public Channel CreateChannel(string name)
        {
            lock (_channels)
            {
                var channel = new Channel(this, name);
                _channels.Add(channel);
                return channel;
            }
        }

        public List<Channel> GetChannels()
        {
            lock (_channels)
            {
                return _channels;
            }
        }

        public Channel GetChannelByName(string name)
        {
            lock (_channels)
            {
                return _channels.FirstOrDefault(channel => channel.GetChannelName().Equals(name));
            }
        }

        private void SubscribeChannels()
        {
            lock (_channels)
            {
                foreach (var channel in _channels)
                {
                    channel.Subscribe();
                }
            }
        }

        public void SetAuthToken(string token)
        {
            _authToken = token;
        }

        public void SetListerner(IBasicListener listener)
        {
            _listener = listener;
        }

        private void OnWebsocketConnected(object sender, EventArgs e)
        {
            _counter = 0;
            _strategy?.SetAttemptsMade(0);

            var authobject = new Dictionary<string, object>
            {
                {"event", "#handshake"},
                {"data", new Dictionary<string, object> {{"authToken", _authToken}}},
                {"cid", Interlocked.Increment(ref _counter)}
            };
            var json = JsonConvert.SerializeObject(authobject, Formatting.Indented);

            ((WebSocket) sender).Send(json);

            _listener.OnConnected(this);
        }

        private void OnWebsocketError(object sender, ErrorEventArgs e)
        {
            _listener.OnConnectError(this, e);
        }

        private void OnWebsocketClosed(object sender, EventArgs e)
        {
            _listener.OnDisconnected(this);
            if (!_strategy.AreAttemptsComplete())
            {
                new Thread(ReconnectThread).Start();
            }
        }

        private void OnWebsocketMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (e.Message == _ping)
            {
                _socket.Send(_pong);
                return;
            }

//                Console.WriteLine("Message received :: "+e.Message);

            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(e.Message);

            var dataobject = dict.GetValue<object>("data", null);
            var rid = dict.GetValue<long?>("rid", null);
            var cid = dict.GetValue<long?>("cid", null);
            var Event = dict.GetValue<string>("event", null);

//                Console.WriteLine("data is "+e.Message);
//                Console.WriteLine("data is "+dataobject +" rid is "+rid+" cid is "+cid+" event is "+Event);

            switch (Parser.Parse(dataobject, rid, cid, Event))
            {
                case Parser.MessageType.Isauthenticated:
//                        Console.WriteLine("IS authenticated got called");
                    id = (string) ((JObject) dataobject).GetValue("id");
                    _listener.OnAuthentication(this, (bool) ((JObject) dataobject).GetValue("isAuthenticated"));
                    SubscribeChannels();
                    break;
                case Parser.MessageType.Publish:
                    HandlePublish((string) ((JObject) dataobject).GetValue("channel"),
                        ((JObject) dataobject).GetValue("data"));
//                        Console.WriteLine("Publish got called");
                    break;
                case Parser.MessageType.Removetoken:
                    SetAuthToken(null);
//                        Console.WriteLine("Removetoken got called");
                    break;
                case Parser.MessageType.Settoken:
                    _listener.OnSetAuthToken((string) ((JObject) dataobject).GetValue("token"), this);
//                        Console.WriteLine("Set token got called");
                    break;
                case Parser.MessageType.Event:

                    if (HasEventAck(Event))
                    {
                        HandleEmitAck(Event, dataobject, Ack(cid));
                    }
                    else
                    {
                        HandleEmit(Event, dataobject);
                    }

                    break;
                case Parser.MessageType.Ackreceive:

//                        Console.WriteLine("Ack receive got called");
                    if (acks.ContainsKey(rid))
                    {
                        var Object = acks[rid];
                        acks.Remove(rid);
                        if (Object != null)
                        {
                            var fn = (Ackcall) Object[1];
                            if (fn != null)
                            {
                                fn((string) Object[0], dict.GetValue<object>("error", null),
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

//            _socket.Send("Hello World!");
        }


        private void OnWebsocketDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine("Data received ");
        }

        public void Connect()
        {
            if (_socket.State == WebSocketState.Connecting || _socket.State == WebSocketState.Open)
            {
                Console.WriteLine("Socket already connected");
                return;
            }
            _socket.Open();
        }

        private void ReconnectThread()
        {
            // Catch any exception, we can't have a thread that we have no control over
            //  crashing our applications
            try
            {
                _strategy.ProcessValues();
                Thread.Sleep(_strategy.GetReconnectInterval());
                Connect();
            }
            catch (Exception ex)
            {
                //System.Diagnostics.Trace.WriteLine(ex);
            }
        }

        public void Disconnect()
        {
            _socket.Close();
        }

        private Ackcall Ack(long? cid)
        {
            return (name, error, data) =>
            {
                Dictionary<string, object> dataObject =
                    new Dictionary<string, object> {{"error", error}, {"data", data}, {"rid", cid}};
                var json = JsonConvert.SerializeObject(dataObject, Formatting.Indented);
                _socket.Send(json);
            };
        }


        public Socket Emit(string Event, object Object)
        {
//            Console.WriteLine("Emit got called");
            Dictionary<string, object>
                eventObject = new Dictionary<string, object> {{"event", Event}, {"data", Object}};
            var json = JsonConvert.SerializeObject(eventObject, Formatting.Indented);
            _socket.Send(json);
            return this;
        }

        public Socket Emit(string Event, object Object, Ackcall ack)
        {
            long count = Interlocked.Increment(ref _counter);
            Dictionary<string, object> eventObject =
                new Dictionary<string, object> {{"event", Event}, {"data", Object}, {"cid", count}};
            acks.Add(count, GetAckObject(Event, ack));
            var json = JsonConvert.SerializeObject(eventObject, Formatting.Indented);
            _socket.Send(json);
            return this;
        }

        public Socket Subscribe(string channel)
        {
            Dictionary<string, object> subscribeObject = new Dictionary<string, object>
            {
                {"event", "#subscribe"},
                {"data", new Dictionary<string, string> {{"channel", channel}}},
                {"cid", Interlocked.Increment(ref _counter)}
            };
            var json = JsonConvert.SerializeObject(subscribeObject, Formatting.Indented);
            _socket.Send(json);
            return this;
        }

        public Socket Subscribe(string channel, Ackcall ack)
        {
            long count = Interlocked.Increment(ref _counter);
            Dictionary<string, object> subscribeObject = new Dictionary<string, object>
            {
                {"event", "#subscribe"},
                {"data", new Dictionary<string, string>() {{"channel", channel}}},
                {"cid", count}
            };
            acks.Add(count, GetAckObject(channel, ack));
            var json = JsonConvert.SerializeObject(subscribeObject, Formatting.Indented);
            _socket.Send(json);
            return this;
        }

        public Socket Unsubscribe(string channel)
        {
            Dictionary<string, object> subscribeObject = new Dictionary<string, object>
            {
                {"event", "#unsubscribe"},
                {"data", channel},
                {"cid", Interlocked.Increment(ref _counter)}
            };
            var json = JsonConvert.SerializeObject(subscribeObject, Formatting.Indented);
            _socket.Send(json);
            return this;
        }

        public Socket Unsubscribe(string channel, Ackcall ack)
        {
            long count = Interlocked.Increment(ref _counter);
            Dictionary<string, object> subscribeObject =
                new Dictionary<string, object> {{"event", "#unsubscribe"}, {"data", channel}, {"cid", count}};
            acks.Add(count, GetAckObject(channel, ack));
            var json = JsonConvert.SerializeObject(subscribeObject, Formatting.Indented);
            _socket.Send(json);
            return this;
        }

        public Socket Publish(string channel, object data)
        {
            Dictionary<string, object> publishObject = new Dictionary<string, object>
            {
                {"event", "#publish"},
                {"data", new Dictionary<string, object> {{"channel", channel}, {"data", data}}},
                {"cid", Interlocked.Increment(ref _counter)}
            };
            var json = JsonConvert.SerializeObject(publishObject, Formatting.Indented);
            _socket.Send(json);
            return this;
        }

        public Socket Publish(string channel, object data, Ackcall ack)
        {
            long count = Interlocked.Increment(ref _counter);
            Dictionary<string, object> publishObject = new Dictionary<string, object>
            {
                {"event", "#publish"},
                {"data", new Dictionary<string, object> {{"channel", channel}, {"data", data}}},
                {"cid", count}
            };
            acks.Add(count, GetAckObject(channel, ack));
            var json = JsonConvert.SerializeObject(publishObject, Formatting.Indented);
            _socket.Send(json);
            return this;
        }


        private object[] GetAckObject(string Event, Ackcall ack)
        {
            object[] Object = {Event, ack};
            return Object;
        }

        public class Channel
        {
            private readonly string _channelname;
            private readonly Socket _socket;

            public Channel(Socket socket, string channelName)
            {
                this._socket = socket;
                _channelname = channelName;
            }

            public Channel Subscribe()
            {
                _socket.Subscribe(_channelname);
                return this;
            }

            public Channel Subscribe(Ackcall ack)
            {
                _socket.Subscribe(_channelname, ack);
                return this;
            }

            public void OnMessage(Listener listener)
            {
                _socket.OnSubscribe(_channelname, listener);
            }

            public void Publish(object data)
            {
                _socket.Publish(_channelname, data);
            }

            public void Publish(object data, Ackcall ack)
            {
                _socket.Publish(_channelname, data, ack);
            }

            public void Unsubscribe()
            {
                lock (_socket._channels)
                {
                    _socket.Unsubscribe(_channelname);
                    _socket._channels.Remove(this);
                }
            }

            public void Unsubscribe(Ackcall ack)
            {
                lock (_socket._channels)
                {
                    _socket.Unsubscribe(_channelname, ack);
                    _socket._channels.Remove(this);
                }
            }

            public string GetChannelName()
            {
                return _channelname;
            }
        }
    }
}
