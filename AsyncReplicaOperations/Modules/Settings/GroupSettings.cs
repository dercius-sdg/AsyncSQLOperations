using System;
using System.Collections;
using System.Reflection;
using System.Xml;
using System.Linq;

namespace AsyncReplicaOperations
{
    public class GroupSettings:SettingsBase
    {
        private XmlNodeList nodeList;
        private bool isManualRequere = false;

        public GroupSettings(string groupPath):base(groupPath)
        {
        }

        
        public string getValue(int nodeId,ConnectNodesParamsEnum enumValue)
        {
            var ret = "";
            var node = nodeList.Item(nodeId);

            switch(enumValue)
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
                var setting = new RuntimeRegionSettings()
                {
                    Direction = (DirectionsEnum)Convert.ToInt32(this.getValue(node, ConnectNodesParamsEnum.Direction)),
                    RegionId = this.getValue(node, ConnectNodesParamsEnum.Id),
                    RegionName = this.getValue(node, ConnectNodesParamsEnum.Name),
                    ServerName = this.getValue(node, ConnectNodesParamsEnum.Server),
                    StageDBName = this.getValue(node, ConnectNodesParamsEnum.DBName)
                };
                var restrictionNode = node.SelectSingleNode("Restrictions");
                var restrictions = new Restrictions();
                if (restrictionNode != null)
                {
                    if (restrictionNode.SelectSingleNode("@Count")!=null)
                    {
                        restrictions.CountRestrictions = Convert.ToInt32(restrictionNode.SelectSingleNode("@Count").Value);
                    }
                    var restrictionsNode = restrictionNode.SelectNodes("Restriction");
                    foreach(XmlNode rnode in restrictionsNode)
                    {
                        var restriction = new Restriction();
                        if (rnode.SelectSingleNode("@ReplicaId") == null) continue;
                        restriction.ReplicaId = rnode.SelectSingleNode("@ReplicaId").Value;
                        if (rnode.SelectSingleNode("@WorkDirectory") != null)
                        {
                            restriction.WorkDirectory = rnode.SelectSingleNode("@WorkDirectory").Value;
                        }
                        restrictions.Add(restriction);
                    }
                    setting.Restrictions = restrictions;
                }
                EntitiesList.Add(setting);
            }
        }

        [ParameterMethod("ConfigPath")]
        protected override void loadSettingsFile(string customPath)
        {
            var path = ((customPath == "") ? System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + OperationsAPI.ConfigPath : customPath);
            document.Load(path);
        }

        protected override void constructPrerequres()
        {
            if (!StageConnectSettings.getInstance().isValid)
            {
                isManualRequere = true;
                return;
            }
        }

        protected override void loadSettingsFile()
        {
            throw new NotImplementedException();
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
                XmlElement restrictionsNode = null ;
                if(setting.Restrictions.CountRestrictions > 0)
                {
                    restrictionsNode = xmlDocument.CreateElement("Restrictions");
                    var attributeCount = xmlDocument.CreateAttribute("Count");
                    attributeCount.Value = setting.Restrictions.CountRestrictions.ToString();
                    restrictionsNode.Attributes.Append(attributeCount);
                }
                foreach(var r in setting.Restrictions)
                {
                    if(restrictionsNode == null)
                    {
                        restrictionsNode = xmlDocument.CreateElement("Restrictions");
                    }
                    var restrictionNode = xmlDocument.CreateElement("Restriction");
                    attribute = xmlDocument.CreateAttribute("ReplicaId");
                    attribute.Value = r.ReplicaId;
                    restrictionNode.Attributes.Append(attribute);
                    if(r.WorkDirectory != string.Empty)
                    {
                        attribute = xmlDocument.CreateAttribute("WorkDirectory");
                        attribute.Value = r.WorkDirectory;
                        restrictionNode.Attributes.Append(attribute);
                    }
                    restrictionsNode.AppendChild(restrictionNode);
                }
                if(restrictionsNode != null)
                {
                    childTag.AppendChild(restrictionsNode);
                }
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
                return !isManualRequere && (EntitiesList.Count > 0);
            }
        }
    }
}
