using Jackett.Common.Utils.Clients;

namespace Jackett.Common.Indexers
{
    public class IndexerResponse
    {
        private readonly IndexerRequest _indexerRequest;
        private readonly WebResult _webResponse;

        public IndexerResponse(IndexerRequest indexerRequest, WebResult webResponse)
        {
            _indexerRequest = indexerRequest;
            _webResponse = webResponse;
        }

        public IndexerRequest Request => _indexerRequest;

        public WebRequest HttpRequest => _webResponse.Request;

        public WebResult WebResponse => _webResponse;

        public string Content => _webResponse.ContentString;
    }
}
