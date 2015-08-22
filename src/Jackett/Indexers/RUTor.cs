using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Models;
using Newtonsoft.Json.Linq;
using NLog;
using Jackett.Utils;
using System.Net;
using System.Net.Http;
using CsQuery;
using System.Web;
using Jackett.Services;
using Jackett.Utils.Clients;
using System.Text.RegularExpressions;
using Jackett.Models.IndexerConfig;
using System.Globalization;
using Newtonsoft.Json;

namespace Jackett.Indexers
{
    public class RuTor : BaseIndexer, IIndexer
    {
        private string SearchUrl { get { return SiteLink + "search/0/{0}/000/0/{1}"; } }
        private string BrowseUrl { get { return SiteLink + "browse/0/{0}/0/0"; } }
        readonly static string defaultSiteLink = "http://rutor.org/";

        new ConfigurationDataRuTor configData
        {
            get { return (ConfigurationDataRuTor)base.configData; }
            set { base.configData = value; }
        }

        public RuTor(IIndexerManagerService i, Logger l, IWebClient wc, IProtectionService ps)
            : base(name: "RUTor",
                description: "Свободный торрент трекер",
                link: "http://rutor.org/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataRuTor(defaultSiteLink))
        {
            TorznabCaps.Categories.Add(TorznabCatType.TVAnime);
            TorznabCaps.Categories.Add(TorznabCatType.Movies);
            TorznabCaps.Categories.Add(TorznabCatType.Audio);
            TorznabCaps.Categories.Add(TorznabCatType.Books);
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var oldConfig = configData;
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Count() > 0, () =>
            {
                configData = oldConfig;
                throw new Exception("Could not find releases from this URL");
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }


        protected override void SaveConfig()
        {
            indexerService.SaveConfig(this as IIndexer, JsonConvert.SerializeObject(configData));
        }

        // Override to load legacy config format
        public override void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            var json = jsonConfig.ToString();
            configData = JsonConvert.DeserializeObject<ConfigurationDataRuTor>(json);
            IsConfigured = true;
        }

        private readonly int CAT_ANY = 0;
        private readonly int CAT_FOREIGN_MOVIE = 1;
        // private readonly int CAT_OUR_MOVIES = 5;
        // private readonly int CAT_POP_SCIFI_MOVIES = 12;
        private readonly int CAT_TV_SERIES = 4;
        // private readonly int CAT_TV = 6;
        // private readonly int CAT_ANIMATION = 7;
        private readonly int CAT_ANIME = 10;
        private readonly int CAT_MUSIC = 2;
        // private readonly int CAT_GAMES = 8;
        // private readonly int CAT_SOFTWARE = 9;
        // private readonly int CAT_SPORTS_HEALTH = 13;
        // private readonly int CAT_HUMOR = 15;
        // private readonly int CAT_ECONOMY_LIFE = 14;
        private readonly int CAT_BOOKS = 11;
        // private readonly int CAT_OTHER = 3;

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var searchCategory = CAT_ANY;

            if (query.Categories.Contains(TorznabCatType.TV.ID) ||
                query.Categories.Contains(TorznabCatType.TVHD.ID) ||
                query.Categories.Contains(TorznabCatType.TVSD.ID))
            {
                searchCategory = CAT_TV_SERIES;
            }

            if ((searchCategory == CAT_ANY) &&
               (query.Categories.Contains(TorznabCatType.Movies.ID) ||
               query.Categories.Contains(TorznabCatType.MoviesForeign.ID) ||
               query.Categories.Contains(TorznabCatType.MoviesHD.ID) ||
               query.Categories.Contains(TorznabCatType.MoviesSD.ID)))
            {
                searchCategory = CAT_FOREIGN_MOVIE;
            }

            if ((searchCategory == CAT_ANY) &&
               (query.Categories.Contains(TorznabCatType.TVAnime.ID)))
            {
                searchCategory = CAT_ANIME;
            }

            if ((searchCategory == CAT_ANY) &&
              (query.Categories.Contains(TorznabCatType.Books.ID)))
            {
                searchCategory = CAT_BOOKS;
            }

            if ((searchCategory == CAT_ANY) &&
              (query.Categories.Contains(TorznabCatType.Audio.ID) ||
              query.Categories.Contains(TorznabCatType.AudioLossless.ID) ||
              query.Categories.Contains(TorznabCatType.AudioMP3.ID)))
            {
                searchCategory = CAT_MUSIC;
            }

            string queryUrl = string.Empty;
            if (string.IsNullOrWhiteSpace(searchString))
            {
                queryUrl = string.Format(BrowseUrl, searchCategory);
            }
            else
            {
                queryUrl = string.Format(SearchUrl, searchCategory, HttpUtility.UrlEncode(searchString.Trim()));
            }

            var results = await RequestStringWithCookiesAndRetry(queryUrl, string.Empty);
            try
            {
                CQ dom = results.Content;
                var rows = dom["#index table tr"];
                foreach (var row in rows.Skip(1))
                {
                    var release = new ReleaseInfo();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    var date = StringUtil.StripNonAlphaNumeric(row.Cq().Find("td:eq(0)").Text().Trim()
                        .Replace("Янв", "01")
                        .Replace("Фев", "02")
                        .Replace("Мар", "03")
                        .Replace("Апр", "04")
                        .Replace("Май", "05")
                        .Replace("Июн", "06")
                        .Replace("Июл", "07")
                        .Replace("Авг", "08")
                        .Replace("Сен", "09")
                        .Replace("Окт", "10")
                        .Replace("Ноя", "11")
                        .Replace("Дек", "12"));

                    release.PublishDate = DateTime.ParseExact(date, "ddMMyy", CultureInfo.InvariantCulture);

                    var hasTorrent = row.Cq().Find("td:eq(1) a").Length == 3;
                    var titleIndex = 1;
                    if (hasTorrent)
                        titleIndex++;

                    release.Title = row.Cq().Find("td:eq(" + titleIndex + ")").Text().Trim();
                    if (configData.StripRussian.Value)
                    {
                        var split = release.Title.IndexOf('/');
                        if (split > -1)
                        {
                            release.Title = release.Title.Substring(split + 1).Trim();
                        }
                    }

                    release.Description = release.Title;

                    var hasComments = row.Cq().Find("td:eq(2) img").Length > 0;
                    var sizeCol = 2;

                    if (hasComments)
                        sizeCol++;

                    var sizeStr = StringUtil.StripRegex(row.Cq().Find("td:eq(" + sizeCol + ")").Text(), "[^a-zA-Z0-9\\. -]", " ").Trim();
                    string[] sizeSplit = sizeStr.Split(' ');
                    release.Size = ReleaseInfo.GetBytes(sizeSplit[1].ToLower(), ParseUtil.CoerceFloat(sizeSplit[0]));

                    release.Seeders = ParseUtil.CoerceInt(row.Cq().Find(".green").Text().Trim());
                    release.Peers = ParseUtil.CoerceInt(row.Cq().Find(".red").Text().Trim()) + release.Seeders;

                    release.Guid = new Uri(configData.Url.Value + row.Cq().Find("td:eq(1) a:eq(" + titleIndex + ")").Attr("href").Substring(1));
                    release.Comments = release.Guid;

                    if (hasTorrent)
                    {
                        release.Link = new Uri(row.Cq().Find("td:eq(1) a:eq(0)").Attr("href"));
                        release.MagnetUri = new Uri(row.Cq().Find("td:eq(1) a:eq(1)").Attr("href"));
                    }
                    else
                    {
                        release.MagnetUri = new Uri(row.Cq().Find("td:eq(1) a:eq(0)").Attr("href"));
                    }

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }

            return releases;
        }
    }
}
