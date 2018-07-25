using System;
using ScClient.Models;

namespace ScClient
{
    public class Parser
    {
        public enum MessageType
        {
            Isauthenticated,
            Publish,
            Removetoken,
            Settoken,
            Event,
            Ackreceive
        }

        public static MessageType Parse(MessageEvent messageEvent)
        {
            if (messageEvent.Event == null) return messageEvent.Rid == 1 ? MessageType.Isauthenticated : MessageType.Ackreceive;
            switch (messageEvent.Event)
            {
                case "#publish":
                    return MessageType.Publish;
                case "#removeAuthToken":
                    return MessageType.Removetoken;
                case "#setAuthToken":
                    return MessageType.Settoken;
                default:
                    return MessageType.Event;
            }
        }
    }
}