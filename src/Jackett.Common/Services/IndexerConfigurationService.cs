using System;
using System.IO;
using Jackett.Common.Indexers;
using Jackett.Common.Services.Interfaces;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Services
{
    public class IndexerConfigurationService : IIndexerConfigurationService
    {
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
            _configService = configService;
            _logger = logger;
        }

        public void Delete(IIndexer indexer)
        {
            var configFilePath = GetIndexerConfigFilePath(indexer);
            File.Delete(configFilePath);
            var configFilePathBak = $"{configFilePath}.bak";
            if (File.Exists(configFilePathBak))
                File.Delete(configFilePathBak);
        }

        public void Load(IIndexer idx)
        {
            var configFilePath = GetIndexerConfigFilePath(idx);
            if (File.Exists(configFilePath))
                try
                {
                    var fileStr = File.ReadAllText(configFilePath);
                    var jsonString = JToken.Parse(fileStr);
                    idx.LoadFromSavedConfiguration(jsonString);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed loading configuration for {0}, trying backup", idx.DisplayName);
                    var configFilePathBak = $"{configFilePath}.bak";
                    if (File.Exists(configFilePathBak))
                        try
                        {
                            var fileStrBak = File.ReadAllText(configFilePathBak);
                            var jsonStringBak = JToken.Parse(fileStrBak);
                            idx.LoadFromSavedConfiguration(jsonStringBak);
                            _logger.Info("Successfully loaded backup config for {0}", idx.DisplayName);
                            idx.SaveConfig();
                        }
                        catch (Exception exbak)
                        {
                            _logger.Error(
                                exbak, "Failed loading backup configuration for {0}, you must reconfigure this indexer",
                                idx.DisplayName);
                        }
                    else
                        _logger.Error(
                            ex,
                            "Failed loading backup configuration for {0} (no backup available), you must reconfigure this indexer",
                            idx.DisplayName);
                }
        }

        public void Save(IIndexer indexer, JToken obj)
        {
            lock (s_ConfigWriteLock)
            {
                var uId = Guid.NewGuid().ToString("N");
                var configFilePath = GetIndexerConfigFilePath(indexer);
                var configFilePathBak = $"{configFilePath}.bak";
                var configFilePathTmp = $"{configFilePath}.{uId}.tmp";
                var content = obj.ToString();
                _logger.Debug(string.Format("Saving new config file: {0}", configFilePathTmp));
                if (string.IsNullOrWhiteSpace(content))
                    throw new Exception(
                        string.Format("New config content for {0} is empty, please report this bug.", indexer.ID));
                if (content.Contains("\x00"))
                    throw new Exception(
                        string.Format(
                            "New config content for {0} contains 0x00, please report this bug. Content: {1}", indexer.ID,
                            content));

                // make sure the config directory exists
                if (!Directory.Exists(_configService.GetIndexerConfigDir()))
                    Directory.CreateDirectory(_configService.GetIndexerConfigDir());

                // create new temporary config file
                File.WriteAllText(configFilePathTmp, content);
                var fileInfo = new FileInfo(configFilePathTmp);
                if (fileInfo.Length == 0)
                    throw new Exception(
                        string.Format("New config file {0} is empty, please report this bug.", configFilePathTmp));

                // create backup file
                File.Delete(configFilePathBak);
                if (File.Exists(configFilePath))
                    try
                    {
                        File.Move(configFilePath, configFilePathBak);
                    }
                    catch (IOException ex)
                    {
                        _logger.Error(
                            string.Format("Error while moving {0} to {1}: {2}", configFilePath, configFilePathBak, ex));
                    }

                // replace the actual config file
                File.Delete(configFilePath);
                try
                {
                    File.Move(configFilePathTmp, configFilePath);
                }
                catch (IOException ex)
                {
                    _logger.Error(string.Format("Error while moving {0} to {1}: {2}", configFilePathTmp, configFilePath, ex));
                }
            }
        }

        private string GetIndexerConfigFilePath(IIndexer indexer) => Path.Combine(
            _configService.GetIndexerConfigDir(), $"{indexer.ID}.json");

        private readonly IConfigurationService _configService;
        private readonly Logger _logger;

        private static readonly object s_ConfigWriteLock = new object();
    }
}
