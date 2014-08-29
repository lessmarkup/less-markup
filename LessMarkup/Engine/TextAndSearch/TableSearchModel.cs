/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using LessMarkup.Interfaces.Module;

namespace LessMarkup.Engine.TextAndSearch
{
    class TableSearchModel
    {
        public string TableName { get; set; }
        public string Name { get; set; }
        public List<string> Columns { get; set; }
        public bool FullTextEnabled { get; set; }
        public string Query { get; set; }
        public string IdColumn { get; set; }
        public string UpdatedColumn { get; set; }
        public string CreatedColumn { get; set; }
        public string SiteIdColumn { get; set; }
        public int CollectionId { get; set; }
        public IEntitySearch EntitySearch { get; set; }

        public void Initialize(int fieldCount)
        {
            string updatedQuery;

            if (!string.IsNullOrWhiteSpace(CreatedColumn))
            {
                if (!string.IsNullOrWhiteSpace(UpdatedColumn))
                {
                    updatedQuery = string.Format(", ISNULL(t.{0}, t.{1}) as [updated]", UpdatedColumn, CreatedColumn);
                }
                else
                {
                    updatedQuery = string.Format(", t.{0} as [updated]", CreatedColumn);
                }
            }
            else if (!string.IsNullOrWhiteSpace(UpdatedColumn))
            {
                updatedQuery = string.Format(", t.{0} as [updated]", UpdatedColumn);
            }
            else
            {
                updatedQuery = ", DATEFROMPARTS(1990, 1, 1) as [updated]";
            }

            if (FullTextEnabled)
            {
                Query = string.Format("select s.[key], s.[rank], {0} as [type]" + updatedQuery, CollectionId);
                for (int i = 0; i < fieldCount; i++)
                {
                    var fieldName = string.Format("text{0}", i + 1);

                    if (i < Columns.Count)
                    {
                        Query = Query + string.Format(", t.{0} as {1}", Columns[i], fieldName);
                    }
                    else
                    {
                        Query = Query + string.Format(", '' as {0}", fieldName);
                    }
                }

                Query = Query + string.Format(" from containstable({0}, *, @{2}) s, {0} t where s.[key] = t.{1} and t.[SiteId] = @{3}", TableName, IdColumn,
                    SearchModelCache.FulltextSearchParameter, SearchModelCache.SiteIdSearchParameter);
            }
            else
            {
                Query = string.Format("select t.[{0}] as [key], 50 as [rank], {1} as [type] {2}", IdColumn, CollectionId, updatedQuery);
                
                for (int i = 0; i < fieldCount; i++)
                {
                    var fieldName = string.Format("text{0}", i + 1);

                    if (i < Columns.Count)
                    {
                        Query = Query + string.Format(", t.{0} as {1}", Columns[i], fieldName);
                    }
                    else
                    {
                        Query = Query + string.Format(", '' as {0}", fieldName);
                    }
                }

                Query = Query + string.Format(" from {0} t where t.[SiteId] = @{1} and (", TableName, SearchModelCache.SiteIdSearchParameter);

                for (var i = 0; i < Columns.Count; i++)
                {
                    if (i > 0)
                    {
                        Query = Query + " or";
                    }

                    Query = Query + string.Format(" upper([{0}]) like @{1}", Columns[i], SearchModelCache.LikeSearchParameter);
                }

                Query += ") ";
            }
        }
    }
}
