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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Jackett.Models.IndexerConfig;

namespace Jackett.Indexers
{
    public class Trezzor : BaseWebIndexer
    {
        string LoginUrl { get { return SiteLink + "prihlasenie.php"; } }
        private string SearchUrl { get { return SiteLink + "torrents.php?"; } }


        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public Trezzor(IIndexerConfigurationService configService, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "Trezzor",
                description: "SK/CZ Tracker.",
                link: "https://tracker.czech-server.com/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.GetEncoding("windows-1250");
            Language = "cs-cz";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.MoviesDVD, "DVD CZ/SK dabing");
            AddCategoryMapping(2, TorznabCatType.MoviesDVD, "DVD CZ/SK titulky");
            AddCategoryMapping(3, TorznabCatType.AudioVideo, "DVD Hudební video");
            AddCategoryMapping(4, TorznabCatType.MoviesSD, "XviD, DivX CZ/SK dabing");
            AddCategoryMapping(13, TorznabCatType.Audio, "Hudba CZ/SK scéna");
            AddCategoryMapping(24, TorznabCatType.Audio, "Mluv. slovo CZ/SK dabing");
            AddCategoryMapping(10, TorznabCatType.AudioOther, "DTS audio");
            AddCategoryMapping(14, TorznabCatType.PCGames, "Hry");
            AddCategoryMapping(17, TorznabCatType.PC, "Programy");
            AddCategoryMapping(14, TorznabCatType.PC, "Cestiny,patche,upgrady");
            AddCategoryMapping(18, TorznabCatType.Books, "Knihy CZ/SK lokalizace");
            AddCategoryMapping(18, TorznabCatType.BooksComics, "Komiks CZ/SK lokalizace");
            AddCategoryMapping(16, TorznabCatType.Other, "Foto,obrázky");
            AddCategoryMapping(20, TorznabCatType.Console, "Konzole");
            AddCategoryMapping(21, TorznabCatType.PCPhoneOther, "Mobilmánia");
            AddCategoryMapping(22, TorznabCatType.Other, "Ostatní CZ/SK scéna");
            AddCategoryMapping(23, TorznabCatType.Other, "Na prani non CZ/SK");
            AddCategoryMapping(27, TorznabCatType.Other, "TreZzoR rls");
            AddCategoryMapping(35, TorznabCatType.AudioVideo, "HDTV Hudební video");
            AddCategoryMapping(36, TorznabCatType.MoviesSD, "XviD, DivX CZ/SK titulky");
            AddCategoryMapping(31, TorznabCatType.MoviesHD, "HDTV CZ/SK Dabing");
            AddCategoryMapping(33, TorznabCatType.MoviesHD, "HDTV CZ/SK Titulky");
            AddCategoryMapping(39, TorznabCatType.Movies3D, "3D HDTV CZ/SK Dabing");
            AddCategoryMapping(40, TorznabCatType.Movies3D, "3D HDTV CZ/SK Titulky");
            AddCategoryMapping(5, TorznabCatType.MoviesSD, "TV-rip CZ/SK dabing");

            AddCategoryMapping(41, TorznabCatType.TVHD, "HD Seriály CZ/SK dabing");
            AddCategoryMapping(42, TorznabCatType.TVHD, "HD Seriály CZ/SK titulky");
            AddCategoryMapping(7, TorznabCatType.TVSD, "Seriály CZ/SK dabing");
            AddCategoryMapping(37, TorznabCatType.TVSD, "Seriály CZ/SK titulky");

            AddCategoryMapping(9, TorznabCatType.XXXXviD, "XXX CZ/SK dabing");
            AddCategoryMapping(32, TorznabCatType.XXXx264, "XXX HD CZ/SK dabing");
            AddCategoryMapping(27, TorznabCatType.Other, "TreZzoR rls");

        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var loginPage = await RequestStringWithCookies(LoginUrl, string.Empty);

