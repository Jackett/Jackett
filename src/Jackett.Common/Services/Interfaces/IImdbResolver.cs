using System.Threading.Tasks;
using Jackett.Utils;

namespace Jackett.Services.Interfaces
{
    public interface IImdbResolver
    {
        Task<Movie> MovieForId(NonNull<string> imdbId);
    }
}
