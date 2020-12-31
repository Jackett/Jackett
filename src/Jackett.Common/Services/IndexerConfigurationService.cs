using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jackett.Common.Indexers;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Services
{

    public class IndexerConfigurationService : IIndexerConfigurationService
    {
        private const string ConfigFileSuffix = ".json";

        //public override void LoadFromSavedConfiguration(JToken jsonConfig)
        //{
        //    if (jsonConfig is JObject)
        //    {
        //        configData.CookieHeader.Value = jsonConfig.Value<string>("cookies");
        //        configData.IncludeRaw.Value = jsonConfig.Value<bool>("raws");
        //        IsConfigured = true;
        //        SaveConfig();
        //        return;
        //    }

        //    base.LoadFromSavedConfiguration(jsonConfig);
        //}

        public IndexerConfigurationService(IConfigurationService configService, Logger logger)
        {
            this.configService = configService;
            this.logger = logger;
        }

        public void Delete(IIndexer indexer)
        {
            var configFilePath = GetIndexerConfigFilePath(indexer.Id);
            File.Delete(configFilePath);
            var configFilePathBak = configFilePath + ".bak";
            if (File.Exists(configFilePathBak))
                File.Delete(configFilePathBak);
        }

        public void Load(IIndexer indexer)
        {
            var configFilePath = GetIndexerConfigFilePath(indexer.Id);
            if (!File.Exists(configFilePath))
                return;
            try
            {
                var fileStr = File.ReadAllText(configFilePath);
                var jsonString = JToken.Parse(fileStr);
                indexer.LoadFromSavedConfiguration(jsonString);
            }
            catch (Exception e)
            {
                logger.Error($"Failed loading configuration for {indexer.DisplayName}, trying backup\n{e}");
                var configFilePathBak = configFilePath + ".bak";
                if (File.Exists(configFilePathBak))
                    try
                    {
                        var fileStrBak = File.ReadAllText(configFilePathBak);
                        var jsonStringBak = JToken.Parse(fileStrBak);
                        indexer.LoadFromSavedConfiguration(jsonStringBak);
                        logger.Info($"Successfully loaded backup config for {indexer.DisplayName}");
                        indexer.SaveConfig();
                    }
                    catch (Exception e2)
                    {
                        logger.Error($"Failed loading backup configuration for {indexer.DisplayName}, you must reconfigure this indexer\n{e2}");
                    }
                else
                    logger.Error($"Failed loading backup configuration for {indexer.DisplayName} (no backup available), you must reconfigure this indexer\n{e}");
            }
        }

        public void Save(IIndexer indexer, JToken obj)
        {
            lock (configWriteLock)
            {
                var uId = Guid.NewGuid().ToString("N");
                var configFilePath = GetIndexerConfigFilePath(indexer.Id);
                var configFilePathBak = configFilePath + ".bak";
                var configFilePathTmp = configFilePath + "." + uId + ".tmp";
                var content = obj.ToString();

                logger.Debug($"Saving new config file: {configFilePathTmp}");

                if (string.IsNullOrWhiteSpace(content))
                    throw new Exception($"New config content for {indexer.Id} is empty, please report this bug.");

                if (content.Contains("\x00"))
                    throw new Exception($"New config content for {indexer.Id} contains 0x00, please report this bug. Content: {content}");

                // make sure the config directory exists
                if (!Directory.Exists(configService.GetIndexerConfigDir()))
                    Directory.CreateDirectory(configService.GetIndexerConfigDir());

                // create new temporary config file
                File.WriteAllText(configFilePathTmp, content);
                var fileInfo = new FileInfo(configFilePathTmp);
                if (fileInfo.Length == 0)
                    throw new Exception($"New config file {configFilePathTmp} is empty, please report this bug.");

                // create backup file
                File.Delete(configFilePathBak);
                if (File.Exists(configFilePath))
                    try
                    {
                        File.Move(configFilePath, configFilePathBak);
                    }
                    catch (IOException e)
                    {
                        logger.Error($"Error while moving {configFilePath} to {configFilePathBak}\n{e}");
                    }

                // replace the actual config file
                File.Delete(configFilePath);
                try
                {
                    File.Move(configFilePathTmp, configFilePath);
                }
                catch (IOException e)
                {
                    logger.Error($"Error while moving {configFilePathTmp} to {configFilePath}\n{e}");
                }
            }
        }

        public IEnumerable<string> FindConfiguredIndexerIds()
        {
            var directoryInfo = new DirectoryInfo(configService.GetIndexerConfigDir());

            if (!directoryInfo.Exists)
            {
                return new List<string>();
            }

            var fileNames = directoryInfo
                            .GetFiles().Select(file => file.Name)
                            .Where(fileName => fileName.EndsWith(ConfigFileSuffix))
                            .ToList();

            return fileNames.Select(FilesystemUtil.getFileNameWithoutExtension)
                            .Where(indexId => indexId != null)
                            .ToList();
        }

        public string GetIndexerConfigFilePath(string indexerId)
            => Path.Combine(configService.GetIndexerConfigDir(), indexerId + ConfigFileSuffix);

        private readonly IConfigurationService configService;
        private readonly Logger logger;

        private static readonly object configWriteLock = new object();
    }
}
