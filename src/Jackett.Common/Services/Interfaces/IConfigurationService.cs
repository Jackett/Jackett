using System.Collections.Generic;
using Jackett.Common.Models.Config;

namespace Jackett.Common.Services.Interfaces
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
        List<string> GetCardigannDefinitionsFolders();
        void CreateOrMigrateSettings();
        void PerformMigration(string oldDirectory);
        ServerConfig BuildServerConfig(RuntimeSettings runtimeSettings);
    }
}
