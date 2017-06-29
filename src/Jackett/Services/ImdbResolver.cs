using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using CsQuery;
using Jackett.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jackett.Services
{
    public interface IImdbResolver
    {
        Task<IEnumerable<string>> GetAllTitles(string imdbId);
    }

    public struct Movie
    {
        public string Title;
    }

    public class OmdbResolver : IImdbResolver
    {
        public OmdbResolver(IWebClient webClient, string omdbApiKey)
        {
            WebClient = webClient;
            apiKey = omdbApiKey;
        }

        public async Task<IEnumerable<string>> GetAllTitles(string imdbId)
        {
            if (apiKey == null)
                return new string[] { };

            if (!imdbId.StartsWith("tt", StringComparison.Ordinal))
                imdbId = "tt" + imdbId;
            var request = new WebRequest("http://omdbapi.com/?apikey=" + apiKey + "&i=" + imdbId);
            var result = await WebClient.GetString(request);
            var movie = JsonConvert.DeserializeObject<Movie>(result.Content);

            return new string[] { movie.Title };
        }

        private IWebClient WebClient;
        private string apiKey;
    }
}
