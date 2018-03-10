using System.Threading.Tasks;
using Jackett.Common.Utils;

namespace Jackett.Common.Services.Interfaces
{
    public interface IImdbResolver
    {
        Task<Movie> MovieForId(NonNull<string> imdbId);
    }
}
