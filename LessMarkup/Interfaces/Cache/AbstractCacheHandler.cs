using System.Linq;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Interfaces.Cache
{
    public abstract class AbstractCacheHandler : ICacheHandler
    {
        private readonly EntityType[] _handledTypes;

        protected AbstractCacheHandler(EntityType[] handledTypes)
        {
            _handledTypes = handledTypes;
        }

        void ICacheHandler.Initialize(long? siteId, long? objectId)
        {
            Initialize(siteId, objectId);
        }

        protected abstract void Initialize(long? siteId, long? objectId);

        bool ICacheHandler.Expires(EntityType entityType, long entityId, EntityChangeType changeType)
        {
            return Expires(entityType, entityId, changeType);
        }

        protected virtual bool Expires(EntityType entityType, long entityId, EntityChangeType changeType)
        {
            return _handledTypes != null && _handledTypes.Contains(entityType);
        }

        EntityType[] ICacheHandler.HandledTypes { get { return _handledTypes; } }

        protected bool Expired { get; set; }

        bool ICacheHandler.Expired { get { return Expired; } }
    }
}
