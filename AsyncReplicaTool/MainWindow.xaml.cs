using System;
using System.Collections.Generic;
using System.Windows;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections.ObjectModel;
using AsyncReplicaOperations;
using System.Data;
using System.IO;
using System.Reflection;
using System.Linq;
using Microsoft.Win32;
using AsyncReplicaTool.Windows;

namespace AsyncReplicaTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        private string serverName, extractPathSQL, procName, stageName;
        private SqlConnection sqlConnection;
        private List<AsyncSQLConnectPuller> pullers;
        private List<RegionSetting> stageServers;
        private string groupPath;
        private GroupSettings runtimeConfig;
        private ObservableCollection<PullValue> dataSource;
        private Stopwatch timer;
        private AsyncReplicaOperations.DirectionsEnum direction;
        SqlConnectionStringBuilder sqlConnectionBuilder;
        public MainWindow()
        {
            InitializeComponent();
            sqlConnectionBuilder = new SqlConnectionStringBuilder();
            sqlConnectionBuilder.ConnectRetryInterval = 3;
            sqlConnectionBuilder.IntegratedSecurity = true;
            sqlConnectionBuilder.InitialCatalog = "master";
            StageServerList.Items.IsLiveSorting = true;
            timer = new Stopwatch();

            InitAPI();
            this.LoadConfigs();
            pullers = new List<AsyncSQLConnectPuller>();
        }

        private static void InitAPI()
        {
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
            Properties.Settings.Default.PropertyChanged += Default_PropertyChanged;
        }

        private static void Default_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ConfigPath":
                    {
                        OperationsAPI.ConfigPath = Properties.Settings.Default.ConfigPath;
                        break;
                    }
                case "ExportReplicaProc":
                    {
                        OperationsAPI.ExportReplicaProcedure = Properties.Settings.Default.ExportReplicaProc;
                        break;
                    }
                case "Host":
                    {
                        OperationsAPI.Host = Properties.Settings.Default.Host;
                        break;
                    }
                case "ImportReplicaProc":
                    {
                        OperationsAPI.ImportReplicaProcedure = Properties.Settings.Default.ImportReplicaProc;
                        break;
                    }
                case "MailCCRecep":
                    {
                        OperationsAPI.MailCCRecep = Properties.Settings.Default.MailCCRecep;
                        break;
                    }
                case "MailRecep":
                    {
                        OperationsAPI.MailRecep = Properties.Settings.Default.MailRecep;
                        break;
                    }
                case "Port":
                    {
                        OperationsAPI.Port = Properties.Settings.Default.Port;
                        break;
                    }
                case "RetryPackageEndCount":
                    {
                        OperationsAPI.RetryPackageCount = Properties.Settings.Default.RetryPackageEndCount;
                        break;
                    }
                case "SenderMail":
                    {
                        OperationsAPI.SenderMail = Properties.Settings.Default.SenderMail;
                        break;
                    }
                case "StageListPath":
                    {
                        OperationsAPI.StageListPath = Properties.Settings.Default.StageListPath;
                        break;
                    }
                case "ThreadCount":
                    {
                        OperationsAPI.ThreadCount = Properties.Settings.Default.ThreadCount;
                        break;
                    }
                case "UseNotify":
                    {
                        OperationsAPI.UseNotify = Properties.Settings.Default.UseNotify;
                        break;
                    }
                case "WaitPackageForExist":
                    {
                        OperationsAPI.PackageExistWaitTimeOut = Properties.Settings.Default.WaitPackageForExist;
                        break;
                    }
                default:
                    break;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Settings settingsWindow = new Settings();
            settingsWindow.ShowDialog();
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            TabElement.Items.Clear();
            Progress.Value = 0;
            pullers.Clear();
            Global.dataSources = new Dictionary<string, ObservableCollection<PullValue>>();
            sqlConnection = new SqlConnection();
            timer = new Stopwatch();
            try
            {
                //var connectSettings = AsyncReplicaOperations.ConnectSettings.getInstance();
                if (!runtimeConfig.isValid)
                {
                    operateManually();
                }
                else
                {
                    OperateAuto();
                }
                if (pullers.Count > 0)
                {
                    AsyncSQLConnectPuller puller = pullers.First(x => x.Status == RunningStatusEnum.NotRunning);
                    puller.operatePull();
                    Progress.IsIndeterminate = true;
                }

            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Возникла ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (sqlConnection.State == System.Data.ConnectionState.Open)
                {
                    sqlConnection.Close();
                }
                if (timer.IsRunning)
                {
                    timer.Stop();
                }
                Progress.IsIndeterminate = false;
                Progress.Value = 0;
            }
        }

        private void OperateAuto()
        {
            try
            {
                var runTimeEnum = runtimeConfig.EntitiesList.Cast<RuntimeRegionSettings>().ToList().GetEnumerator();
                while (runTimeEnum.MoveNext())
                {

                    TabItem tabItem = CreateLogTabItem(runTimeEnum.Current.RegionId);
                    TabElement.Items.Add(tabItem);
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
                        CommandText = string.Format("use {0} select CAST(REPLICAID as varchar(8)) from REPLICAS", sqlConnection.Database)

                    };
                    if (command.Connection.State != ConnectionState.Open)
                    {
                        command.Connection.Open();
                    }

                    StartPuller(tabItem, command, runTimeEnum.Current);


                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "Возникла ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void operateManually()
        {
            try
            {
                var manualSettings = new StageConnector();
                manualSettings.Closing += ManualSettings_Closing;
                manualSettings.ShowDialog();
                if (serverName != "" && procName != "" && stageName != "")
                {
                    TabItem tabItem = CreateLogTabItem("MANUAL");
                    TabElement.Items.Add(tabItem);
                    sqlConnectionBuilder.DataSource = serverName;
                    sqlConnectionBuilder.InitialCatalog = stageName;
                    sqlConnection = new SqlConnection(sqlConnectionBuilder.ToString());
                    extractPathSQL = "select top 1 FILEPATHLOAD,FILEPATHSAVE from Replicas where REPLICAID = @Replica";


                    var command = new SqlCommand
                    {
                        Connection = sqlConnection,
                        CommandText = string.Format("use {0} select CAST(REPLICAID as varchar(8)) from REPLICAS", sqlConnection.Database)
                    };
                    if (command.Connection.State != System.Data.ConnectionState.Open)
                    {
                        command.Connection.Open();
                    }

                    StartPuller(tabItem, command);
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "Возникла ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartPuller(TabItem tabItem, SqlCommand command)
        {
            var asyncSqlPuller = new AsyncReplicaOperations.AsyncSQLConnectPuller(OperationsAPI.ThreadCount);
            asyncSqlPuller.fillPull(command.ExecuteReader());
            asyncSqlPuller.CommandText = string.Format("use {0} exec {1} @StoreId", sqlConnection.Database, procName);
            asyncSqlPuller.ExtractPathSQL = extractPathSQL;
            asyncSqlPuller.ConnectionString = sqlConnection.ConnectionString;
            asyncSqlPuller.RegionID = (string)tabItem.Header;
            asyncSqlPuller.Direction = direction;
            SetProgressCount(asyncSqlPuller.RegionID, asyncSqlPuller.lastElementCount);
            if (!timer.IsRunning)
            {
                timer.Start();
            }
            asyncSqlPuller.StatusChanged += AsyncSqlPuller_StatusChanged;
            asyncSqlPuller.LogProcessed += AsyncSqlPuller_LogProcessed;
            pullers.Add(asyncSqlPuller);
        }

        private void StartPuller(TabItem tabItem, SqlCommand command, RuntimeRegionSettings settings)
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
            asyncSqlPuller.RegionID = (string)tabItem.Header;
            asyncSqlPuller.Direction = direction;
            SetProgressCount(asyncSqlPuller.RegionID, asyncSqlPuller.lastElementCount);
            if (!timer.IsRunning)
            {
                timer.Start();
            }
            asyncSqlPuller.StatusChanged += AsyncSqlPuller_StatusChanged;
            asyncSqlPuller.LogProcessed += AsyncSqlPuller_LogProcessed;
            pullers.Add(asyncSqlPuller);
        }

        private void AsyncSqlPuller_LogProcessed(AsyncReplicaOperations.AsyncSQLConnectPuller sender, AsyncLogEventArgs e)
        {
            this.AttachLog(e.LogValue, sender.RegionID);
            if (e.LogValue.Status != TaskRunningStatus.Running)
            {
                this.IncrementProgress(sender.RegionID);
            }
            //FileLogger.GetInstance().WriteLog(e.LogValue, DateTime.Now);
        }

        private void AsyncSqlPuller_StatusChanged(AsyncReplicaOperations.AsyncSQLConnectPuller sender, AsyncStatusChangedEventArgs e)
        {
            if (e.NewStatus == RunningStatusEnum.Completed)
            {
                if (!pullers.All(x => x.Status == RunningStatusEnum.Completed))
                {
                    AsyncSQLConnectPuller puller = pullers.First(x => x.Status == RunningStatusEnum.NotRunning);
                    puller.operatePull();
                }
                else
                {
                    this.Notify();
                }
            }
        }

        private void ManualSettings_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var settings = (StageConnector)sender;
            serverName = settings.ServerName;
            stageName = settings.DBName;
            procName = settings.ProcName;
            direction = settings.Direction;
        }

        private TabItem CreateLogTabItem(string id)
        {
            var tabItem = new TabItem
            {
                Header = id
            };
            tabItem.Name = tabItem.Header + "TabItem";

            var grid = new Grid
            {
                Name = tabItem.Header + "Grid"
            };
            var gridRowDefinition = new RowDefinition
            {
                Height = new GridLength(1, GridUnitType.Star)
            };
            grid.RowDefinitions.Add(gridRowDefinition);
            gridRowDefinition = new RowDefinition
            {
                Height = new GridLength(40)
            };
            grid.RowDefinitions.Add(gridRowDefinition);
            tabItem.Content = grid;

            var dataGrid = new DataGrid
            {
                Name = tabItem.Header + "DataGrid",
                RowHeight = 24
            };

            var columnImage = new DataGridTemplateColumn();
            var dataImageTemplate = new DataTemplate();
            var imageFactory = new FrameworkElementFactory(typeof(Image));
            var binding = new Binding("Status")
            {
                Converter = new StatusImageConverter()
            };
            imageFactory.SetBinding(Image.SourceProperty, binding);
            dataImageTemplate.VisualTree = imageFactory;
            columnImage.CellTemplate = dataImageTemplate;
            columnImage.CanUserResize = false;
            columnImage.Width = 24;
            dataGrid.Columns.Add(columnImage);

            var column = new DataGridTextColumn
            {
                Header = "ОПС",
                Binding = new Binding("Id")
            };
            dataGrid.Columns.Add(column);

            column = new DataGridTextColumn
            {
                Header = "Время",
                Binding = new Binding("Time")
            };
            dataGrid.Columns.Add(column);

            column = new DataGridTextColumn
            {
                Header = "Лог",
                Binding = new Binding("Log")
            };
            dataGrid.Columns.Add(column);

            dataSource = new ObservableCollection<PullValue>();
            if (!Global.dataSources.ContainsKey((string)tabItem.Header))
            {
                Global.dataSources.Add((string)tabItem.Header, dataSource);
            }
            dataGrid.CanUserAddRows = false;
            dataGrid.CanUserDeleteRows = false;
            dataGrid.CanUserResizeRows = false;
            dataGrid.AutoGenerateColumns = false;
            dataGrid.ItemsSource = dataSource;

            grid.Children.Add(dataGrid);
            Grid.SetRow(dataGrid, 0);

            var progress = new ProgressBar
            {
                Name = tabItem.Header + "Progress",
                Margin = new Thickness(3)
            };
            grid.Children.Add(progress);
            Grid.SetRow(progress, 1);

            return tabItem;
        }

        public void IncrementProgress(string _id)
        {
            var progress = findProgress(_id);
            if (progress != null)
            {
                progress.Value++;
            }
        }

        private ProgressBar findProgress(string _id)
        {
            TabItem tabItem = null;
            Grid grid = null;
            ProgressBar progress = null;
            try
            {
                foreach (TabItem ti in TabElement.Items)
                {
                    if (ti.Header.ToString() != _id) continue;
                    tabItem = ti;
                    break;
                }
                grid = (Grid)tabItem.Content;
                progress = (ProgressBar)grid.Children[1];
            }
            catch
            {

            }
            return progress;
        }

        public void SetProgressCount(string _id, double _maximum)
        {
            var progress = findProgress(_id);
            if (progress != null)
            {
                progress.Maximum = _maximum;
            }
        }
        public void AttachLog(object _log, string regionId)
        {
            var log = (PullValue)_log;
            var findResult = Global.dataSources[regionId].SingleOrDefault(x => x.Id == log.Id);
            if (findResult == null)
            {
                Global.dataSources[regionId].Add(log);
            }
            else
            {
                Global.dataSources[regionId][Global.dataSources[regionId].IndexOf(findResult)] = log;
                var view = CollectionViewSource.GetDefaultView(Global.dataSources[regionId]);
                view.Refresh();
            }
        }


        private void AddGroup_Click(object sender, RoutedEventArgs e)
        {
            var createGroup = new CreateGroup();
            createGroup.Closing += CreateGroup_Closing;
            createGroup.ShowDialog();
        }

        private void CreateGroup_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            groupPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Properties.Settings.Default.GroupsFolder + "\\" + ((CreateGroup)sender).GroupNameText + ".cnfgroup";
            runtimeConfig = new GroupSettings(groupPath);
            GroupContainer.Header = ((CreateGroup)sender).GroupNameText;
            RefreshRunTimeList();
        }

        private void OpenGroup_Click(object sender, RoutedEventArgs e)
        {
            var openFileWindow = new OpenFileDialog()
            {
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = "*.cnfgroup",
                Multiselect = false,
                Filter = "Файлы групп конфигураций|*.cnfgroup",
                InitialDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Properties.Settings.Default.GroupsFolder
            };
            if ((bool)openFileWindow.ShowDialog())
            {
                this.LoadGroups(openFileWindow.FileName);
            }
        }

        private void SaveGroup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (groupPath != "")
                {
                    runtimeConfig.SaveConfig(groupPath);
                }
                else
                {
                    SaveAsGroup_Click(null, null);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Возникла ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FlushGroup_Click(object sender, RoutedEventArgs e)
        {
            runtimeConfig.EntitiesList.Clear();
            RefreshRunTimeList();
        }

        private void RefreshRunTimeList()
        {
            RunTimeServer.ItemsSource = runtimeConfig.EntitiesList.Cast<RuntimeRegionSettings>().ToList();
            RunTimeServer.Items.Refresh();
        }

        private void SaveAsGroup_Click(object sender, RoutedEventArgs e)
        {

            var saveFileDialog = new SaveFileDialog()
            {
                AddExtension = true,
                CheckPathExists = true,
                DefaultExt = "*.cnfgroup",
                Filter = "Файлы групп конфигураций|*.cnfgroup",

                InitialDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Properties.Settings.Default.GroupsFolder,
                OverwritePrompt = false
            };
            if ((bool)saveFileDialog.ShowDialog())
            {
                runtimeConfig.SaveConfig(saveFileDialog.FileName);
                LoadGroups(saveFileDialog.FileName);
            }

        }

        private void AddToGroupButton_Click(object sender, RoutedEventArgs e)
        {
            if (StageServerList.SelectedIndex > -1 && runtimeConfig != null)
            {
                var directionSelector = new SelectDirection();
                directionSelector.Closing += DirectionSelector_Closing;
                directionSelector.ShowDialog();
                runtimeConfig.EntitiesList.Add(new RuntimeRegionSettings()
                {
                    Direction = direction,
                    RegionId = ((RegionSetting)StageServerList.SelectedItem).RegionId,
                    RegionName = ((RegionSetting)StageServerList.SelectedItem).RegionName,
                    ServerName = ((RegionSetting)StageServerList.SelectedItem).ServerName,
                    StageDBName = ((RegionSetting)StageServerList.SelectedItem).StageDBName
                });
                RefreshRunTimeList();

            }
        }

        private void DirectionSelector_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            direction = ((SelectDirection)sender).Direction;
        }

        private void RemoveFromGroupButton_Click(object sender, RoutedEventArgs e)
        {
            if (RunTimeServer.SelectedIndex > -1)
            {
                runtimeConfig.EntitiesList.RemoveAt(RunTimeServer.SelectedIndex);
            }
            RefreshRunTimeList();
        }

        private void CountFilter_Click(object sender, RoutedEventArgs e)
        {
            if (RunTimeServer.SelectedIndex > -1)
            {
                var window = new SelectCountRestrictions(((RuntimeRegionSettings)RunTimeServer.SelectedItem).Restrictions.CountRestrictions);
                window.Closing += CountRestrictionsClosing;
                window.ShowDialog();
            }
        }

        private void CountRestrictionsClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ((RuntimeRegionSettings)RunTimeServer.SelectedItem).Restrictions.CountRestrictions = ((SelectCountRestrictions)sender).Count;
        }

        private void IDFilter_Click(object sender, RoutedEventArgs e)
        {
            if (RunTimeServer.SelectedIndex > -1)
            {
                var window = new AsyncReplicaTool.Windows.Restrictions(((RuntimeRegionSettings)RunTimeServer.SelectedItem).Restrictions.ToList<Restriction>(), ((RuntimeRegionSettings)RunTimeServer.SelectedItem).Direction);
                window.Closing += RestrictionWindowClosing;
                window.ShowDialog();
            }
        }

        private void RestrictionWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ((RuntimeRegionSettings)RunTimeServer.SelectedItem).Restrictions.Clear();
            foreach (Restriction r in ((AsyncReplicaTool.Windows.Restrictions)sender).RestrictionsList)
            {
                ((RuntimeRegionSettings)RunTimeServer.SelectedItem).Restrictions.Add(r);
            }
        }

        private void ResetFilter_Click(object sender, RoutedEventArgs e)
        {
            if (RunTimeServer.SelectedIndex > -1)
            {
                ((RuntimeRegionSettings)RunTimeServer.SelectedItem).Restrictions.Clear();
                ((RuntimeRegionSettings)RunTimeServer.SelectedItem).Restrictions.CountRestrictions = 0;
            }
        }

        public void Notify()
        {
            if (pullers.TrueForAll(x => x.Status == AsyncReplicaOperations.RunningStatusEnum.Completed))
            {
                if (Properties.Settings.Default.UseNotify)
                {
                    try
                    {
                        var notifier = NotifyMailer.getInstance();

                        var body = "<html><head><style type = 'text/css'>";
                        body += "h1{text-align:center;font-family:arial;font-size:22px;}";
                        body += "body{background-color:azure}";
                        body += "caption{margin-bottom:15px;}";
                        body += "table{margin-top:10px;border-spacing:0;border-collapse:collapse;font-family:sans-serif;width:100%}";
                        body += "td{border:1px solid black;font-size:13px;font-family:arial;}";
                        body += "th{background-color:darkgreen;color:lavender;font-size:14px;font-family:sans-serif;}";
                        body += "tr.evenrow{background-color:floralwhite;}";
                        body += "tr.notevenrow{background-color:lightgreen;}";
                        body += "</style></head>";
                        body += "<body><h1>Выгрузка реплик от " + DateTime.Now.ToString("dd.MM.yyyy") + "</h1>";

                        var listEnum = pullers.GetEnumerator();
                        while (listEnum.MoveNext())
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
                        var recepCCEnum = Properties.Settings.Default.MailCCRecep.Split(';').GetEnumerator();
                        var recepCCList = new List<string>();
                        while (recepCCEnum.MoveNext())
                        {
                            recepCCList.Add(recepCCEnum.Current.ToString());
                        }
                        notifier.sendMail(recepList, recepCCList);
                        Console.WriteLine("Сообщение успешно отправлено");
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(string.Format("Оповещение невозможно отправить оповещение по причине следующей ошибки : {0}", exception.Message), "Возникла ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                Progress.IsIndeterminate = false;
                Progress.Value = Progress.Maximum;
            }
        }

        private void LoadConfigs()
        {
            try
            {
                stageServers = new List<RegionSetting>();
                StageServerList.ItemsSource = stageServers;
                if (StageConnectSettings.getInstance().isValid)
                {
                    var e = StageConnectSettings.getInstance().EntitiesList.GetEnumerator();
                    while (e.MoveNext())
                    {
                        var regionSettings = (RegionSetting)e.Current;
                        stageServers.Add(regionSettings);
                    }
                }
                else
                {
                    MessageBox.Show("Настройки не валидны.Продолжение возможно только в ручном режиме.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Возникла ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadGroups(string path)
        {
            try
            {
                var file = new FileInfo(path);
                runtimeConfig = new GroupSettings(file.FullName);
                if (runtimeConfig.isValid)
                {
                    this.RefreshRunTimeList();
                    GroupContainer.Header = Path.GetFileNameWithoutExtension(path);
                    groupPath = path;
                }
                else
                {
                    MessageBox.Show("Настройки не валидны.Необходимо выбрать другой файл или продолжить в ручном режиме.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Возникла ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Dispose()
        {
            if (sqlConnection.State == ConnectionState.Open)
            {
                sqlConnection.Close();
            }
        }
    }
}
