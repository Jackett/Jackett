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
        private readonly WebClient _webClient;
        private readonly string _apiKey;
        private readonly string _apiUrl;

        public OmdbResolver(WebClient webClient, string omdbApiKey, string omdbApiApiUrl)
        {
            _webClient = webClient;
            _apiKey = omdbApiKey ?? throw new ArgumentNullException($"{nameof(omdbApiKey)} cannot be null");
            _apiUrl = omdbApiApiUrl;
        }

        public async Task<Movie> MovieForId(string id)
        {
            var imdbId = id ?? throw new ArgumentNullException($"{nameof(id)} cannot be null");

            if (!imdbId.StartsWith("tt", StringComparison.Ordinal))
            {
                imdbId = $"tt{imdbId}";
            }

            if (string.IsNullOrWhiteSpace(_apiUrl) || !Uri.TryCreate(_apiUrl, UriKind.Absolute, out var baseUri))
            {
                baseUri = new Uri("https://omdbapi.com");
            }

            var request = new WebRequest(new Uri(baseUri, $"/?apikey={_apiKey}&i={imdbId}").AbsoluteUri)
            {
                Encoding = Encoding.UTF8
            };
            var result = await _webClient.GetResultAsync(request);
            var movie = JsonConvert.DeserializeObject<Movie>(result.ContentString);

            return movie;
        }
    }
}
