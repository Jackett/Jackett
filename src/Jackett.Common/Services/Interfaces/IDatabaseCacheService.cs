using System;
using System.Collections.Generic;
using System.Text;

namespace Jackett.Common.Services.Interfaces
{
    public interface IDatabaseCacheService : ICacheService
    {
        void Initialize();
    }
}
