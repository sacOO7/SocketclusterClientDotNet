using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using ScClient.Models;
using SuperSocket.ClientEngine;
using SuperSocket.ClientEngine.Proxy;
using WebSocket4Net;

namespace ScClient
{
    public class Socket : Emitter
    {
        private readonly WebSocket _socket;
        private string _id;
        private long _counter;
        private string _authToken;
        private readonly List<Channel> _channels;
        private IReconnectStrategy _strategy;
        private readonly Dictionary<long?, object[]> acks;
        private IBasicListener _listener;
        private IJsonConverter _jsonConverter;

        public Socket(string url, IJsonConverter jsonConverter = null)
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
            _jsonConverter = jsonConverter ?? new NewtonSoftJsonConverter();
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
            var channel = new Channel(this, name);
            _channels.Add(channel);
            return channel;
        }

        public List<Channel> GetChannels()
        {
            return _channels;
        }

        public Channel GetChannelByName(string name)
        {
            return _channels.FirstOrDefault(channel => channel.GetChannelName().Equals(name));
        }

        private void SubscribeChannels()
        {
            foreach (var channel in _channels)
            {
                channel.Subscribe();
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

            var authObject = EventFactory.GetHandshakeEventObject(_authToken, Interlocked.Increment(ref _counter));
            var json = _jsonConverter.Serialize(authObject);

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
                new Thread(Reconnect).Start();
            }
        }

        private void OnWebsocketMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (e.Message == "#1")
            {
                _socket.Send("#2");
            }
            else
            {
                Console.WriteLine("Message received :: "+e.Message);

                var messageEvent = _jsonConverter.Deserialize<MessageEvent>(e.Message);

                AuthEvent authEvent;
                switch (Parser.Parse(messageEvent))
                {
                    case Parser.MessageType.Isauthenticated:
                        authEvent = _jsonConverter.Deserialize<AuthEvent>(_jsonConverter.Serialize(messageEvent.Data));
                        _id = authEvent.Id;
                        _listener.OnAuthentication(this, authEvent.IsAuthenticated);
                        SubscribeChannels();
                        break;
                    case Parser.MessageType.Publish:
                        var channelEvent = _jsonConverter.Deserialize<ChannelEvent>(_jsonConverter.Serialize(messageEvent.Data));
                        HandlePublish(channelEvent.Channel, channelEvent.Data);
                        break;
                    case Parser.MessageType.Removetoken:
                        SetAuthToken(null);
                        break;
                    case Parser.MessageType.Settoken:
                        authEvent = _jsonConverter.Deserialize<AuthEvent>(_jsonConverter.Serialize(messageEvent.Data));
                        _listener.OnSetAuthToken(authEvent.Token, this);
                        break;
                    case Parser.MessageType.Event:

                        if (HasEventAck(messageEvent.Event))
                        {
                            HandleEmitAck(messageEvent.Event, messageEvent.Data, Ack(messageEvent.Cid.Value));
                        }
                        else
                        {
                            HandleEmit(messageEvent.Event, messageEvent.Data);
                        }

                        break;
                    case Parser.MessageType.Ackreceive:
                        if (acks.ContainsKey(messageEvent.Rid))
                        {
                            var Object = acks[messageEvent.Rid];
                            acks.Remove(messageEvent.Rid);
                            if (Object != null)
                            {
                                var fn = (Ackcall) Object[1];
                                if (fn != null)
                                {
                                    fn((string) Object[0], messageEvent.Error,
                                        messageEvent.Data);
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
        }


        private void OnWebsocketDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine("Data received ");
        }

        public void Connect()
        {
            _socket.Open();
        }

        private void Reconnect()
        {
            _strategy.ProcessValues();
            Thread.Sleep(_strategy.GetReconnectInterval());
            Connect();
        }

        public void Disconnect()
        {
            _socket.Close();
        }

        private Ackcall Ack(long cid)
        {
            return (name, error, data) =>
            {
                var dataObject = EventFactory.GetReceiveEventObject(data, error, cid);
                var json = _jsonConverter.Serialize(dataObject);
                _socket.Send(json);
            };
        }


        public Socket Emit(string @event, object data)
        {
            long count = Interlocked.Increment(ref _counter);
            var eventObject = EventFactory.GetEmitEventObject(@event, data, count);
            var json = _jsonConverter.Serialize(eventObject);
            _socket.Send(json);
            return this;
        }

        public Socket Emit(string @event, object data, Ackcall ack)
        {
            long count = Interlocked.Increment(ref _counter);
            var eventObject = EventFactory.GetEmitEventObject(@event, data, count);
            acks.Add(count, GetAckObject(@event, ack));
            var json = _jsonConverter.Serialize(eventObject);
            _socket.Send(json);
            return this;
        }

        public Socket Subscribe(string channel)
        {
            long count = Interlocked.Increment(ref _counter);
            var subscribeObject = EventFactory.GetSubscribeEventObject(channel, count);
            var json = _jsonConverter.Serialize(subscribeObject);
            _socket.Send(json);
            return this;
        }

        public Socket Subscribe(string channel, Ackcall ack)
        {
            long count = Interlocked.Increment(ref _counter);
            var subscribeObject = EventFactory.GetSubscribeEventObject(channel, count);
            acks.Add(count, GetAckObject(channel, ack));
            var json = _jsonConverter.Serialize(subscribeObject);
            _socket.Send(json);
            return this;
        }

        public Socket Unsubscribe(string channel)
        {
            long count = Interlocked.Increment(ref _counter);
            var subscribeObject = EventFactory.GetUnsubscribeEventObject(channel, count);
            var json = _jsonConverter.Serialize(subscribeObject);
            _socket.Send(json);
            return this;
        }

        public Socket Unsubscribe(string channel, Ackcall ack)
        {
            long count = Interlocked.Increment(ref _counter);
            var subscribeObject = EventFactory.GetUnsubscribeEventObject(channel, count);
            acks.Add(count, GetAckObject(channel, ack));
            var json = _jsonConverter.Serialize(subscribeObject);
            _socket.Send(json);
            return this;
        }

        public Socket Publish(string channel, object data)
        {
            long count = Interlocked.Increment(ref _counter);
            var publishObject = EventFactory.GetPublishEventObject(channel, data, count);
            var json = _jsonConverter.Serialize(publishObject);
            _socket.Send(json);
            return this;
        }

        public Socket Publish(string channel, object data, Ackcall ack)
        {
            long count = Interlocked.Increment(ref _counter);
            var publishObject = EventFactory.GetPublishEventObject(channel, data, count);
            acks.Add(count, GetAckObject(channel, ack));
            var json = _jsonConverter.Serialize(publishObject);
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
                _socket.Unsubscribe(_channelname);
                _socket._channels.Remove(this);
            }

            public void Unsubscribe(Ackcall ack)
            {
                _socket.Unsubscribe(_channelname, ack);
                _socket._channels.Remove(this);
            }

            public string GetChannelName()
            {
                return _channelname;
            }
        }
    }
}