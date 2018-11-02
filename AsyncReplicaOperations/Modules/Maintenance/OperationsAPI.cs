using System;
using System.Collections.Generic;
using System.Linq;

namespace AsyncReplicaOperations
{
    public static class OperationsAPI
    {
        private static int port;
        private static string host;
        private static string importReplicaProcedure;
        private static string exportReplicaProcedure;
        private static string stageListPath;
        private static string configPath;
        private static int threadCount;
        private static string mailRecep;
        private static string mailCCRecep;
        private static string senderMail;
        private static bool useNotify;
        private static string configServer;
        private static string configDatabase;
        private static int retryPackageCount;
        private static int packageExistWaitTimeOut;

        public static int Port { get { return port; } set { port = value; } }
        public static string Host { get { return host; } set { host = value; } }
        public static string ImportReplicaProcedure { get { return importReplicaProcedure; } set { importReplicaProcedure = value; } }
        public static string ExportReplicaProcedure { get { return exportReplicaProcedure; } set { exportReplicaProcedure = value; } }
        public static string StageListPath { get { return stageListPath; } set { stageListPath = value; } }
        public static string ConfigPath { get { return configPath; } set { configPath = value; } }
        public static int ThreadCount { get { return threadCount; } set { threadCount = value; } }
        public static string MailRecep { get { return mailRecep; } set { mailRecep = value; } }
        public static string MailCCRecep { get { return mailCCRecep; } set { mailCCRecep = value; } }
        public static string SenderMail { get { return senderMail; } set { senderMail = value; } }
        public static bool UseNotify { get { return useNotify; } set { useNotify = value; } }
        public static string ConfigServer { get { return configServer; } }
        public static string ConfigDatabase { get {return configDatabase; } }
        public static int RetryPackageCount { get { return retryPackageCount; } set { retryPackageCount = value; } }

        public static int PackageExistWaitTimeOut
        {
            get
            {
                return packageExistWaitTimeOut;
            }

            set
            {
                packageExistWaitTimeOut = value;
            }
        }

        public static void initAPI()
        {
            configServer = Properties.Settings.Default.ConfigServer;
            configDatabase = Properties.Settings.Default.ConfigDatabase;
        }

        public static KeyValuePair<bool,List<string>> IsValid(List<ParameterMethodAttribute> parameterMethodAttributes)
        {
            KeyValuePair<bool, List<string>> ret = new KeyValuePair<bool, List<string>>();
            bool key = true;
            List<string> valueKey = new List<string>();

            var t = typeof(OperationsAPI);

            foreach(ParameterMethodAttribute param in parameterMethodAttributes)
            {
                var value = t.GetProperty(param.VariableName).GetValue(null);
                if(value == null)
                {
                    key = false;
                    valueKey.Add(param.VariableName);
                }
            }

            ret = new KeyValuePair<bool, List<string>>(key, valueKey);
            return ret;
        }

        public static KeyValuePair<bool, List<string>> IsValid(string variableName)
        {
            KeyValuePair<bool, List<string>> ret = new KeyValuePair<bool, List<string>>();
            bool key = true;
            List<string> valueKey = new List<string>();

            var t = typeof(OperationsAPI);

            var value = t.GetProperty(variableName).GetValue(null);
            if (value == null)
            {
                key = false;
                valueKey.Add(variableName);
            }

            ret = new KeyValuePair<bool, List<string>>(key, valueKey);
            return ret;
        }

        public static List<SlaveBase> GetObject(Type type)
        {
            return GlobalCoordinator.GetInstance().Where(x => x.GetType() == type).ToList<SlaveBase>();
        }
    }
}
