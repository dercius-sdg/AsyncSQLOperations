using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Data.SqlClient;
using System.IO;
using AsyncReplicaOperations;

namespace AsyncReplicaTool
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        SqlConnection aupConnection;

        public Settings()
        {
            InitializeComponent();

            ImportNameBox.Text = Properties.Settings.Default.ImportReplicaProc;
            ExportNameBox.Text = Properties.Settings.Default.ExportReplicaProc;
            GlobalConfigBox.Text = Properties.Settings.Default.StageListPath;
            ActiveConfigBox.Text = Properties.Settings.Default.ConfigPath;
            ThreadsBox.Text = Convert.ToString(Properties.Settings.Default.ThreadCount);
            UseNotify.IsChecked = Properties.Settings.Default.UseNotify;
            PortBox.Text = Convert.ToString(Properties.Settings.Default.Port);
            SMTPServerBox.Text = Properties.Settings.Default.Host;
            RecepBox.Text = Properties.Settings.Default.MailRecep;
            RecepCCBox.Text = Properties.Settings.Default.MailCCRecep;
            SenderBox.Text = Properties.Settings.Default.SenderMail;
            FormConfigButton.IsEnabled = this.availableServer();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.ImportReplicaProc = ImportNameBox.Text;
            Properties.Settings.Default.ImportReplicaProc = ImportNameBox.Text;
            Properties.Settings.Default.ExportReplicaProc = ExportNameBox.Text;
            Properties.Settings.Default.StageListPath = GlobalConfigBox.Text;
            Properties.Settings.Default.ConfigPath = ActiveConfigBox.Text;
            if (ThreadsBox.Text != "")
            {
                if (Convert.ToInt32(ThreadsBox.Text) > 0)
                {
                    Properties.Settings.Default.ThreadCount = Convert.ToInt32(ThreadsBox.Text);
                }
            }
            if (PortBox.Text != "")
            {
                if (Convert.ToInt32(PortBox.Text) > 0)
                {
                    Properties.Settings.Default.Port = Convert.ToInt32(PortBox.Text);
                }
            }
            Properties.Settings.Default.UseNotify = (bool)UseNotify.IsChecked;
            Properties.Settings.Default.Host = SMTPServerBox.Text;
            Properties.Settings.Default.MailRecep = RecepBox.Text;
            Properties.Settings.Default.MailCCRecep = RecepCCBox.Text;
            Properties.Settings.Default.SenderMail = SenderBox.Text;
            Properties.Settings.Default.Save();
        }

        private void ThreadsBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!Char.IsDigit(e.Text[0]))
            {
                e.Handled = true;
            }
        }

        private void PortBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!Char.IsDigit(e.Text[0]))
            {
                e.Handled = true;
            }
        }

        private void FormConfigButton_Click(object sender, RoutedEventArgs e)
        {
            StageConnectSettings.initAUPSettings();
            GlobalConfigBox.Text = @"\Configs\ConnectStageList.xml";
        }

        private bool availableServer()
        {
            bool ret = true;
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = Properties.Settings.Default.AUPServer;
            builder.InitialCatalog = Properties.Settings.Default.AUPDatabase;
            builder.IntegratedSecurity = true;
            builder.ConnectRetryCount = 2;
            builder.ConnectTimeout = 25;

            try
            {
                aupConnection = new SqlConnection(builder.ToString());
                aupConnection.Open();
                aupConnection.Close();
                ret = true;
            }
            catch
            {
                ret = false;
            }
            return ret;
        }
    }
}
