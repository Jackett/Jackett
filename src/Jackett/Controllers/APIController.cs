using Jackett.Models;
using Jackett.Services;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace Jackett.Controllers
{
    [AllowAnonymous]
    public class APIController : ApiController
    {
        private IIndexerManagerService indexerService;
        private Logger logger;
        private IServerService serverService;


        public APIController(IIndexerManagerService i, Logger l, IServerService s)
        {
            indexerService = i;
            logger = l;
            serverService = s;
        }

        [HttpGet]
        public async Task<HttpResponseMessage> Call(string indexerID)
        {
            var indexer = indexerService.GetIndexer(indexerID);
            var torznabQuery = TorznabQuery.FromHttpQuery(HttpUtility.ParseQueryString(Request.RequestUri.Query));

            if (string.Equals(torznabQuery.QueryType, "caps", StringComparison.InvariantCultureIgnoreCase))
            {
                return new HttpResponseMessage()
                {
                    Content = new StringContent(indexer.TorznabCaps.ToXml(), Encoding.UTF8, "application/rss+xml")
                };
            }

            var allowBadApiDueToDebug = false;
#if DEBUG
            allowBadApiDueToDebug = Debugger.IsAttached;
#endif

            if (!allowBadApiDueToDebug && !string.Equals(torznabQuery.ApiKey, serverService.Config.APIKey, StringComparison.InvariantCultureIgnoreCase))
            {
                logger.Warn(string.Format("A request from {0} was made with an incorrect API key.", Request.GetOwinContext().Request.RemoteIpAddress));
                return Request.CreateResponse(HttpStatusCode.Forbidden, "Incorrect API key");
            }

            if (!indexer.IsConfigured)
            {
                logger.Warn(string.Format("Rejected a request to {0} which is unconfigured.", indexer.DisplayName));
                return Request.CreateResponse(HttpStatusCode.Forbidden, "This indexer is not configured.");
            }

            var releases = await indexer.PerformQuery(torznabQuery);

            if (string.IsNullOrWhiteSpace(torznabQuery.SanitizedSearchTerm))
            {
                logger.Info(string.Format("Found {0} releases from {1}", releases.Length, indexer.DisplayName));
            }
            else
            {
                logger.Info(string.Format("Found {0} releases from {1} for: {2}", releases.Length, indexer.DisplayName, torznabQuery.SanitizedSearchTerm));
            }

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
                var proxyLink = string.Format("{0}api/{1}/download/{2}", severUrl, indexer.ID, encodedLink);
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
