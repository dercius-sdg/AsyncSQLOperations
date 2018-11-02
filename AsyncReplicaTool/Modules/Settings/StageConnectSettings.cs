using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using System.Data.SqlClient;
using System.Windows;
using System.IO;

namespace AsyncReplicaTool
{
    class StageConnectSettings
    {
        private List<RegionSetting> regions;
        private XmlDocument document;
        private static StageConnectSettings instanse;

        private StageConnectSettings()
        {
            regions = new List<RegionSetting>();
            try
            {
                document = new XmlDocument();
                document.Load(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Properties.Settings.Default.StageListPath);

                var xmlRoot = document.DocumentElement;
                var nodes = xmlRoot.SelectNodes("Region");

                foreach(XmlNode node in nodes)
                {
                    var region = new RegionSetting();
                    region.RegionId = node.SelectSingleNode("@ID").Value;
                    region.RegionName = node.SelectSingleNode("@Name").Value;
                    var regionConnection = node.SelectSingleNode("Connection");
                    if(regionConnection != null)
                    {
                        region.ServerName = regionConnection.SelectSingleNode("@Server").Value;
                        region.StageDBName = regionConnection.SelectSingleNode("@DatabaseName").Value;
                        regions.Add(region);
                    }
                }
            }
            catch
            {
                regions = new List<RegionSetting>();
            }
        }

        public bool isValid
        {
            get
            {
                return regions.Count != 0;
            }
        }

        public RegionSetting findId(string id)
        {
            return regions.Find(x => x.RegionId == id);
        }

        public static StageConnectSettings getInstance()
        {
            if(instanse == null)
            {
                instanse = new StageConnectSettings();
            }
            return instanse;
        }

        public static void initAUPSettings()
        {
            var initPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            initPath += @"\\Configs\\ConnectStageList.xml";
            var aupConnectionBuilder = new SqlConnectionStringBuilder();
            aupConnectionBuilder.DataSource = Properties.Settings.Default.AUPServer;
            aupConnectionBuilder.InitialCatalog = Properties.Settings.Default.AUPDatabase;
            aupConnectionBuilder.IntegratedSecurity = true;
            var aupConnection = new SqlConnection(aupConnectionBuilder.ToString());
            var aupCommand = new SqlCommand("select ID,NAME,SERVERNAME,STAGEDBNAME from GM_REPLICAMONITORSETTINGS", aupConnection);
            try
            {
                aupCommand.Connection.Open();
                var document = new XmlDocument();
                document.AppendChild(document.CreateXmlDeclaration("1.0", "utf-8", ""));
                var root = document.CreateElement("ServersList");
                using (SqlDataReader reader = aupCommand.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                {
                    while (reader.Read())
                    {
                        var regionNode = document.CreateElement("Region");
                        regionNode.Attributes.Append(createAttribute(document, "ID", reader["ID"].ToString()));
                        regionNode.Attributes.Append(createAttribute(document, "Name", reader["NAME"].ToString()));
                        var connectNode = document.CreateElement("Connection");
                        connectNode.Attributes.Append(createAttribute(document, "Server", reader["SERVERNAME"].ToString()));
                        connectNode.Attributes.Append(createAttribute(document, "DatabaseName", reader["STAGEDBNAME"].ToString()));
                        regionNode.AppendChild(connectNode);
                        root.AppendChild(regionNode);
                    }
                }
                document.AppendChild(root);
                if (File.Exists(initPath))
                {
                    File.Delete(initPath);
                }
                if (!Directory.Exists(Path.GetDirectoryName(initPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(initPath));
                }
                document.Save(initPath);
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "Возникла ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
        private static XmlAttribute createAttribute(XmlDocument _document, string _name, string _value)
        {
            var attribute = _document.CreateAttribute(_name);
            attribute.Value = _value;
            return attribute;
        }
    }
}
