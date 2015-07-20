using Newtonsoft.Json.Linq;
using NLog;
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
        string GetSonarrConfigFile();
        T GetConfig<T>();
        void SaveConfig<T>(T config);
        string ApplicationFolder();
    }

    public class ConfigurationService : IConfigurationService
    {
        private ISerializeService serializeService;
        private Logger logger;

        public ConfigurationService(ISerializeService s, Logger l)
        {
            serializeService = s;
            logger = l;
            CreateOrMigrateSettings();
        }

        private void CreateOrMigrateSettings()
        {
            try
            {
                if (!Directory.Exists(GetAppDataFolder()))
                {
                    Directory.CreateDirectory(GetAppDataFolder());
                }

                logger.Debug("App config/log directory: " + GetAppDataFolder());
            }
            catch (Exception ex)
            {
                throw new Exception("Could not create settings directory. " + ex.Message);
            }

            try
            {
                string oldDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Jackett");
                if (Directory.Exists(oldDir))
                {
                    foreach (var file in Directory.GetFiles(oldDir, "*", SearchOption.AllDirectories))
                    {
                        var path = file.Replace(oldDir, "");
                        var destFolder = GetAppDataFolder() + path;
                        if (!Directory.Exists(Path.GetDirectoryName(destFolder)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(destFolder));
                        }
                        if (!File.Exists(destFolder))
                        {
                            File.Move(file, destFolder);
                        }
                    }
                    Directory.Delete(oldDir, true);
                }
            }
            catch (Exception ex)
            {
                logger.Error("ERROR could not migrate settings directory " + ex);
            }
        }

        public T GetConfig<T>()
        {
            var type = typeof(T);
            var fullPath = Path.Combine(GetAppDataFolder(), type.Name + ".json");
            try
            {
                if (!File.Exists(fullPath))
                {
                    logger.Debug("Config file does not exist: " + fullPath);
                    return default(T);
                }

                return serializeService.DeSerialise<T>(File.ReadAllText(fullPath));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error reading config file " + fullPath);
                return default(T);
            }
        }

        public void SaveConfig<T>(T config)
        {
            var type = typeof(T);
            var fullPath = Path.Combine(GetAppDataFolder(), type.Name + ".json");
            try
            {
                var json = serializeService.Serialise(config);
                if (!Directory.Exists(GetAppDataFolder()))
                    Directory.CreateDirectory(GetAppDataFolder());
                File.WriteAllText(fullPath, json);
            }
            catch (Exception e)
            {
                logger.Error(e, "Error reading config file " + fullPath);
            }
        }

        public string ApplicationFolder()
        {
            return Path.GetDirectoryName(Application.ExecutablePath);
        }

        public string GetContentFolder()
        {
            // If we are debugging we can use the non copied content.
            var dir = Path.Combine(ApplicationFolder(), "Content");
            if (!Directory.Exists(dir))
            {
                dir = Path.Combine(ApplicationFolder(), "..\\..\\..\\Jackett\\Content");
            }

            return dir;
        }

        public string GetVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public string GetAppDataFolder()
        {
            return GetAppDataFolderStatic();
        }

        /// <summary>
        ///  This is needed for the logger prior to ioc setup.
        /// </summary>
        /// <returns></returns>
        public static string GetAppDataFolderStatic()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Jackett");
        }

        public string GetIndexerConfigDir()
        {
            return Path.Combine(GetAppDataFolder(), "Indexers");
        }

        public string GetConfigFile()
        {
            return Path.Combine(GetAppDataFolder(), "config.json");
        }

        public string GetSonarrConfigFile()
        {
            return Path.Combine(GetAppDataFolder(), "sonarr_api.json");
        }
    }
}
