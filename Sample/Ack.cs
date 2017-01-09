namespace Sample
{
    public interface Ack
    {
        void call(string name, object error, object data);
    }
}