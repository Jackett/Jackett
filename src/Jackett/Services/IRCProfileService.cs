using CuttingEdge.Conditions;
using Jackett.Models.Irc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Services
{
    public interface IIRCProfileService
    {
        IRCProfile Get(string name);
        void Set(IRCProfile profile);
        List<IRCProfile> All { get; }
    }

    public class IRCProfileService: IIRCProfileService
    {
        private List<IRCProfile> profiles = new List<IRCProfile>();

        public IRCProfile Get(string name)
        {
            return profiles.Where(p => string.Equals(p.Name, name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
        }

        public void Set(IRCProfile profile)
        {
            Condition.Requires<string>(profile.Name, "Name").IsNotNullOrWhiteSpace();
            var existing = Get(profile.Name);
            if (existing != null)
            {
                profiles.Remove(existing);
            }

            profiles.Add(profile);
        }

        public List<IRCProfile> All
        {
            get
            {
                return profiles;
            }
        }
    }
}
