using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using AsyncReplicaOperations;

namespace AsyncReplicaTool
{
    static class Global
    {
        public static StageConnectSettings stageSettings;
        public static MainWindow mainWindow;
        public static Dictionary<string, ObservableCollection<PullValue>> dataSources;
    }
}
