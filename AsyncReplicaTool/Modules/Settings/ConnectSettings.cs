using System;
using System.Collections;
using System.Reflection;
using System.Xml;

namespace AsyncReplicaTool
{
    class ConnectSettings
    {
        private static ConnectSettings instance;
        private XmlDocument settingsDoc;
        private XmlNodeList nodeList;
        private bool isManualRequere = false;

        private ConnectSettings()
        {
            try
            {
                if (!Global.stageSettings.isValid)
                {
                    isManualRequere = true;
                    return;
                }
                settingsDoc = new XmlDocument();
                settingsDoc.Load(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Properties.Settings.Default.ConfigPath);
                var xmlRoot = settingsDoc.DocumentElement;
                nodeList = xmlRoot.SelectNodes("ConnectPoint");
            }
            catch
            {
                isManualRequere = true;
            }
        }

            
        public static ConnectSettings getInstance()
        {
            if(instance == null)
            {
                instance = new ConnectSettings();
            }

            return instance;
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
                        var regionSettings = Global.stageSettings.findId(node.SelectSingleNode("@ID").Value);
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
                        var regionSettings = Global.stageSettings.findId(node.SelectSingleNode("@ID").Value);
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

        public bool isManual
        {
            get
            {
                return isManualRequere;
            }
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
    }
}
