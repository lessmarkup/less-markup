namespace LessMarkup.Interfaces.Security
{
    public interface IHtmlSanitizer
    {
        string Sanitize(string htmlText);
    }
}
