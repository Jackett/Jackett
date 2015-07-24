using Jackett.Utils;
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
        void CreateOrMigrateSettings();
        void PerformMigration();
    }

    public class ConfigurationService : IConfigurationService
    {
        private ISerializeService serializeService;
        private Logger logger;
        private IProcessService processService;

        public ConfigurationService(ISerializeService s, IProcessService p, Logger l)
        {
            serializeService = s;
            logger = l;
            processService = p;
            CreateOrMigrateSettings();
        }

        public void CreateOrMigrateSettings()
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
                    if (System.Environment.OSVersion.Platform != PlatformID.Unix)
                    {
                        // On Windows we need admin permissions to migrate as they were made with admin permissions.
                        if (ServerUtil.IsUserAdministrator())
                        {
                            PerformMigration();
                        }
                        else
                        {
                            try
                            {
                                processService.StartProcessAndLog(Application.ExecutablePath, "--MigrateSettings", true);
                            }
                            catch
                            {
                                Engine.Logger.Error("Unable to migrate settings when not running as administrator.");
                                Environment.ExitCode = 1;
                                return;
                            }
                        }
                    } else
                    {
                        PerformMigration();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("ERROR could not migrate settings directory " + ex);
            }
        }

        public void PerformMigration()
        {
            var oldDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Jackett");
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
                    File.Copy(file, destFolder);
                }
            }
            Directory.Delete(oldDir, true);
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
            string dir = Path.Combine(ApplicationFolder(), "Content"); ;

#if DEBUG
            // When we are running in debug use the source files
            var sourcePath = Path.GetFullPath(Path.Combine(ApplicationFolder(), "..\\..\\..\\Jackett\\Content"));
            if (Directory.Exists(sourcePath))
            {
                dir = sourcePath;
            }
#endif
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
            if (System.Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Jackett");
            }
            else
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Jackett");
            }
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
