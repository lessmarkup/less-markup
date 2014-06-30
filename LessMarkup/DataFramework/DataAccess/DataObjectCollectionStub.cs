/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataFramework.DataAccess
{
    class DataObjectCollectionStub<T> : IDataObjectCollection<T> where T: class
    {
        private readonly List<T> _collection = new List<T>();

        public IEnumerator<T> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Expression Expression { get { return _collection.AsQueryable().Expression; } }
        public Type ElementType { get { return _collection.AsQueryable().ElementType; } }
        public IQueryProvider Provider { get { return _collection.AsQueryable().Provider; } }
        public T Find(params object[] keyValues)
        {
            return null;
        }

        public T Add(T entity)
        {
            return entity;
        }

        public T Remove(T entity)
        {
            return entity;
        }

        public T Attach(T entity)
        {
            return entity;
        }

        public T Create()
        {
            return DependencyResolver.Resolve<T>();
        }

        public TDerivedEntity Create<TDerivedEntity>() where TDerivedEntity : class, T
        {
            return null;
        }

        public ObservableCollection<T> Local { get { return null; } }
        public IList GetList()
        {
            return _collection;
        }

        public bool ContainsListCollection { get { return false; } }
        public DbSet<T> InnerCollection { get { return null; } }
    }
}
