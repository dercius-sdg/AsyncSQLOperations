using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncReplicaOperations
{
    public class AsyncLogEventArgs:EventArgs
    {
        private PullValue logValue;

        public PullValue LogValue { get { return logValue; } set { logValue = value; } }
    }
}
