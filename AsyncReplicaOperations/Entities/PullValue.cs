namespace AsyncReplicaOperations
{
    public class PullValue
    {
        private string id;
        private string execTime;
        private string resultLog;
        private bool isError;
        private TaskRunningStatus status;

        public string Id
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
            }
        }

        public string Time
        {
            get
            {
                return execTime;
            }
            set
            {
                execTime = value;
            }
        }
        public string Log
        {
            get
            {
                return resultLog;
            }
            set
            {
                resultLog = value;
            }
        }
        public bool ISError
        {
            get
            {
                return isError;
            }
            set
            {
                isError = value;
            }
        }

        public TaskRunningStatus Status
        {
            get
            {
                return status;
            }

            set
            {
                status = value;
            }
        }
    }
}
