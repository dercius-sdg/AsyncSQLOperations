using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace AsyncReplicaOperations
{
    public class FileLogger:SlaveBase
    {
        private static FileLogger instance;
        private string globalLogPath;

        public static FileLogger GetInstance()
        {
            if(instance == null)
            {
                instance = new FileLogger(); ;
            }
            return instance;
        }

        private FileLogger()
        {
            globalLogPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\logs";
        }

        public void WriteLog(PullValue logValue,DateTime logDate,string RegionId)
        {
            if (!Directory.Exists(globalLogPath + "\\" + RegionId + "\\" + String.Format("{0}", logValue.Id)))
            {
                Directory.CreateDirectory(globalLogPath + "\\" + RegionId + "\\" + String.Format("{0}", logValue.Id));
            }
            var logFileName = globalLogPath + "\\" + RegionId + "\\" + String.Format("{0}", logValue.Id) + "\\" + String.Format("{0:ddMMyyyy}.log", logDate);
            File.AppendAllText(logFileName, String.Format("[{0}] {1:HH:mm:ss} Message: {2}", (logValue.Status == TaskRunningStatus.Failure) ? "ERROR" : "MESSAGE", logDate, logValue.Log) + Environment.NewLine);
        }

        public void WriteLog(string message,DateTime logDate, string RegionId)
        {
            if (!Directory.Exists(globalLogPath + "\\" + RegionId +  "\\globals" ))
            {
                Directory.CreateDirectory(globalLogPath + "\\" + RegionId + "\\globals");
            }
            var logFileName = globalLogPath + "\\" + RegionId + "\\globals" + "\\" + String.Format("{0:ddMMyyyy}.log", logDate);
            File.AppendAllText(logFileName, String.Format("{0:HH:mm:ss} Message: {1}\n", logDate, message) + Environment.NewLine);
        }
    }
}
