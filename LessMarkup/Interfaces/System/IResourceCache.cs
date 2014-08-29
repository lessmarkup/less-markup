using System;
using System.Collections.Generic;
using System.IO;
using LessMarkup.Interfaces.Cache;

namespace LessMarkup.Interfaces.System
{
    public interface IResourceCache : ICacheHandler
    {
        bool ResourceExists(string path);
        Stream ReadResource(string path);
        string ReadText(string path);
        bool DirectoryExists(string path);
        List<string> GetFiles(string path);
        Type LoadType(string path);
    }
}
