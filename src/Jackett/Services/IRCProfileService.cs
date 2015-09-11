using CuttingEdge.Conditions;
using Jackett.Models.Commands.IRC;
using Jackett.Models.Irc;
using MediatR;
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
        void Delete(string name);
        void Load();
    }

    public class IRCProfileService: IIRCProfileService
    {
        List<IRCProfile> profiles = new List<IRCProfile>();
        IMediator mediator;
        IIDService idService;
        IConfigurationService configService;

        public IRCProfileService(IMediator m, IIDService i, IConfigurationService c)
        {
            mediator = m;
            idService = i;
            configService = c;
        }

        public void Save()
        {
            var config = new IRCProfiles()
            {
                Profiles = this.profiles
            };

            configService.SaveConfig<IRCProfiles>(config);
        }

        public void Load()
        {
            var config = configService.GetConfig<IRCProfiles>();
            if (config != null)
            {
                this.profiles = config.Profiles;
            }
        }

        public IRCProfile Get(string id)
        {
            return profiles.Where(p => string.Equals(p.Id, id, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
        }

        public void Set(IRCProfile profile)
        {
            Condition.Requires<string>(profile.Name, "Name").IsNotNullOrWhiteSpace();

            if (profile.Id == null)
            {
                profile.Id = idService.NewId();
            }
            else
            {
                var existing = Get(profile.Id);
                if (existing != null)
                {
                    profiles.Remove(existing);
                }
            }

            profiles.Add(profile);
            mediator.Publish(new AddProfileEvent() { Profile = profile });
            Save();
        }

        public List<IRCProfile> All
        {
            get
            {
                return profiles;
            }
        }

        public void Delete(string name)
        {
            var existing = Get(name);
            if (existing != null)
            {
                profiles.Remove(existing);
            }

            Save();
        }
    }
}
