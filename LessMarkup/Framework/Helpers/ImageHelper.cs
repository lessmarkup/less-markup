namespace LessMarkup.Framework.Helpers
{
    public static class ImageHelper
    {
        public static string ImageUrl(long imageId)
        {
            return string.Format("/Image/Get/{0}", imageId);
        }

        public static string ThumbnailUrl(long imageId)
        {
            return string.Format("/Image/Thumbnail/{0}", imageId);
        }

        public static string SmileUrl(string code)
        {
            return string.Format("/Image/Smile/{0}", code);
        }
    }
}
