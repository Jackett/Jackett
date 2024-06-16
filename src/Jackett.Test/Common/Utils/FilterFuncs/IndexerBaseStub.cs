using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Indexers;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Test.TestHelpers;
using Newtonsoft.Json.Linq;

namespace Jackett.Test.Common.Utils.FilterFuncs
{
    public abstract class IndexerBaseStub : IIndexer
    {
        public virtual string SiteLink => throw TestExceptions.UnexpectedInvocation;

        public virtual string[] AlternativeSiteLinks => throw TestExceptions.UnexpectedInvocation;

        public virtual string Name => throw TestExceptions.UnexpectedInvocation;

        public virtual string Description => throw TestExceptions.UnexpectedInvocation;

        public virtual string Type => throw TestExceptions.UnexpectedInvocation;

        public virtual string Language => throw TestExceptions.UnexpectedInvocation;

        public virtual bool SupportsPagination => false;

        public virtual string LastError
        {
            get => throw TestExceptions.UnexpectedInvocation;
            set => throw TestExceptions.UnexpectedInvocation;
        }

        public virtual string Id => throw TestExceptions.UnexpectedInvocation;

        public virtual string[] Replaces => throw TestExceptions.UnexpectedInvocation;

        public virtual Encoding Encoding => throw TestExceptions.UnexpectedInvocation;

        public virtual TorznabCapabilities TorznabCaps => throw TestExceptions.UnexpectedInvocation;

        public virtual bool IsConfigured => throw TestExceptions.UnexpectedInvocation;

        public virtual string[] Tags => throw TestExceptions.UnexpectedInvocation;

        public virtual bool IsHealthy => throw TestExceptions.UnexpectedInvocation;

        public virtual bool IsFailing => throw TestExceptions.UnexpectedInvocation;

        public virtual Task<ConfigurationData> GetConfigurationForSetup() => throw TestExceptions.UnexpectedInvocation;

        public virtual Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson) => throw TestExceptions.UnexpectedInvocation;

        public virtual void LoadFromSavedConfiguration(JToken jsonConfig) => throw TestExceptions.UnexpectedInvocation;

        public virtual void SaveConfig() => throw TestExceptions.UnexpectedInvocation;

        public virtual void Unconfigure() => throw TestExceptions.UnexpectedInvocation;

        public virtual Task<IndexerResult> ResultsForQuery(TorznabQuery query, bool isMetaIndexer = false) => throw TestExceptions.UnexpectedInvocation;

        public virtual bool CanHandleQuery(TorznabQuery query) => throw TestExceptions.UnexpectedInvocation;
    }
}
