using System;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;

namespace Jackett.Common.Services
{

    public struct Movie
    {
        public string Title;
        public string Year;
    }

    public class OmdbResolver : IImdbResolver
    {
        public OmdbResolver(WebClient webClient, string omdbApiKey, string omdbApiUrl)
        {
            WebClient = webClient;
            apiKey = omdbApiKey ?? throw new ArgumentNullException($"{nameof(omdbApiKey)} cannot be null");
            url = omdbApiUrl;
        }

        public async Task<Movie> MovieForId(string id)
        {
            var imdbId = id ?? throw new ArgumentNullException($"{nameof(id)} cannot be null");

            if (!imdbId.StartsWith("tt", StringComparison.Ordinal))
                imdbId = "tt" + imdbId;

            if (string.IsNullOrWhiteSpace(url))
                url = "http://omdbapi.com";

            var request = new WebRequest(url + "/?apikey=" + apiKey + "&i=" + imdbId)
            {
                Encoding = Encoding.UTF8
            };
            var result = await WebClient.GetResultAsync(request);
            var movie = JsonConvert.DeserializeObject<Movie>(result.ContentString);

            return movie;
        }

        private readonly WebClient WebClient;
        private readonly string apiKey;
        private string url;
    }
}
