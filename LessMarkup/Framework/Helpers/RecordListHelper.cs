using System;
using System.Collections.Generic;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LessMarkup.Interfaces.RecordModel;
using Newtonsoft.Json;

namespace LessMarkup.Framework.Helpers
{
    public static class RecordListHelper
    {
        public static string PageLink(string baseUrl, int page)
        {
            return page == 1 ? baseUrl : string.Format("{0}?p={1}", baseUrl, page);
        }

        public static string LastPageLink(string baseUrl)
        {
            return string.Format("{0}?p=last", baseUrl);
        }

        private static IQueryable<TR> GetFilterQuery<TR>(IQueryable<TR> sourceQuery, string searchText, Type modelType)
        {
            var sourceProperties = new HashSet<string>(modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<UseInSearchAttribute>() != null && p.PropertyType == typeof(string))
                .Select(p => p.Name));

            if (sourceProperties.Count == 0)
            {
                return sourceQuery;
            }

            var propertiesToSearch = typeof(TR).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => sourceProperties.Contains(p.Name))
                .Select(p => p.Name)
                .ToList();

            if (propertiesToSearch.Count == 0)
            {
                return sourceQuery;
            }

            var parameter = Expression.Parameter(typeof(TR), "x");
            var constant = Expression.Constant(searchText, typeof(string));
            var zero = Expression.Convert(Expression.Constant(0, typeof(int)), typeof(int?));
            var patIndex = typeof(SqlFunctions).GetMethod("PatIndex");

            Expression mergedExpression = null;

            foreach (var property in propertiesToSearch)
            {
                var propertyExpression = Expression.MakeMemberAccess(parameter, typeof(TR).GetProperty(property));
                var callToPatIndex = Expression.Call(patIndex, constant, propertyExpression);
                var compareExpression = Expression.GreaterThan(callToPatIndex, zero);

                if (mergedExpression == null)
                {
                    mergedExpression = compareExpression;
                }
                else
                {
                    mergedExpression = Expression.Or(mergedExpression, compareExpression);
                }
            }

            if (mergedExpression == null)
            {
                return sourceQuery;
            }

            var lambda = Expression.Lambda<Func<TR, bool>>(mergedExpression, parameter);

            return sourceQuery.Where(lambda);
        }

        // It is called by reflection
        // ReSharper disable once UnusedMember.Local
        private static IQueryable<TR> GetOrderPropertyInternal<TR, TP>(IQueryable<TR> sourceQuery, bool ascending, Expression expression, ParameterExpression parameter)
        {
            var lambda = Expression.Lambda<Func<TR, TP>>(expression, parameter);
            return ascending ? sourceQuery.OrderBy(lambda) : sourceQuery.OrderByDescending(lambda);
        }

        private static IQueryable<TR> GetOrderQuery<TR>(IQueryable<TR> sourceQuery, string orderBy, bool ascending)
        {
            var property = typeof(TR).GetProperty(orderBy);

            if (property == null)
            {
                return sourceQuery;
            }

            var parameter = Expression.Parameter(typeof(TR), "x");

            var propertyExpression = Expression.MakeMemberAccess(parameter, property);

            var method = typeof(RecordListHelper).GetMethod("GetOrderPropertyInternal");
            method = method.MakeGenericMethod(typeof(TR), property.PropertyType);

            return (IQueryable<TR>)method.Invoke(null, new object[] { sourceQuery, ascending, propertyExpression, parameter });
        }

        public static IQueryable<TR> GetFilterAndOrderQuery<TR>(IQueryable<TR> sourceQuery, string filter, Type modelType)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return sourceQuery;
            }

            var searchProperties = JsonConvert.DeserializeObject<Dictionary<string, object>>(filter);

            object searchObject;
            if (searchProperties.TryGetValue("search", out searchObject))
            {
                var searchText = "%" + searchObject.ToString().Trim() + "%";
                sourceQuery = GetFilterQuery(sourceQuery, searchText, modelType);
            }

            object orderByObject;
            object directionObject;
            if (searchProperties.TryGetValue("orderBy", out orderByObject) && searchProperties.TryGetValue("direction", out directionObject))
            {
                var orderBy = orderByObject.ToString();
                var ascending = directionObject.ToString() == "asc";
                sourceQuery = GetOrderQuery(sourceQuery, orderBy, ascending);
            }

            return sourceQuery;
        }
    }
}
