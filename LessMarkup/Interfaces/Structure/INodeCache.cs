using LessMarkup.Interfaces.Cache;

namespace LessMarkup.Interfaces.Structure
{
    public interface INodeCache : ICacheHandler
    {
        ICachedNodeInformation GetNode(long nodeId);
        void GetNode(string path, out ICachedNodeInformation node, out string rest);
        ICachedNodeInformation RootNode { get; }
    }
}
