using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Jackett.Common.Models.Config
{
    public class ServerConfig : IObservable<ServerConfig>
    {
        [JsonIgnore]
        protected List<IObserver<ServerConfig>> observers;

        public ServerConfig(RuntimeSettings runtimeSettings)
        {
            observers = new List<IObserver<ServerConfig>>();
            // Default values
            Port = 9117;
            AllowExternal = Environment.OSVersion.Platform == PlatformID.Unix;
            CacheEnabled = true;
            // Sonarr 15min, Radarr 60min, LazyLibrarian 20min, Readarr 15min, Lidarr = 15min
            CacheTtl = 2100; // 35 minutes is a reasonable value for all of them and to avoid race conditions
            CacheMaxResultsPerIndexer = 1000;
            RuntimeSettings = runtimeSettings;
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (string.IsNullOrWhiteSpace(ProxyUrl))
                ProxyType = ProxyType.Disabled;
        }

        public int Port { get; set; }
        public bool AllowExternal { get; set; }
        public string APIKey { get; set; }
        public string AdminPassword { get; set; }
        public string InstanceId { get; set; }
        public string BlackholeDir { get; set; }
        public bool UpdateDisabled { get; set; }
        public bool UpdatePrerelease { get; set; }
        public string BasePathOverride { get; set; }
        public bool CacheEnabled { get; set; }
        public long CacheTtl { get; set; }
        public long CacheMaxResultsPerIndexer { get; set; }
        public string FlareSolverrUrl { get; set; }
        public string OmdbApiKey { get; set; }
        public string OmdbApiUrl { get; set; }

        /// <summary>
        /// Ignore as we don't really want to be saving settings specified in the command line.
        /// This is a bit of a hack, but in future it might not be all that bad to be able to override config values using settings that were provided at runtime. (and save them if required)
        /// </summary>
        [JsonIgnore]
        public RuntimeSettings RuntimeSettings { get; set; }

        public ProxyType ProxyType { get; set; }
        public string ProxyUrl { get; set; }
        public int? ProxyPort { get; set; }
        public string ProxyUsername { get; set; }
        public string ProxyPassword { get; set; }

        public bool ProxyIsAnonymous => string.IsNullOrWhiteSpace(ProxyUsername) || string.IsNullOrWhiteSpace(ProxyPassword);

        public string GetProxyAuthString() =>
            !ProxyIsAnonymous
                ? $"{ProxyUsername}:{ProxyPassword}"
                : null;

        public string GetProxyUrl(bool withCreds = true)
        {
            var url = ProxyUrl;

            // if disabled
            if (ProxyType == ProxyType.Disabled || string.IsNullOrWhiteSpace(url))
                return null;

            // remove protocol from url
            var index = url.IndexOf("://", StringComparison.Ordinal);
            if (index > -1)
                url = url.Substring(index + 3);

            // add port
            url = ProxyPort.HasValue ? $"{url}:{ProxyPort}" : url;

            // add credentials
            var authString = GetProxyAuthString();
            if (withCreds && authString != null)
                url = $"{authString}@{url}";

            // add protocol
            if (ProxyType == ProxyType.Socks4 || ProxyType == ProxyType.Socks5)
            {
                var protocol = (Enum.GetName(typeof(ProxyType), ProxyType) ?? "").ToLower();
                if (!string.IsNullOrEmpty(protocol))
                    url = $"{protocol}://{url}";
            }
            return url;
        }

        public string[] GetListenAddresses(bool? external = null)
        {
            if (external == null)
            {
                external = AllowExternal;
            }
            if (external.Value)
            {
                return new string[] { "http://*:" + Port + "/" };
            }
            else
            {
                return new string[] {
                    "http://127.0.0.1:" + Port + "/"
                };
            }
        }

        public IDisposable Subscribe(IObserver<ServerConfig> observer)
        {
            if (!observers.Contains(observer))
            {
                observers.Add(observer);
            }
            return new UnSubscriber(observers, observer);
        }

        private class UnSubscriber : IDisposable
        {
            private readonly List<IObserver<ServerConfig>> lstObservers;
            private readonly IObserver<ServerConfig> observer;

            public UnSubscriber(List<IObserver<ServerConfig>> ObserversCollection, IObserver<ServerConfig> observer)
            {
                lstObservers = ObserversCollection;
                this.observer = observer;
            }

            public void Dispose()
            {
                if (observer != null)
                {
                    lstObservers.Remove(observer);
                }
            }
        }

        public void ConfigChanged() =>
            observers.ForEach(obs => obs.OnNext(this));
    }
}
