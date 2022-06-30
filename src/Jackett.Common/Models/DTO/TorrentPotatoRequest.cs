namespace Jackett.Common.Models.DTO
{
    public class TorrentPotatoRequest
    {
        public string Username { get; set; }
        public string Imdbid { get; set; }
        public string Search { get; set; }

        public static TorznabQuery ToTorznabQuery(TorrentPotatoRequest request)
        {
            var torznabQuery = new TorznabQuery()
            {
                Categories = new int[1] { TorznabCatType.Movies.ID },
                SearchTerm = request.Search,
                ImdbID = request.Imdbid,
                QueryType = "TorrentPotato"
            };
            return torznabQuery;
        }
    }
}
