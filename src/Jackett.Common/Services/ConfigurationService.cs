using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Services
{

    public class ConfigurationService : IConfigurationService
    {
        private readonly ISerializeService serializeService;
        private readonly Logger logger;
        private readonly IProcessService processService;
        private readonly RuntimeSettings runtimeSettings;

        public ConfigurationService(ISerializeService s, IProcessService p, Logger l, RuntimeSettings settings)
        {
            serializeService = s;
            logger = l;
            processService = p;
            runtimeSettings = settings;
            CreateOrMigrateSettings();
        }

        public string GetAppDataFolder() => runtimeSettings.DataFolder;

        public void CreateOrMigrateSettings()
        {
            try
            {
                if (!Directory.Exists(GetAppDataFolder()))
                {
                    var dir = Directory.CreateDirectory(GetAppDataFolder());
                    if (System.Environment.OSVersion.Platform != PlatformID.Unix)
                    {
                        var access = dir.GetAccessControl();
                        var directorySecurity = new DirectorySecurity(GetAppDataFolder(), AccessControlSections.All);
                        directorySecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
                        dir.SetAccessControl(directorySecurity);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not create settings directory. " + ex.Message);
            }

            if (Environment.OSVersion.Platform != PlatformID.Unix)
            {
                try
                {
                    var oldDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Jackett");
                    if (Directory.Exists(oldDir))
                    {

                        // On Windows we need admin permissions to migrate as they were made with admin permissions.
                        if (ServerUtil.IsUserAdministrator())
                        {
                            PerformMigration(oldDir);
                        }
                        else
                        {
                            try
                            {
                                processService.StartProcessAndLog(EnvironmentUtil.JackettExecutablePath(), "", true);
                            }
                            catch
                            {
                                logger.Error("Unable to migrate settings when not running as administrator.");
                                Environment.ExitCode = 1;
                                return;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.Error($"ERROR could not migrate settings directory\n{e}");
                }
            }

            // Perform a migration in case of https://github.com/Jackett/Jackett/pull/11173#issuecomment-787520128
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                // In cases where the app data folder is the same as "$(cwd)/Jackett" we don't need to perform a migration
                var fullConfigPath = Path.GetFullPath("Jackett");
                if (GetAppDataFolder() != fullConfigPath && !File.Exists(Path.Combine(fullConfigPath, "jackett")))
                {
                    PerformMigration(fullConfigPath);
                }
            }
        }

        public void PerformMigration(string oldDirectory)
        {
            if (!Directory.Exists(oldDirectory))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(oldDirectory, "*", SearchOption.AllDirectories))
            {
                var path = file.Replace(oldDirectory, "");
                var destPath = GetAppDataFolder() + path;
                var destFolder = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destFolder))
                {
                    var dir = Directory.CreateDirectory(destFolder);
                    if (System.Environment.OSVersion.Platform != PlatformID.Unix)
                    {
                        var directorySecurity = new DirectorySecurity(destFolder, AccessControlSections.All);
                        directorySecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
                        dir.SetAccessControl(directorySecurity);
                    }
                }
                if (!File.Exists(destPath))
                {
                    File.Copy(file, destPath);
                    // The old files were created when running as admin so make sure they are editable by normal users / services.
                    if (System.Environment.OSVersion.Platform != PlatformID.Unix)
                    {
                        var fileInfo = new FileInfo(destFolder);
                        var fileSecurity = new FileSecurity(destPath, AccessControlSections.All);
                        fileSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.None, PropagationFlags.None, AccessControlType.Allow));
                        fileInfo.SetAccessControl(fileSecurity);
                    }
                }
            }

            // Don't remove configs that have been migrated to the same folder
            if (GetAppDataFolder() != oldDirectory)
            {
                Directory.Delete(oldDirectory, true);
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
                logger.Error($"Error reading config file {fullPath}\n{e}");
                return default;
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
                logger.Error($"Error writing config file {fullPath}\n{e}");
            }
        }

        public string ApplicationFolder() => EnvironmentUtil.JackettInstallationPath();

        public string GetContentFolder()
        {
            // If we are debugging we can use the non copied content.
            var dir = Path.Combine(ApplicationFolder(), "Content");

#if DEBUG
            // When we are running in debug use the source files
            var sourcePath = Path.GetFullPath(Path.Combine(ApplicationFolder(), "..\\..\\..\\Jackett.Common\\Content"));
            if (Directory.Exists(sourcePath))
            {
                dir = sourcePath;
            }
#endif
            return dir;
        }

        public List<string> GetCardigannDefinitionsFolders()
        {
            var dirs = new List<string>();

            if (System.Environment.OSVersion.Platform == PlatformID.Unix)
            {
                dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "cardigann/definitions/"));
                dirs.Add("/etc/xdg/cardigan/definitions/");
            }
            else
            {
                dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "cardigann\\definitions\\"));
                dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "cardigann\\definitions\\"));
            }

            // If we are debugging we can use the non copied definitions.
            var dir = Path.Combine(ApplicationFolder(), "Definitions");

#if DEBUG
            // When we are running in debug use the source files
            var sourcePath = Path.GetFullPath(Path.Combine(ApplicationFolder(), "..\\..\\..\\Jackett.Common\\Definitions"));
            if (Directory.Exists(sourcePath))
            {
                dir = sourcePath;
            }
#endif
            dirs.Add(dir);
            return dirs;
        }



        public string GetIndexerConfigDir() => Path.Combine(GetAppDataFolder(), "Indexers");

        public string GetConfigFile() => Path.Combine(GetAppDataFolder(), "config.json");

        public string GetSonarrConfigFile() => Path.Combine(GetAppDataFolder(), "sonarr_api.json");

        public string GetVersion() => EnvironmentUtil.JackettVersion();

        public ServerConfig BuildServerConfig(RuntimeSettings runtimeSettings)
        {
            // Load config
            var config = GetConfig<ServerConfig>();
            if (config == null)
            {
                config = new ServerConfig(runtimeSettings);
            }
            else
            {
                //We don't load these out of the config files as it could get confusing to users who accidently save.
                //In future we could flatten the serverconfig, and use command line parameters to override any configuration.
                config.RuntimeSettings = runtimeSettings;
            }

            if (string.IsNullOrWhiteSpace(config.APIKey))
            {
                // Check for legacy key config
                var apiKeyFile = Path.Combine(GetAppDataFolder(), "api_key.txt");
                if (File.Exists(apiKeyFile))
                {
                    config.APIKey = File.ReadAllText(apiKeyFile);
                }

                // Check for legacy settings

                var path = Path.Combine(GetAppDataFolder(), "config.json");
                if (File.Exists(path))
                {
                    var jsonReply = JObject.Parse(File.ReadAllText(path));
                    config.Port = (int)jsonReply["port"];
                    config.AllowExternal = (bool)jsonReply["public"];
                }

                if (string.IsNullOrWhiteSpace(config.APIKey))
                    config.APIKey = StringUtil.GenerateRandom(32);

                SaveConfig(config);
            }

            if (string.IsNullOrWhiteSpace(config.InstanceId))
            {
                config.InstanceId = StringUtil.GenerateRandom(64);
                SaveConfig(config);
            }

            config.ConfigChanged();
            return config;
        }
    }
}
