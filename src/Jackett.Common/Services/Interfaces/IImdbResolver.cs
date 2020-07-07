using System.Threading.Tasks;

namespace Jackett.Common.Services.Interfaces
{
    public interface IImdbResolver
    {
        Task<Movie> MovieForId(string imdbId);
    }
}
