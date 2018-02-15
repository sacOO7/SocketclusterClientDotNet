using System;

namespace ScClient
{
    public class Parser
    {
        public enum ParseResult
        {
            Isauthenticated,
            Publish,
            Removetoken,
            Settoken,
            Event,
            Ackreceive
        }

        public static ParseResult Parse(object dataobject, long? rid, long? cid, string Event)
        {
            if (Event == null) return rid == 1 ? ParseResult.Isauthenticated : ParseResult.Ackreceive;
            switch (Event)
            {
                case "#publish":
                    return ParseResult.Publish;
                case "#removeAuthToken":
                    return ParseResult.Removetoken;
                case "#setAuthToken":
                    return ParseResult.Settoken;
                default:
                    return ParseResult.Event;
            }
        }
    }
}