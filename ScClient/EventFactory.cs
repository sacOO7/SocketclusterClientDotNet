using System;
using System.Collections.Generic;
using System.Text;
using ScClient.Models;

namespace ScClient
{
    public class EventFactory
    {
        public static EmitEvent GetEmitEventObject(string eventName, object data, long messageId)
        {
            return new EmitEvent()
            {
                Event = eventName,
                Data = data,
                Cid = messageId
            };
        }

        public static ReceiveEvent GetReceiveEventObject(object data, object error, long messageId)
        {
            return new ReceiveEvent()
            {
                Data = data,
                Error = error,
                Rid = messageId
            };
        }


        public static EmitEvent GetSubscribeEventObject(string channelName, long messageId)
        {
            return new EmitEvent()
            {
                Event = "#subscribe",
                Data = new ChannelEvent {Channel = channelName},
                Cid = messageId
            };
        }

        public static EmitEvent GetUnsubscribeEventObject(string channelName, long messageId)
        {
            return new EmitEvent()
            {
                Event = "#unsubscribe",
                Data = new ChannelEvent {Channel = channelName},
                Cid = messageId
            };
        }

        public static EmitEvent GetPublishEventObject(string channelName, object data, long messageId)
        {
            return new EmitEvent()
            {
                Event = "#publish",
                Data = new ChannelEvent {Channel = channelName, Data = data},
                Cid = messageId
            };
        }

        public static HandshakeEvent GetHandshakeEventObject(string authToken, long messageId)
        {
            return new HandshakeEvent()
            {
                Event = "#handshake",
                Data = new AuthData() { AuthToken = authToken},
                Cid = messageId
            };
        }
    }
}