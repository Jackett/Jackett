using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
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
    public class BJShare : BaseWebIndexer
    {
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private string BrowseUrl { get { return SiteLink + "torrents.php"; } }
        private string TodayUrl { get { return SiteLink + "torrents.php?action=today"; } }
        private char[] digits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get { return (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData; }
            set { base.configData = value; }
        }

        public BJShare(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "BJ-Share",
                   description: "A brazilian tracker.",
                   link: "https://bj-share.me/",
                   caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
Encoding = Encoding.UTF8;
            Language = "pt-br";
            Type = "private";

            AddCategoryMapping(14, TorznabCatType.TVAnime); // Anime
            AddCategoryMapping(3, TorznabCatType.PC0day); // Aplicativos
            AddCategoryMapping(8, TorznabCatType.Other); // Apostilas/Tutoriais
            AddCategoryMapping(19, TorznabCatType.AudioAudiobook); // Audiobook
            AddCategoryMapping(16, TorznabCatType.TVOTHER); // Desenho Animado
            AddCategoryMapping(18, TorznabCatType.TVDocumentary); // Documentários
            AddCategoryMapping(10, TorznabCatType.Books); // E-Books
            AddCategoryMapping(20, TorznabCatType.TVSport); // Esportes
            AddCategoryMapping(1, TorznabCatType.Movies); // Filmes
            AddCategoryMapping(12, TorznabCatType.MoviesOther); // Histórias em Quadrinhos
            AddCategoryMapping(5, TorznabCatType.Audio); // Músicas
            AddCategoryMapping(7, TorznabCatType.Other); // Outros
            AddCategoryMapping(9, TorznabCatType.BooksMagazines); // Revistas
            AddCategoryMapping(2, TorznabCatType.TV); // Seriados
            AddCategoryMapping(17, TorznabCatType.TV); // Shows
            AddCategoryMapping(13, TorznabCatType.TV); // Stand Up Comedy
            AddCategoryMapping(11, TorznabCatType.Other); // Video-Aula
            AddCategoryMapping(6, TorznabCatType.TV); // Vídeos de TV
            AddCategoryMapping(4, TorznabCatType.Other); // Jogos
            AddCategoryMapping(199, TorznabCatType.XXX); // Filmes Adultos
            AddCategoryMapping(200, TorznabCatType.XXX); // Jogos Adultos
            AddCategoryMapping(201, TorznabCatType.XXXImageset); // Fotos Adultas
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
            {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "keeplogged", "1" }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl, true);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("logout.php"), () =>
            {
                var errorMessage = result.Content;
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        private string StripSearchString(string term, bool isAnime)
        {
            // Search does not support searching with episode numbers so strip it if we have one
            // Ww AND filter the result later to archive the proper result
            if (isAnime)
            {
                return term.TrimEnd(digits);
            }
            
            var ret = Regex.Replace(term, @"[S|E]\d\d", string.Empty).Trim();
            return ret.Replace("Agents of SHIELD", "Agents of S.H.I.E.L.D.");
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            // if the search string is empty use the "last 24h torrents" view
            if (string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var results = await RequestStringWithCookies(TodayUrl);
                try
                {
                    string RowsSelector = "table.torrent_table > tbody > tr:not(tr.colhead)";

                    var SearchResultParser = new HtmlParser();
                    var SearchResultDocument = SearchResultParser.Parse(results.Content);
                    var Rows = SearchResultDocument.QuerySelectorAll(RowsSelector);
                    foreach (var Row in Rows)
                    {
                        try
                        {
                            var release = new ReleaseInfo();

                            release.MinimumRatio = 1;
                            release.MinimumSeedTime = 0;

                            var qDetailsLink = Row.QuerySelector("a.BJinfoBox");
                            var qTitle = qDetailsLink.QuerySelector("font");
                            release.Title = qTitle.TextContent;

                            var qBJinfoBox = qDetailsLink.QuerySelector("span");
                            var qCatLink = Row.QuerySelector("a[href^=\"/torrents.php?filter_cat\"]");
                            var qDLLink = Row.QuerySelector("a[href^=\"torrents.php?action=download\"]");
                            var qSeeders = Row.QuerySelector("td:nth-child(4)");
                            var qLeechers = Row.QuerySelector("td:nth-child(5)");
                            var qFreeLeech = Row.QuerySelector("font[color=\"green\"]:contains(Free)");

                            release.Description = "";
                            foreach (var Child in qBJinfoBox.ChildNodes)
                            {
                                var type = Child.NodeType;
                                if (type != NodeType.Text)
                                    continue;

                                var line = Child.TextContent;
                                if (line.StartsWith("Tamanho:"))
                                {
                                    string Size = line.Substring("Tamanho: ".Length); ;
                                    release.Size = ReleaseInfo.GetBytes(Size);
                                }
                                else if (line.StartsWith("Lançado em: "))
                                {
                                    string PublishDateStr = line.Substring("Lançado em: ".Length).Replace("às ", "");
                                    PublishDateStr += " +0";
                                    var PublishDate = DateTime.SpecifyKind(DateTime.ParseExact(PublishDateStr, "dd/MM/yyyy HH:mm z", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);
                                    release.PublishDate = PublishDate.ToLocalTime();
                                }
                                else
                                {
                                    release.Description += line + "\n";
                                }
                            }

                            var catStr = qCatLink.GetAttribute("href").Split('=')[1];
                            // if result is an anime, convert title from SXXEXX to EXX
                            if (catStr == "14")
                            {
                                release.Title = Regex.Replace(release.Title, @"(Ep[\.]?[ ]?)|([S]\d\d[Ee])", "E");
                            }
                            release.Category = MapTrackerCatToNewznab(catStr);

                            release.Link = new Uri(SiteLink + qDLLink.GetAttribute("href"));
                            release.Comments = new Uri(SiteLink + qDetailsLink.GetAttribute("href"));
                            release.Guid = release.Link;

                            release.Seeders = ParseUtil.CoerceInt(qSeeders.TextContent);
                            release.Peers = ParseUtil.CoerceInt(qLeechers.TextContent) + release.Seeders;

                            if (qFreeLeech != null)
                                release.DownloadVolumeFactor = 0;
                            else
                                release.DownloadVolumeFactor = 1;

                            release.UploadVolumeFactor = 1;

                            releases.Add(release);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(string.Format("{0}: Error while parsing row '{1}': {2}", ID, Row.OuterHtml, ex.Message));
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnParseError(results.Content, ex);
                }
            }
            else // use search
            {
                var searchUrl = BrowseUrl;
                var isSearchAnime = query.Categories.Any(s => s == TorznabCatType.TVAnime.ID);
                
                query.SearchTerm = query.SearchTerm.Replace("Agents of SHIELD", "Agents of S.H.I.E.L.D.");
                var searchString = query.GetQueryString();

                var queryCollection = new NameValueCollection
                {
                    {"searchstr", StripSearchString(searchString, isSearchAnime)},
                    {"order_by", "time"},
                    {"order_way", "desc"},
                    {"group_results", "1"},
                    {"action", "basic"},
                    {"searchsubmit", "1"}
                };

                foreach (var cat in MapTorznabCapsToTrackers(query))
                {
                    queryCollection.Add("filter_cat[" + cat + "]", "1");
                }

                searchUrl += "?" + queryCollection.GetQueryString();

                var results = await RequestStringWithCookies(searchUrl);
                try
                {
                    string RowsSelector = "table.torrent_table > tbody > tr:not(tr.colhead)";

                    var SearchResultParser = new HtmlParser();
                    var SearchResultDocument = SearchResultParser.Parse(results.Content);
                    var Rows = SearchResultDocument.QuerySelectorAll(RowsSelector);

                    ICollection<int> GroupCategory = null;
                    string GroupTitle = null;
                    string GroupYearStr = null;
                    Nullable<DateTime> GroupPublishDate = null;

                    foreach (var Row in Rows)
                    {
                        try
                        {
                            var qDetailsLink = Row.QuerySelector("a[href^=\"torrents.php?id=\"]");
                            string Title = qDetailsLink.TextContent;
                            ICollection<int> Category = null;
                            string YearStr = null;
                            Nullable<DateTime> YearPublishDate = null;
                            string CategoryStr = "";

                            if (Row.ClassList.Contains("group") || Row.ClassList.Contains("torrent")) // group/ungrouped headers
                            {
                                var qCatLink = Row.QuerySelector("a[href^=\"/torrents.php?filter_cat\"]");
                                CategoryStr = qCatLink.GetAttribute("href").Split('=')[1].Split('&')[0];
                                Category = MapTrackerCatToNewznab(CategoryStr);
                                
                                YearStr = qDetailsLink.NextSibling.TextContent.Trim().TrimStart('[').TrimEnd(']');
                                YearPublishDate = DateTime.SpecifyKind(DateTime.ParseExact(YearStr, "yyyy", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);

                                // if result is an anime, convert title from SXXEXX to EXX
                                if (CategoryStr == "14")
                                {
                                    Title = Regex.Replace(Title, @"(Ep[\.]?[ ]?)|([S]\d\d[Ee])", "E");
                                }

                                if (Row.ClassList.Contains("group")) // group headers
                                {
                                    GroupCategory = Category;
                                    GroupTitle = Title;
                                    GroupYearStr = YearStr;
                                    GroupPublishDate = YearPublishDate;
                                    continue;
                                }
                            }

                            var release = new ReleaseInfo();

                            release.MinimumRatio = 1;
                            release.MinimumSeedTime = 0;

                            var qDLLink = Row.QuerySelector("a[href^=\"torrents.php?action=download\"]");
                            var qSize = Row.QuerySelector("td:nth-last-child(4)");
                            var qGrabs = Row.QuerySelector("td:nth-last-child(3)");
                            var qSeeders = Row.QuerySelector("td:nth-last-child(2)");
                            var qLeechers = Row.QuerySelector("td:nth-last-child(1)");
                            var qFreeLeech = Row.QuerySelector("strong[title=\"Free\"]");

                            if (Row.ClassList.Contains("group_torrent")) // torrents belonging to a group
                            {
                                release.Description = qDetailsLink.TextContent;

                                string cleanTitle = Regex.Replace(GroupTitle, @" - S?(?<season>\d{1,2})?E?(?<episode>\d{1,4})?", "");
                                string seasonEp = Regex.Replace(GroupTitle, @"^(.*?) - (S?(\d{1,2})?E?(\d{1,4})?)?", "$2");
                                release.Title = CategoryStr == "14" ? GroupTitle : cleanTitle + " " + GroupYearStr + " " + seasonEp;

                                release.PublishDate = GroupPublishDate.Value;
                                release.Category = GroupCategory;
                            }
                            else if (Row.ClassList.Contains("torrent")) // standalone/un grouped torrents
                            {
                                var qDescription = Row.QuerySelector("div.torrent_info");
                                release.Description = qDescription.TextContent;

                                string cleanTitle = Regex.Replace(Title, @" - ((S(\d{1,2}))?E(\d{1,4}))", "");
                                string seasonEp = Regex.Replace(Title, @"^(.*?) - ((S(\d{1,2}))?E(\d{1,4}))", "$2");
                                release.Title = CategoryStr == "14" ? Title : cleanTitle + " " + YearStr + " " + seasonEp;

                                release.PublishDate = YearPublishDate.Value;
                                release.Category = Category;
                            }

                            release.Description = release.Description.Replace(" / Free", ""); // Remove Free Tag

                            release.Description = release.Description.Replace("Full HD", "1080p");
                            release.Description = release.Description.Replace("/ HD / ", "/ 720p /");
                            release.Description = release.Description.Replace(" / HD]", " / 720p]");
                            release.Description = release.Description.Replace("4K", "2160p");

                            int nBarra = release.Title.IndexOf("[");
                            if (nBarra != -1)
                            {
                                release.Title = release.Title.Substring(nBarra + 1);
                                release.Title = release.Title.Replace("]", "");
                            }

                            release.Title += " " + release.Description; // add year and Description to the release Title to add some meaning to it

                            // check for previously stripped search terms
                            if (!query.MatchQueryStringAND(release.Title))
                                continue;

                            var Size = qSize.TextContent;
                            release.Size = ReleaseInfo.GetBytes(Size);

                            release.Link = new Uri(SiteLink + qDLLink.GetAttribute("href"));
                            release.Comments = new Uri(SiteLink + qDetailsLink.GetAttribute("href"));
                            release.Guid = release.Link;

                            release.Grabs = ParseUtil.CoerceLong(qGrabs.TextContent);
                            release.Seeders = ParseUtil.CoerceInt(qSeeders.TextContent);
                            release.Peers = ParseUtil.CoerceInt(qLeechers.TextContent) + release.Seeders;

                            if (qFreeLeech != null)
                                release.DownloadVolumeFactor = 0;
                            else
                                release.DownloadVolumeFactor = 1;

                            release.UploadVolumeFactor = 1;

                            releases.Add(release);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(string.Format("{0}: Error while parsing row '{1}': {2}", ID, Row.OuterHtml, ex.Message));
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnParseError(results.Content, ex);
                }
            }

            return releases;
        }
    }
}
