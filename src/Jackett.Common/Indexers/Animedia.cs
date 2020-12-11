using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System.Linq;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    internal class Animedia : BaseWebIndexer
    {
        private static readonly Regex EpisodesInfoQueryRegex = new Regex(@"серии (\d+)-(\d+) из.*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ResolutionInfoQueryRegex = new Regex(@"Качество (\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SizeInfoQueryRegex = new Regex(@"Размер:(.*)\n", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ReleaseDateInfoQueryRegex = new Regex(@"Добавлен:(.*)\n", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public Animedia(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "Animedia",
                   name: "Animedia",
                   description: "Animedia is a public russian tracker and release group for anime.",
                   link: "https://tt.animedia.tv/",
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
                   configData: new ConfigurationData())
        {
            Encoding = Encoding.UTF8;
            Language = "ru-ru";
            Type = "public";

            // Configure the category mappings
            AddCategoryMapping(1, TorznabCatType.TVAnime, "Anime");
        }

        /// <summary>
        /// https://tt.animedia.tv/ajax/search_result/P0
        /// </summary>
        private string SearchUrl => SiteLink + "ajax/search_result/P0";

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(), () =>
                throw new Exception("Could not find releases from this URL"));

            return IndexerConfigurationStatus.Completed;
        }

        // If the search string is empty use the latest releases
        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query) {
            WebResult result;
            if (query.IsTest || string.IsNullOrWhiteSpace(query.SearchTerm)) {
                result = await RequestWithCookiesAndRetryAsync(SiteLink);
            } else {
                // Prepare the search query
                var queryParameters = new NameValueCollection
                {
                    { "keywords", query.SearchTerm },
                    { "limit", "20"},
                    { "orderby_sort", "entry_date|desc"}
                };
                result = await RequestWithCookiesAndRetryAsync(SearchUrl + "?" + queryParameters.GetQueryString());
            }

            const string ReleaseLinksSelector = "a.ads-list__item__title";

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

        private async Task<List<ReleaseInfo>> FetchShowReleases(string url)
        {
            var releases = new List<ReleaseInfo>();
            var uri = new Uri(url);
            //Some URLs in search are broken
            if (url.StartsWith("//"))
            {
                url = "https:" + url;
            }

            var result = await RequestWithCookiesAndRetryAsync(url);

            try
            {
                var parser = new HtmlParser();
                var document = await parser.ParseDocumentAsync(result.ContentString);

                var baseRelease = new ReleaseInfo
                {
                    Title = composeBaseTitle(document),
                    Poster = new Uri(document.QuerySelector("div.widget__post-info__poster > a").Attributes["href"].Value),
                    Details = uri,
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1,
                    Category = new[]{ TorznabCatType.TVAnime.ID }
                };
                foreach (var t in document.QuerySelectorAll("ul.media__tabs__nav > li > a"))
                {
                    var release = (ReleaseInfo)baseRelease.Clone();
                    var tr_id = t.Attributes["href"].Value;
                    var tr = document.QuerySelector("div" + tr_id);
                    release.Title += " - " + composeTitleAdditionalInfo(t, tr);
                    release.Link = new Uri(document.QuerySelector("div.download_tracker > a.btn__green").Attributes["href"].Value);
                    release.MagnetUri = new Uri(document.QuerySelector("div.download_tracker > a.btn__d-gray").Attributes["href"].Value);
                    release.Seeders = long.Parse(document.QuerySelector("div.circle_green_text_top").Text());
                    release.Peers = release.Seeders + long.Parse(document.QuerySelector("div.circle_red_text_top").Text());
                    release.Grabs = long.Parse(document.QuerySelector("div.circle_grey_text_top").Text());
                    release.PublishDate = getReleaseDate(tr);
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

        private string composeBaseTitle(IHtmlDocument r) {
            var name_ru = r.QuerySelector("div.media__post__header > h1").Text().Trim();
            var name_en = r.QuerySelector("div.media__panel > div:nth-of-type(1) > div.col-l:nth-of-type(1) > div > span").Text().Trim();
            var name_orig = r.QuerySelector("div.media__panel > div:nth-of-type(1) > div.col-l:nth-of-type(2) > div > span").Text().Trim();

            var title = name_ru + " / " + name_en;
            if (name_en != name_orig) {
                title += " / " + name_orig;
            }
            return title;
        }

        private string composeTitleAdditionalInfo(IElement t, IElement tr) {
            var tabName = t.Text();
            tabName = tabName.Replace("Сезон", "Season");
            if (tabName.Contains("Серии")) {
                tabName = "";
            }

            var heading = tr.QuerySelector("h3.tracker_info_bold").Text();
            // Transform episodes info if header contains that
            heading = EpisodesInfoQueryRegex.Replace(
                heading,
                match => match.Success ? $"E{int.Parse(match.Groups[1].Value)}-{int.Parse(match.Groups[2].Value)}" : heading
            );

            var resolution = tr.QuerySelector("div.tracker_info_left").Text();
            resolution = ResolutionInfoQueryRegex.Match(resolution).Groups[1].Value;

            return tabName + " " + heading + " [" + resolution + "p]";
        }

        private static long getReleaseSize(IElement tr)
        {
            var sizeStr = tr.QuerySelector("div.tracker_info_left").Text();
            return ReleaseInfo.GetBytes(SizeInfoQueryRegex.Match(sizeStr).Groups[1].Value.Trim());
        }

        private static DateTime getReleaseDate(IElement tr)
        {
            var sizeStr = tr.QuerySelector("div.tracker_info_left").Text();
            return DateTime.Parse(ReleaseDateInfoQueryRegex.Match(sizeStr).Groups[1].Value.Trim());
        }
    }
}
