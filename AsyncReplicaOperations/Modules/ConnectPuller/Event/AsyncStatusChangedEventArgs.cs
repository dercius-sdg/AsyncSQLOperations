using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncReplicaOperations
{
    public class AsyncStatusChangedEventArgs:EventArgs
    {
        private RunningStatusEnum oldStatus;
        private RunningStatusEnum newStatus;

        public AsyncStatusChangedEventArgs(RunningStatusEnum oldStatus,RunningStatusEnum newStatus)
        {
            this.oldStatus = oldStatus;
            this.newStatus = newStatus;
        }

        public RunningStatusEnum OldStatus { get { return oldStatus; } }
        public RunningStatusEnum NewStatus { get { return newStatus; } }
    }
}
