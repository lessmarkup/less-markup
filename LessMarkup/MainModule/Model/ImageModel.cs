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
                var image = domainModel.GetSiteCollection<Image>().FirstOrDefault(i => i.Id == imageId);

                if (image == null)
                {
                    return new HttpNotFoundResult();
                }

                return new FileContentResult(image.Thumbnail, image.ThumbnailContentType ?? "image/png");
            }
        }

        public ActionResult Get(long imageId)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var image = domainModel.GetSiteCollection<Image>().FirstOrDefault(i => i.Id == imageId);

                if (image == null)
                {
                    return new HttpNotFoundResult();
                }

                return new FileContentResult(image.Data, image.ContentType ?? "image/png");
            }
        }

        public ActionResult Smile(long smileId)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var smile = domainModel.GetSiteCollection<Smile>().FirstOrDefault(s => s.Id == smileId);

                if (smile == null)
                {
                    return new HttpNotFoundResult();
                }

                return new FileContentResult(smile.Data, smile.ContentType);
            }
        }
    }
}
