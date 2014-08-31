using System.Collections.Generic;

namespace LessMarkup.Interfaces.Module
{
    public interface IUserPropertyProvider
    {
        IEnumerable<UserProperty> GetProperties(long userId);
    }
}
