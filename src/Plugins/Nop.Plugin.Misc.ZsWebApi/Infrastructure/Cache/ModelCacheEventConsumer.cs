using System.Threading.Tasks;
using Nop.Core.Caching;
using Nop.Core.Domain.Configuration;
using Nop.Core.Events;
using Nop.Services.Events;

namespace Nop.Plugin.Misc.ZsWebApi.Infrastructure.Cache
{
    /// <summary>
    /// Model cache event consumer (used for caching of presentation layer models)
    /// </summary>
    public partial class ModelCacheEventConsumer :
        IConsumer<EntityInsertedEvent<Setting>>,
        IConsumer<EntityUpdatedEvent<Setting>>,
        IConsumer<EntityDeletedEvent<Setting>>
    {
        private readonly IStaticCacheManager _staticCacheManager;

        /// <summary>
        /// Key for caching all cross integrations
        /// </summary>
        public static CacheKey CrossIntegrationRecord_ALL_KEY = new CacheKey("Nop.plugins.Misc.ZsWebApi.CrossIntegrationRecord.all-{0}-{1}-{2}", CrossIntegrationRecord_PATTERN_KEY);
        public const string CrossIntegrationRecord_PATTERN_KEY = "Nop.plugins.Misc.ZsWebApi.CrossIntegrationRecord";

        public ModelCacheEventConsumer(IStaticCacheManager staticCacheManager)
        {
            _staticCacheManager = staticCacheManager;
        }

        public async Task HandleEventAsync(EntityInsertedEvent<Setting> eventMessage)
        {
        }
        public async Task HandleEventAsync(EntityUpdatedEvent<Setting> eventMessage)
        {
        }
        public async Task HandleEventAsync(EntityDeletedEvent<Setting> eventMessage)
        {
        }
    }
}