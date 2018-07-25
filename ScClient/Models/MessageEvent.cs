namespace ScClient.Models
{
    public class MessageEvent
    {
        public object Data { get; set; }
        public long? Rid { get; set; }
        public long? Cid { get; set; }
        public string Event { get; set; }
        public object Error { get; set; }
    }
}
