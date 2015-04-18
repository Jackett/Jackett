using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Jackett
{
    public class SonarrApi
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

        static string SonarrConfigFile = Path.Combine(Program.AppConfigDirectory, "sonarr_api.json");

        string Host;
        int Port;
        string ApiKey;

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public SonarrApi()
        {
            LoadSettings();

            cookies = new CookieContainer();
            handler = new HttpClientHandler
            {
                CookieContainer = cookies,
                AllowAutoRedirect = true,
                UseCookies = true,
            };
            client = new HttpClient(handler);
        }

        void LoadSettings()
        {
            try
            {
                if (File.Exists(SonarrConfigFile))
                {
                    var json = JObject.Parse(File.ReadAllText(SonarrConfigFile));
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
            File.WriteAllText(SonarrConfigFile, json.ToString());
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
            await TestConnection(config.Host.Value, int.Parse(config.Port.Value), config.ApiKey.Value);
            Host = "http://" + new Uri(config.Host.Value).Host;
            Port = int.Parse(config.Port.Value);
            ApiKey = config.ApiKey.Value;
            SaveSettings();
        }

        public async Task TestConnection()
        {
            await TestConnection(Host, Port, ApiKey);
        }

        async Task TestConnection(string host, int port, string apiKey)
        {
            Uri hostUri = new Uri(host);
            var queryUrl = string.Format("http://{0}:{1}/api/series?apikey={2}", hostUri.Host, port, apiKey);
            var response = await client.GetStringAsync(queryUrl);
            var json = JArray.Parse(response);
        }
    }
}
