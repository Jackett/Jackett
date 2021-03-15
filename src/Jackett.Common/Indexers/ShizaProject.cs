using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    internal class ShizaProject : BaseWebIndexer
    {
        public ShizaProject(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "ShizaProject",
                   name: "ShizaProject",
                   description: "ShizaProject Tracker is a semi-private russian tracker and release group for anime",
                   link: "http://shiza-project.com/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new  ConfigurationDataBasicLoginWithEmail())
        {
            Encoding = Encoding.UTF8;
            Language = "ru-ru";
            Type = "semi-private";

            AddCategoryMapping(1, TorznabCatType.TVAnime, "Anime");
        }

        private ConfigurationDataBasicLoginWithEmail Configuration => (ConfigurationDataBasicLoginWithEmail)configData;

        /// <summary>
        /// http://shiza-project.com/accounts/login
        /// </summary>
        private string LoginUrl => SiteLink + "accounts/login";

        /// <summary>
        /// http://shiza-project.com/releases/search
        /// </summary>
        private string SearchUrl => SiteLink + "releases/search";

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var data = new Dictionary<string, string>
            {
                { "field-email", Configuration.Email.Value },
                { "field-password", Configuration.Password.Value }
            };

            var result = await RequestLoginAndFollowRedirect(
                LoginUrl,
                data,
                null,
                returnCookiesFromFirstCall: true
            );

            var parser = new HtmlParser();
            var document = await parser.ParseDocumentAsync(result.ContentString);

            await ConfigureIfOK(result.Cookies, IsAuthorized(result), () =>
            {
                var errorMessage = document.QuerySelector("div.alert-error").Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, Configuration);
            });

            return IndexerConfigurationStatus.Completed;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            await EnsureAuthorized();
            return await base.Download(link);
        }

        // If the search string is empty use the latest releases
        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query) {
            await EnsureAuthorized();

            WebResult result;
            if (query.IsTest || string.IsNullOrWhiteSpace(query.SearchTerm)) {
                result = await RequestWithCookiesAndRetryAsync(SiteLink);
            } else {
                // Prepare the search query
                var queryParameters = new NameValueCollection
                {
                    { "q", query.SearchTerm}
                };
                result = await RequestWithCookiesAndRetryAsync(SearchUrl + "?" + queryParameters.GetQueryString());
            }

            const string ReleaseLinksSelector = "article.grid-card > a.card-box";
            var releases = new List<ReleaseInfo>();

            try
            {
                var parser = new HtmlParser();
                var document = await parser.ParseDocumentAsync(result.ContentString);

                foreach (var linkNode in document.QuerySelectorAll(ReleaseLinksSelector))
                {
                    var url = linkNode.GetAttribute("href");
                    releases.AddRange(await FetchShowReleases(url));
                }
            }
            catch (Exception ex)
            {
                OnParseError(result.ContentString, ex);
            }

            return releases;
        }

        private async Task EnsureAuthorized()
        {
            var result = await RequestWithCookiesAndRetryAsync(SiteLink);

            if (!IsAuthorized(result))
            {
                await ApplyConfiguration(null);
            }
        }

        private async Task<List<ReleaseInfo>> FetchShowReleases(string url)
        {
            var releases = new List<ReleaseInfo>();
            var uri = new Uri(url);
            var result = await RequestWithCookiesAndRetryAsync(url);

            try
            {
                var parser = new HtmlParser();
                var document = await parser.ParseDocumentAsync(result.ContentString);
                var r = document.QuerySelector("div.release > div.wrapper-release");

                var baseRelease = new ReleaseInfo
                {
                    Title = composeBaseTitle(r),
                    Poster = new Uri(SiteLink + r.QuerySelector("a[data-fancybox]").Attributes["href"].Value),
                    Details = uri,
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1,
                    Category = new[]{ TorznabCatType.TVAnime.ID }
                };

                foreach (var t in r.QuerySelectorAll("a[data-toggle]"))
                {
                    var release = (ReleaseInfo)baseRelease.Clone();
                    release.Title += " " + t.Text().Trim();
                    var tr_id = t.Attributes["href"].Value;
                    var tr = r.QuerySelector("div" + tr_id);
                    release.Link = new Uri(tr.QuerySelector("a.button--success").Attributes["href"].Value);
                    release.Seeders = long.Parse(tr.QuerySelector("div.torrent-counter > div:nth-of-type(1)").Text().Trim().Split(' ')[0]);
                    release.Peers = release.Seeders + long.Parse(tr.QuerySelector("div.torrent-counter > div:nth-of-type(2)").Text().Trim().Split(' ')[0]);
                    release.Grabs = long.Parse(tr.QuerySelector("div.torrent-counter > div:nth-of-type(3)").Text().Trim().Split(' ')[0]);
                    release.PublishDate = DateTime.Parse(tr.QuerySelector("time.torrent-time").Text());
                    release.Size = getReleaseSize(tr);
                    release.Guid = new Uri(uri.ToString() + tr_id);
                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(result.ContentString, ex);
            }

            return releases;
        }

        private string composeBaseTitle(IElement release) {
            var titleDiv = release.QuerySelector("section:nth-of-type(2) > div.card > article:nth-of-type(1) > div.card-header");
            return titleDiv.QuerySelector("h3").Text() + " " + titleDiv.QuerySelector("p").Text();
        }

        // Appending id to differentiate between different quality versions
        private bool IsAuthorized(WebResult result) {
            return result.ContentString.Contains("/logout");
        }

        private static long getReleaseSize(IElement tr)
        {
            var size = tr.QuerySelector("a.torrent-size").Text().Trim();
            size = size.Replace("КБ", "KB");
            size = size.Replace("МБ", "MB");
            size = size.Replace("ГБ", "GB");
            size = size.Replace("ТБ", "TB");
            return ReleaseInfo.GetBytes(size);
        }
    }
}
