using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers
{
	public class Corsarored : BaseWebIndexer
	{
		private string APILatest { get { return SiteLink + "api/latests"; } }
		private string APISearch { get { return SiteLink + "api/search"; } }

		private Dictionary<string, string> APIHeaders = new Dictionary<string, string>()
		{
			{"Content-Type", "application/json"},
		};

		private readonly int MAX_SEARCH_PAGE_LIMIT = 8; // 1page 25 items, 200

		private new ConfigurationData configData
		{
			get { return (ConfigurationData)base.configData; }
			set { base.configData = value; }
		}

		public Corsarored(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
			: base(name: "Corsaro.red",
				   description: "Italian Torrents",
				   link: "https://corsaro.red/",
				   caps: new TorznabCapabilities(),
				   configService: configService,
				   client: wc,
				   logger: l,
				   p: ps,
				   configData: new ConfigurationData())
		{
			Encoding = Encoding.UTF8;
			Language = "it-it";
			Type = "public";

			//wc.requestDelay = 2.5;

			// TNTVillage cats
			AddCategoryMapping(1, TorznabCatType.TV, "TV");
			AddCategoryMapping(2, TorznabCatType.Audio, "Music");
			AddCategoryMapping(3, TorznabCatType.BooksEbook, "Books");
			AddCategoryMapping(4, TorznabCatType.Movies, "Movies");
			AddCategoryMapping(6, TorznabCatType.PC, "Software");
			AddCategoryMapping(7, TorznabCatType.TVAnime, "Anime");
			AddCategoryMapping(8, TorznabCatType.TVAnime, "Cartoons");
			AddCategoryMapping(9, TorznabCatType.PC, "Software");
			AddCategoryMapping(10, TorznabCatType.PC0day, "Software");
			AddCategoryMapping(11, TorznabCatType.PCGames, "Games");
			AddCategoryMapping(12, TorznabCatType.Console, "Games");
			AddCategoryMapping(13, TorznabCatType.Books, "Books");
			AddCategoryMapping(14, TorznabCatType.TVDocumentary, "Documentaries");
			AddCategoryMapping(21, TorznabCatType.AudioVideo, "Music Video");
			AddCategoryMapping(22, TorznabCatType.TVSport, "Sport");
			AddCategoryMapping(23, TorznabCatType.TV, "TV");
			AddCategoryMapping(24, TorznabCatType.TV, "TV");
			AddCategoryMapping(26, TorznabCatType.Console, "Games");
			AddCategoryMapping(27, TorznabCatType.Other, "Wallpaper");
			AddCategoryMapping(29, TorznabCatType.TV, "TV Series");
			AddCategoryMapping(30, TorznabCatType.BooksComics, "Comics");
			AddCategoryMapping(31, TorznabCatType.TV, "TV");
			AddCategoryMapping(32, TorznabCatType.Console, "Games");
			AddCategoryMapping(34, TorznabCatType.AudioAudiobook, "Audiobook");
			AddCategoryMapping(35, TorznabCatType.Audio, "Music");
			AddCategoryMapping(36, TorznabCatType.Books, "Books");
			AddCategoryMapping(37, TorznabCatType.PC, "Software");
		}

		public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
		{
			base.LoadValuesFromJson(configJson);
			var releases = await PerformQuery(new TorznabQuery());

			await ConfigureIfOK(String.Empty, releases.Count() > 0, () =>
			{
				throw new Exception("Could not find release from this URL.");
			});

			return IndexerConfigurationStatus.Completed;
		}

		private dynamic checkResponse(WebClientStringResult result)
		{
			try
			{
				dynamic json = JsonConvert.DeserializeObject<dynamic>(result.Content);

				if (json is JObject)
					if (json["ok"] != null && ((bool)json["ok"]) == false)
						throw new Exception("Server error");

				return json;
			}
			catch (Exception e)
			{
				logger.Error("checkResponse() Error: ", e.Message);
				throw new ExceptionWithConfigData(result.Content, configData);
			}
		}

		private async Task<dynamic> SendAPIRequest(List<KeyValuePair<string, string>> data)
		{
			var jsonData = JsonConvert.SerializeObject(data);
			var result = await PostDataWithCookiesAndRetry(APISearch, data, null, SiteLink, APIHeaders, null, true);
			return checkResponse(result);
		}

		private async Task<dynamic> SendAPIRequestLatest()
		{
			var result = await RequestStringWithCookiesAndRetry(APILatest, null, SiteLink, APIHeaders);
			return checkResponse(result);
		}

		protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
		{
			var releases = new List<ReleaseInfo>();

			var searchString = query.GetQueryString();
			var queryCollection = new List<KeyValuePair<string, string>>();
			int page = 0;

			if (!string.IsNullOrWhiteSpace(searchString))
				queryCollection.Add("term", searchString);
			else
			{
				// no term execute latest search
				var result = await SendAPIRequestLatest();

				try
				{
					// this time is a jarray
					JArray json = (JArray)result;

					foreach (var torrent in json)
					{
						// add release
						releases.Add(makeRelease(torrent));
					}
				}
				catch (Exception ex)
				{
					OnParseError(result.ToString(), ex);
				}

				return releases;
			}

			var cats = MapTorznabCapsToTrackers(query);
			if (cats.Count > 0)
				queryCollection.Add("category", string.Join(",", cats));
			else
				queryCollection.Add("category", "0");   // set ALL category

			// lazy horrible page initialization
			queryCollection.Add("page", page.ToString());

			do
			{
				// update page number
				queryCollection.RemoveAt(queryCollection.Count - 1); // remove last elem: page number 
				queryCollection.Add("page", (++page).ToString());

				var result = await SendAPIRequest(queryCollection);
				try
				{
					// this time is a jobject
					JObject json = (JObject)result;

					if (json["results"] == null)
						throw new Exception("Error invalid JSON response");

					// check number result
					if (((JArray)json["results"]).Count() == 0)
						break;

					foreach (var torrent in json["results"])
					{
						// add release
						releases.Add( makeRelease(torrent) );
					}
				}
				catch (Exception ex)
				{
					OnParseError(result.ToString(), ex);
				}

			} while (page < MAX_SEARCH_PAGE_LIMIT);

			return releases;
		}

		private ReleaseInfo makeRelease(JToken torrent)
		{
			var release = new ReleaseInfo();

			release.Title = (String)torrent["title"];

			//https://corsaro.red/details/E5BB62E2E58C654F4450325046723A3F013CD7A4
			release.Comments = new Uri(SiteLink + "details/" + (string)torrent["hash"]);
			release.Guid = release.Comments;
			//release.Link = release.Comments;

			release.PublishDate = DateTime.Now;
			if (torrent["last_updated"] != null)
				release.PublishDate = DateTime.Parse((string)torrent["last_updated"]);

			// TODO: don't know how to map this cats..
			int cat = (int)torrent["category"];
			release.Category = MapTrackerCatToNewznab(cat.ToString());

			if (torrent["size"] != null)
				release.Size = (long)torrent["size"];

			release.Grabs = (long)torrent["completed"];

			release.Description = (string)torrent["category"] + " " + (string)torrent["description"];

			/*
			RageID = copyFrom.RageID;
			Imdb = copyFrom.Imdb;
			TMDb = copyFrom.TMDb;
			*/

			release.Seeders = (int)torrent["seeders"];
			release.Peers = release.Seeders + (int)torrent["leechers"];

			release.InfoHash = (string)torrent["hash"];
			release.MagnetUri = new Uri((string)torrent["magnet"]);

			/*
			MinimumRatio = copyFrom.MinimumRatio;
			MinimumSeedTime = copyFrom.MinimumSeedTime;
			*/

			release.DownloadVolumeFactor = 0;
			release.UploadVolumeFactor = 1;

			return release;
		}
	}
}
