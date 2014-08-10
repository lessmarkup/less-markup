using System.Linq;
using System.Web.Mvc;
using LessMarkup.DataObjects.Common;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.MainModule.Model
{
    public class ImageModel
    {
        private readonly IDomainModelProvider _domainModelProvider;

        public ImageModel(IDomainModelProvider domainModelProvider)
        {
            _domainModelProvider = domainModelProvider;
        }

        public ActionResult Thumbnail(long imageId)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var image = domainModel.GetSiteCollection<Image>().FirstOrDefault(i => i.ImageId == imageId);

                if (image == null)
                {
                    return new HttpNotFoundResult();
                }

                string contentType;

                switch (image.ImageType)
                {
                    case ImageType.Gif:
                        contentType = "image/gif";
                        break;
                    case ImageType.Png:
                        contentType = "image/png";
                        break;
                    case ImageType.Jpeg:
                        contentType = "image/jpeg";
                        break;
                    default:
                        return new HttpNotFoundResult();
                }

                return new FileContentResult(image.Thumbnail, contentType);
            }
        }

        public ActionResult Get(long imageId)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var image = domainModel.GetSiteCollection<Image>().FirstOrDefault(i => i.ImageId == imageId);

                if (image == null)
                {
                    return new HttpNotFoundResult();
                }

                string contentType;

                switch (image.ImageType)
                {
                    case ImageType.Gif:
                        contentType = "image/gif";
                        break;
                    case ImageType.Png:
                        contentType = "image/png";
                        break;
                    case ImageType.Jpeg:
                        contentType = "image/jpeg";
                        break;
                    default:
                        return new HttpNotFoundResult();
                }

                return new FileContentResult(image.Data, contentType);
            }
        }
    }
}
