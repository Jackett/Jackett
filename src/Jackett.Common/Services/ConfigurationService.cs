using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using NLog;

namespace Jackett.Common.Services
{

    public class ConfigurationService : IConfigurationService
    {
        private ISerializeService serializeService;
        private Logger logger;
        private IProcessService processService;
        private RuntimeSettings runtimeSettings;

        public ConfigurationService(ISerializeService s, IProcessService p, Logger l, RuntimeSettings settings)
        {
            serializeService = s;
            logger = l;
            processService = p;
            runtimeSettings = settings;
            CreateOrMigrateSettings();
        }

        public string GetAppDataFolder()
        {
            return runtimeSettings.DataFolder;
        }

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

                logger.Info("App config/log directory: " + GetAppDataFolder());
            }
            catch (Exception ex)
            {
                throw new Exception("Could not create settings directory. " + ex.Message);
            }

            if (System.Environment.OSVersion.Platform != PlatformID.Unix)
            {
                try
                {
                    string oldDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Jackett");
                    if (Directory.Exists(oldDir))
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
                                processService.StartProcessAndLog(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath, "--MigrateSettings", true);
                            }
                            catch
                            {
                                logger.Error("Unable to migrate settings when not running as administrator.");
                                Environment.ExitCode = 1;
                                return;
                            }
                        }
                    }
                    else
                    {
                        PerformMigration();
                    }

                }
                catch (Exception ex)
                {
                    logger.Error("ERROR could not migrate settings directory " + ex);
                }
            }
        }

        public void PerformMigration()
        {
            var oldDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Jackett");
            if (Directory.Exists(oldDir))
            {
                foreach (var file in Directory.GetFiles(oldDir, "*", SearchOption.AllDirectories))
                {
                    var path = file.Replace(oldDir, "");
                    var destPath = GetAppDataFolder() + path;
                    var destFolder = Path.GetDirectoryName(destPath);
                    if (!Directory.Exists(destFolder))
                    {
                        var dir = Directory.CreateDirectory(destFolder);
                        var directorySecurity = new DirectorySecurity(destFolder, AccessControlSections.All);
                        directorySecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
                        dir.SetAccessControl(directorySecurity);
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
                Directory.Delete(oldDir, true);
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
                logger.Error(e, "Error writing config file " + fullPath);
            }
        }

        public string ApplicationFolder()
        {
            return Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
        }

        public string GetContentFolder()
        {
            // If we are debugging we can use the non copied content.
            string dir = Path.Combine(ApplicationFolder(), "Content"); ;

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
            List<string> dirs = new List<string>();

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
            string dir = Path.Combine(ApplicationFolder(), "Definitions"); ;

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

        public string GetVersion()
        {
            return EnvironmentUtil.JackettVersion;
        }
    }
}
