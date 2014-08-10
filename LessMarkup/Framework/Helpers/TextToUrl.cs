using System.Text;

namespace LessMarkup.Framework.Helpers
{
    public static class TextToUrl
    {
        public static string Generate(string source)
        {
            var ret = new StringBuilder();

            foreach (var c in source)
            {
                switch (c)
                {
                    case '/':
                    case '\\':
                    case ':':
                    case '%':
                    case '&':
                    case '<':
                    case '>':
                    case '#':
                    case '@':
                    case '!':
                    case '*':
                    case '=':
                    case '?':
                    case '\'':
                    case '.':
                    case ' ':
                    case ',':
                    case ';':
                    case '~':
                    case '`':
                    case '$':
                    case '{':
                    case '}':
                    case '(':
                    case ')':
                        ret.Append('_');
                        continue;
                }

                ret.Append(c);
            }

            return ret.ToString();
        }
    }
}
