using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Data.SqlClient;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AsyncReplicaOperations;

namespace AsyncReplicaTool
{
    /// <summary>
    /// Interaction logic for StageConnector.xaml
    /// </summary>
    public partial class StageConnector : Window
    {
        private Boolean connectionEstablish;
        private SqlConnection connection;
        private SqlCommand command;
        private SqlConnectionStringBuilder stringBuilder;
        
        public StageConnector()
        {
            InitializeComponent();
            DirectionBox.ItemsSource = Enum.GetValues(typeof(DirectionsEnum)).Cast<DirectionsEnum>();
            connectionEstablish = false;
            DBBox.IsEnabled = connectionEstablish && DirectionBox.SelectedIndex > 0;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        private List<string> getStageDB()
        {
            List<string> ret = new List<string>();

            if (DBBox.IsEnabled)
            {
                try
                {
                    command = new SqlCommand();
                    var procName = ProcName;
                    command.CommandText = @"
                                        declare @dbname as varchar(200)
                                        declare @statement as varchar(max)

                                        create table ##t1 (dbname varchar(200),count int)

                                        declare procCur cursor for
                                        select name
                                        from sys.databases

                                        open procCur

                                        fetch next from procCur into @dbName

                                        while @@FETCH_STATUS = 0
                                        begin

                                        SET @statement = 'use ' + @dbname + ' insert ##t1 (dbName,count) select ''' + @dbname + ''',count(*) from sys.procedures where name=''" +  procName
                                        + @"'''
                                        exec sp_sqlexec @statement

                                        fetch next from procCur into @dbName

                                        end

                                        close procCur
                                        deallocate procCur

                                        select dbname
                                        from ##t1
                                        where count > 0

                                        drop table ##t1";
                    command.Connection = connection;
                    command.Connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ret.Add(reader["dbname"].ToString());
                        }
                        reader.Close();
                    }
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Message, "Возникла ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return ret;
        }

        private void ServerBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            stringBuilder = new SqlConnectionStringBuilder();
            stringBuilder.DataSource = ServerBox.Text;
            stringBuilder.InitialCatalog = "master";
            stringBuilder.IntegratedSecurity = true;
            stringBuilder.ConnectRetryCount = 1;
            stringBuilder.ConnectTimeout = 5;
            connection = new SqlConnection(stringBuilder.ToString());
            if(SqlConnectionChecker.checkConnection(connection))
            {
                connectionEstablish = true;
            }
            DBBox.IsEnabled = connectionEstablish && DirectionBox.SelectedIndex > 0;
            DBBox.Items.Clear();
            var items = getStageDB();
            var ei = items.GetEnumerator();
            while (ei.MoveNext())
            {
                DBBox.Items.Add(ei.Current);
                DBBox.Items.Refresh();
            }
        }

        private void DirectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DBBox.IsEnabled = connectionEstablish && DirectionBox.SelectedIndex > 0;
            DBBox.Items.Clear();
            var items = getStageDB();
            var ei = items.GetEnumerator();
            while(ei.MoveNext())
            {
                DBBox.Items.Add(ei.Current);
                DBBox.Items.Refresh();
            }
        }
        public string ServerName
        {
            get
            {
                return ServerBox.Text;
            }
        }
        public string DBName
        {
            get
            {
                return DBBox.Text;
            }
        }
        public string ProcName
        {
            get
            {
                return (DirectionBox.SelectedIndex == 0) ? Properties.Settings.Default.ImportReplicaProc : Properties.Settings.Default.ExportReplicaProc;
            }
        }
        public AsyncReplicaOperations.DirectionsEnum Direction
        {
            get
            {
                return (AsyncReplicaOperations.DirectionsEnum)DirectionBox.SelectedIndex;
            }
        }
    }
}
