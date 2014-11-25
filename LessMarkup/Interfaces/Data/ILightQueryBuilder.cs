using System.Collections.Generic;

namespace LessMarkup.Interfaces.Data
{
    public interface ILightQueryBuilder
    {
        ILightQueryBuilder From<T>(string name = null) where T : IDataObject;
        ILightQueryBuilder Join<T>(string name, string @on) where T : IDataObject;
        ILightQueryBuilder LeftJoin<T>(string name, string @on) where T : IDataObject;
        ILightQueryBuilder RightJoin<T>(string name, string @on) where T : IDataObject;
        ILightQueryBuilder Where(string filter, params object[] args);
        ILightQueryBuilder WhereIds(IEnumerable<long> ids);
        ILightQueryBuilder OrderBy(string column);
        ILightQueryBuilder OrderByDescending(string column);
        ILightQueryBuilder GroupBy(string name);
        ILightQueryBuilder Limit(int from, int count);
        T Find<T>(long id) where T : class, IDataObject;
        T FindOrDefault<T>(long id) where T : class, IDataObject;
        List<T> Execute<T>(string sql, params object[] args) where T : class;
        List<T> ToList<T>(string selectText = null) where T : class;
        IReadOnlyCollection<long> ToIdList();
        T First<T>(string selectText = null) where T : class;
        T FirstOrDefault<T>(string selectText = null) where T : class;
        ILightQueryBuilder New();
    }
}
