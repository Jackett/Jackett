using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Jackett.Common.Services.Interfaces
{
    public interface IServerService
    {
        void Initalize();
        void Start();
        void Stop();
        void ReserveUrls(bool doInstall = true);
        Uri ConvertToProxyLink(Uri link, string serverUrl, string indexerId, string action = "dl", string file = "t");
        string BasePath();
        string GetServerUrl(object Request); //TODO: Once Mono is removed, change type to HttpRequest
        List<string> notices { get; }
        string GetBlackholeDirectory();
        string GetApiKey();
    }
}
