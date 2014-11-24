/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Drawing.Imaging;
using System.IO;
using LessMarkup.DataFramework;
using LessMarkup.DataObjects.Common;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Framework.Helpers
{
    public static class ImageUploader
    {
        public static void DeleteImage(ILightDomainModel domainModel, long imageId)
        {
            domainModel.Delete<Image>(imageId);
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

        public static long SaveImage(ILightDomainModel domainModel, long? imageId, InputFile file, long? userId, ISiteConfiguration siteConfiguration)
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
                image = domainModel.Query().From<Image>().Where("Id = $", imageId.Value).First<Image>();
            }

            var newImage = false;

            if (image == null)
            {
                newImage = true;
                image = new Image {Created = DateTime.UtcNow};
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

            if (newImage)
            {
                domainModel.Create(image);
            }
            else
            {
                domainModel.Update(image);
            }

            return image.Id;
        }

        public static void LimitImageSize(long imageId, ILightDomainModel domainModel, int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException("width");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException("height");
            }

            var image = domainModel.Query().From<Image>().Where("Id = $", imageId).First<Image>();

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
