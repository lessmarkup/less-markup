/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;

namespace LessMarkup.Engine.Scripting
{
    class ExpressionParser
    {
        private readonly List<ExpressionAtom> _atoms = new List<ExpressionAtom>();

        public void Parse(string expression)
        {
            for (int i = 0; i < expression.Length;)
            {
                var c = expression[i];
                if (c == ' ' || c == '\r' || c == '\n' || c == '\t')
                {
                    i++;
                    continue;
                }
                if (c == '=' && i + 1 < expression.Length && expression[i+1] == '=')
                {
                    i += 2;
                    _atoms.Add(new ExpressionAtom { Type = AtomType.Equal });
                    continue;
                }
                if (c == '!' && i + 1 < expression.Length && expression[i + 1] == '=')
                {
                    i += 2;
                    _atoms.Add(new ExpressionAtom { Type = AtomType.NotEqual});
                    continue;
                }
                if (c == '!')
                {
                    i += 1;
                    _atoms.Add(new ExpressionAtom { Type = AtomType.Inverse});
                    continue;
                }
                if (c == '\'' || c == '"')
                {
                    var next = expression.IndexOf(c, i + 1);
                    if (next > i)
                    {
                        _atoms.Add(new ExpressionAtom { Type = AtomType.Object, Value = expression.Substring(i+1, next-i-1)});
                        i = next + 1;
                        continue;
                    }
                }
                if (c == '(')
                {
                    _atoms.Add(new ExpressionAtom { Type = AtomType.Open});
                    i += 1;
                    continue;
                }
                if (c == ')')
                {
                    _atoms.Add(new ExpressionAtom { Type = AtomType.Close});
                    i += 1;
                    continue;
                }
                if (c == '&' && i + 1 < expression.Length && expression[i + 1] == '&')
                {
                    _atoms.Add(new ExpressionAtom { Type= AtomType.And});
                    i += 2;
                    continue;
                }
                if (c == '|' && i + 1 < expression.Length && expression[i + 1] == '|')
                {
                    _atoms.Add(new ExpressionAtom { Type = AtomType.Or});
                    i += 2;
                    continue;
                }
                if (c == '+')
                {
                    _atoms.Add(new ExpressionAtom { Type = AtomType.Plus});
                    i += 1;
                    continue;
                }
                if (c == '-')
                {
                    _atoms.Add(new ExpressionAtom { Type = AtomType.Minus});
                    i += 1;
                    continue;
                }

                if (!char.IsLetterOrDigit(c))
                {
                    throw new Exception("Encountered unknown symbol '" + c + "' at position " + i);
                }

                var start = i;
                for (i++; i < expression.Length; i++)
                {
                    c = expression[i];
                    if (!char.IsLetterOrDigit(c) && c != '.')
                    {
                        break;
                    }
                }

                var parameterName = expression.Substring(start, i - start);

                if (char.IsDigit(c))
                {
                    int intValue;
                    if (!int.TryParse(parameterName, out intValue))
                    {
                        throw new Exception("Expected int value");
                    }
                    _atoms.Add(new ExpressionAtom { Type = AtomType.Object, Value = intValue });
                    continue;
                }

                if (parameterName == "true")
                {
                    _atoms.Add(new ExpressionAtom { Type = AtomType.Object, Value = true });
                    continue;
                }

                if (parameterName == "false")
                {
                    _atoms.Add(new ExpressionAtom { Type = AtomType.Object, Value = false });
                    continue;
                }

                if (parameterName == "null")
                {
                    _atoms.Add(new ExpressionAtom { Type = AtomType.Null });
                    continue;
                }

                _atoms.Add(new ExpressionAtom { Type = AtomType.Parameter, Value = parameterName });
            }
        }

        public bool Evaluate(object objectToEvaluate)
        {
            var evaluator = new ScriptEvaluator(_atoms, objectToEvaluate);
            return ScriptEvaluator.ToBoolean(evaluator.Evaluate());
        }
    }
}
