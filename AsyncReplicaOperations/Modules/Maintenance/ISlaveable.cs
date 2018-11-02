using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncReplicaOperations
{
    internal interface ISlaveable:IDisposable
    {
        void registerClass();

        void validationAPI();
    }
}
