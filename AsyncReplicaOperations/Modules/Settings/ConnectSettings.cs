using System;
using System.Collections;
using System.Reflection;
using System.Xml;
using System.Linq;

namespace AsyncReplicaOperations
{
    public class ConnectSettings : SettingsBase
    {
        private static ConnectSettings instance;
        private XmlNodeList nodeList;
        private bool isManualRequere = false;

        public ConnectSettings() : base()
        {
        }


        public static ConnectSettings getInstance()
        {
            if (instance == null)
            {
                instance = new ConnectSettings();
            }

            return instance;
        }

        public string getValue(int nodeId, ConnectNodesParamsEnum enumValue)
        {
            var ret = "";
            var node = nodeList.Item(nodeId);

            switch (enumValue)
            {
                case ConnectNodesParamsEnum.Id:
                    {
                        ret = node.SelectSingleNode("@ID").Value;
                        break;
                    }
                case ConnectNodesParamsEnum.Direction:
                    {
                        ret = node.SelectSingleNode("@Direction").Value;
                        break;
                    }
                default:
                    {
                        var regionSettings = StageConnectSettings.getInstance().FindId(node.SelectSingleNode("@ID").Value);
                        if (regionSettings == null) throw new Exception("Указанного сервера не существует");
                        switch (enumValue)
                        {
                            case ConnectNodesParamsEnum.Name:
                                {
                                    ret = regionSettings.RegionName;
                                    break;
                                }
                            case ConnectNodesParamsEnum.Server:
                                {
                                    ret = regionSettings.ServerName;
                                    break;
                                }
                            case ConnectNodesParamsEnum.DBName:
                                {
                                    ret = regionSettings.StageDBName;
                                    break;
                                }
                            default:
                                {
                                    break;
                                }
                        }
                        break;
                    }
            }

            return ret;
        }

        public string getValue(XmlNode node, ConnectNodesParamsEnum enumValue)
        {
            var ret = "";

            switch (enumValue)
            {
                case ConnectNodesParamsEnum.Id:
                    {
                        ret = node.SelectSingleNode("@ID").Value;
                        break;
                    }
                case ConnectNodesParamsEnum.Direction:
                    {
                        ret = node.SelectSingleNode("@Direction").Value;
                        break;
                    }
                default:
                    {
                        var regionSettings = StageConnectSettings.getInstance().FindId(node.SelectSingleNode("@ID").Value);
                        if (regionSettings == null) throw new Exception("Указанного сервера не существует");
                        switch (enumValue)
                        {
                            case ConnectNodesParamsEnum.Name:
                                {
                                    ret = regionSettings.RegionName;
                                    break;
                                }
                            case ConnectNodesParamsEnum.Server:
                                {
                                    ret = regionSettings.ServerName;
                                    break;
                                }
                            case ConnectNodesParamsEnum.DBName:
                                {
                                    ret = regionSettings.StageDBName;
                                    break;
                                }
                            default:
                                {
                                    break;
                                }
                        }
                        break;
                    }
            }

            return ret;
        }

        protected override void ParseXML(XmlElement xmlRoot)
        {
            nodeList = xmlRoot.SelectNodes("ConnectPoint");
            var enumer = nodeList.GetEnumerator();
            while (enumer.MoveNext())
            {
                var node = (XmlNode)enumer.Current;
                EntitiesList.Add(new RuntimeRegionSettings()
                {
                    Direction = (DirectionsEnum)Convert.ToInt32(this.getValue(node, ConnectNodesParamsEnum.Direction)),
                    RegionId = this.getValue(node, ConnectNodesParamsEnum.Id),
                    RegionName = this.getValue(node, ConnectNodesParamsEnum.Name),
                    ServerName = this.getValue(node, ConnectNodesParamsEnum.Server),
                    StageDBName = this.getValue(node, ConnectNodesParamsEnum.DBName)
                }
                );
            }
        }

        [ParameterMethod("ConfigPath")]
        protected override void loadSettingsFile()
        {
            document.Load(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + OperationsAPI.ConfigPath);
        }

        protected override void constructPrerequres()
        {
            if (!StageConnectSettings.getInstance().isValid)
            {
                isManualRequere = true;
                return;
            }
        }

        protected override void loadSettingsFile(string customPath)
        {
            loadSettingsFile();
        }

        public override void SaveConfig(string path)
        {

            var xmlDocument = new XmlDocument();
            xmlDocument.AppendChild(xmlDocument.CreateXmlDeclaration("1.0", "utf-8", ""));
            var parentTag = xmlDocument.CreateElement("Settings");
            foreach (var setting in EntitiesList.Cast<RuntimeRegionSettings>().ToList())
            {
                var childTag = xmlDocument.CreateElement("ConnectPoint");
                var attribute = xmlDocument.CreateAttribute("ID");
                attribute.Value = setting.RegionId;
                childTag.Attributes.Append(attribute);
                attribute = xmlDocument.CreateAttribute("Direction");
                attribute.Value = ((int)setting.Direction).ToString();
                childTag.Attributes.Append(attribute);
                parentTag.AppendChild(childTag);
            }
            xmlDocument.AppendChild(parentTag);
            xmlDocument.Save(path);

        }

    public int ConnectCount
    {
        get
        {
            return nodeList.Count;
        }
    }

    public IEnumerator ConnectEnumerator
    {
        get
        {
            return nodeList.GetEnumerator();
        }
    }

    public override bool isValid
    {
        get
        {
            return !isManualRequere;
        }
    }
}
}
