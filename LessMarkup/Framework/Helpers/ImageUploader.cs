/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using LessMarkup.DataFramework;
using LessMarkup.DataObjects.Common;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Framework.Helpers
{
    public static class ImageUploader
    {
        /*public static long? SaveGallery(IDomainModel domainModel, long? galleryId, IList<HttpPostedFile> files,
            long[] unchangedImages, ICurrentUser currentUser, IDataCache dataCache)
        {
            var fileCount = files.Count(f => f.ContentLength > 0);

            Gallery gallery;
            if (!galleryId.HasValue)
            {
                if (fileCount == 0)
                {
                    return null;
                }

                gallery = domainModel.GetSiteCollection<Gallery>().Create();
                domainModel.GetSiteCollection<Gallery>().Add(gallery);
            }
            else
            {
                gallery = domainModel.GetSiteCollection<Gallery>().Single(g => g.GalleryId == galleryId);
                var galleryImageCount = 0;
                if (unchangedImages == null || unchangedImages.Length <= 0)
                {
                    if (fileCount == 0)
                    {
                        galleryImageCount = domainModel.GetSiteCollection<GalleryImage>().Count(g => g.GalleryId == galleryId);
                    }
                }
                else
                {
                    var imagesToRemove = new List<GalleryImage>();

                    foreach (var galleryImage in domainModel.GetSiteCollection<GalleryImage>().Where(g => g.GalleryId == galleryId.Value).Include(g => g.Image))
                    {
                        if (!unchangedImages.Contains(galleryImage.ImageId))
                        {
                            imagesToRemove.Add(galleryImage);
                        }
                        else
                        {
                            galleryImageCount++;
                        }
                    }

                    foreach (var galleryImage in imagesToRemove)
                    {
                        domainModel.GetSiteCollection<Image>().Remove(galleryImage.Image);
                        domainModel.GetSiteCollection<GalleryImage>().Remove(galleryImage);
                    }
                }

                if (fileCount == 0)
                {
                    if (galleryImageCount == 0)
                    {
                        domainModel.GetSiteCollection<Gallery>().Remove(gallery);
                        domainModel.SaveChanges();
                        return null;
                    }

                    domainModel.SaveChanges();
                    return galleryId;
                }
            }

            domainModel.SaveChanges();

            foreach (var file in files)
            {
                if (file.ContentLength == 0)
                {
                    continue;
                }

                var galleryImage = domainModel.GetSiteCollection<GalleryImage>().Create();
                galleryImage.GalleryId = gallery.GalleryId;
                galleryImage.ImageId = SaveImage(domainModel, null, file, currentUser, dataCache);

                domainModel.GetSiteCollection<GalleryImage>().Add(galleryImage);
            }

            domainModel.SaveChanges();

            return gallery.GalleryId;
        }

        public static long SaveImage(IDomainModel domainModel, long? imageId, HttpPostedFile file, ICurrentUser currentUser, IDataCache dataCache)
        {
            return SaveImage(domainModel, imageId, file.ContentLength, file.InputStream, file.FileName, currentUser, dataCache);
        }*/

        public static void DeleteImage(IDomainModel domainModel, long imageId)
        {
            var image = domainModel.GetSiteCollection<Image>().Single(i => i.Id == imageId);
            domainModel.GetSiteCollection<Image>().Remove(image);
        }

        public static void ReduceToAllowedImageSize(InputFile file, ISiteConfiguration siteConfiguration)
        {
            using (var inputStream = new MemoryStream(file.File))
            using (var imageData = System.Drawing.Image.FromStream(inputStream, true, true))
            {
                if (imageData.Width <= siteConfiguration.MaximumImageWidth &&
                    imageData.Height <= siteConfiguration.MaximumImageHeight)
                {
                    return;
                }

                var newImageWidth = (double)imageData.Width;
                var newImageHeight = (double)imageData.Height;

                if (newImageWidth > siteConfiguration.MaximumImageWidth)
                {
                    newImageHeight *= siteConfiguration.MaximumImageWidth / newImageWidth;
                    newImageWidth = siteConfiguration.MaximumImageWidth;
                }

                if (newImageHeight > siteConfiguration.MaximumImageHeight)
                {
                    newImageWidth *= siteConfiguration.MaximumImageHeight / newImageHeight;
                    newImageHeight = siteConfiguration.MaximumImageHeight;
                }

                var imageWidth = (int)newImageWidth;
                var imageHeight = (int)newImageHeight;

                using (var reducedImage = imageData.GetThumbnailImage(imageWidth, imageHeight, () => false, IntPtr.Zero))
                {
                    using (var stream = new MemoryStream())
                    {
                        reducedImage.Save(stream, ImageFormat.Png);
                        file.File = stream.ToArray();
                        file.Type = "image/png";
                        if (!string.IsNullOrWhiteSpace(file.Name))
                        {
                            file.Name = Path.ChangeExtension(file.Name, "png");
                        }
                    }
                }
            }
        }

        public static long SaveImage(IDomainModel domainModel, long? imageId, InputFile file, long? userId, ISiteConfiguration siteConfiguration)
        {
            if (file.File.Length > siteConfiguration.MaximumFileSize)
            {
                throw new Exception(string.Format(LanguageHelper.GetText(Constants.ModuleType.MainModule, MainModuleTextIds.ImageSizeIsBigger), siteConfiguration.MaximumFileSize));
            }

            byte[] imageBytes;
            byte[] thumbnailBytes;
            int imageWidth, imageHeight;

            if (!file.Type.ToLower().StartsWith("image/"))
            {
                throw new Exception(LanguageHelper.GetText(Constants.ModuleType.MainModule, MainModuleTextIds.UnsupportedFileType));
            }

            using (var inputStream = new MemoryStream(file.File))
            {
                using (var imageData = System.Drawing.Image.FromStream(inputStream, true, true))
                {
                    imageWidth = imageData.Width;
                    imageHeight = imageData.Height;

                    if (imageData.Width > siteConfiguration.MaximumImageWidth ||
                        imageData.Height > siteConfiguration.MaximumImageHeight)
                    {
                        var newImageWidth = (double) imageData.Width;
                        var newImageHeight = (double) imageData.Height;

                        if (newImageWidth > siteConfiguration.MaximumImageWidth)
                        {
                            newImageHeight *= siteConfiguration.MaximumImageWidth/newImageWidth;
                            newImageWidth = siteConfiguration.MaximumImageWidth;
                        }

                        if (newImageHeight > siteConfiguration.MaximumImageHeight)
                        {
                            newImageWidth *= siteConfiguration.MaximumImageHeight/newImageHeight;
                            newImageHeight = siteConfiguration.MaximumImageHeight;
                        }

                        imageWidth = (int) newImageWidth;
                        imageHeight = (int) newImageHeight;

                        using (var reducedImage = imageData.GetThumbnailImage(imageWidth, imageHeight, () => false, IntPtr.Zero))
                        {
                            using (var stream = new MemoryStream())
                            {
                                reducedImage.Save(stream, ImageFormat.Png);
                                imageBytes = stream.ToArray();
                            }
                        }
                    }
                    else
                    {
                        using (var stream = new MemoryStream())
                        {
                            imageData.Save(stream, ImageFormat.Png);
                            imageBytes = stream.ToArray();
                        }
                    }

                    var thumbnailWidth = (double) imageData.Width;
                    var thumbnailHeight = (double) imageData.Height;

                    if (thumbnailWidth > siteConfiguration.ThumbnailWidth)
                    {
                        thumbnailHeight *= siteConfiguration.ThumbnailWidth/thumbnailWidth;
                        thumbnailWidth = siteConfiguration.ThumbnailWidth;
                    }

                    if (thumbnailHeight > siteConfiguration.ThumbnailHeight)
                    {
                        thumbnailWidth *= siteConfiguration.ThumbnailHeight/thumbnailHeight;
                        thumbnailHeight = siteConfiguration.ThumbnailHeight;
                    }

                    using (var thumbnail = imageData.GetThumbnailImage((int)thumbnailWidth, (int)thumbnailHeight, () => false, IntPtr.Zero))
                    {
                        using (var stream = new MemoryStream())
                        {
                            thumbnail.Save(stream, ImageFormat.Png);
                            thumbnailBytes = stream.ToArray();
                        }
                    }
                }
            }


            Image image = null;

            if (imageId.HasValue)
            {
                image = domainModel.GetSiteCollection<Image>().Single(i => i.Id == imageId.Value);
            }

            if (image == null)
            {
                image = domainModel.GetSiteCollection<Image>().Create();
                domainModel.GetSiteCollection<Image>().Add(image);
                image.Created = DateTime.UtcNow;
            }
            else
            {
                image.Updated = DateTime.UtcNow;
            }

            image.Width = imageWidth;
            image.Height = imageHeight;
            image.FileName = file.Name;
            image.ContentType = "image/png";
            image.UserId = userId;
            image.Data = imageBytes;
            image.Thumbnail = thumbnailBytes;
            image.ThumbnailContentType = "image/png";

            domainModel.SaveChanges();
 
            return image.Id;
        }

        public static void LimitImageSize(long imageId, IDomainModel domainModel, int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException("width");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException("height");
            }

            var image = domainModel.GetSiteCollection<Image>().Single(i => i.Id == imageId);

            if (image.Width <= width && image.Height <= height)
            {
                return;
            }

            var newImageWidth = image.Width;
            var newImageHeight = image.Height;

            if (newImageWidth > width)
            {
                newImageHeight = (int)(newImageHeight * ((double) newImageWidth/image.Width));
                newImageWidth = width;
            }

            if (newImageHeight > height)
            {
                newImageWidth = (int) (newImageWidth*((double) newImageHeight/image.Height));
                newImageHeight = height;
            }

            System.Drawing.Image thumbnail;

            using (var stream = new MemoryStream(image.Data))
            {
                using (var imageData = System.Drawing.Image.FromStream(stream, true, true))
                {
                    thumbnail = imageData.GetThumbnailImage(newImageWidth, newImageHeight, () => false, IntPtr.Zero);
                }
            }

            using (var stream = new MemoryStream())
            {
                thumbnail.Save(stream, ImageFormat.Png);
                image.Data = stream.ToArray();
            }

            image.Width = newImageWidth;
            image.Height = newImageHeight;
            image.ContentType = "image/png";
            image.ThumbnailContentType = "image/png";
        }
    }
}
