using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncReplicaOperations;
using System.Data.SqlClient;
using System.IO;

namespace FlushFolders
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = args[0];
            OperationsAPI.initAPI();
            OperationsAPI.StageListPath = @"\Configs\ConnectStageList.xml";
            OperationsAPI.ConfigPath = @"\Configs\ConnectConfig.xml";
            var globalConf = AsyncReplicaOperations.StageConnectSettings.getInstance();
            var localConf = new GroupSettings(path);
            if (!localConf.isValid) return;

            foreach (RuntimeRegionSettings regionSetting in localConf.EntitiesList)
            {
                SqlConnection sqlConnection = new SqlConnection();
                try
                {
                    var sqlConnectionBuilder = new SqlConnectionStringBuilder();
                    sqlConnectionBuilder.DataSource = globalConf.FindId(regionSetting.RegionId).ServerName;
                    sqlConnectionBuilder.InitialCatalog = globalConf.FindId(regionSetting.RegionId).StageDBName;
                    sqlConnectionBuilder.IntegratedSecurity = true;
                    sqlConnection = new SqlConnection(sqlConnectionBuilder.ToString());
                    if (!SqlConnectionChecker.checkConnection(sqlConnection)) throw new Exception("Невозможно подключиться к региону " + regionSetting.RegionId);
                    var command = new SqlCommand("select FILEPATHSAVE,FILEPATHLOAD from REPLICAS", sqlConnection);
                    sqlConnection.Open();
                    using (var reader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                    {
                        while (reader.Read())
                        {
                            var flushPath = reader.GetString(0);
                            FlushFolder(flushPath);
                            Console.WriteLine(string.Format("[MESSAGE] {0:HH:mm:ss} Папка {1} успешно очищена", DateTime.Now, flushPath));
                            flushPath = reader.GetString(1);
                            FlushFolder(flushPath);
                            Console.WriteLine(string.Format("[MESSAGE] {0:HH:mm:ss} Папка {1} успешно очищена", DateTime.Now, flushPath));
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(string.Format("[ERROR] {0:HH:mm:ss} {1}", DateTime.Now, e.Message));
                }
                finally
                {
                    if (sqlConnection.State == System.Data.ConnectionState.Open)
                    {
                        sqlConnection.Close();
                    }
                }
            }
        }

        private static void FlushFolder(string folderPath)
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

        }
    }
}
