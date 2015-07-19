using Jackett.Models;
using Jackett.Services;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace Jackett.Controllers
{
    public class APIController : ApiController
    {
        private IIndexerManagerService indexerService;
        private Logger logger;

        public APIController(IIndexerManagerService i,  Logger l)
        {
            indexerService = i;
            logger = l;
        }

        [HttpGet]
        public async Task<HttpResponseMessage> Call(string indexerName)
        {
            var indexer = indexerService.GetIndexer(indexerName);
            var torznabQuery = TorznabQuery.FromHttpQuery(HttpUtility.ParseQueryString(Request.RequestUri.Query));

            if (torznabQuery.RageIDLookupEnabled && indexer.RequiresRageIDLookupDisabled)
            {
                throw new ArgumentException("This indexer requires RageID lookup disabled");
            }

            var releases = await indexer.PerformQuery(torznabQuery);

            logger.Debug(string.Format("Found {0} releases from {1}", releases.Length, indexer.DisplayName));
            var severUrl = string.Format("{0}://{1}:{2}/", Request.RequestUri.Scheme, Request.RequestUri.Host, Request.RequestUri.Port);

            var resultPage = new ResultPage(new ChannelInfo
            {
                Title = indexer.DisplayName,
                Description = indexer.DisplayDescription,
                Link = indexer.SiteLink,
                ImageUrl = new Uri(severUrl + "logos/" + indexer.DisplayName + ".png"),
                ImageTitle = indexer.DisplayName,
                ImageLink = indexer.SiteLink,
                ImageDescription = indexer.DisplayName
            });

            // add Jackett proxy to download links...
            foreach (var release in releases)
            {
                if (release.Link == null || release.Link.Scheme == "magnet")
                    continue;
                var originalLink = release.Link;
                var encodedLink = HttpServerUtility.UrlTokenEncode(Encoding.UTF8.GetBytes(originalLink.ToString())) + "/download.torrent";
                var proxyLink = string.Format("{0}api/{1}/download/{2}", severUrl, indexer.DisplayName, encodedLink);
                release.Link = new Uri(proxyLink);
            }

            resultPage.Releases.AddRange(releases);
            var xml = resultPage.ToXml(new Uri(severUrl));
            // Force the return as XML
            return new HttpResponseMessage()
            {
                Content = new StringContent(xml, Encoding.UTF8, "application/rss+xml")
            };
        }
    }
}
