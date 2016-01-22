using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Jackett.Models.IndexerConfig;
using System.Xml;
using HtmlAgilityPack;
using System.IO;
using System.Collections.Specialized;
using System.Diagnostics;

namespace Jackett.Indexers
{
    public class SpeedCD : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "take.login.php"; } }
        private string SearchUrl { get { return SiteLink + "browse.php?s=4&t=2&"; } }
        private string CommentsUrl { get { return SiteLink + "t/{0}"; } }
        private string DownloadUrl { get { return SiteLink + "download.php?torrent={0}"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public SpeedCD(IIndexerManagerService i, Logger l, IWebClient wc, IProtectionService ps)
            : base(name: "Speed.cd",
                description: "Your home now!",
                link: "http://speed.cd/",
                caps: new TorznabCapabilities(),
                manager: i,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            AddMultiCategoryMapping(TorznabCatType.Movies,
                    1	//	Moviex/Xvid
                    , 42	//	Movies/Packs    42
                    , 32	//	Movies/Kids     32
                    , 43	//	Movies/HD       43
                    , 47	//	Movies/Deversity   47
                    , 28	//	Movies/BluRay      28
                    , 48	//	Movies/3D          48
                    , 40	//	Movies/DVD-R       40
                    );

            AddMultiCategoryMapping(TorznabCatType.Movies3D
                    , 48	//	Movies/3D          48
                    );

            AddMultiCategoryMapping(TorznabCatType.MoviesBluRay
        , 28    //	Movies/BluRay      28
        );

            AddMultiCategoryMapping(TorznabCatType.MoviesDVD
        , 40    //	Movies/DVD-R       40
        );

            AddMultiCategoryMapping(TorznabCatType.MoviesHD
, 43    //	Movies/HD       43
, 47    //	Movies/Deversity   47
);

            AddMultiCategoryMapping(TorznabCatType.MoviesOther
, 1   //	Moviex/Xvid
, 42    //	Movies/Packs    42
, 32    //	Movies/Kids     32
, 47    //	Movies/Deversity   47
);

            AddMultiCategoryMapping(TorznabCatType.MoviesSD
, 1   //	Moviex/Xvid
);


            AddMultiCategoryMapping(TorznabCatType.TV
        , 49    //	TV/HD              49
        , 50    //	TV/Sports          50
        , 52    //	TV/BluRay          52
        , 53    //	TV/DVD-R           53
        , 41    //	TV/Packs           41
        , 55    //	TV/Kids            55
        , 2 //	TV/Episodes        2
                    , 30	//	Anime              30
        );

            AddMultiCategoryMapping(TorznabCatType.TVAnime
        , 30    //	Anime              30
);

            AddMultiCategoryMapping(TorznabCatType.TVHD
, 49    //	TV/HD              49
, 52    //	TV/BluRay          52
);

            AddMultiCategoryMapping(TorznabCatType.TVOTHER
, 41    //	TV/Packs           41
, 55    //	TV/Kids            55
);

            AddMultiCategoryMapping(TorznabCatType.TVSD
, 53    //	TV/DVD-R           53
, 2 //	TV/Episodes        2
);

            AddMultiCategoryMapping(TorznabCatType.TVSport
, 50    //	TV/Sports          50
);

            AddMultiCategoryMapping(TorznabCatType.Console
                    , 39	//	Games/WII
                    , 45	//	Games/PS3
                    , 35	//	Games/Nintendo
                    , 33	//	Games/XBOX360
                    );

            AddMultiCategoryMapping(TorznabCatType.ConsoleWii
        , 39    //	Games/WII
        );

            AddMultiCategoryMapping(TorznabCatType.ConsolePS3
        , 45    //	Games/PS3
        );

            AddMultiCategoryMapping(TorznabCatType.Console3DS
        , 35    //	Games/Nintendo
        );

            AddMultiCategoryMapping(TorznabCatType.ConsoleXbox360
                    , 33	//	Games/XBOX360
                    );



            AddMultiCategoryMapping(TorznabCatType.PCGames
        , 25    //	Games/PC           25
        );


            AddMultiCategoryMapping(TorznabCatType.PC0day
        , 24    //	Other/0Day
        );


            AddMultiCategoryMapping(TorznabCatType.PCPhoneAndroid
        , 46    //	Other/Mobile
        );

            AddMultiCategoryMapping(TorznabCatType.PCPhoneIOS
, 46    //	Other/Mobile
);

            AddMultiCategoryMapping(TorznabCatType.PCPhoneOther
, 46    //	Other/Mobile
);

            AddMultiCategoryMapping(TorznabCatType.PCMac
        , 51    //	Other/Mac
        );

            AddMultiCategoryMapping(TorznabCatType.Other
, 54    //	Other/Educational
);

            AddMultiCategoryMapping(TorznabCatType.Books
, 27    //	Other/Books
);

            AddMultiCategoryMapping(TorznabCatType.Audio
, 26    //	Music/Audio
, 44    //	Music/Pack
, 29    //	Music/Video
);

            AddMultiCategoryMapping(TorznabCatType.AudioMP3
, 26    //	Music/Audio
, 44    //	Music/Pack
);

            AddMultiCategoryMapping(TorznabCatType.AudioVideo
, 29    //	Music/Video
);


            TorznabCaps.Categories.AddRange(TorznabCatType.AllCats);

        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, SiteLink);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("logout.php"), () =>
            {
                CQ dom = result.Content;
                var errorMessage = dom["h5"].First().Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var releasesAppend = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var searchUrl = SearchUrl;
            var queryCollection = new NameValueCollection();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("search", searchString);
            }

            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                queryCollection.Add("c" + cat, "1");
                Console.WriteLine("c" + cat);
            }

            searchUrl += queryCollection.GetQueryString();

            for (int i = 1; i <= 10; i++)
            {

                var response = await RequestStringWithCookiesAndRetry(searchUrl + "&p=" + i);

                try
                {


                    HtmlDocument html = new HtmlDocument();
                    TextReader reader = new StringReader(response.Content);
                    html.Load(reader);

                    HtmlNode tableDiv = html.GetElementbyId("torrentTable");
                    if (tableDiv == null) goto EndSearch;

                    HtmlNode table = tableDiv.ChildNodes[0].ChildNodes[1].ChildNodes[1];
                    if (table == null) goto EndSearch;

                    HtmlNode tableBody = table.ChildNodes[1];
                    if(tableBody == null) goto EndSearch;

                    if (tableBody.ChildNodes.Count == 0) goto EndSearch;

                    releasesAppend = (from r in tableBody.ChildNodes
                                      select new ReleaseInfo()
                                      {
                                          Guid = new Uri(string.Format(CommentsUrl, r.GetAttributeValue("id", Guid.NewGuid().ToString()).Remove(0, 2)))
                                          ,
                                          Title = r.ChildNodes[1].ChildNodes[0].ChildNodes[0].ChildNodes[0].InnerText
                                          ,
                                          Comments = new Uri(string.Format(CommentsUrl, r.GetAttributeValue("id", Guid.NewGuid().ToString()).Remove(0, 2)))
                                          ,
                                          Link = new Uri(string.Format(DownloadUrl, r.GetAttributeValue("id", Guid.NewGuid().ToString()).Remove(0, 2)))
                                          ,
                                          PublishDate = DateTime.ParseExact(r.SelectSingleNode(string.Format("//*[contains(@class,'{0}')]", "elapsedDate")).GetAttributeValue("title", "").Replace(" at", ""), "dddd, MMMM d, yyyy h:mmtt", CultureInfo.InvariantCulture)
                                          ,
                                          Size = ReleaseInfo.GetBytes(r.ChildNodes[4].InnerText)
                                          ,
                                          Seeders = ParseUtil.CoerceInt(r.ChildNodes[5].InnerText)
                                          ,
                                          Peers = ParseUtil.CoerceInt(r.ChildNodes[5].InnerText)
                                      }).ToList<ReleaseInfo>();

                    releases.AddRange(releasesAppend);


                    if (releases.Count(x => x.Seeders == 0) > 0) goto EndSearch;

                }
                catch (Exception ex)
                {
                    OnParseError(response.Content, ex);
                }

            }

            EndSearch:

            return releases;
        }
    }
}
