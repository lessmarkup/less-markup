/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataFramework.DataAccess
{
    public class SiteDataObjectCollection<T> : DataObjectCollection<T> where T : class, ISiteDataObject
    {
        private readonly IQueryable<T> _restrictedSet;
        private readonly long? _siteId;

        public SiteDataObjectCollection(DbSet<T> set, long? siteId) : base(set)
        {
            _siteId = siteId;

            _restrictedSet = siteId.HasValue ?
                set.Where(t => t.SiteId.HasValue && t.SiteId.Value == siteId.Value) :
                set.Where(t => !t.SiteId.HasValue);
        }

        public override IEnumerator<T> GetEnumerator()
        {
            return _restrictedSet.GetEnumerator();
        }

        public override T Add(T entity)
        {
            entity.SiteId = _siteId;
            return base.Add(entity);
        }

        public override T Remove(T entity)
        {
            if (!entity.SiteId.HasValue || entity.SiteId != _siteId)
            {
                throw new ArgumentException("SiteId");
            }
            return base.Remove(entity);
        }

        public override T Attach(T entity)
        {
            if (entity.SiteId != _siteId)
            {
                throw new ArgumentException("SiteId");
            }
            return base.Attach(entity);
        }

        public override T Create()
        {
            var ret = base.Create();
            ret.SiteId = _siteId;
            return ret;
        }

        public override TDerivedEntity Create<TDerivedEntity>()
        {
            var ret = base.Create<TDerivedEntity>();
            ret.SiteId = _siteId;
            return ret;
        }

        public override IList GetList()
        {
            return _restrictedSet.ToList();
        }

        public override Type ElementType
        {
            get { return _restrictedSet.ElementType; }
        }

        public override Expression Expression
        {
            get { return _restrictedSet.Expression; }
        }

        public override IQueryProvider Provider
        {
            get { return _restrictedSet.Provider; }
        }
    }
}
