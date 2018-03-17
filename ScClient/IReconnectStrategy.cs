namespace ScClient
{
    public interface IReconnectStrategy
    {
        bool AreAttemptsComplete();
        int GetReconnectInterval();
        void ProcessValues();
        void Reset();
        void SetAttemptsMade(int count);
        IReconnectStrategy SetMaxAttempts(int attempts);
    }
}