using AsyncReplicaOperations;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CheckStageConfig
{
    class Program
    {
        static void Main(string[] args)
        {
            List<string> errorList = new List<string>();
            try
            {
                OperationsAPI.initAPI();
                OperationsAPI.StageListPath = @"\Configs\ConnectStageList.xml";
                Console.WriteLine("Начинается проверка...");
                var globalConf = StageConnectSettings.getInstance();

                if (globalConf.isValid)
                {
                    foreach (RegionSetting globalSetting in globalConf.EntitiesList)
                    {
                        var connectionBuilder = new SqlConnectionStringBuilder();
                        connectionBuilder.DataSource = globalSetting.ServerName;
                        connectionBuilder.InitialCatalog = globalSetting.StageDBName;
                        connectionBuilder.IntegratedSecurity = true;
                        connectionBuilder.ConnectTimeout = 5;
                        connectionBuilder.ConnectRetryCount = 1;
                        if(!SqlConnectionChecker.checkConnection(new SqlConnection(connectionBuilder.ToString())))
                        {
                            errorList.Add(globalSetting.RegionId);
                        }      
                    }
                    if(errorList.Count > 0)
                    {
                        Console.WriteLine("Обнаружены ошибки при валидации:");
                        foreach(string item in errorList)
                        {
                            Console.WriteLine(string.Format("Регион {0} не валиден. Проверьте настройки соединения",item));
                        }
                    }
                    else
                    {
                        Console.WriteLine("Не обнаружены ошибки при валидации.");
                    }

                }
                else
                {
                    throw new Exception("Файл конфигурации не валиден");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Произошла следующая ошибка: " + e.Message);
            }
            finally
            {
                Console.ReadKey();
            }
        }
    }
}
