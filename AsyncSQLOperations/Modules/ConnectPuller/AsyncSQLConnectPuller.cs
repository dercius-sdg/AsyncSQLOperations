using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace AsyncSQLOperations
{

    class AsyncSQLConnectPuller
    {
        private int windowsSize,QueueSize,workerThread, asyncWorkerThread;
        private List<PullValue> logs;
        private Queue<string> pullValue;
        private RunningStatusEnum enumValue;
        private List<CancellationToken> tokens;
        private String commandText;
        private String connectionString;
        private String extractPathSQL;

        protected event RunProcessEventHandler RunProcess;
        protected event FinishProcessEventHandler FinishProcess;

        protected delegate void RunProcessEventHandler();
        protected delegate void FinishProcessEventHandler();

        public AsyncSQLConnectPuller(int _windowsSize)
        {
            windowsSize = _windowsSize;
            pullValue = new Queue<string>();
            enumValue = RunningStatusEnum.NotRunning;
            tokens = new List<CancellationToken>();

            this.RunProcess += AsyncSQLConnectPuller_RunProcess;
            this.FinishProcess += AsyncSQLConnectPuller_FinishProcess;
        }

        private void AsyncSQLConnectPuller_FinishProcess()
        {
            lock (pullValue)
            {
                if (pullValue.Count > 0)
                {
                    InitConnection();
                }
                else
                {
                    if (QueueSize == 0 && enumValue != RunningStatusEnum.Completed)
                    {
                        this.enumValue = RunningStatusEnum.Completed;

                        Console.WriteLine("Обработка завершена");
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        private void InitConnection()
        {
            if (pullValue.Count > 0)
            {
                ThreadPool.GetAvailableThreads(out workerThread, out asyncWorkerThread);
                string value;
                while (workerThread == 0)
                {
                    Thread.Sleep(1000);
                    ThreadPool.GetAvailableThreads(out workerThread, out asyncWorkerThread);
                }
                lock (pullValue)
                {
                    value = pullValue.Dequeue();
                }
                var sqlConnection = new SqlConnection(connectionString);
                var command = sqlConnection.CreateCommand();
                command.CommandText = commandText;
                command.Parameters.Add(new SqlParameter("@StoreId", value));
                this.operateAsyncCommand(command);
            }
        }

        private void AsyncSQLConnectPuller_RunProcess()
        {
            logs = new List<PullValue>();
            for (int i = 0; i < windowsSize; i++)
            {
                InitConnection();
            }
        }

        public void fillPull(SqlDataReader _dataReader)
        {
            while (_dataReader.Read())
            {
                pullValue.Enqueue(_dataReader.GetString(0));
            }
            QueueSize = pullValue.Count;
        }

        public void operatePull()
        {
            if (commandText == String.Empty) throw new Exception("Не заполнен шаблон текста команды.Операция будет прервана");
            if (connectionString == String.Empty) throw new Exception("Не заполнена строка соединения.Операция будет прервана");
            if (pullValue.Count == 0) throw new Exception("Пулл значений пуст.Операция будет прервана");
            ThreadPool.SetMaxThreads(windowsSize, windowsSize);
            enumValue = RunningStatusEnum.Running;
            this.RunProcess();
        }

        public int WindowSize
        {
            get
            {
                return windowsSize;
            }
        }

        public RunningStatusEnum Status
        {
            get
            {
                return enumValue;
            }
        }

        public int lastElementCount
        {
            get
            {
                return QueueSize;
            }
        }

        public string CommandText
        {
            get
            {
                return commandText;
            }
            set
            {
                commandText = value;
            }
        }

        public string ExtractPathSQL
        {
            get
            {
                return extractPathSQL;
            }
            set
            {
                extractPathSQL = value;
            }
        }

        public string ConnectionString
        {
            get
            {
                return connectionString;
            }
            set
            {
                connectionString = value;
            }
        }

        public List<PullValue> LogList
        {
            get
            {
                return logs;
            }
        }

        private async void operateAsyncCommand(SqlCommand _command)
        {
            var commandLocal = _command;
            SqlCommand command = new SqlCommand(); ;
            var timer = new Stopwatch();
            var token = new CancellationToken();
            var pullValue = new PullValue();
            try
            {
                commandLocal.CommandTimeout = 99999;
                command.Connection = _command.Connection;
                command.CommandText = extractPathSQL;
                command.Parameters.Add(new SqlParameter("Replica", _command.Parameters[0].Value.ToString()));
                command.Connection.Open();

                var flushPath = command.ExecuteScalar().ToString();
                if(Directory.Exists(flushPath))
                {
                    DirectoryInfo di = new DirectoryInfo(flushPath);
                    foreach(FileInfo fi in di.GetFiles())
                    {
                        if (fi.Exists)
                        {
                            fi.Delete();
                        }
                    }

                    Console.WriteLine(string.Format("Рабочая директория {0} успешно очищена.", flushPath));
                }

                command.Connection.Close();

                timer.Start();
                commandLocal.Connection.Open();
                tokens.Add(token);
                pullValue.Id = _command.Parameters[0].Value.ToString();
                Console.WriteLine("Начинается обработка значения " + _command.Parameters[0].Value);
                var task = await commandLocal.ExecuteNonQueryAsync(token);
                pullValue.Log = string.Format("Операция успешна");
                pullValue.ISError = false;
            }
            catch (Exception e)

            {
                Console.WriteLine(e.Message);
                pullValue.Log = string.Format("Операция завершилась со следующей ошибкой:{0}", e.Message);
                pullValue.ISError = true;
                if(command.Connection.State == System.Data.ConnectionState.Open)
                {
                    command.Connection.Close();
                }
            }
            finally
            {
                timer.Stop();
                lock (this)
                {
                    --QueueSize;
                }
                Console.WriteLine(string.Format("Осталось обработать {0} записей", QueueSize));
                Console.WriteLine(string.Format("Обработано значение {0} за {1:d2}:{2:d2}:{3:d4}", _command.Parameters[0].Value, timer.Elapsed.Minutes, timer.Elapsed.Seconds, timer.Elapsed.Milliseconds));
                pullValue.Time = string.Format("{0:d2}:{1:d2}:{2:d4}",timer.Elapsed.Minutes, timer.Elapsed.Seconds, timer.Elapsed.Milliseconds);

                logs.Add(pullValue);

                tokens.Remove(token);
                commandLocal.Connection.Close();
                FinishProcess();
            }
        }
    }
}
