namespace LessMarkup.Framework.Helpers
{
    public static class TextHelper
    {
        public static string ToJsonCase(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return value.Substring(0, 1).ToLower() + value.Substring(1);
        }

        public static string FromJsonCase(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return value.Substring(0, 1).ToUpper() + value.Substring(1);
        }
    }
}
