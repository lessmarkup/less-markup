namespace LessMarkup.Framework.Helpers
{
    public static class RecordListHelper
    {
        public static string PageLink(string baseUrl, int page)
        {
            return string.Format("{0}?p={1}", baseUrl, page);
        }

        public static string LastPageLink(string baseUrl)
        {
            return string.Format("{0}?p=last", baseUrl);
        }
    }
}
