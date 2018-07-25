using System;
using System.Collections.Generic;
using System.Text;

namespace ScClient.Models
{
    public class ReceiveEvent
    {
        public object Data { get; set; }
        public object Error { get; set; }
        public long? Rid { get; set; }
    }
}
