using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Parser.Html;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class Newpct : BaseWebIndexer
    {
        private string _mostRecentUrl;
        Regex _searchStringRegex = new Regex(@"(.+) S0?(\d+)E0?(\d+)");
        Regex _titleListRegex = new Regex(@"Serie(.+?)(Temporada(.+?)(\d+)(.+?))?Capitulos?(.+?)(\d+)((.+?)(\d+))?(.+?)-(.+?)Calidad(.*)");

        private int _maxDailyPages = 7;

        private string _dailyUrl = "/ultimas-descargas/pg/{0}";
        private string[] _seriesLetterUrl = new string[] { "/series/letter/{0}", "/series-hd/letter/{0}" };
        private string[] _seriesUrl = new string[] { "/series", "/series-hd" };

        private new ConfigurationData configData
        {
            get { return (ConfigurationData)base.configData; }
            set { base.configData = value; }
        }

        public Newpct(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "Newpct",
                description: "Newpct - descargar torrent peliculas, series",
                link: "http://www.tvsinpagar.com/",
                caps: new TorznabCapabilities(TorznabCatType.TV,
                                              TorznabCatType.TVSD,
                                              TorznabCatType.TVHD,
                                              TorznabCatType.Movies),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationData())
        {
            Encoding = Encoding.GetEncoding("windows-1252");
            Language = "es-es";
            Type = "public";

        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Count() > 0, () =>
            {
                throw new Exception("Could not find releases from this URL");
            });

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            return await PerformQuery(query, 0);
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var results = await RequestStringWithCookies(link.AbsoluteUri);
            var content = results.Content;

            Regex regex = new Regex("[^\"]*/descargar-torrent/\\d+_[^\"]*");
            Match match = regex.Match(content);
            if (match.Success)
                link = new Uri(match.Groups[0].Value);
            else
                this.logger.Warn("Newpct - download link not found in " + link);

            return await base.Download(link);
        }

        private async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query, int attempts)
        {
            var releases = new List<ReleaseInfo>();

            bool rssMode = string.IsNullOrEmpty(query.SanitizedSearchTerm);
            Uri siteLinkUri = new Uri(SiteLink);

            if (rssMode)
            {
                int pg = 1;
                while (pg <= _maxDailyPages)
                {
                    Uri url = new Uri(siteLinkUri, string.Format(_dailyUrl, pg));
                    var results = await RequestStringWithCookies(url.AbsoluteUri);

                    var items = ParseDailyContent(query, results.Content);
                    if (items == null || !items.Any())
                        break;

                    releases.AddRange(items);

                    //Check if we need to go to next page
                    if (items.Any(r => r.Link.AbsoluteUri == _mostRecentUrl))
                        break;
                    if (pg == 1)
                        _mostRecentUrl = items.First().Link.AbsoluteUri;

                    pg++;
                }
            }
            else
            {
                Match match = _searchStringRegex.Match(query.SanitizedSearchTerm);
                if (match.Success)
                {
                    var seriesName = match.Groups[1].Value;
                    var season = int.Parse(match.Groups[2].Value);
                    var episode = int.Parse(match.Groups[3].Value);



                }



            }

            return releases;
        }

        private IEnumerable<ReleaseInfo> ParseDailyContent(TorznabQuery query, string content)
        {
            var SearchResultParser = new HtmlParser();
            var doc = SearchResultParser.Parse(content);

            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            try
            {
                var rows = doc.QuerySelectorAll(".content .info");
                foreach (var row in rows)
                {
                    var anchor = row.QuerySelector("a");
                    var title = anchor.TextContent.Replace("\t", "").Trim();
                    var detailsUrl = anchor.GetAttribute("href");

                    var span = row.QuerySelector("span");
                    var qualityText = span.ChildNodes[0].TextContent.Trim();
                    var sizeText = span.ChildNodes[1].TextContent.Replace("Tamaño", "").Trim();

                    var div = row.QuerySelector("div");
                    var languageText = div.ChildNodes[1].TextContent.Trim();

                    ReleaseInfo release = new ReleaseInfo()
                    {
                        Title = ParseTitle(title, qualityText, languageText, out ICollection<int> category),
                        Category = category,
                        Link = new Uri(detailsUrl),
                        Size = ReleaseInfo.GetBytes(sizeText),
                        Seeders = 1,
                        Peers = 1,
                        PublishDate = DateTime.Now,
                    };

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(content, ex);
            }

            return releases;
        }

        private void ParseSeriesList()
        {

        }

        private IEnumerable<ReleaseInfo> ParseEpisodesListContent(TorznabQuery query, string content)
        {

            return null;
        }

        private string ParseTitle(string title, string quality, string language, out ICollection<int> categories)
        {
            if (quality.ToLower().StartsWith("hdtv"))
            {
                if (quality.Contains("720") || quality.Contains("1080"))
                    categories = new List<int> { TorznabCatType.TVHD.ID };
                else
                    categories = new List<int> { TorznabCatType.TV.ID };

                return SeriesTitleToNewpctFormat(string.Format("Serie {0} - {1} Calidad [{2}]", title, language, quality));
            }
            else
            {
                categories = new List<int> { TorznabCatType.Movies.ID };
                return title;
            }
        }

        private string SeriesTitleToNewpctFormat(string title)
        {
            Match match = _titleListRegex.Match(title);
            if (match.Success)
            {
                string name = match.Groups[1].Value.Trim(' ', '-');
                string seasonText = match.Groups[4].Success ? match.Groups[4].Value.Trim() : "1";
                string episodeText = match.Groups[7].Value.Trim().PadLeft(2, '0');
                string episode_toText = match.Groups[10].Success ? match.Groups[10].Value.Trim().PadLeft(2, '0') : null;
                string audio_quality = match.Groups[12].Value.Trim(' ', '[', ']');
                string video_quality = match.Groups[13].Value.Trim(' ', '[', ']');

                if (!string.IsNullOrEmpty(episode_toText))
                    title = string.Format("{0} - Temporada {1} [{2}][Cap.{3}{4}_{5}{6}][{7}]", name, seasonText, video_quality,
                        seasonText, episodeText, seasonText, episode_toText, audio_quality);
                else
                    title = string.Format("{0} - Temporada {1} [{2}][Cap.{3}{4}][{5}]", name, seasonText, video_quality,
                        seasonText, episodeText, audio_quality);

                return title;
            }
            else
            {
                return title;
            }
        }

    }
}
