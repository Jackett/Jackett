using System;
using System.Collections.Generic;
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

namespace Jackett.Common.Indexers
{
	public class Corsarored : BaseWebIndexer
	{
		private const int MaxSearchPageLimit = 8; // 1page 25 items, 200

		private readonly Dictionary<string, string> _apiHeaders = new Dictionary<string, string>
		{
			{"Content-Type", "application/json"}
		};

		public Corsarored(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
			: base("Corsaro.red",
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

		private string ApiLatest => $"{SiteLink}api/latests";
		private string ApiSearch => $"{SiteLink}api/search";

		public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
		{
			base.LoadValuesFromJson(configJson);
			var releases = await PerformQuery(new TorznabQuery());

			await ConfigureIfOK(string.Empty, releases.Any(),
				() => throw new Exception("Could not find release from this URL."));

			return IndexerConfigurationStatus.Completed;
		}

		private dynamic CheckResponse(WebClientStringResult result)
		{
			try
			{
				var json = JsonConvert.DeserializeObject<dynamic>(result.Content);

				switch (json)
				{
					case JObject _ when json["ok"] != null && (bool) json["ok"] == false:
						throw new Exception("Server error");
					default:
						return json;
				}
			}
			catch (Exception e)
			{
				logger.Error("checkResponse() Error: ", e.Message);
				throw new ExceptionWithConfigData(result.Content, configData);
			}
		}

		private async Task<dynamic> SendApiRequest(IEnumerable<KeyValuePair<string, string>> data)
		{
			//var jsonData = JsonConvert.SerializeObject(data);
			var result = await PostDataWithCookiesAndRetry(ApiSearch, data, null, SiteLink, _apiHeaders, null, true);
			return CheckResponse(result);
		}

		private async Task<dynamic> SendApiRequestLatest()
		{
			var result = await RequestStringWithCookiesAndRetry(ApiLatest, null, SiteLink, _apiHeaders);
			return CheckResponse(result);
		}

		protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
		{
			var releases = new List<ReleaseInfo>();

			var searchString = query.GetQueryString();
			var queryCollection = new List<KeyValuePair<string, string>>();
			var page = 0;

			if (!string.IsNullOrWhiteSpace(searchString))
			{
				queryCollection.Add("term", searchString);
			}
			else
			{
				// no term execute latest search
				var result = await SendApiRequestLatest();

				try
				{
					// this time is a jarray
					var json = (JArray) result;

					releases.AddRange(json.Select(MakeRelease));
				}
				catch (Exception ex)
				{
					OnParseError(result.ToString(), ex);
				}

				return releases;
			}

			var cats = MapTorznabCapsToTrackers(query);
			queryCollection.Add("category", cats.Count > 0 ? string.Join(",", cats) : "0");

			// lazy horrible page initialization
			queryCollection.Add("page", page.ToString());

			do
			{
				// update page number
				queryCollection.RemoveAt(queryCollection.Count - 1); // remove last elem: page number
				queryCollection.Add("page", (++page).ToString());

				var result = await SendApiRequest(queryCollection);
				try
				{
					// this time is a jobject
					var json = (JObject) result;

					if (json["results"] == null)
						throw new Exception("Error invalid JSON response");

					// check number result
					if (!((JArray) json["results"]).Any())
						break;

					releases.AddRange(json["results"].Select(MakeRelease));
				}
				catch (Exception ex)
				{
					OnParseError(result.ToString(), ex);
				}
			} while (page < MaxSearchPageLimit);

			return releases;
		}

		private ReleaseInfo MakeRelease(JToken torrent)
		{
			//https://corsaro.red/details/E5BB62E2E58C654F4450325046723A3F013CD7A4
			var release = new ReleaseInfo
			{
				Title = (string) torrent["title"],
				Grabs = (long) torrent["completed"],
				Description = $"{(string) torrent["category"]} {(string) torrent["description"]}",
				Seeders = (int) torrent["seeders"],
				InfoHash = (string) torrent["hash"],
				MagnetUri = new Uri((string) torrent["magnet"]),
				Comments = new Uri($"{SiteLink}details/{(string) torrent["hash"]}"),
				DownloadVolumeFactor = 0,
				UploadVolumeFactor = 1
			};

			release.Guid = release.Comments;
			release.Peers = release.Seeders + (int) torrent["leechers"];

			release.PublishDate = DateTime.Now;
			if (torrent["last_updated"] != null)
				release.PublishDate = DateTime.Parse((string) torrent["last_updated"]);

			// TODO: don't know how to map this cats..
			var cat = (int) torrent["category"];
			release.Category = MapTrackerCatToNewznab(cat.ToString());

			if (torrent["size"] != null)
				release.Size = (long) torrent["size"];

			return release;
		}
	}
}