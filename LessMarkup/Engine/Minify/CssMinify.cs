using System.Text;

namespace LessMarkup.Engine.Minify
{
    internal class CssMinify : IMinify
    {
        public string Process(string source)
        {
            var result = new StringBuilder();
            bool lastSpace = false;
            bool anySymbol = false;
            bool addSpace = true;

            for (int pos = 0; pos < source.Length;)
            {
                char c = source[pos];

                if (c == '{' || c == '}')
                {
                    anySymbol = false;
                    addSpace = false;
                    lastSpace = false;
                    result.Append(c);
                    pos++;
                    continue;
                }

                if (c == ' ' || c == '\r' || c == '\n' || c == '\t' || c == '\b')
                {
                    if (anySymbol && !lastSpace)
                    {
                        addSpace = true;
                        lastSpace = true;
                    }

                    pos++;
                    continue;
                }

                if (c == '/' && pos + 1 < source.Length && source[pos + 1] == '*')
                {
                    for (pos += 2; pos < source.Length; pos++)
                    {
                        c = source[pos];
                        if (c != '*')
                        {
                            continue;
                        }
                        if (pos + 1 < source.Length && source[pos + 1] == '/')
                        {
                            pos += 2;
                            break;
                        }
                    }
                    continue;
                }

                if (addSpace)
                {
                    if (c != '(' && c != ')' && c != ',' && c != ':' && c != ';')
                    {
                        result.Append(' ');
                    }
                    addSpace = false;
                }

                anySymbol = true;
                lastSpace = c == ',' || c == ':' || c == ')' || c == '(' || c == ';';

                if (c == '\'' || c == '\"')
                {
                    var delimiter = c;
                    int s;
                    var found = false;
                    for (s = pos + 1; s < source.Length; s++)
                    {
                        c = source[s];
                        if (c == '\\')
                        {
                            s++;
                            continue;
                        }
                        if (c == delimiter)
                        {
                            result.Append(source.Substring(pos, s - pos + 1));
                            pos = s + 1;
                            found = true;
                            break;
                        }
                        if (c == '\\')
                        {
                            s++;
                        }
                    }
                    if (!found)
                    {
                        result.Append(delimiter);
                        pos++;
                    }
                    continue;
                }

                result.Append(c);
                pos++;
            }

            return result.ToString();
        }
    }
}
