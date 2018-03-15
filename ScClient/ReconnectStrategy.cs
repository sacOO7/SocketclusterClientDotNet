namespace ScClient
{
    public class ReconnectStrategy : IReconnectStrategy
    {
        /**
     *The number of milliseconds to delay before attempting to reconnect.
     * Default: 2000
     */

        private int _reconnectInterval;

        /**
         * The maximum number of milliseconds to delay a reconnection attempt.
         * Default: 30000
         */

        int _maxReconnectInterval;

        /**
     * The maximum number of reconnection attempts that will be made before giving up. If null, reconnection attempts will be continue to be made forever.
     * Default: null
     */

        int? _maxAttempts;

        int _attmptsMade;

        public ReconnectStrategy()
        {
            _reconnectInterval = 3000;
            _maxReconnectInterval = 30000;
            _maxAttempts = null; //forever
            _attmptsMade = 0;
        }

        public IReconnectStrategy SetMaxAttempts(int attempts)
        {
            _maxAttempts = attempts;
            return this;
        }

        public void Reset()
        {
            _attmptsMade = 0;
            _reconnectInterval = 3000;
            _maxAttempts = null;
        }

        public void SetAttemptsMade(int count)
        {
            _attmptsMade = count;
        }

        public ReconnectStrategy(int reconnectInterval, int maxReconnectInterval, int maxAttempts)
        {
            if (reconnectInterval > maxReconnectInterval)
            {
                this._reconnectInterval = maxReconnectInterval;
            }
            else
            {
                this._reconnectInterval = reconnectInterval;
            }

            this._maxReconnectInterval = maxReconnectInterval;
            this._maxAttempts = maxAttempts;
            _attmptsMade = 0;
        }

        public void ProcessValues()
        {
            _attmptsMade++;
        }

        public int GetReconnectInterval()
        {
            return _reconnectInterval;
        }

        public bool AreAttemptsComplete()
        {
            return _attmptsMade == _maxAttempts;
        }
    }
}