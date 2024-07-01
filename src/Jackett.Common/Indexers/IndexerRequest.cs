using Jackett.Common.Utils.Clients;

namespace Jackett.Common.Indexers
{
    public class IndexerRequest
    {
        public WebRequest WebRequest { get; private set; }

        public IndexerRequest(string url)
        {
            WebRequest = new WebRequest(url);
        }

        public IndexerRequest(WebRequest webRequest)
        {
            WebRequest = webRequest;
        }

        public string Url => WebRequest.Url;
    }
}
