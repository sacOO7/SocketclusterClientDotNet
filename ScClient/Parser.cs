using System;

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

        public static MessageType Parse(object dataobject, long? rid, long? cid, string Event)
        {
            if (Event == null) return rid == 1 ? MessageType.Isauthenticated : MessageType.Ackreceive;
            switch (Event)
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