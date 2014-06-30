/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataFramework;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.Exceptions;

namespace LessMarkup.UserInterface.Model.Structure
{
    public class TypeaheadModel
    {
        public List<string> Records { get; set; }

        private readonly IDataCache _dataCache;
        private readonly IDomainModelProvider _domainModelProvider;

        public TypeaheadModel(IDataCache dataCache, IDomainModelProvider domainModelProvider)
        {
            _dataCache = dataCache;
            _domainModelProvider = domainModelProvider;
        }

        public void Initialize(string path, string property, string searchText)
        {
            var pageCache = _dataCache.Get<PageCache>();

            CachedPageInformation page;
            string rest;
            pageCache.GetPage(path, out page, out rest);
            if (page == null)
            {
                throw new UnknownActionException();
            }

            var collectionManagers = new List<IPropertyCollectionManager>();

            var handler = (IPageHandler) DependencyResolver.Resolve(page.HandlerType);

            if (handler is IPropertyCollectionManager)
            {
                collectionManagers.Add(handler as IPropertyCollectionManager);
            }

            while (!string.IsNullOrWhiteSpace(rest))
            {
                var childSettings = handler.GetChildHandler(rest);
                if (childSettings == null || !childSettings.Id.HasValue)
                {
                    throw new UnknownActionException();
                }
                handler = childSettings.Handler;
                if (handler is IPropertyCollectionManager)
                {
                    collectionManagers.Add(handler as IPropertyCollectionManager);
                }
                rest = childSettings.Rest;
            }

            if (collectionManagers.Any())
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    foreach (var collectionManager in collectionManagers)
                    {
                        var collection = collectionManager.GetCollection(domainModel, property, searchText);
                        if (collection == null)
                        {
                            continue;
                        }

                        Records = collection.Take(10).ToList();
                        return;
                    }
                }
            }

            throw new ArgumentException("Unknown collection");
        }
    }
}
