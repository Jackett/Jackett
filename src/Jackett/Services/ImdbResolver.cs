using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using CsQuery;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jackett.Services
{
    public interface IImdbResolver
    {
        Task<Movie> MovieForId(NonNull<string> imdbId);
    }

    public struct Movie
    {
        public string Title;
        public string Year;
    }

    public class OmdbResolver : IImdbResolver
    {
        public OmdbResolver(IWebClient webClient, NonNull<string> omdbApiKey)
        {
            WebClient = webClient;
            apiKey = omdbApiKey;
        }

        public async Task<Movie> MovieForId(NonNull<string> id)
        {
            string imdbId = id;

            if (!imdbId.StartsWith("tt", StringComparison.Ordinal))
                imdbId = "tt" + imdbId;

            var request = new WebRequest("http://omdbapi.com/?apikey=" + apiKey + "&i=" + imdbId);
            var result = await WebClient.GetString(request);
            var movie = JsonConvert.DeserializeObject<Movie>(result.Content);

            return movie;
        }

        private IWebClient WebClient;
        private string apiKey;
    }
}
