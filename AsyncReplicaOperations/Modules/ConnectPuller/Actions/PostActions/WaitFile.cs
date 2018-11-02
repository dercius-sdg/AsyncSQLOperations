using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncReplicaOperations
{
    public class WaitFile : IActioncs
    {
        private string folderPath;
        private bool isReady = false;

        public string FolderPath { get { return folderPath; } set { folderPath = value; } }
        [ParameterMethod("RetryPackageCount")]
        public async void processAction()
        {
            var retryCount = 0;
            var watcher = new FileSystemWatcher(folderPath, "gmmq.package.end");
            watcher.Deleted += Watcher_Deleted;
            while (!isReady || retryCount < OperationsAPI.RetryPackageCount)
            {
                await Task.Delay(1000);
                retryCount++;
            }
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            if(e.ChangeType == WatcherChangeTypes.Deleted)
            {
                isReady = true;
            }
        }
    }
}
