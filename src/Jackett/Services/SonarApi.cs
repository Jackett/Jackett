using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Jackett
{
    public interface ISonarrApi
    {
        Task TestConnection();
        SonarrApi.ConfigurationSonarr GetConfiguration();
        Task ApplyConfiguration(JToken configJson);
        Task<string[]> GetShowTitle(int rid);
    }

    public class SonarrApi: ISonarrApi
    {
        public class ConfigurationSonarr : ConfigurationData
        {
            public StringItem Host { get; private set; }
            public StringItem Port { get; private set; }
            public StringItem ApiKey { get; private set; }

            DisplayItem ApiInfo;

            public ConfigurationSonarr()
            {
                Host = new StringItem { Name = "Host", Value = "http://localhost" };
                Port = new StringItem { Name = "Port", Value = "8989" };
                ApiKey = new StringItem { Name = "API Key" };
                ApiInfo = new DisplayItem("API Key can be found in Sonarr > Settings > General > Security") { Name = "API Info" };
            }

            public override Item[] GetItems()
            {
                return new Item[] { Host, Port, ApiKey, ApiInfo };
            }

        }

      

        string Host;
        int Port;
        string ApiKey;

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        ConcurrentDictionary<int, string[]> IdNameMappings;

        private IConfigurationService configService;

        public SonarrApi(IConfigurationService c)
        {
            configService = c;

            LoadSettings();

            cookies = new CookieContainer();
            handler = new HttpClientHandler
            {
                CookieContainer = cookies,
                AllowAutoRedirect = true,
                UseCookies = true,
            };
            client = new HttpClient(handler);

            IdNameMappings = new ConcurrentDictionary<int, string[]>();
        }

        async Task ReloadNameMappings(string host, int port, string apiKey)
        {
            Uri hostUri = new Uri(host);
            var queryUrl = string.Format("http://{0}:{1}/api/series?apikey={2}", hostUri.Host, port, apiKey);
            var response = await client.GetStringAsync(queryUrl);
            var json = JArray.Parse(response);

            IdNameMappings.Clear();
            foreach (var item in json)
            {
                var titles = new List<string>();
                titles.Add(SanitizeTitle((string)item["title"]));
                foreach (var t in item["alternateTitles"])
                {
                    titles.Add(SanitizeTitle((string)t["title"]));
                }
                IdNameMappings.TryAdd((int)item["tvRageId"], titles.ToArray());
            }
        }

        string SanitizeTitle(string title)
        {
            char[] arr = title.ToCharArray();

            arr = Array.FindAll<char>(arr, c => (char.IsLetterOrDigit(c)
                                              || char.IsWhiteSpace(c)
                                              || c == '-'
                                              || c == '.'
                                              ));
            title = new string(arr);
            return title;
        }

        void LoadSettings()
        {
            try
            {
                if (File.Exists(configService.GetSonarrConfigFile()))
                {
                    var json = JObject.Parse(File.ReadAllText(configService.GetSonarrConfigFile()));
                    Host = (string)json["host"];
                    Port = (int)json["port"];
                    ApiKey = (string)json["api_key"];
                }
            }
            catch (Exception) { }
        }

        void SaveSettings()
        {
            JObject json = new JObject();
            json["host"] = Host;
            json["port"] = Port;
            json["api_key"] = ApiKey;
            File.WriteAllText(configService.GetSonarrConfigFile(), json.ToString());
        }

        public ConfigurationSonarr GetConfiguration()
        {
            var config = new ConfigurationSonarr();
            if (ApiKey != null)
            {
                config.Host.Value = Host;
                config.Port.Value = Port.ToString();
                config.ApiKey.Value = ApiKey;
            }
            return config;
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            var config = new ConfigurationSonarr();
            config.LoadValuesFromJson(configJson);
            await ReloadNameMappings(config.Host.Value, ParseUtil.CoerceInt(config.Port.Value), config.ApiKey.Value);
            Host = "http://" + new Uri(config.Host.Value).Host;
            Port = ParseUtil.CoerceInt(config.Port.Value);
            ApiKey = config.ApiKey.Value;
            SaveSettings();
        }

        public async Task TestConnection()
        {
            await ReloadNameMappings(Host, Port, ApiKey);
        }

        public async Task<string[]> GetShowTitle(int rid)
        {
            if (rid == 0)
                return null;

            int tries = 0;
            while (tries < 2)
            {
                string[] titles;
                if (IdNameMappings.TryGetValue(rid, out titles))
                    return titles;
                await ReloadNameMappings(Host, Port, ApiKey);
                tries++;
            }
            return null;
        }
    }
}
