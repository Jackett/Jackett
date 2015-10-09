using CsQuery;
using Jackett.Indexers;
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
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.WebControls;
using Jackett.Models.IndexerConfig;

namespace Jackett.Indexers
{
	public class Hounddawgs : BaseIndexer, IIndexer
	{
		private string LoginUrl { get { return SiteLink + "login.php"; } }
		private string SearchUrl { get { return SiteLink + "torrents.php"; } }

		new ConfigurationDataBasicLogin configData
		{
			get { return (ConfigurationDataBasicLogin)base.configData; }
			set { base.configData = value; }
		}

		public Hounddawgs(IIndexerManagerService i, Logger l, IWebClient c, IProtectionService ps)
			: base(name: "Hounddawgs",
				description: "A danish closed torrent tracker",
				link: "https://hounddawgs.org/",
				caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
				manager: i,
				client: c,
				logger: l,
				p: ps,
				configData: new ConfigurationDataBasicLogin())
		{
		}

		public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
		{
			configData.LoadValuesFromJson(configJson);
			var pairs = new Dictionary<string, string> {
				{ "username", configData.Username.Value },
				{ "password", configData.Password.Value },
				{ "Submit", "Submit" }

			};
			// Get inital cookies
			var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl);

			await ConfigureIfOK(response.Cookies, response.Content != null && response.Content.Contains("Velkommen til vores seneste bruger"), () =>
				{
					CQ dom = response.Content;
					var messageEl = dom["inputs"];
					var errorMessage = messageEl.Text().Trim();
					throw new ExceptionWithConfigData(errorMessage, configData);
				});
			return IndexerConfigurationStatus.RequiresTesting;
		}

		public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
		{
			var releases = new List<ReleaseInfo>();
			var episodeSearchUrl = string.Format("{0}?&searchstr={1}", SearchUrl, HttpUtility.UrlEncode(query.GetQueryString()));
				var results = await RequestStringWithCookiesAndRetry(episodeSearchUrl);
			if (results.Content.Contains("Din søgning gav intet resultat."))
			{
				return releases;
			}
				try
				{
					CQ dom = results.Content;

				var rows = dom["#torrent_table > tbody > tr"].ToArray();

					foreach (var row in rows.Skip(1))
					{
					var release = new ReleaseInfo();
					release.MinimumRatio = 1;
					release.MinimumSeedTime = 172800;

					release.PublishDate = DateTime.Now;

//					var qAdded = row.ChildElements.ElementAt(4).Cq();
//					var addedStr = qAdded.Attr("title").Trim();
//					//var addedStr = "Oct 02 2015, 03:55";
//					//release.PublishDate = DateTime.ParseExact(addedStr, "MMM dd yyyy, HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();

					var qLink = row.ChildElements.ElementAt(1).ChildElements.ElementAt(2).Cq();
					release.Title = qLink.Text().Trim();
					release.Description = release.Title;

					release.Comments = new Uri(SiteLink + qLink.Attr("href"));
					release.Guid = release.Comments;

					var qDownload = row.ChildElements.ElementAt(1).ChildElements.ElementAt(1).ChildElements.ElementAt(0).Cq();
					release.Link = new Uri(SiteLink + qDownload.Attr("href"));

					var sizeStr = row.ChildElements.ElementAt(5).Cq().Text();
					release.Size = ReleaseInfo.GetBytes(sizeStr);

					release.Seeders = ParseUtil.CoerceInt(row.ChildElements.ElementAt(6).Cq().Text());
					release.Peers = ParseUtil.CoerceInt(row.ChildElements.ElementAt(7).Cq().Text()) + release.Seeders;

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
