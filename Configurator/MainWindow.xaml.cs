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
using System.Windows.Navigation;
using AsyncReplicaOperations;
using Microsoft.Win32;
using System.Reflection;
using System.IO;

namespace Configurator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GroupSettings runtimeConfig;
        private AsyncReplicaOperations.DirectionsEnum direction;
        private List<RegionSetting> stageServers;
        private string groupPath;
        private List<AsyncSQLConnectPuller> pullers;
        public MainWindow()
        {
            InitializeComponent();
            
            OperationsAPI.initAPI();
            OperationsAPI.StageListPath = @"\Configs\ConnectStageList.xml";
            OperationsAPI.ConfigPath = @"\Configs\ConnectConfig.xml";
            this.LoadConfigs();
            pullers = new List<AsyncSQLConnectPuller>();
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
                var window = new Restrictions(((RuntimeRegionSettings)RunTimeServer.SelectedItem).Restrictions.ToList<Restriction>(), ((RuntimeRegionSettings)RunTimeServer.SelectedItem).Direction);
                window.Closing += RestrictionWindowClosing;
                window.ShowDialog();
            }
        }

        private void RestrictionWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ((RuntimeRegionSettings)RunTimeServer.SelectedItem).Restrictions.Clear();
            foreach (Restriction r in ((Restrictions)sender).RestrictionsList)
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
    }
}
