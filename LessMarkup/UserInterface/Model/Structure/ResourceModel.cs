using System;
using System.Web.Mvc;
using LessMarkup.Engine.FileSystem;
using LessMarkup.Interfaces.Cache;

namespace LessMarkup.UserInterface.Model.Structure
{
    public class ResourceModel
    {
        private readonly IDataCache _dataCache;
        private string _path;
        private string _contentType;

        public ResourceModel(IDataCache dataCache)
        {
            _dataCache = dataCache;
        }

        public bool Initialize(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var resourceCache = _dataCache.Get<ResourceCache>();

            if (!resourceCache.ResourceExists(path))
            {
                return false;
            }

            var lastDotPoint = path.LastIndexOf('.');

            if (lastDotPoint <= 0)
            {
                return false;
            }

            var extension = path.Substring(lastDotPoint + 1).ToLower();

            switch (extension)
            {
                case "html":
                    _contentType = "text/html";
                    break;
                case "js":
                    _contentType = "text/javascript";
                    break;
                case "css":
                    _contentType = "text/css";
                    break;
                case "jpeg":
                case "jpg":
                    _contentType = "image/jpeg";
                    break;
                case "gif":
                    _contentType = "image/gif";
                    break;
                case "png":
                    _contentType = "image/png";
                    break;
                case "eot":
                    _contentType = "font/opentype";
                    break;
                case "otf":
                    _contentType = "application/x-font-opentype";
                    break;
                case "svg":
                    _contentType = "image/svg+xml";
                    break;
                case "ttf":
                    _contentType = "application/x-font-truetype";
                    break;
                case "woff":
                    _contentType = "application/font-woff";
                    break;
                default:
                    return false;
            }

            _path = path;
            return true;
        }

        public ActionResult CreateResult(System.Web.Mvc.Controller controller)
        {
            controller.Response.Cache.SetExpires(DateTime.Now.AddHours(-1));
            var resourceCache = _dataCache.Get<ResourceCache>();
            return new FileStreamResult(resourceCache.ReadResource(_path), _contentType);
        }
    }
}
