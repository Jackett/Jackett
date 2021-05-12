using System;
using System.Collections.Generic;
using System.IO;

using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;

namespace Jackett.Performance.Services
{
    public class PerformanceConfigurationService : IConfigurationService
    {
        public string GetContentFolder() => throw new NotImplementedException();

        public string GetVersion() => throw new NotImplementedException();

        public string GetIndexerConfigDir() => Path.Combine(GetAppDataFolder(), "Indexers");

        public string GetAppDataFolder() => Path.GetDirectoryName(typeof(Program).Assembly.Location);

        public string GetSonarrConfigFile() => throw new NotImplementedException();

        public T GetConfig<T>() => throw new NotImplementedException();

        public void SaveConfig<T>(T config) => throw new NotImplementedException();

        public string ApplicationFolder() => throw new NotImplementedException();

        public List<string> GetCardigannDefinitionsFolders() => new List<string>() { Path.Combine(GetAppDataFolder(), "Definitions") };

        public void CreateOrMigrateSettings() => throw new NotImplementedException();

        public void PerformMigration(string oldDirectory) => throw new NotImplementedException();

        public ServerConfig BuildServerConfig(RuntimeSettings runtimeSettings) => throw new NotImplementedException();
    }
}
