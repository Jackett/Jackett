using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using CsQuery;
using Jackett.Utils.Clients;

namespace Jackett.Services
{
    public interface IImdbResolver
    {
        Task<IEnumerable<string>> GetAllTitles(string imdbId);
    }

    public class ImdbResolver : IImdbResolver
    {
        public ImdbResolver(IWebClient webClient)
        {
            WebClient = webClient;
        }

        public async Task<IEnumerable<string>> GetAllTitles(string imdbId)
        {
            if (!imdbId.StartsWith("tt", StringComparison.Ordinal))
                imdbId = "tt" + imdbId;
            var request = new WebRequest("http://www.imdb.com/title/" + imdbId + "/releaseinfo");
            var result = await WebClient.GetString(request);

            CQ dom = result.Content;

            var mainTitle = dom["h3[itemprop=name]"].Find("a")[0].InnerHTML.Replace("\"", "");

            var akas = dom["table#akas"].Find("tbody").Find("tr");
            var titleList = new List<string>();
            titleList.Add(mainTitle);
            foreach (var row in akas) {
                string title = row.FirstElementChild.InnerHTML;
                if (title == "(original title)" || title == "")
                    titleList.Add(HttpUtility.HtmlDecode(row.FirstElementChild.NextElementSibling.InnerHTML));
            }

            return titleList;
        }

        private IWebClient WebClient;
    }
}
