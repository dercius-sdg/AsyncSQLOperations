using AsyncReplicaOperations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RDPBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                OperationsAPI.initAPI();
                OperationsAPI.StageListPath = @"\Configs\ConnectStageList.xml";
                Console.WriteLine("Начинается формирование RDP файлов...");
                var globalConf = StageConnectSettings.getInstance();

                if (globalConf.isValid)
                {
                    var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\AutoCreateRDP";
                    if (Directory.Exists(path))
                    {
                        Console.WriteLine("Папка с файлами RDP существует.Папка будет удалена");
                        Directory.Delete(path, true);
                    }
                    foreach (RegionSetting globalSetting in globalConf.EntitiesList)
                    {
                        SaveRDP(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\AutoCreateRDP", globalSetting.RegionId, globalSetting.ServerName);
                    }
                    Console.WriteLine("Создание файлов RDP успешно");

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

        static private void SaveRDP(string path, string ID, string serverName)
        {
            var rdpSettingString = string.Format(@"screen mode id:i:2
                                    use multimon:i:0
                                    desktopwidth:i:1920
                                    desktopheight:i:1080
                                    session bpp:i:32
                                    winposstr:s:0,1,398,15,1198,615
                                    compression:i:1
                                    keyboardhook:i:2
                                    audiocapturemode:i:0
                                    videoplaybackmode:i:1
                                    connection type:i:7
                                    networkautodetect:i:1
                                    bandwidthautodetect:i:1
                                    displayconnectionbar:i:1
                                    enableworkspacereconnect:i:0
                                    disable wallpaper:i:0
                                    allow font smoothing:i:0
                                    allow desktop composition:i:0
                                    disable full window drag:i:1
                                    disable menu anims:i:1
                                    disable themes:i:0
                                    disable cursor setting:i:0
                                    bitmapcachepersistenable:i:1
                                    full address:s:{0}
                                    audiomode:i:0
                                    redirectprinters:i:1
                                    redirectcomports:i:0
                                    redirectsmartcards:i:1
                                    redirectclipboard:i:1
                                    redirectposdevices:i:0
                                    autoreconnection enabled:i:1
                                    authentication level:i:2
                                    prompt for credentials:i:0
                                    negotiate security layer:i:1
                                    remoteapplicationmode:i:0
                                    alternate shell:s:
                                    shell working directory:s:
                                    gatewayhostname:s:
                                    gatewayusagemethod:i:4
                                    gatewaycredentialssource:i:4
                                    gatewayprofileusagemethod:i:0
                                    promptcredentialonce:i:0
                                    gatewaybrokeringtype:i:0
                                    use redirection server name:i:0
                                    rdgiskdcproxy:i:0
                                    kdcproxyname:s:
                                    drivestoredirect:s:*
                                    ",serverName.Split('\\')[0]);

            if (!File.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            File.WriteAllText(path + "\\" + ID + ".rdp",rdpSettingString);
        }
    }
}