            var pairs = new Dictionary<string, string> {
                { "uid", configData.Username.Value },
                { "pwd", configData.Password.Value }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, null, LoginUrl);

            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("If your browser doesn't have javascript enabled"), () =>
            {
                var errorMessage = "Couldn't login";
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchurls = new List<string>();
            var searchUrl = SearchUrl;
            var queryCollection = new NameValueCollection();
            var searchString = query.GetQueryString();


            // assign category only if one cat is required, otherwise search in whole tracker (tracker has ability to search only in one category or in everything)
            var _cats = MapTorznabCapsToTrackers(query);
            if (_cats.Count == 1) searchUrl += "category=" + _cats.First() + "&";



            if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("search", searchString);
            }



            queryCollection.Add("active", "1");

            searchUrl += queryCollection.GetQueryString().Replace("(", "%28").Replace(")", "%29"); // maually url encode brackets to prevent "hacking" detection


            var results = await RequestStringWithCookiesAndRetry(searchUrl);

            // Check for being logged out
            if (results.IsRedirect)
                if (results.RedirectingTo.Contains("prihlasenie.php"))
                    throw new ExceptionWithConfigData("Login failed, please reconfigure the tracker to update the cookies", configData);
                else
                    throw new ExceptionWithConfigData(string.Format("Got a redirect to {0}, please adjust your the alternative link", results.RedirectingTo), configData);

            if (results.Content.Contains("prihlasenie.php")) throw new ExceptionWithConfigData("Login failed, please reconfigure the tracker to update the cookies", configData);

            try
            {
                CQ dom = results.Content;
                ReleaseInfo release;

                var rows = dom[".torrenty_lista"];
                foreach (var row in rows)
                {
                    CQ qRow = row.Cq();

                    release = new ReleaseInfo();

                    string _cat = qRow.Find("td").Get(0).FirstChild.GetAttribute("href");
                    string _catName = qRow.Find("td a img").Get(0).GetAttribute("alt");
                    int _from = _cat.IndexOf("=");
                    _cat = _cat.Substring(_from + 1);

                    // filter results from categories, that were not requested
                    if ((_cats.Count > 1) && (!_cats.Contains(_cat))) continue;
                    release.Category = MapTrackerCatToNewznab(_cat);

                    string _guid = qRow.Find("td").Get(1).FirstChild.GetAttribute("href");
                    release.Guid = new Uri(SiteLink + _guid);

                    string _title = qRow.Find("td").Get(1).FirstChild.InnerText;
                    release.Title = _title + " [" + _catName + "]";

                    string _commentsLink = qRow.Find("td").Get(2).FirstChild.GetAttribute("href");
                    release.Comments = new Uri(SiteLink + _guid);

                    string _downloadLink = qRow.Find("td").Get(3).FirstChild.GetAttribute("href");
                    release.Link = new Uri(SiteLink + _downloadLink);

                    string _publishDate = qRow.Find("td").Get(5).InnerText;
                    DateTime _parsed;
                    DateTime.TryParse(_publishDate, out _parsed);
                    release.PublishDate = _parsed;

                    string _size = qRow.Find("td").Get(6).InnerText;
                    release.Size = ReleaseInfo.GetBytes(_size);

                    string _seeds = qRow.Find("td").Get(7).FirstChild.InnerText;
                    string _leechs = qRow.Find("td").Get(8).FirstChild.InnerText;
                    int seeders, peers;
                    if (ParseUtil.TryCoerceInt(_seeds, out seeders))
                    {
                        release.Seeders = seeders;
                        if (ParseUtil.TryCoerceInt(_leechs, out peers))
                        {
                            release.Peers = peers + release.Seeders;
                        }
                    }

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    release.UploadVolumeFactor = 1;

                    if (release.Title.Contains("FREELEECH")) release.DownloadVolumeFactor = 0;
                    else release.DownloadVolumeFactor = 1;

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
