/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Reflection;
using System.Transactions;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.System;

namespace LessMarkup.DataFramework.DataAccess
{
    public abstract class AbstractDomainModel : DbContext, IDomainModel
    {
        private readonly List<Type> _modelCreateTypes = new List<Type>();
        private static int _collectionId;
        private readonly static Dictionary<Type, int> _collectionTypeToId = new Dictionary<Type, int>(); 
        private readonly static Dictionary<int, Type> _collectionIdToType = new Dictionary<int, Type>(); 
        private readonly static Dictionary<Type, PropertyInfo> _collectionProperties = new Dictionary<Type, PropertyInfo>();
        private readonly static Dictionary<Type, PropertyInfo> _siteCollectionProperties = new Dictionary<Type, PropertyInfo>();

        private readonly Dictionary<Type, object> _collections = new Dictionary<Type, object>();
        private readonly Dictionary<Type, object> _siteCollections = new Dictionary<Type, object>();

        private long? _siteId;
        private bool _siteIdSet;
        private TransactionScope _transactionScope;

        private static string GetDatabase()
        {
            return DependencyResolver.Resolve<IEngineConfiguration>().Database;
        }

        protected AbstractDomainModel() : base(GetDatabase())
        {
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
            Configuration.ValidateOnSaveEnabled = false;
        }

        protected AbstractDomainModel(long? siteId) : base(GetDatabase())
        {
            _siteId = siteId;
            _siteIdSet = true;
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
            Configuration.ValidateOnSaveEnabled = false;
        }

        internal void SetSiteId(ISiteMapper siteMapper)
        {
            if (!_siteIdSet)
            {
                _siteId = siteMapper.SiteId;
                _siteIdSet = true;
            }
        }

        internal void SetSiteId(long? siteId)
        {
            _siteId = siteId;
            _siteIdSet = true;
        }

        protected void AddModelCreate(Type type)
        {
            _modelCreateTypes.Add(type);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var type in _modelCreateTypes)
            {
                var constructorInfo = type.GetConstructor(Type.EmptyTypes);
                if (constructorInfo == null)
                {
                    continue;
                }
                var modelCreate = (IModelCreate) constructorInfo.Invoke(null);
                modelCreate.OnModelCreating(modelBuilder);
            }

            var methodInfo = GetType().GetMethod("ConfigureProperty", BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var property in _siteCollectionProperties)
            {
                var propertyMethod = methodInfo.MakeGenericMethod(new[] {property.Key});
                propertyMethod.Invoke(this, new[] { (object)modelBuilder, property.Value });
            }
        }

        protected void ConfigureProperty<T>(DbModelBuilder modelBuilder, PropertyInfo property) where T : SiteDataObject
        {
            modelBuilder.Entity<T>().HasRequired(p => p.Site).WithMany().WillCascadeOnDelete(false);
        }

        protected static void AddProperty(Type type, string propertyName)
        {
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            var genericTypes = property.PropertyType.GenericTypeArguments;
            if (genericTypes.Length != 1)
            {
                return;
            }

            var propertyType = genericTypes[0];

            if (typeof (SiteDataObject).IsAssignableFrom(propertyType))
            {
                _siteCollectionProperties[propertyType] = property;
            }
            else if (typeof(NonSiteDataObject).IsAssignableFrom(propertyType))
            {
                _collectionProperties[propertyType] = property;
            }

            var collectionId = ++_collectionId;

            _collectionTypeToId[propertyType] = collectionId;
            _collectionIdToType[collectionId] = propertyType;
        }

        public void AddObject<T>(T newObject) where T : class, INonSiteDataObject
        {
            GetCollection<T>().Add(newObject);
        }

        public static int GetCollectionId<T>() where T : IDataObject
        {
            return _collectionTypeToId[typeof (T)];
        }

        public static int GetCollectionId(Type type)
        {
            return _collectionTypeToId[type];
        }

        public static Type GetCollectionType(int collectionId)
        {
            return _collectionIdToType[collectionId];
        }

        public void AddSiteObject<T>(T newObject) where T : class, ISiteDataObject
        {
            GetSiteCollection<T>().Add(newObject);
        }

        public IDataObjectCollection<T> GetSiteCollection<T>() where T : class, ISiteDataObject
        {
            if (!_siteId.HasValue)
            {
                return new DataObjectCollectionStub<T>();
            }

            object ret;
            if (_siteCollections.TryGetValue(typeof(T), out ret))
            {
                return (SiteDataObjectCollection<T>)ret;
            }

            var innerCollection = (DbSet<T>)_siteCollectionProperties[typeof(T)].GetValue(this);

            var collection = new SiteDataObjectCollection<T>(innerCollection, _siteId.Value, _collectionTypeToId[typeof(T)]);
            _siteCollections[typeof(T)] = collection;

            return collection;
        }

        public IDataObjectCollection<T> GetCollection<T>() where T: class, INonSiteDataObject
        {
            object ret;
            if (_collections.TryGetValue(typeof (T), out ret))
            {
                return (DataObjectCollection<T>) ret;
            }

            var innerCollection = (DbSet<T>) _collectionProperties[typeof (T)].GetValue(this);

            var collection = new DataObjectCollection<T>(innerCollection, _collectionTypeToId[typeof(T)]);
            _collections[typeof (T)] = collection;

            return collection;
        }

        public IDataObjectCollection<T> GetSiteCollection<T>(long? siteId, bool rawCollection = false) where T : class, ISiteDataObject
        {
            var innerCollection = (DbSet<T>)_siteCollectionProperties[typeof(T)].GetValue(this);

            if (rawCollection)
            {
                return new DataObjectCollection<T>(innerCollection, _collectionTypeToId[typeof(T)]);
            }

            return new SiteDataObjectCollection<T>(innerCollection, siteId, _collectionTypeToId[typeof(T)]);
        }

        public void CreateTransaction()
        {
            if (_transactionScope == null)
            {
                _transactionScope = new TransactionScope();
            }
        }

        public void CompleteTransaction()
        {
            if (_transactionScope == null)
            {
                throw new InvalidOperationException("Cannot complete empty transaction");
            }

            _transactionScope.Complete();
        }

        int IDomainModel.SaveChanges()
        {
            return SaveChanges();
        }

        void IDisposable.Dispose()
        {
            Dispose();

            if (_transactionScope != null)
            {
                _transactionScope.Dispose();
                _transactionScope = null;
            }
        }

        public void ImprovePerformance()
        {
            Configuration.ValidateOnSaveEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }

        public bool AutoDetectChangesEnabled
        {
            get { return Configuration.AutoDetectChangesEnabled; }
            set { Configuration.AutoDetectChangesEnabled = value; }
        }
    }
}
