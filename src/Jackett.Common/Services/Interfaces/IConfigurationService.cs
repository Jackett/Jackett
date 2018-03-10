using System.Collections.Generic;

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
        void PerformMigration();
    }
}
