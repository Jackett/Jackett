using System;
using System.Collections.Generic;
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
            Port = 9117;
            AllowExternal = System.Environment.OSVersion.Platform == PlatformID.Unix;
            RuntimeSettings = runtimeSettings;
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
        public string OmdbApiKey { get; set; }

        /// <summary>
        /// Ignore as we don't really want to be saving settings specified in the command line. 
        /// This is a bit of a hack, but in future it might not be all that bad to be able to override config values using settings that were provided at runtime. (and save them if required)
        /// </summary>
        [JsonIgnore]
        public RuntimeSettings RuntimeSettings { get; set; }

        public string ProxyUrl { get; set; }
        public ProxyType ProxyType { get; set; }
        public int? ProxyPort { get; set; }
        public string ProxyUsername { get; set; }
        public string ProxyPassword { get; set; }

        public bool ProxyIsAnonymous
        {
            get
            {
                return string.IsNullOrWhiteSpace(ProxyUsername) || string.IsNullOrWhiteSpace(ProxyPassword);
            }
        }

        public string GetProxyAuthString()
        {
            if (!ProxyIsAnonymous)
            {
                return $"{ProxyUsername}:{ProxyPassword}";
            }
            return null;
        }

        public string GetProxyUrl(bool withCreds = false)
        {
            var url = ProxyUrl;
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }
            //remove protocol from url
            var index = url.IndexOf("://");
            if (index > -1)
            {
                url = url.Substring(index + 3);
            }
            url = ProxyPort.HasValue ? $"{url}:{ProxyPort}" : url;

            var authString = GetProxyAuthString();
            if (withCreds && authString != null)
            {
                url = $"{authString}@{url}";
            }

            if (ProxyType != ProxyType.Http)
            {
                var protocol = (Enum.GetName(typeof(ProxyType), ProxyType) ?? "").ToLower();
                if (!string.IsNullOrEmpty(protocol))
                {
                    url = $"{protocol}://{url}";
                }
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
                    "http://127.0.0.1:" + Port + "/",
                    "http://localhost:" + Port + "/",
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
            private List<IObserver<ServerConfig>> lstObservers;
            private IObserver<ServerConfig> observer;

            public UnSubscriber(List<IObserver<ServerConfig>> ObserversCollection, IObserver<ServerConfig> observer)
            {
                this.lstObservers = ObserversCollection;
                this.observer = observer;
            }

            public void Dispose()
            {
                if (this.observer != null)
                {
                    lstObservers.Remove(this.observer);
                }
            }
        }

        public void ConfigChanged()
        {
            foreach (var obs in observers)
            {
                obs.OnNext(this);
            }
        }
    }
}
