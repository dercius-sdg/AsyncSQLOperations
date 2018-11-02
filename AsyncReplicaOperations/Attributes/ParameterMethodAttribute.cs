using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncReplicaOperations
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor,AllowMultiple = true,Inherited = true)]
    public class ParameterMethodAttribute:Attribute
    {
        private string variableName;

        public ParameterMethodAttribute(string value)
        {
            this.variableName = value;
        }

        public string VariableName { get { return variableName; } }
    }
}
