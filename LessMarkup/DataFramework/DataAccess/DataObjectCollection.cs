/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataFramework.DataAccess
{
    public class DataObjectCollection<T> : IDataObjectCollection<T> where T : class, IDataObject
    {
        private readonly DbSet<T> _set;
        private readonly int _collectionId;

        public DataObjectCollection(DbSet<T> set, int collectionId)
        {
            _set = set;
            _collectionId = collectionId;
        }

        protected DbSet<T> Set { get { return _set; }}

        public virtual IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)_set).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public virtual Expression Expression { get { return ((IQueryable<T>)_set).Expression; } }
        public virtual Type ElementType { get { return ((IQueryable<T>) _set).ElementType; } }
        public virtual IQueryProvider Provider { get { return ((IQueryable<T>) _set).Provider;} }

        public T Find(params object[] keyValues)
        {
            return _set.Find(keyValues);
        }

        public virtual T Add(T entity)
        {
            return _set.Add(entity);
        }

        public virtual T Remove(T entity)
        {
            return _set.Remove(entity);
        }

        public virtual T Attach(T entity)
        {
            return _set.Attach(entity);
        }

        public virtual T Create()
        {
            return _set.Create();
        }

        public virtual TDerivedEntity Create<TDerivedEntity>() where TDerivedEntity : class, T
        {
            return _set.Create<TDerivedEntity>();
        }

        public override string ToString()
        {
            return _set.ToString();
        }

        public ObservableCollection<T> Local { get { return _set.Local; } }

        public virtual IList GetList()
        {
            return ((IListSource)_set).GetList();
        }

        public bool ContainsListCollection { get { return ((IListSource) _set).ContainsListCollection; } }

        public DbSet<T> InnerCollection { get { return _set; } }
        public int CollectionId { get { return _collectionId; } }
    }
}
