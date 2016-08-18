using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Models.IndexerConfig.Bespoke;

namespace Jackett.Indexers
{
    public class BlueTigers : BaseIndexer, IIndexer
    {
        private string LoginUrl => SiteLink + "account-login.php";
        private string TorrentSearchUrl => SiteLink + "torrents-search.php";
        private string IndexUrl => SiteLink + "index.php";

        private ConfigurationDataBlueTigers ConfigData
        {
            get { return (ConfigurationDataBlueTigers)configData; }
            set { base.configData = value; }
        }

        public BlueTigers(IIndexerManagerService i, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "BlueTigers",
                description: "BlueTigers - No Ratio - Private",
                link: "https://www.bluetigers.ca/",
                caps: new TorznabCapabilities(),
                manager: i,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBlueTigers(@"BlueTigers can search for one or all languages. 
                                                            If you select 2 languages below, results will contain all 3 languages.
                                                            <br> For best results change the torrents per page setting to 50 in your BlueTigers profile."))
        {
            AddCategoryMapping("14", TorznabCatType.ConsolePSP);
            AddCategoryMapping("150", TorznabCatType.ConsoleWii);
            AddCategoryMapping("150", TorznabCatType.ConsoleWiiwareVC);
            AddCategoryMapping("150", TorznabCatType.ConsoleWiiU);
            AddCategoryMapping("52", TorznabCatType.ConsoleXbox);
            AddCategoryMapping("52", TorznabCatType.ConsoleXbox360);
            AddCategoryMapping("52", TorznabCatType.ConsoleXBOX360DLC);
            AddCategoryMapping("52", TorznabCatType.ConsoleXboxOne);
            AddCategoryMapping("11", TorznabCatType.PCGames);
            AddCategoryMapping("13", TorznabCatType.ConsolePS4);
            AddCategoryMapping("13", TorznabCatType.ConsolePS3);
            AddCategoryMapping("13", TorznabCatType.ConsolePSVita);
            AddCategoryMapping("12", TorznabCatType.Console3DS);
            AddCategoryMapping("160", TorznabCatType.PCPhoneIOS);
            AddCategoryMapping("160", TorznabCatType.PCPhoneAndroid);
            AddCategoryMapping("1", TorznabCatType.PCPhoneAndroid);
            AddCategoryMapping("29", TorznabCatType.PCMac);
            AddCategoryMapping("27", TorznabCatType.PC);
            AddCategoryMapping("41", TorznabCatType.PC);
            AddCategoryMapping("50", TorznabCatType.PC);
            AddCategoryMapping("333", TorznabCatType.BooksMagazines);
            AddCategoryMapping("38", TorznabCatType.TVDocumentary);
            AddCategoryMapping("37", TorznabCatType.BooksEbook);
            AddCategoryMapping("61", TorznabCatType.Movies3D);
            AddCategoryMapping("45", TorznabCatType.XXX);
            AddCategoryMapping("59", TorznabCatType.MoviesHD);
            AddCategoryMapping("222", TorznabCatType.MoviesHD);
            AddCategoryMapping("22", TorznabCatType.MoviesHD);
            AddCategoryMapping("60", TorznabCatType.MoviesHD);
            AddCategoryMapping("56", TorznabCatType.MoviesBluRay);
            AddCategoryMapping("23", TorznabCatType.MoviesOther);
            AddCategoryMapping("15", TorznabCatType.MoviesOther);
            AddCategoryMapping("43", TorznabCatType.MoviesDVD);
            AddCategoryMapping("24", TorznabCatType.MoviesDVD);
            AddCategoryMapping("25", TorznabCatType.MoviesOther);
            AddCategoryMapping("21", TorznabCatType.MoviesOther);
            AddCategoryMapping("20", TorznabCatType.MoviesDVD);
            AddCategoryMapping("26", TorznabCatType.MoviesWEBDL);
            AddCategoryMapping("9", TorznabCatType.TVAnime);
            AddCategoryMapping("34", TorznabCatType.Other);
            AddCategoryMapping("35", TorznabCatType.Audio);
            AddCategoryMapping("36", TorznabCatType.AudioVideo);
            AddCategoryMapping("31", TorznabCatType.AudioVideo);
            AddCategoryMapping("2", TorznabCatType.TVOTHER);
            AddCategoryMapping("16", TorznabCatType.TVHD);
            AddCategoryMapping("130", TorznabCatType.TVHD);
            AddCategoryMapping("10", TorznabCatType.TVSD);
            AddCategoryMapping("131", TorznabCatType.TV);
            AddCategoryMapping("17", TorznabCatType.TV);
            AddCategoryMapping("18", TorznabCatType.TV);
            AddCategoryMapping("19", TorznabCatType.TV);
            AddCategoryMapping("58", TorznabCatType.TVSport);
            AddCategoryMapping("33", TorznabCatType.TVOTHER);
            AddCategoryMapping("34", TorznabCatType.Other);
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            ConfigData.LoadValuesFromJson(configJson);

            if (ConfigData.French.Value == false && ConfigData.English.Value == false && ConfigData.Spanish.Value == false)
                throw new ExceptionWithConfigData("Please select at least one language.", ConfigData);

            await RequestStringWithCookies(LoginUrl, string.Empty);
            var pairs = new Dictionary<string, string> {
                { "username", ConfigData.Username.Value },
                { "password", ConfigData.Password.Value },
                { "take_login", "1" }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, IndexUrl, SiteLink);
            Regex rgx = new Regex(@"uid=[0-9]{1,10}; pass=[a-z0-9]{1,40};");
            await ConfigureIfOK(result.Cookies, rgx.IsMatch(result.Cookies), () =>
            {
                var errorMessage = "Error while trying to login.";
                throw new ExceptionWithConfigData(errorMessage, ConfigData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            NameValueCollection qParams = new NameValueCollection();
            if (ConfigData.French.Value && !ConfigData.English.Value && !ConfigData.Spanish.Value)
            {
                qParams.Add("lang", "1");
            }
            else
            {
                if (!ConfigData.French.Value && ConfigData.English.Value && !ConfigData.Spanish.Value)
                {
                    qParams.Add("lang", "2");
                }
                else
                {
                    if (!ConfigData.French.Value && !ConfigData.English.Value && ConfigData.Spanish.Value)
                    {
                        qParams.Add("lang", "3");
                    }
                    else
                    {
                        qParams.Add("lang", "0");
                    }
                }
                     
            } 
                
            List<string> catList = MapTorznabCapsToTrackers(query);
            foreach (string cat in catList)
            {
                qParams.Add("c" + cat, "1");
            }

            if (!string.IsNullOrEmpty(query.SanitizedSearchTerm))
            {
                qParams.Add("search", query.GetQueryString());
            }

            string queryStr = qParams.GetQueryString();
            string searchUrl = $"{TorrentSearchUrl}?incldead=0&freeleech=0&sort=id&order=ascdesc&{queryStr}";

            List<CQ> torrentRowList = new List<CQ>();

            var results = await RequestStringWithCookiesAndRetry(searchUrl);
            try
            {
                CQ fDom = results.Content;
                var firstPageRows = fDom["table[class='ttable_headinner'] > tbody > tr:not(:First-child)"];
                torrentRowList.AddRange(firstPageRows.Select(fRow => fRow.Cq()));

                //If a search term is used, follow upto the first 4 pages (initial plus 3 more)
                if (!string.IsNullOrWhiteSpace(query.GetQueryString()) && fDom["a[class='boutons']"].Filter("a[href*=&page=]").Length > 0)
                {
                    int pageLinkCount;
                    int.TryParse(fDom["a[class='boutons']"].Filter("a[href*=&page=]").Last().Attr("href").Split(new[] { "&page=" }, StringSplitOptions.None).LastOrDefault(), out pageLinkCount);
                    for (int i = 1; i < Math.Min(4, pageLinkCount + 1); i++)
                    {
                        var sResults = await RequestStringWithCookiesAndRetry($"{searchUrl}&page={i}");
                        CQ sDom = sResults.Content;
                        var additionalPageRows = sDom["table[class='ttable_headinner'] > tbody > tr:not(:First-child)"];
                        torrentRowList.AddRange(additionalPageRows.Select(sRow => sRow.Cq()));
                    }
                }

                foreach (CQ tRow in torrentRowList)
                {
                    long torrentId = 0;
                    string idTarget = "bookmarks.php?torrent=";
                    string id = tRow.Find("a[href*=" + idTarget + "]").First().Attr("href").Trim();
                    if (!string.IsNullOrEmpty(id) && id.Contains(idTarget))
                    {
                        long.TryParse(id.Substring(id.LastIndexOf(idTarget, StringComparison.Ordinal) + idTarget.Length), out torrentId);
                    }

                    if (torrentId <= 0) continue;

                    long category = 0;
                    string catTarget = "torrents.php?cat=";
                    string cat = tRow.Find("a[href*=" + catTarget + "]").First().Attr("href").Trim();
                    if (!string.IsNullOrEmpty(cat) && cat.Contains(catTarget))
                    {
                        long.TryParse(cat.Substring(cat.LastIndexOf(catTarget, StringComparison.Ordinal) + catTarget.Length), out category);
                    }

                    Uri guid = new Uri($"{SiteLink}torrents-details.php?hit=1&id={torrentId}");
                    Uri link = new Uri($"{SiteLink}download.php?hit=1&id={torrentId}");
                    Uri comments = new Uri($"{SiteLink}comments.php?type=torrent&id={torrentId}");
                    string title = tRow.Find("a[href*=torrents-details.php?id=]").First().Text().Trim();
                    string stats = tRow.Find("div[id=kt" + torrentId.ToString() + "]").First().Text();
                    string sizeStr = new Regex("Taille:(.*)Vitesse:").Match(stats).Groups[1].ToString().Trim();
                    string pubDateStr = new Regex("Ajout.:(.*)Compl.t.s").Match(stats).Groups[1].ToString().Trim();
                    DateTime pubDate = DateTime.ParseExact(pubDateStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();

                    string statistics = tRow.Find("a[href*=torrents-details.php?id=]").First().RenderSelection().Trim();
                    string startTag = "<table ";
                    string endTag = "</table>";
                    CQ statsCq = startTag + new Regex(startTag + "(.*)" + endTag).Match(statistics).Groups[1].ToString().Trim() + endTag;
                    int seeders;
                    int leechers;
                    int.TryParse(statsCq.Find("font[color=#05FC09]").First().Text(), out seeders);
                    int.TryParse(statsCq.Find("font[color=red]").First().Text(), out leechers);

                    var release = new ReleaseInfo();

                    release.Title = title;
                    release.Guid = guid;
                    release.Link = link;
                    release.PublishDate = pubDate;
                    release.Size = ReleaseInfo.GetBytes(sizeStr);
                    release.Description = title;
                    release.Seeders = seeders;
                    release.Peers = leechers + seeders;
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    release.Category = MapTrackerCatToNewznab(category.ToString());
                    release.Comments = comments;

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
