using System;

namespace LessMarkup.Interfaces.System
{
    public interface IResolverCallback
    {
        T Resolve<T>();
        object Resolve(Type type);
    }
}
