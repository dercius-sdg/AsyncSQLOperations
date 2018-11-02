using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncReplicaOperations
{
    public class Restriction
    {
        private string replicaId;
        private string workDirectory;

        public Restriction()
        {
            workDirectory = "";
        }

        public string ReplicaId
        {
            get
            {
                return replicaId;
            }

            set
            {
                replicaId = value;
            }
        }

        public string WorkDirectory
        {
            get
            {
                return workDirectory;
            }

            set
            {
                workDirectory = value;
            }
        }
    }
}
