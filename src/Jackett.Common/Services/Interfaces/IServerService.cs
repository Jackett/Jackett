using Jackett.Models.Config;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Jackett.Services.Interfaces
{
    public interface IServerService
    {
        void Initalize();
        void Start();
        void Stop();
        void ReserveUrls(bool doInstall = true);
        ServerConfig Config { get; }
        void SaveConfig();
        Uri ConvertToProxyLink(Uri link, string serverUrl, string indexerId, string action = "dl", string file = "t.torrent");
        string BasePath();
        string GetServerUrl(HttpRequestMessage Request);
        List<string> notices { get; } 
    }
}
