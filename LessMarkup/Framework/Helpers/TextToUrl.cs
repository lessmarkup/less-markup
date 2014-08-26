using System.Text;

namespace LessMarkup.Framework.Helpers
{
    public static class TextToUrl
    {
        public static string Generate(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return "";
            }

            var ret = new StringBuilder();

            foreach (var c in source)
            {
                string[] array;
                ret.Append(UniCharacters.Characters.TryGetValue(c >> 8, out array) ? array[c & 0xff] : "-");
            }

            source = ret.ToString();

            ret = new StringBuilder();

            var last = ' ';

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
                    case '\"':
                    case '-':
                    case '|':
                    case '+':
                    case '\t':
                    case '\r':
                    case '\n':
                        if (last == '-')
                        {
                            continue;
                        }
                        ret.Append('-');
                        last = '-';
                        continue;
                }

                ret.Append(c);
                last = c;
            }

            return ret.ToString().Trim(new []{'-'}).ToLower();
        }
    }
}
