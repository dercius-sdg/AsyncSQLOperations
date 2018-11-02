using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncReplicaOperations
{
     public class FlushFolder : IActioncs
    {
        private string folderPath;
        private string regionId;

        public string FolderPath { get { return folderPath; } set { folderPath = value; } }

        public string RegionId { get { return regionId; }  set { regionId = value; } }

        public void processAction()
        {
            try
            {
                if (Directory.Exists(folderPath))
                {
                    DirectoryInfo di = new DirectoryInfo(folderPath);
                    foreach (FileInfo fi in di.GetFiles())
                    {
                        if (fi.Exists)
                        {
                            fi.Delete();
                        }
                    }
                    foreach (DirectoryInfo fi in di.GetDirectories())
                    {
                        if (fi.Exists)
                        {
                            fi.Delete(true);
                        }
                    }
                }
                FileLogger.GetInstance().WriteLog("Директория " + folderPath + " успешно очищена.", DateTime.Now,regionId);
            }
            catch (Exception e)
            {
                FileLogger.GetInstance().WriteLog(e.Message, DateTime.Now,regionId);
            }
        }
    }
}
