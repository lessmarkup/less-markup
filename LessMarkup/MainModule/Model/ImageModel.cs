/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Web.Mvc;
using LessMarkup.DataObjects.Common;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.MainModule.Model
{
    public class ImageModel
    {
        private readonly ILightDomainModelProvider _domainModelProvider;

        public ImageModel(ILightDomainModelProvider domainModelProvider)
        {
            _domainModelProvider = domainModelProvider;
        }

        public ActionResult Thumbnail(long imageId)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var image = domainModel.Query().Find<Image>(imageId);

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
                var image = domainModel.Query().Find<Image>(imageId);

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
                var smile = domainModel.Query().Find<Smile>(smileId);

                if (smile == null)
                {
                    return new HttpNotFoundResult();
                }

                return new FileContentResult(smile.Data, smile.ContentType);
            }
        }
    }
}
