using System.Linq;
using AutoMapper;
using Jackett.Common.Models;
using Jackett.Common.Utils.Clients;

namespace Jackett.Common.Utils
{
    public static class MapperUtil
    {
        public static Mapper Mapper = new Mapper(
            new MapperConfiguration(
                cfg =>
                {
                    cfg.CreateMap<WebResult, WebResult>();

                    cfg.CreateMap<ReleaseInfo, ReleaseInfo>();

                    cfg.CreateMap<ReleaseInfo, TrackerCacheResult>().AfterMap((r, t) =>
                    {
                        t.CategoryDesc = r.Category != null
                            ? string.Join(", ", r.Category.Select(x => TorznabCatType.GetCatDesc(x)).Where(x => !string.IsNullOrEmpty(x)))
                            : "";
                    });
                }));
    }
}
