using AutoMapper;
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
    [JackettAPINoCache]
    public class TorznabController : ApiController
    {
        private IIndexerManagerService indexerService;
        private Logger logger;
        private IServerService serverService;
        private ICacheService cacheService;

        public TorznabController(IIndexerManagerService i, Logger l, IServerService s, ICacheService c)
        {
            indexerService = i;
            logger = l;
            serverService = s;
            cacheService = c;
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
                    Content = new StringContent(indexer.TorznabCaps.ToXml(), Encoding.UTF8, "application/xml")
                };
            }

            torznabQuery.ExpandCatsToSubCats();
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
            releases = indexer.CleanLinks(releases);

            // Some trackers do not keep their clocks up to date and can be ~20 minutes out!
            foreach (var release in releases)
            {
                if (release.PublishDate > DateTime.Now)
                    release.PublishDate = DateTime.Now;
            }

            // Some trackers do not support multiple category filtering so filter the releases that match manually.
            var filteredReleases = releases = indexer.FilterResults(torznabQuery, releases); 
            int? newItemCount = null;

            // Cache non query results
            if (string.IsNullOrEmpty(torznabQuery.SanitizedSearchTerm))
            {
                newItemCount = cacheService.GetNewItemCount(indexer, filteredReleases);
                cacheService.CacheRssResults(indexer, releases);
            }

            // Log info
            var logBuilder = new StringBuilder();
            if (newItemCount != null)  {
                logBuilder.AppendFormat(string.Format("Found {0} ({1} new) releases from {2}", releases.Count(), newItemCount, indexer.DisplayName));
            }
            else {
                logBuilder.AppendFormat(string.Format("Found {0} releases from {1}", releases.Count(), indexer.DisplayName));
            }

            if (!string.IsNullOrWhiteSpace(torznabQuery.SanitizedSearchTerm)) { 
                logBuilder.AppendFormat(" for: {0}", torznabQuery.GetQueryString());
            }

            logger.Info(logBuilder.ToString());

            var serverUrl = string.Format("{0}://{1}:{2}{3}", Request.RequestUri.Scheme, Request.RequestUri.Host, Request.RequestUri.Port, serverService.BasePath());
            var resultPage = new ResultPage(new ChannelInfo
            {
                Title = indexer.DisplayName,
                Description = indexer.DisplayDescription,
                Link = new Uri(indexer.SiteLink),
                ImageUrl = new Uri(serverUrl + "logos/" + indexer.ID + ".png"),
                ImageTitle = indexer.DisplayName,
                ImageLink = new Uri(indexer.SiteLink),
                ImageDescription = indexer.DisplayName
            });


            foreach(var result in releases)
            {
                var clone = Mapper.Map<ReleaseInfo>(result);
                clone.Link = serverService.ConvertToProxyLink(clone.Link, serverUrl, indexerID, "dl", result.Title + ".torrent");
                resultPage.Releases.Add(clone);
            }

            var xml = resultPage.ToXml(new Uri(serverUrl));
            // Force the return as XML
            return new HttpResponseMessage()
            {
                Content = new StringContent(xml, Encoding.UTF8, "application/rss+xml")
            };
        }


    }
}
