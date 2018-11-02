using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;

namespace AsyncSQLOperations
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
                    region.RegionId = node.SelectSingleNode("@Id").Value;
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
                Console.WriteLine("Неверные параметры файла настроек соединений с регионами");
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
    }
}
