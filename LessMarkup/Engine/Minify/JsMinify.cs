/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Text;

namespace LessMarkup.Engine.Minify
{
    class JsMinify : IMinify
    {
        private static bool IsControlChar(char c)
        {
            return c == ',' || c == ':' || c == ')' || c == '(' || c == ';' || c == '=' || c == '+' || c == '-' || c == '!' || c == '&' || c == '|' || c == '?' || c == '}' || c == '{' || c == '<' || c == '>';
        }

        public string Process(string source)
        {
            var result = new StringBuilder();
            bool lastSpace = false;
            bool anySymbol = false;
            bool addSpace = true;
            char lastChar = '\0';
            bool addReturn = false;

            for (int pos = 0; pos < source.Length; )
            {
                char c = source[pos];

                if (c == '{' || c == '}')
                {
                    anySymbol = false;
                    addSpace = false;
                    lastSpace = false;
                    addReturn = false;
                    result.Append(c);
                    /*if (c == '}')
                    {
                        result.AppendLine();
                    }*/
                    pos++;
                    continue;
                }

                if (c == ' ' || c == '\r' || c == '\n' || c == '\t' || c == '\b')
                {
                    if (c == '\r' || c == '\n')
                    {
                        addReturn = true;
                    }

                    if (anySymbol && !lastSpace)
                    {
                        addSpace = true;
                        lastSpace = true;
                    }

                    pos++;
                    continue;
                }

                if (c == '/' && pos + 1 < source.Length)
                {
                    var c1 = source[pos + 1];
                    if (c1 == '*')
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

                    if (c1 == '/')
                    {
                        for (pos += 2; pos < source.Length; pos++)
                        {
                            c = source[pos];
                            if (c == '\r' || c == '\n')
                            {
                                break;
                            }
                        }
                        addSpace = true;
                        anySymbol = true;
                        lastSpace = false;
                        continue;
                    }
                }

                if (addReturn)
                {
                    addSpace = false;
                    //anySymbol = false;
                    result.Append('\n');
                    addReturn = false;
                }

                if (addSpace)
                {
                    if (!IsControlChar(c))
                    {
                        result.Append(' ');
                    }
                    addSpace = false;
                }

                if (c == '/' && IsControlChar(lastChar))
                {
                    var start = pos;
                    bool insidePattern = false;
                    for (pos += 1; pos < source.Length; pos++)
                    {
                        c = source[pos];
                        if (c == '\\')
                        {
                            pos++;
                            continue;
                        }
                        if (c == '[')
                        {
                            insidePattern = true;
                            continue;
                        }
                        if (c == ']')
                        {
                            insidePattern = false;
                            continue;
                        }
                        if (c == '/' && !insidePattern)
                        {
                            pos++;
                            for (; pos < source.Length; pos++)
                            {
                                c = source[pos];
                                if (c != 'g' && c != 'i' && c != 'm' && c != 'y')
                                {
                                    break;
                                }
                            }
                            result.Append(source.Substring(start, pos - start));
                            break;
                        }
                        if (c == '\r' || c == '\n')
                        {
                            result.Append(source.Substring(start, pos - start));
                            pos++;
                            break;
                        }
                    }
                    anySymbol = true;
                    //addSpace = false;
                    lastChar = '/';
                    continue;
                }

                anySymbol = true;
                lastSpace = IsControlChar(c);

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

                        if (c == '\r' || c == '\n')
                        {
                            result.Append(source.Substring(pos, s - pos));
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
                    lastChar = delimiter;
                    continue;
                }

                result.Append(c);
                pos++;
                lastChar = c;
            }

            return result.ToString();
        }
    }
}
