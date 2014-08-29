using System;
using System.Linq;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.UserInterface.NodeHandlers.Common
{
    public abstract class RecordListWithNotifyNodeHandler<T> : RecordListNodeHandler<T>, INotificationProvider where T : class
    {
        private readonly IDomainModelProvider _domainModelProvider;

        protected RecordListWithNotifyNodeHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache, ICurrentUser currentUser) : base(domainModelProvider, dataCache, currentUser)
        {
            _domainModelProvider = domainModelProvider;
        }

        public abstract string Title { get; }
        public abstract string Tooltip { get; }
        public abstract string Icon { get; }

        public long? Version
        {
            get
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    var update = GetRecordUpdates(GetCollection(), domainModel, null, null, true, true)
                        .OrderByDescending(h => h.Id)
                        .FirstOrDefault();

                    if (update == null)
                    {
                        return null;
                    }

                    return update.Id;
                }
            }
        }

        public Tuple<int, long?> GetCountAndVersion(long? lastVersion, IDomainModel domainModel)
        {
            var query = GetRecordUpdates(GetCollection(), domainModel, null, lastVersion, true, true);

            var count = query.Select(h => h.EntityId).Distinct().Count();
            var newVersion = query.OrderByDescending(h => h.Id).FirstOrDefault();

            return Tuple.Create(count, newVersion != null ? newVersion.Id : (long?) null);
        }

        protected override bool SupportsLiveUpdates
        {
            get { return false; }
        }

        protected override bool SupportsManualRefresh
        {
            get { return true; }
        }
    }
}
