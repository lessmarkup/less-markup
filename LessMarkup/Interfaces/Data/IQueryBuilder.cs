using System.Collections.Generic;

namespace LessMarkup.Interfaces.Data
{
    public interface IQueryBuilder
    {
        IQueryBuilder From<T>(string name = null) where T : IDataObject;
        IQueryBuilder Join<T>(string name, string @on) where T : IDataObject;
        IQueryBuilder LeftJoin<T>(string name, string @on) where T : IDataObject;
        IQueryBuilder RightJoin<T>(string name, string @on) where T : IDataObject;
        IQueryBuilder Where(string filter, params object[] args);
        IQueryBuilder WhereIds(IEnumerable<long> ids);
        IQueryBuilder OrderBy(string column);
        IQueryBuilder OrderByDescending(string column);
        IQueryBuilder GroupBy(string name);
        IQueryBuilder Limit(int from, int count);
        T Find<T>(long id) where T : class, IDataObject;
        T FindOrDefault<T>(long id) where T : class, IDataObject;
        List<T> Execute<T>(string sql, params object[] args) where T : class;
        List<T> ToList<T>(string selectText = null) where T : class;
        IReadOnlyCollection<long> ToIdList();
        T First<T>(string selectText = null) where T : class;
        T FirstOrDefault<T>(string selectText = null) where T : class;
        IQueryBuilder New();
    }
}
