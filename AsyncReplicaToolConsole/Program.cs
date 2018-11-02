using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncReplicaOperations;

namespace AsyncReplicaToolConsole
{
    class Program
    {
        static SqlConnectionStringBuilder sqlConnectionBuilder;
        static private Stopwatch timer;
        static private List<AsyncSQLConnectPuller> pullers;
        static private SqlConnection sqlConnection;
        static private GroupSettings runTimeConfig;
        static private AsyncReplicaOperations.DirectionsEnum direction;
        static private string extractPathSQL, procName;
        static void Main(string[] args)
        {
            try
            {
                InitSettings();
                //runTimeConfig = LoadConfig(@"C:\Users\Ext-D.Sushchevskii\Documents\Replica2\Groups\1 wave\AST.cnfgroup");
                runTimeConfig = LoadConfig(args[0]);
                if (runTimeConfig.isValid)
                {
                    OperateAuto();
                }
                else
                {
                    Console.WriteLine("Операция невозможна в ручном режиме");
                }
                if (pullers.Count > 0)
                {
                    AsyncSQLConnectPuller puller = pullers.First(x => x.Status == RunningStatusEnum.NotRunning);
                    Console.WriteLine("");
                    Console.WriteLine("================================================");
                    Console.WriteLine(String.Format("Начинается обработка региона {0}", puller.RegionID));
                    puller.operatePull();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
            finally
            {
                Console.ReadKey();
            }
        }

        static void InitSettings()
        {
            sqlConnectionBuilder = new SqlConnectionStringBuilder();
            sqlConnectionBuilder.ConnectRetryInterval = 3;
            sqlConnectionBuilder.IntegratedSecurity = true;
            sqlConnectionBuilder.InitialCatalog = "master";
            timer = new Stopwatch();
            pullers = new List<AsyncSQLConnectPuller>();
            OperationsAPI.initAPI();
            OperationsAPI.ConfigPath = Properties.Settings.Default.ConfigPath;
            OperationsAPI.ExportReplicaProcedure = Properties.Settings.Default.ExportReplicaProc;
            OperationsAPI.Host = Properties.Settings.Default.Host;
            OperationsAPI.ImportReplicaProcedure = Properties.Settings.Default.ImportReplicaProc;
            OperationsAPI.MailCCRecep = Properties.Settings.Default.MailCCRecep;
            OperationsAPI.MailRecep = Properties.Settings.Default.MailRecep;
            OperationsAPI.Port = Properties.Settings.Default.Port;
            OperationsAPI.RetryPackageCount = Properties.Settings.Default.RetryPackageEndCount;
            OperationsAPI.SenderMail = Properties.Settings.Default.SenderMail;
            OperationsAPI.StageListPath = Properties.Settings.Default.StageListPath;
            OperationsAPI.ThreadCount = Properties.Settings.Default.ThreadCount;
            OperationsAPI.UseNotify = Properties.Settings.Default.UseNotify;
            OperationsAPI.PackageExistWaitTimeOut = Properties.Settings.Default.WaitPackageForExist;
            sqlConnection = new SqlConnection();
        }

        static private GroupSettings LoadConfig(string path)
        {
            return new GroupSettings(path);
        }

        static private void OperateAuto()
        {

                var runTimeEnum = runTimeConfig.EntitiesList.Cast<RuntimeRegionSettings>().ToList().GetEnumerator();
                while (runTimeEnum.MoveNext())
                {
                try
                {
                    sqlConnectionBuilder.DataSource = runTimeEnum.Current.ServerName;
                    sqlConnectionBuilder.InitialCatalog = runTimeEnum.Current.StageDBName;
                    sqlConnection = new SqlConnection(sqlConnectionBuilder.ToString());
                    if (!AsyncReplicaOperations.SqlConnectionChecker.checkConnection(sqlConnection)) throw new Exception(string.Format("Неудачное подключение к серверу по региону {0}", runTimeEnum.Current.RegionId));
                    switch (runTimeEnum.Current.Direction)
                    {
                        case DirectionsEnum.Import:
                            {
                                direction = AsyncReplicaOperations.DirectionsEnum.Import;
                                procName = OperationsAPI.ImportReplicaProcedure;

                                break;
                            }
                        case DirectionsEnum.Export:
                            {
                                direction = AsyncReplicaOperations.DirectionsEnum.Export;
                                procName = OperationsAPI.ExportReplicaProcedure;

                                break;
                            }
                    }
                    extractPathSQL = "select top 1 FILEPATHLOAD,FILEPATHSAVE from Replicas where REPLICAID = @Replica";
                    if (!AsyncReplicaOperations.SqlConnectionChecker.checkProcedure(sqlConnection, direction)) throw new Exception(string.Format("Процедура не найдена в базе {0}.Продолжение невозможно.", sqlConnection.Database));

                    var command = new SqlCommand
                    {
                        Connection = sqlConnection,
                        CommandText = string.Format(@"select CAST(REPLICAID as varchar(8)) 
                                                      from [{0}].[dbo].[REPLICAS] r
                                                      inner join[{1}].[dbo].[GM_RETAILSTOREPARAMETERS] p on r.REPLICAID = p.STOREID
                                                      where p.ISOFFLINE = 0
                                                      order by AWAREVERSION desc", sqlConnection.Database, sqlConnection.Database.Replace("STAGE", "AX"))
                        
                                                      



                    };
                    if (command.Connection.State != ConnectionState.Open)
                    {
                        command.Connection.Open();
                    }

                    StartPuller(command, runTimeEnum.Current);
                }

                catch (Exception error)
                {
                    Console.WriteLine(error.Message);
                }
            }
        }

        static private void StartPuller(SqlCommand command, RuntimeRegionSettings settings)
        {
            var asyncSqlPuller = new AsyncReplicaOperations.AsyncSQLConnectPuller(OperationsAPI.ThreadCount);
            if (settings.Restrictions.CountRestrictions > 0)
            {
                asyncSqlPuller.fillPull(command.ExecuteReader(), settings.Restrictions.CountRestrictions);
            }
            else
            {
                if (settings.Restrictions.Count > 0)
                {
                    asyncSqlPuller.fillPull(settings.Restrictions.ToList<Restriction>());
                }
                else
                {
                    asyncSqlPuller.fillPull(command.ExecuteReader());
                }
            }
            asyncSqlPuller.CommandText = string.Format("use {0} exec {1} @StoreId", sqlConnection.Database, procName);
            asyncSqlPuller.ExtractPathSQL = extractPathSQL;
            asyncSqlPuller.ConnectionString = sqlConnection.ConnectionString;
            asyncSqlPuller.Direction = direction;
            asyncSqlPuller.RegionID = settings.RegionId;
            if (!timer.IsRunning)
            {
                timer.Start();
            }
            asyncSqlPuller.StatusChanged += AsyncSqlPuller_StatusChanged;
            pullers.Add(asyncSqlPuller);
        }

        private static void AsyncSqlPuller_StatusChanged(AsyncSQLConnectPuller sender, AsyncStatusChangedEventArgs e)
        {
            if (e.NewStatus == RunningStatusEnum.Completed)
            {
                Console.WriteLine("Обработка завершена");
                Console.WriteLine(String.Format("Количество обработанных записей {0}, из них успешно {1}, с ошибками {2}", sender.LogList.Count, sender.LogList.Count(x => x.Status == TaskRunningStatus.Success), sender.LogList.Count(x => x.Status == TaskRunningStatus.Failure)));
                Console.WriteLine("Подробности в файлах лога");
                Console.WriteLine("");
                Console.WriteLine("================================================");
                if (!pullers.All(x => x.Status == RunningStatusEnum.Completed))
                {
                    AsyncSQLConnectPuller puller = pullers.FirstOrDefault(x => x.Status == RunningStatusEnum.NotRunning);
                    if (puller != null)
                    {
                        Console.WriteLine("");
                        Console.WriteLine("================================================");
                        Console.WriteLine(String.Format("Начинается обработка региона {0}", puller.RegionID));
                        puller.operatePull();
                    }
                }
                else
                {
                    if (sqlConnection.State == System.Data.ConnectionState.Open)
                    {
                        sqlConnection.Close();
                    }
                    if (timer.IsRunning)
                    {
                        timer.Stop();
                    }
                    Console.WriteLine(String.Format("Обработка завершена за {0:d2}:{1:d2}:{2:d2}", timer.Elapsed.Minutes, timer.Elapsed.Seconds, timer.Elapsed.Milliseconds));
                }
            }
        }
    }
}
