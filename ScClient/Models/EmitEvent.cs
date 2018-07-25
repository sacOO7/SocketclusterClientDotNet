namespace ScClient.Models
{
    public class EmitEvent
    {
        public string Event { get; set; }
        public object Data { get; set; }
        public long? Cid { get; set; }
    }
}
