using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace AsyncReplicaOperations
{

    public class AsyncSQLConnectPuller
    {
        private int workerThread, asyncWorkerThread;
        private List<PullValue> logs;
        private Queue<string> pullValue;
        private List<CancellationToken> tokens;
        private PreProcessDelegate preProcess;
        private PostProcessDelegate postProcess;
        private Dictionary<string, string> workDirectories;
        private String connectionString;
        private DirectionsEnum direction;
        private RunningStatusEnum status;

        protected event RunProcessEventHandler RunProcess;
        protected event FinishProcessEventHandler FinishProcess;
        public event PostLogEventHandler LogProcessed;
        public event StatusChangedHandler StatusChanged;

        protected delegate void RunProcessEventHandler();
        protected delegate void FinishProcessEventHandler();
        protected delegate void PreProcessDelegate();
        protected delegate void PostProcessDelegate();
        public delegate void PostLogEventHandler(AsyncSQLConnectPuller sender, AsyncLogEventArgs e);
        public delegate void StatusChangedHandler(AsyncSQLConnectPuller sender, AsyncStatusChangedEventArgs e);


        public AsyncSQLConnectPuller(int _windowsSize)
        {
            WindowSize = _windowsSize;
            pullValue = new Queue<string>();
            status = RunningStatusEnum.NotRunning;
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
                    if (lastElementCount == 0 && Status != RunningStatusEnum.Completed)
                    {
                        if(this.LogList.Count(x=>x.Status == TaskRunningStatus.Failure) > 0)
                        {
                            FileLogger.GetInstance().WriteLog("Следующие элементы завершились ошибкой при работе:",DateTime.Now,this.RegionID);
                            foreach(var element in this.LogList.Where(x=>x.Status == TaskRunningStatus.Failure))
                            {
                                FileLogger.GetInstance().WriteLog(element.Id, DateTime.Now, this.RegionID);
                            }
                        }
                        else
                        {
                            FileLogger.GetInstance().WriteLog("Не обнаружено ошибок при работе:", DateTime.Now, this.RegionID);
                        }
                        this.Status = RunningStatusEnum.Completed;
                    }
                }
            }
        }
        private void InitConnection()
        {
            if (pullValue.Count > 0)
            {
                //ThreadPool.GetAvailableThreads(out workerThread, out asyncWorkerThread);
                string value;
                /*while (workerThread == 0)
                {
                    Thread.Sleep(100);
                    ThreadPool.GetAvailableThreads(out workerThread, out asyncWorkerThread);
                }*/
                lock (pullValue)
                {
                    value = pullValue.Dequeue();
                }
                var sqlConnection = new SqlConnection(ConnectionString);
                var command = sqlConnection.CreateCommand();
                command.CommandText = CommandText;
                command.Parameters.Add(new SqlParameter("@StoreId", value));
                this.OperateAsyncCommand(command);
            }
        }

        private void AsyncSQLConnectPuller_RunProcess()
        {
            logs = new List<PullValue>();
            for (int i = 0; i < WindowSize; i++)
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
            lastElementCount = pullValue.Count;
        }

        public void fillPull(SqlDataReader _dataReader, int count)
        {
            var countLocal = count;
            while (_dataReader.Read() && countLocal > 0)
            {
                pullValue.Enqueue(_dataReader.GetString(0));
                countLocal--;
            }
            lastElementCount = pullValue.Count;
        }

        public void fillPull(List<Restriction> values)
        {
            workDirectories = new Dictionary<string, string>();
            foreach (var s in values)
            {
                pullValue.Enqueue(s.ReplicaId);
                if (s.WorkDirectory != string.Empty)
                {
                    workDirectories.Add(s.ReplicaId, s.WorkDirectory);
                    if (!Directory.Exists(s.WorkDirectory))
                    {
                        Directory.CreateDirectory(s.WorkDirectory);
                    }
                    else
                    {
                        var flushCommand = new FlushFolder();
                        flushCommand.FolderPath = s.WorkDirectory;
                        flushCommand.RegionId = this.RegionID;
                        preProcess += flushCommand.processAction;
                    }
                }
            }
            lastElementCount = pullValue.Count;
        }

        public void operatePull()
        {
            if (CommandText == String.Empty) throw new Exception("Не заполнен шаблон текста команды.Операция будет прервана");
            if (ConnectionString == String.Empty) throw new Exception("Не заполнена строка соединения.Операция будет прервана");
            if (pullValue.Count == 0) throw new Exception("Пулл значений пуст.Операция будет прервана");
            //ThreadPool.SetMaxThreads(WindowSize, WindowSize);
            if (workDirectories == null)
            {
                workDirectories = new Dictionary<string, string>();
            }
            foreach (var s in pullValue)
            {
                if (workDirectories.ContainsKey(s)) continue;
                var command = new SqlCommand(this.ExtractPathSQL, new SqlConnection(this.ConnectionString));
                command.Parameters.Add(new SqlParameter("Replica", s));
                if (command.Connection.State == System.Data.ConnectionState.Closed)
                {
                    command.Connection.Open();
                }
                using (var dataReader = command.ExecuteReader())
                {
                    dataReader.Read();
                    switch (direction)
                    {
                        case DirectionsEnum.Import:
                            {
                                workDirectories.Add(s, dataReader.GetString(0));
                                break;
                            }
                        case DirectionsEnum.Export:
                            {
                                workDirectories.Add(s, dataReader.GetString(1));
                                break;
                            }
                        default:
                            break;
                    }
                    var flushImportCommand = new FlushFolder();
                    flushImportCommand.FolderPath = dataReader.GetString(0);
                    flushImportCommand.RegionId = this.RegionID;
                    this.preProcess += flushImportCommand.processAction;
                    var flushExportCommand = new FlushFolder();
                    flushExportCommand.FolderPath = dataReader.GetString(1);
                    flushExportCommand.RegionId = this.RegionID;
                    this.preProcess += flushExportCommand.processAction;
                }
                if (command.Connection.State == System.Data.ConnectionState.Open)
                {
                    command.Connection.Close();
                }
            }
            try
            {
                preProcess();
            }
            catch
            {

            }

            Status = RunningStatusEnum.Running;
            this.RunProcess();
        }

        public int WindowSize { get; }
        public RunningStatusEnum Status
        {
            get
            {
                return status;
            }
            set
            {
                var oldStatus = this.status;
                status = value;
                this.StatusChanged(this, new AsyncStatusChangedEventArgs(oldStatus, value));
            }
        }


        public int lastElementCount { get; private set; }
        public string CommandText { get; set; }

        public string ExtractPathSQL { get; set; }

        public void SetConnectionString(string value)
        {
            ConnectionString = value;
        }

        public List<PullValue> LogList => logs;

        public string RegionID { get; set; }

        protected PreProcessDelegate PreProcess { get { return preProcess; } set { preProcess = value; } }
        protected PostProcessDelegate PostProcess { get { return postProcess; } set { postProcess = value; } }
        public DirectionsEnum Direction { get { return direction; } set { direction = value; } }
        public string ConnectionString { get { return connectionString; } set { connectionString = value; } }

        [ParameterMethod("PackageExistWaitTimeOut")]
        private async void OperateAsyncCommand(SqlCommand _command)
        {
            var commandLocal = _command;
            var timer = new Stopwatch();
            var token = new CancellationToken();
            var pullValue = new PullValue();
            var eventArgs = new AsyncLogEventArgs();
            string directionPath = "";
            try
            {
                commandLocal.CommandTimeout = 99999;
                timer.Start();
                tokens.Add(token);
                pullValue.Id = _command.Parameters[0].Value.ToString();
                if (commandLocal.Connection.State == System.Data.ConnectionState.Closed)
                {
                    commandLocal.Connection.Open();
                }
                if(workDirectories.ContainsKey(pullValue.Id))
                {
                    commandLocal.CommandText += ", N'" + workDirectories[pullValue.Id] + "'";
                }
                pullValue.ISError = false;
                pullValue.Status = TaskRunningStatus.Running;
                pullValue.Log = "Начинается обработка значения...";
                FileLogger.GetInstance().WriteLog(pullValue, DateTime.Now,this.RegionID);
                var task = await commandLocal.ExecuteNonQueryAsync(token);
                directionPath = workDirectories[pullValue.Id];
                pullValue.Log = "Ожидается обработка реплики...";
                FileLogger.GetInstance().WriteLog(pullValue, DateTime.Now,this.RegionID);
                var waitTask = WaitForFile(directionPath + "\\gmmq.package.end");
                waitTask.Wait(OperationsAPI.PackageExistWaitTimeOut);
                pullValue.Log = string.Format("Операция успешна");
                pullValue.ISError = false;
                pullValue.Status = TaskRunningStatus.Success;
                FileLogger.GetInstance().WriteLog(pullValue, DateTime.Now, this.RegionID);
            }
            catch (Exception e)

            {
                pullValue.Log = string.Format("Операция завершилась со следующей ошибкой:{0}", e.Message);
                pullValue.ISError = true;
                pullValue.Status = TaskRunningStatus.Failure;
                FileLogger.GetInstance().WriteLog(pullValue, DateTime.Now,this.RegionID);
                if (commandLocal.Connection.State == System.Data.ConnectionState.Open)
                {
                    commandLocal.Connection.Close();
                }
            }
            finally
            {
                timer.Stop();
                lock (this)
                {
                    --lastElementCount;
                }
                pullValue.Time = string.Format("{0:d2}:{1:d2}:{2:d4}", timer.Elapsed.Minutes, timer.Elapsed.Seconds, timer.Elapsed.Milliseconds);
                logs.Add(pullValue);

                tokens.Remove(token);
                if (commandLocal.Connection.State == System.Data.ConnectionState.Open)
                {
                    commandLocal.Connection.Close();
                }
                FinishProcess();
            }
        }

        private async Task WaitForFile(string path) => await Task.Run(() =>
        {
            while (File.Exists(path)) { }
            return;
        });
    }
}
