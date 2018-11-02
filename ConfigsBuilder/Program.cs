using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncReplicaOperations;
using System.IO;
using System.Reflection;
using System.Xml;

namespace ConfigsBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                OperationsAPI.initAPI();
                OperationsAPI.StageListPath = @"\Configs\ConnectStageList.xml";
                Console.WriteLine("Начинается формирование конфигов...");
                var globalConf = StageConnectSettings.getInstance();

                if (globalConf.isValid)
                {
                    var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\AutoCreateConfigs";
                    if (Directory.Exists(path))
                    {
                        Console.WriteLine("Папка с логами существует.Папка будет удалена");
                        Directory.Delete(path, true);
                    }
                    foreach (RegionSetting globalSetting in globalConf.EntitiesList)
                    {
                        SaveConfig(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\AutoCreateConfigs",globalSetting.RegionId,DirectionsEnum.Import);
                        SaveConfig(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\AutoCreateConfigs", globalSetting.RegionId, DirectionsEnum.Export);
                    }
                    Console.WriteLine("Создание конфигов успешно");

                }
                else
                {
                    throw new Exception("Файл конфигурации не валиден");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Произошла следующая ошибка: " + e.Message);
            }
            finally
            {
                Console.ReadKey();
            }
        }

        static private void SaveConfig(string path, string ID, DirectionsEnum direction)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.AppendChild(xmlDocument.CreateXmlDeclaration("1.0", "utf-8", ""));
            var parentTag = xmlDocument.CreateElement("Settings");
            var childTag = xmlDocument.CreateElement("ConnectPoint");
            var attribute = xmlDocument.CreateAttribute("ID");
            attribute.Value = ID;
            childTag.Attributes.Append(attribute);
            attribute = xmlDocument.CreateAttribute("Direction");
            attribute.Value = ((int)direction).ToString();
            childTag.Attributes.Append(attribute);
            parentTag.AppendChild(childTag);

            xmlDocument.AppendChild(parentTag);
            if(!File.Exists(string.Format(@"{0}\{1}\{2}.cnfgroup", path, direction, ID)))
            {
                Directory.CreateDirectory(string.Format(@"{0}\{1}", path, direction));
            }
            xmlDocument.Save(string.Format(@"{0}\{1}\{2}.cnfgroup",path,direction,ID));
        }
    }
}
