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
            this.configService = configService;
            this.logger = logger;
        }

        public void Delete(IIndexer indexer)
        {
            var configFilePath = GetIndexerConfigFilePath(indexer);
            File.Delete(configFilePath);
        }

        public void Load(IIndexer idx)
        {
            var configFilePath = GetIndexerConfigFilePath(idx);
            if (File.Exists(configFilePath))
            {
                try
                {
                    var fileStr = File.ReadAllText(configFilePath);
                    var jsonString = JToken.Parse(fileStr);
                    idx.LoadFromSavedConfiguration(jsonString);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed loading configuration for {0}, trying backup", idx.DisplayName);
                    var configFilePathBak = configFilePath + ".bak";
                    if (File.Exists(configFilePathBak))
                    {
                        try
                        {
                            var fileStrBak = File.ReadAllText(configFilePathBak);
                            var jsonStringBak = JToken.Parse(fileStrBak);
                            idx.LoadFromSavedConfiguration(jsonStringBak);
                            logger.Info("Successfully loaded backup config for {0}", idx.DisplayName);
                            idx.SaveConfig();
                        }
                        catch (Exception exbak)
                        {
                            logger.Error(exbak, "Failed loading backup configuration for {0}, you must reconfigure this indexer", idx.DisplayName);
                        }
                    }
                    else
                    {
                        logger.Error(ex, "Failed loading backup configuration for {0} (no backup available), you must reconfigure this indexer", idx.DisplayName);
                    }
                }
            }
        }

        public void Save(IIndexer indexer, JToken obj)
        {
            lock (configWriteLock)
            {
                var uID = Guid.NewGuid().ToString("N");
                var configFilePath = GetIndexerConfigFilePath(indexer);
                var configFilePathBak = configFilePath + ".bak";
                var configFilePathTmp = configFilePath + "." + uID + ".tmp";
                var content = obj.ToString();

                logger.Debug(string.Format("Saving new config file: {0}", configFilePathTmp));

                if (string.IsNullOrWhiteSpace(content))
                {
                    throw new Exception(string.Format("New config content for {0} is empty, please report this bug.", indexer.ID));
                }

                if (content.Contains("\x00"))
                {
                    throw new Exception(string.Format("New config content for {0} contains 0x00, please report this bug. Content: {1}", indexer.ID, content));
                }

                // make sure the config directory exists
                if (!Directory.Exists(configService.GetIndexerConfigDir()))
                    Directory.CreateDirectory(configService.GetIndexerConfigDir());

                // create new temporary config file
                File.WriteAllText(configFilePathTmp, content);
                var fileInfo = new FileInfo(configFilePathTmp);
                if (fileInfo.Length == 0)
                {
                    throw new Exception(string.Format("New config file {0} is empty, please report this bug.", configFilePathTmp));
                }

                // create backup file
                File.Delete(configFilePathBak);
                if (File.Exists(configFilePath))
                {
                    try
                    {
                        File.Move(configFilePath, configFilePathBak);
                    }
                    catch (IOException ex)
                    {
                        logger.Error(string.Format("Error while moving {0} to {1}: {2}", configFilePath, configFilePathBak, ex.ToString()));
                    }
                }

                // replace the actual config file
                File.Delete(configFilePath);
                try
                {
                    File.Move(configFilePathTmp, configFilePath);
                }
                catch (IOException ex)
                {
                    logger.Error(string.Format("Error while moving {0} to {1}: {2}", configFilePathTmp, configFilePath, ex.ToString()));
                }
            }
        }

        private string GetIndexerConfigFilePath(IIndexer indexer)
        {
            return Path.Combine(configService.GetIndexerConfigDir(), indexer.ID + ".json");
        }

        private IConfigurationService configService;
        private Logger logger;

        private static readonly object configWriteLock = new object();
    }
}
