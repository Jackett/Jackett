using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Jackett.Services
{
    public interface IConfigurationService
    {
        string GetContentFolder();
        string GetVersion();
        string GetIndexerConfigDir();
        string GetAppDataFolder();
        JObject ReadServerSettingsFile();
        string GetSonarrConfigFile();
    }

    public class ConfigurationService: IConfigurationService
    {
        public string GetContentFolder()
        {
            var baseDir = Path.GetDirectoryName(Application.ExecutablePath);
            // If we are debugging we can use the non copied content.
            if (Debugger.IsAttached)
            {
                return Path.Combine(baseDir, "..\\..\\..\\Jackett\\WebContent");
            }
            else
            {
                return Path.Combine(baseDir, "WebContent");
            }
        }

        public string GetVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public string GetAppDataFolder()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Jackett"); ;
        }

        public string GetIndexerConfigDir()
        {
            return   Path.Combine(GetAppDataFolder(), "Indexers");
        }

        public string GetConfigFile()
        {
            return Path.Combine(GetAppDataFolder(), "config.json");
        }

        public string GetSonarrConfigFile()
        {
            return Path.Combine(GetAppDataFolder(), "sonarr_api.json");
        }


        public JObject ReadServerSettingsFile()
        {
            var path = GetConfigFile();
            JObject jsonReply = new JObject();
            if (File.Exists(path))
            {
                jsonReply = JObject.Parse(File.ReadAllText(path));
               // Port = (int)jsonReply["port"];
              //  ListenPublic = (bool)jsonReply["public"];
            }
            else
            {
               // jsonReply["port"] = Port;
               // jsonReply["public"] = ListenPublic;
            }
            return jsonReply;
        }
    }
}
