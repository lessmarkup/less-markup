using System;

namespace LessMarkup.Interfaces.Cache
{
    public abstract class AbstractCacheHandler : ICacheHandler
    {
        private readonly Type[] _handledCollectionTypes;

        protected AbstractCacheHandler(Type [] handledCollectionTypes)
        {
            _handledCollectionTypes = handledCollectionTypes;
        }

        void ICacheHandler.Initialize(long? siteId, long? objectId)
        {
            Initialize(siteId, objectId);
        }

        protected abstract void Initialize(long? siteId, long? objectId);

        bool ICacheHandler.Expires(int collectionId, long entityId, EntityChangeType changeType)
        {
            return Expires(collectionId, entityId, changeType);
        }

        protected virtual bool Expires(int collectionId, long entityId, EntityChangeType changeType)
        {
            return true;
        }

        Type[] ICacheHandler.HandledCollectionTypes { get { return _handledCollectionTypes; } }

        protected bool Expired { get; set; }

        bool ICacheHandler.Expired { get { return Expired; } }
    }
}
