using System;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Xml;
using AsyncReplicaOperations;

namespace AsyncSQLOperations
{
    class Program
    {
        private static string serverName,extractPathSQL;
        private static Dictionary<int, string> dbNamesMap;
        private static SqlConnection sqlConnection;
        private static List<AsyncSQLConnectPuller> pullers;
        private static Stopwatch timer;
        private static DirectionsEnum direction;
        public static StageConnectSettings stageSettings;
        static void Main()
        {
            try
            {
                stageSettings = StageConnectSettings.getInstance();
                var sqlConnectionBuilder = new SqlConnectionStringBuilder();
                sqlConnectionBuilder.ConnectRetryInterval = 3;
                sqlConnectionBuilder.IntegratedSecurity = true;
                sqlConnectionBuilder.InitialCatalog = "master";
                var connectSettings = ConnectSettings.getInstance();
                timer = new Stopwatch();
                pullers = new List<AsyncSQLConnectPuller>();
                if (connectSettings.isManual)
                {
                    try
                    {
                        Console.WriteLine(@"Введите название instance SQL-Server в формате [Имя сервера]\[Имя экземпляра]");
                        serverName = Console.ReadLine();
                        sqlConnectionBuilder.DataSource = serverName;
                        sqlConnection = new SqlConnection(sqlConnectionBuilder.ToString());
                        if (!SqlConnectionChecker.checkConnection(sqlConnection)) throw new Exception("Неудачная попытка подключения");
                        sqlConnection.Open();

                        var command = sqlConnection.CreateCommand();
                        command.CommandText = "select name from [master].[sys].[databases] order by name asc";
                        dbNamesMap = new Dictionary<int, string>();
                        using (var dataReader = command.ExecuteReader())
                        {
                            while (dataReader.Read())
                            {
                                var nameValue = dataReader.GetValue(0);
                                dbNamesMap.Add(dbNamesMap.Count + 1, nameValue.ToString());
                            }
                        }

                        if (dbNamesMap.Count == 0)
                        {
                            throw new Exception("Список баз данных пуст.Продолжение невозможно.");
                        }

                        var dictEnum = dbNamesMap.GetEnumerator();
                        Console.Clear();
                        Console.WriteLine("Список баз данных. Выберите нужную базу Stage");
                        while (dictEnum.MoveNext())
                        {
                            Console.WriteLine(string.Format("{0}) {1}", dictEnum.Current.Key, dictEnum.Current.Value));
                        }

                        var dbIndex = Convert.ToInt32(Console.ReadLine());
                        if (!dbNamesMap.ContainsKey(dbIndex)) throw new Exception("Искомая база отсутсвует в списке");

                        Console.Clear();
                        Console.WriteLine("Выберите направление операции:\n1) Импорт\n2) Экспорт");
                        var indexOperation = Convert.ToInt32(Console.ReadLine());

                        var procName = "";

                        switch (indexOperation)
                        {
                            case 0:
                                {
                                    direction = DirectionsEnum.Import;
                                    procName = "ReplicaImport";
                                    extractPathSQL = "select top 1 FILEPATHLOAD from Replicas where REPLICAID = @Replica";
                                    break;
                                }
                            case 1:
                                {
                                    direction = DirectionsEnum.Export;
                                    procName = "ReplicaExport";
                                    extractPathSQL = "select top 1 FILEPATHSAVE from Replicas where REPLICAID = @Replica";
                                    break;
                                }
                            default:
                                {
                                    throw new Exception("Неверный код направления");
                                }
                        }

                        if (SqlConnectionChecker.checkProcedure(sqlConnection, direction)) throw new Exception(string.Format("Процедура {0} не найдена в базе {1}.Продолжение невозможно.", procName, dbNamesMap[dbIndex]));
                        command.CommandText = string.Format("use {0} select STORENUMBER from RetailStoreTable", dbNamesMap[dbIndex]);

                        if (Properties.Settings.Default.OPSFilter != "")
                        {
                            command.CommandText += " where STORENUMBER in (" + Properties.Settings.Default.OPSFilter + ")";
                        }

                        var asyncSqlPuller = new AsyncSQLConnectPuller(Properties.Settings.Default.ThreadCount);
                        asyncSqlPuller.fillPull(command.ExecuteReader());
                        asyncSqlPuller.CommandText = string.Format("use {0} exec {1} @StoreId", dbNamesMap[dbIndex], procName);
                        asyncSqlPuller.ExtractPathSQL = extractPathSQL;
                        asyncSqlPuller.ConnectionString = sqlConnection.ConnectionString;
                        timer.Start();
                        asyncSqlPuller.operatePull();
                        pullers.Add(asyncSqlPuller);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    finally
                    {
                        if (pullers.Count > 0)
                        {
                            while (pullers.TrueForAll(x => x.Status != RunningStatusEnum.Completed))
                            {
                                Thread.Sleep(1000);
                            }
                        }
                        sqlConnection.Close();
                        timer.Stop();
                        Console.WriteLine(string.Format("Обработка завершена за {0}:{1}:{2}", timer.Elapsed.Minutes, timer.Elapsed.Seconds, timer.Elapsed.Milliseconds));
                    }

                }
                else
                {
                    try
                    { 
                        var nodeEnum = connectSettings.ConnectEnumerator;
                        var procName = "";
                        while (nodeEnum.MoveNext())
                        {
                            var node = (XmlNode)nodeEnum.Current;
                            sqlConnectionBuilder.DataSource = connectSettings.getValue(node, ConnectNodesParamsEnum.Server);
                            sqlConnectionBuilder.InitialCatalog = connectSettings.getValue(node, ConnectNodesParamsEnum.DBName);
                            sqlConnection = new SqlConnection(sqlConnectionBuilder.ToString());
                            if (!SqlConnectionChecker.checkConnection(sqlConnection)) throw new Exception(string.Format("Неудачное подключение к серверу по региону {0}", connectSettings.getValue(node, ConnectNodesParamsEnum.Id)));
                            switch (connectSettings.getValue(node, ConnectNodesParamsEnum.Direction))
                            {
                                case "0":
                                    {
                                        direction = DirectionsEnum.Import;
                                        procName = Properties.Settings.Default.ImportReplicaProc;
                                        extractPathSQL = "select top 1 FILEPATHLOAD from Replicas where REPLICAID = @Replica";
                                        break;
                                    }
                                case "1":
                                    {
                                        direction = DirectionsEnum.Export;
                                        procName = Properties.Settings.Default.ExportReplicaProc;
                                        extractPathSQL = "select top 1 FILEPATHSAVE from Replicas where REPLICAID = @Replica";
                                        break;
                                    }
                            }
                            if (!SqlConnectionChecker.checkProcedure(sqlConnection, direction)) throw new Exception(string.Format("Процедура не найдена в базе {0}.Продолжение невозможно.", sqlConnection.Database));

                            var asyncSqlPuller = new AsyncSQLConnectPuller(Properties.Settings.Default.ThreadCount);
                            var command = new SqlCommand();
                            command.Connection = sqlConnection;
                            command.CommandText = string.Format("use {0} select STORENUMBER from RetailStoreTable", sqlConnection.Database);

                            if(Properties.Settings.Default.OPSFilter !="")
                            {
                                command.CommandText += " where STORENUMBER in (" + Properties.Settings.Default.OPSFilter + ")";
                            }

                            asyncSqlPuller = new AsyncSQLConnectPuller(Properties.Settings.Default.ThreadCount);
                            asyncSqlPuller.fillPull(command.ExecuteReader());
                            asyncSqlPuller.CommandText = string.Format("use {0} exec {1} @StoreId", sqlConnection.Database, procName);
                            asyncSqlPuller.ExtractPathSQL = extractPathSQL;
                            asyncSqlPuller.ConnectionString = sqlConnection.ConnectionString;
                            if (!timer.IsRunning)
                            {
                                timer.Start();
                            }
                            asyncSqlPuller.operatePull();
                            pullers.Add(asyncSqlPuller);
                        }

                    }
                    catch (Exception error)
                    {
                        Console.WriteLine(error.Message);
                    }
                    finally
                    {
                        if(pullers.Count > 0)
                        {
                            while (pullers.TrueForAll(x=>x.Status != RunningStatusEnum.Completed))
                            {
                                Thread.Sleep(1000);
                            }
                        }
                        sqlConnection.Close();
                        timer.Stop();
                        Console.WriteLine(string.Format("Обработка завершена за {0:d2}:{1:d2}:{2:d4}", timer.Elapsed.Minutes, timer.Elapsed.Seconds, timer.Elapsed.Milliseconds));
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                if(sqlConnection.State == System.Data.ConnectionState.Open)
                {
                    sqlConnection.Close();
                }
                if(timer.IsRunning)
                {
                    timer.Stop();
                    Console.WriteLine(string.Format("Обработка завершена за {0:d2}:{1:d2}:{2:d4}", timer.Elapsed.Minutes, timer.Elapsed.Seconds, timer.Elapsed.Milliseconds));
                }
            }

            try
            {
                var notifier = NotifyMailer.getInstance();

                var body = "<html><head><style type = 'text/css'>";
                body += "h1{text-align:center;font-family:arial;font-size:22px;}";
                body += "body{background-color:azure}";
                body += "caption{margin-bottom:15px;}";
                body += "table{margin-top:10px;border-spacing:0;border-collapse:collapse;font-family:sans-serif;}";
                body += "td{border:1px solid black;font-size:13px;font-family:arial;}";
                body += "th{background-color:darkgreen;color:lavender;font-size:14px;font-family:sans-serif;}";
                body += "tr.evenrow{background-color:floralwhite;}";
                body += "tr.notevenrow{background-color:lightgreen;}";
                body += "</style></head>";
                body += "<body><h1>Выгрузка реплик от "+ DateTime.Now.ToString("dd.MM.yyyy") +"</h1>";

                var listEnum = pullers.GetEnumerator();
                while(listEnum.MoveNext())
                {
                    var asyncPuller = listEnum.Current;
                    var sqlConnection = new SqlConnection(asyncPuller.ConnectionString);
                    body += "<table>";
                    body += "<caption>Сервер " + sqlConnection.DataSource + " база данных " + sqlConnection.Database + "</caption>";
                    body += "<tr><th>Номер ОПС</th><th>Результат</th><th>Время обработки</th></tr>";

                    var logEnum = asyncPuller.LogList.GetEnumerator();
                    var rowNumber = 0;
                    while (logEnum.MoveNext())
                    {
                        body += "<tr class='";
                        body += (rowNumber % 2 == 0) ? "evenrow" : "notevenrow";
                        body += "'>";
                        rowNumber++;
                        var rowLog = logEnum.Current;
                        body += "<td>" + rowLog.Id + "</td>";
                        body += "<td>" + rowLog.Log + "</td>";
                        body += "<td>" + rowLog.Time + "</td>";
                        body += "</tr>";
                    }


                    body += "</table>";
                }

                body += "</body></html>";

                notifier.MessageBody = body;
                var recepEnum = Properties.Settings.Default.MailRecep.Split(';').GetEnumerator();
                var recepList = new List<string>();
                while (recepEnum.MoveNext())
                {
                    recepList.Add(recepEnum.Current.ToString());
                }
                var recepCCEnum = Properties.Settings.Default.MailCCRevep.Split(';').GetEnumerator();
                var recepCCList = new List<string>();
                while (recepCCEnum.MoveNext())
                {
                    recepCCList.Add(recepCCEnum.Current.ToString());
                }
                notifier.sendMail(recepList, recepCCList);
                Console.WriteLine("Сообщение успешно отправлено");
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Оповещение невозможно отправить оповещение по причине следующей ошибки : {0}", e.Message));
            }
            finally
            {
                Console.ReadKey();
            }
        }
    }
}
