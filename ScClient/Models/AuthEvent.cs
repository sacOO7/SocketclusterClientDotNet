using System;
using System.Collections.Generic;
using System.Text;

namespace ScClient.Models
{
    public class AuthEvent
    {
        public string Id { get; set; }
        public bool IsAuthenticated { get; set; }
        public string Token { get; set; }
    }
}