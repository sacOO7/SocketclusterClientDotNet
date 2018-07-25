using System;
using System.Collections.Generic;
using System.Text;

namespace ScClient.Models
{

    public class HandshakeEvent
    {
        public string Event { get; set; }
        public AuthData Data { get; set; }
        public long Cid { get; set; }
    }

    public class AuthData
    {
        public string AuthToken { get; set; }
    }
}
