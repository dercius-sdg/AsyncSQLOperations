using System;
using System.Reflection;
using System.Xml;
using System.Data.SqlClient;
using System.Linq;
using System.IO;

namespace AsyncReplicaOperations
{
    public class StageConnectSettings : SettingsBase
    {
        private static StageConnectSettings instanse;


        private StageConnectSettings() : base() { }

        public override bool isValid
        {
            get
            {
                return EntitiesList.Count != 0;
            }
        }

        public RegionSetting FindId(string id)
        {
            return EntitiesList.Cast<RegionSetting>().ToList().Find(x => x.RegionId == id);
        }

        public static StageConnectSettings getInstance()
        {
            if (instanse == null)
            {
                instanse = new StageConnectSettings();
            }
            return instanse;
        }

        [ParameterMethod("ConfigServer"), ParameterMethod("ConfigDatabase")]
        public static void initAUPSettings()
        {
            var initPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            initPath += @"\\Configs\\ConnectStageList.xml";
            var aupConnectionBuilder = new SqlConnectionStringBuilder();
            aupConnectionBuilder.DataSource = OperationsAPI.ConfigServer;
            aupConnectionBuilder.InitialCatalog = OperationsAPI.ConfigDatabase;
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
                throw new Exception(error.Message);
            }

        }
        private static XmlAttribute createAttribute(XmlDocument _document, string _name, string _value)
        {
            var attribute = _document.CreateAttribute(_name);
            attribute.Value = _value;
            return attribute;
        }

        protected override void ParseXML(XmlElement xmlRoot)
        {
            var nodes = xmlRoot.SelectNodes("Region");

            foreach (XmlNode node in nodes)
            {
                var region = new RegionSetting();
                region.RegionId = node.SelectSingleNode("@ID").Value;
                region.RegionName = node.SelectSingleNode("@Name").Value;
                var regionConnection = node.SelectSingleNode("Connection");
                if (regionConnection != null)
                {
                    region.ServerName = regionConnection.SelectSingleNode("@Server").Value;
                    region.StageDBName = regionConnection.SelectSingleNode("@DatabaseName").Value;
                    EntitiesList.Add(region);
                    //regions.Add(region);
                }
            }
        }
        [ParameterMethod("StageListPath")]
        protected override void loadSettingsFile()
        {
            document.Load(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + OperationsAPI.StageListPath);
        }

        protected override void loadSettingsFile(string customPath)
        {
            loadSettingsFile();
        }

        public override void SaveConfig(string path)
        {
            var document = new XmlDocument();
            document.AppendChild(document.CreateXmlDeclaration("1.0", "utf-8", ""));
            var root = document.CreateElement("ServersList");
            foreach(var setting in EntitiesList.Cast<RegionSetting>().ToList())
            {
                var regionNode = document.CreateElement("Region");
                regionNode.Attributes.Append(createAttribute(document, "ID", setting.RegionId));
                regionNode.Attributes.Append(createAttribute(document, "Name", setting.RegionName));
                var connectNode = document.CreateElement("Connection");
                connectNode.Attributes.Append(createAttribute(document, "Server", setting.ServerName));
                connectNode.Attributes.Append(createAttribute(document, "DatabaseName", setting.StageDBName));
                regionNode.AppendChild(connectNode);
                root.AppendChild(regionNode);
            }
            document.AppendChild(root);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            document.Save(path);
        }
    }
}
