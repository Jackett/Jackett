﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using CsQuery;
using Jackett.Models;
using Jackett.Models.IndexerConfig;
using Jackett.Services;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Indexers.Meta
{
    public class ImdbResolver {
        public ImdbResolver(IWebClient webClient) {
            WebClient = webClient;
        }

        public async Task<IEnumerable<string>> GetAllTitles(string imdbId) {
            if (!imdbId.StartsWith("tt"))
                imdbId = "tt" + imdbId;
            var request = new WebRequest("http://www.imdb.com/title/" + imdbId + "/releaseinfo");
            var result = await WebClient.GetString(request);

            CQ dom = result.Content;

            var mainTitle = dom["h3[itemprop=name]"].Find("a")[0].InnerHTML.Replace("\"", "");

            var akas = dom["table#akas"].Find("tbody").Find("tr");
            var titleList = new List<string>();
            titleList.Add(mainTitle);
            foreach (var row in akas) {
                string title = row.FirstElementChild.InnerHTML;
                if (title == "(original title)" || title == "")
                    titleList.Add(HttpUtility.HtmlDecode(row.FirstElementChild.NextElementSibling.InnerHTML));
            }

            return titleList;
        }

        private IWebClient WebClient;
    }

    public abstract class BaseMetaIndexer : BaseIndexer, IIndexer
    {
        protected BaseMetaIndexer(string name, string description, IIndexerManagerService manager, IWebClient webClient, Logger logger, ConfigurationData configData, IProtectionService p, Func<IIndexer, bool> filter)
            : base(name, "http://127.0.0.1/", description, manager, webClient, logger, configData, p, null, null)
        {
            filterFunc = filter;
        }

        public Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            return Task.FromResult(IndexerConfigurationStatus.Completed);
        }

        public virtual async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            IEnumerable<Task<IEnumerable<ReleaseInfo>>> tasks = Indexers.Where(i => i.CanHandleQuery(query)).Select(i => i.PerformQuery(query)).ToList(); // explicit conversion to List to execute LINQ query

            bool needFallback = query.IsImdbQuery;
            IEnumerable<string> fallbackTitles = null;
            if (needFallback) {
                var imdb = new ImdbResolver(webclient);
                fallbackTitles = await imdb.GetAllTitles(query.ImdbID);
                var fallbackQueries = fallbackTitles.Select(t => query.CreateFallback(t));
                var backupTasks = fallbackQueries.SelectMany(q => Indexers.Where(i => !i.CanHandleQuery(query) && i.CanHandleQuery(q)).Select(i => i.PerformQuery(q.Clone())));
                tasks = tasks.Concat(backupTasks.ToList()); // explicit conversion to List to execute LINQ query
            }

            var aggregateTask = Task.WhenAll<IEnumerable<ReleaseInfo>>(tasks);
            try {
                await aggregateTask;
            } catch {
                logger.Error(aggregateTask.Exception, "Error during request in metaindexer " + ID);
            }

            var unorderedResult = tasks.Where(x => x.Status == TaskStatus.RanToCompletion).SelectMany(x => x.Result);;
            if (needFallback) {
                unorderedResult = unorderedResult.Where (r => {
                    var normalizedTitles = fallbackTitles.Concat (fallbackTitles.Select (t => t.Replace (' ', '.').Replace (":", ""))).Select (t => t.ToLowerInvariant ());
                    foreach (var title in normalizedTitles) {
                        if (r.Title.ToLowerInvariant ().Contains (title))
                            return true;
                    }
                    return false;
                });
            }
            var orderedResult = unorderedResult.OrderByDescending(r => r.Gain);

            var filteredResult = orderedResult.Where(r => {
                if (r.Imdb != null) {
                    try {
                        return Int64.Parse(query.ImdbID.Select(c => char.IsDigit(c)).ToString()) == r.Imdb;
                    } catch {
                        // Cannot safely determine whether result is what we
                        // wanted, so let's just leave it alone...
                    }
                }
                return true;
            });
            // Limiting the response size might be interesting for use-cases where there are
            // tons of trackers configured in Jackett. For now just use the limit param if
            // someone wants to do that.
            IEnumerable<ReleaseInfo> result = filteredResult;
            if (query.Limit > 0)
                result = result.Take(query.Limit);
            return result;
        }

        public override Uri UncleanLink(Uri link)
        {
            var indexer = GetOriginalIndexerForLink(link);
            if (indexer != null)
                return indexer.UncleanLink(link);

            return base.UncleanLink(link);
        }

        public override Task<byte[]> Download(Uri link)
        {
            var indexer = GetOriginalIndexerForLink(link);
            if (indexer != null)
                return indexer.Download(link);

            return base.Download(link);
        }

        private IIndexer GetOriginalIndexerForLink(Uri link)
        {
            var prefix = string.Format("{0}://{1}", link.Scheme, link.Host);
            var validIndexers = Indexers.Where(i => i.SiteLink.StartsWith(prefix, StringComparison.CurrentCulture));
            if (validIndexers.Count() > 0)
                return validIndexers.First();

            return null;
        }

        private Func<IIndexer, bool> filterFunc;
        private IEnumerable<IIndexer> indexers;
        public IEnumerable<IIndexer> Indexers {
            get {
                return indexers;
            }
            set {
                indexers = value.Where(i => i.IsConfigured && filterFunc(i));
                TorznabCaps = value.Select(i => i.TorznabCaps).Aggregate(new TorznabCapabilities(), TorznabCapabilities.Concat); ;
                IsConfigured = true;
            }
        }
    }
}
