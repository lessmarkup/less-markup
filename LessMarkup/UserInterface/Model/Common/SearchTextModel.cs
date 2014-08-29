using System.Linq;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Text;

namespace LessMarkup.UserInterface.Model.Common
{
    public class SearchTextModel
    {
        private readonly IDataCache _dataCache;
        private readonly IDomainModelProvider _domainModelProvider;

        public SearchTextModel(IDataCache dataCache, IDomainModelProvider domainModelProvider)
        {
            _dataCache = dataCache;
            _domainModelProvider = domainModelProvider;
        }

        public object Handle(string searchText)
        {
            var searchCache = _dataCache.Get<ITextSearch>();

            using (var domainModel = _domainModelProvider.Create())
            {
                var results = searchCache.Search(searchText, 0, 10, domainModel);

                if (results == null)
                {
                    return null;
                }

                return new
                {
                    Results = results.Results.Select(r => new SearchResultModel
                    {
                        Name = r.Name,
                        Text = r.Text,
                        Url = r.Url
                    }).ToList()
                };
            }
        }
    }
}
