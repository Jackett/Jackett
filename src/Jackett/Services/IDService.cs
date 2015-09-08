using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Services
{
    public interface IIDService
    {
        string NewId();
    }

    class IDService : IIDService
    {
        public string NewId()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
