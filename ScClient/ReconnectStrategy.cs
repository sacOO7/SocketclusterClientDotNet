namespace ScClient
{
    public class ReconnectStrategy
    {
        /**
     *The number of milliseconds to delay before attempting to reconnect.
     * Default: 2000
     */

        int reconnectInterval;

        /**
         * The maximum number of milliseconds to delay a reconnection attempt.
         * Default: 30000
         */

        int maxReconnectInterval;

        /**
     * The maximum number of reconnection attempts that will be made before giving up. If null, reconnection attempts will be continue to be made forever.
     * Default: null
     */

        int? maxAttempts;

        int attmptsMade;

        public ReconnectStrategy(){
            reconnectInterval=3000;
            maxReconnectInterval=30000;
            maxAttempts=null;  //forever
            attmptsMade=0;
        }

        public ReconnectStrategy setMaxAttempts(int attempts)
        {
            maxAttempts = attempts;
            return this;
        }

        public void Reset()
        {
            attmptsMade = 0;
            reconnectInterval=3000;
            maxAttempts=null;
        }

        public void setAttemptsMade(int count)
        {
            attmptsMade = count;
        }

        public ReconnectStrategy(int reconnectInterval, int maxReconnectInterval, int maxAttempts) {
            if (reconnectInterval>maxReconnectInterval) {
                this.reconnectInterval = maxReconnectInterval;
            }else {
                this.reconnectInterval=reconnectInterval;
            }
            this.maxReconnectInterval = maxReconnectInterval;
            this.maxAttempts = maxAttempts;
            attmptsMade=0;
        }

        public void processValues()
        {
            attmptsMade++;
        }

        public int getReconnectInterval(){
            return reconnectInterval;
        }

        public bool areAttemptsComplete(){
            return attmptsMade==maxAttempts;
        }
    }
}