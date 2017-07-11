using AutoMapper;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
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
using System.Xml.Linq;
using Jackett.Indexers;

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

        public HttpResponseMessage GetErrorXML(int code, string description)
        {
            var xdoc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("error",
                    new XAttribute("code", code.ToString()),
                    new XAttribute("description", description)
                )
            );

            var xml = xdoc.Declaration.ToString() + Environment.NewLine + xdoc.ToString();

            return new HttpResponseMessage()
            {
                Content = new StringContent(xml, Encoding.UTF8, "application/xml")
            };
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

            if (torznabQuery.ImdbID != null)
            {
                if (torznabQuery.QueryType != "movie")
                {
                    logger.Warn(string.Format("A non movie request with an imdbid was made from {0}.", Request.GetOwinContext().Request.RemoteIpAddress));
                    return GetErrorXML(201, "Incorrect parameter: only movie-search supports the imdbid parameter");
                }

                if (!string.IsNullOrEmpty(torznabQuery.SearchTerm))
                {
                    logger.Warn(string.Format("A movie-search request from {0} was made contining q and imdbid.", Request.GetOwinContext().Request.RemoteIpAddress));
                    return GetErrorXML(201, "Incorrect parameter: please specify either imdbid or q");
                }

                torznabQuery.ImdbID = ParseUtil.GetFullImdbID(torznabQuery.ImdbID); // normalize ImdbID
                if (torznabQuery.ImdbID == null)
                {
                    logger.Warn(string.Format("A movie-search request from {0} was made with an invalid imdbid.", Request.GetOwinContext().Request.RemoteIpAddress));
                    return GetErrorXML(201, "Incorrect parameter: invalid imdbid format");
                }

                if (!indexer.TorznabCaps.SupportsImdbSearch)
                {
                    logger.Warn(string.Format("A movie-search request with imdbid from {0} was made but the indexer {1} doesn't support it.", Request.GetOwinContext().Request.RemoteIpAddress, indexer.DisplayName));
                    return GetErrorXML(203, "Function Not Available: imdbid is not supported by this indexer");
                }
            }

            var releases = await indexer.ResultsForQuery(torznabQuery);

            // Some trackers do not keep their clocks up to date and can be ~20 minutes out!
            foreach (var release in releases.Where(r => r.PublishDate > DateTime.Now))
            {
                release.PublishDate = DateTime.Now;
            }

            // Some trackers do not support multiple category filtering so filter the releases that match manually.
            int? newItemCount = null;

            // Cache non query results
            if (string.IsNullOrEmpty(torznabQuery.SanitizedSearchTerm))
            {
                newItemCount = cacheService.GetNewItemCount(indexer, releases);
                cacheService.CacheRssResults(indexer, releases);
            }

            // Log info
            var logBuilder = new StringBuilder();
            if (newItemCount != null)
            {
                logBuilder.AppendFormat(string.Format("Found {0} ({1} new) releases from {2}", releases.Count(), newItemCount, indexer.DisplayName));
            }
            else
            {
                logBuilder.AppendFormat(string.Format("Found {0} releases from {1}", releases.Count(), indexer.DisplayName));
            }

            if (!string.IsNullOrWhiteSpace(torznabQuery.SanitizedSearchTerm))
            {
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


            foreach (var result in releases)
            {
                var clone = Mapper.Map<ReleaseInfo>(result);
                clone.Link = serverService.ConvertToProxyLink(clone.Link, serverUrl, result.Origin.ID, "dl", result.Title + ".torrent");
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
