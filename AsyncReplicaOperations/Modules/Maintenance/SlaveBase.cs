using System;
using System.Collections.Generic;

namespace AsyncReplicaOperations
{
    public abstract class SlaveBase
    {
        private Guid id;

        protected SlaveBase()
        {
            this.Id = Guid.NewGuid();
            this.validationAPI();
            this.registerClass();
        }

        public Guid Id
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

        

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        protected void registerClass()
        {
            GlobalCoordinator.GetInstance().Add(this);
        }

        protected void validationAPI()
        {
            var t = this.GetType();
            List<ParameterMethodAttribute> attributes = new List<ParameterMethodAttribute>();
            foreach (var methodInfo in t.GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static))
            {
                foreach(var attribute in methodInfo.GetCustomAttributes(true))
                {
                    if (!(attribute is ParameterMethodAttribute)) continue;
                    var paramAttr = attribute as ParameterMethodAttribute;
                    if(!attributes.Exists(x=>x.VariableName == paramAttr.VariableName))
                    {
                        attributes.Add(paramAttr);
                    }
                }
            }
            var validResult = OperationsAPI.IsValid(attributes);
            if (!validResult.Key)
            {
                var strException = "Настройка API не валидны.Нужно заполнить следующие поля API:";
                foreach(var s in validResult.Value)
                {
                    strException += "\n" + s.ToUpper();
                }
                throw new Exception(strException);
            }
        }
    }
}
