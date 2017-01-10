using System;

namespace ScClient
{
    public class Parser
    {
        public enum ParseResult
        {
            ISAUTHENTICATED,
            PUBLISH,
            REMOVETOKEN,
            SETTOKEN,
            EVENT,
            ACKRECEIVE
        }

        public static ParseResult parse(object dataobject,long? rid,long? cid,string Event)
        {
            if (Event == null) return rid == 1 ? ParseResult.ISAUTHENTICATED : ParseResult.ACKRECEIVE;
            switch (Event)
            {
                case "#publish":
                    return ParseResult.PUBLISH;
                case "#removeAuthToken":
                    return ParseResult.REMOVETOKEN;
                case "#setAuthToken":
                    return ParseResult.SETTOKEN;
                default:
                    return ParseResult.EVENT;
            }

        }


    }
}