/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Reflection;
using LessMarkup.Framework.Helpers;

namespace LessMarkup.Engine.Scripting
{
    class ScriptEvaluator
    {
        private readonly List<ExpressionAtom> _atoms;
        private readonly Dictionary<string, object> _properties = new Dictionary<string, object>();

        public ScriptEvaluator(List<ExpressionAtom> atoms, object objectToEvaluate)
        {
            _atoms = atoms;

            foreach (var property in objectToEvaluate.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                AddProperty("", property, objectToEvaluate);
            }
        }

        private void AddProperty(string prefix, PropertyInfo property, object instance)
        {
            var childInstance = property.GetValue(instance);

            if (childInstance == null || property.PropertyType.IsPrimitive || property.PropertyType.IsEnum || property.PropertyType == typeof(string))
            {
                _properties.Add((prefix + property.Name).ToJsonCase(), childInstance);
                return;
            }

            if (property.PropertyType.IsClass)
            {
                foreach (var childProperty in property.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    AddProperty(prefix + property.Name + ".", childProperty, childInstance);
                }
            }
        }

        public object Evaluate()
        {
            return EvaluateGroup(0, _atoms.Count);
        }

        public static bool ToBoolean(object result)
        {
            if (result is bool)
            {
                return (bool) result;
            }

            var stringOperand = (string) result;
            bool boolRet;
            if (bool.TryParse(stringOperand, out boolRet))
            {
                return boolRet;
            }
            throw new Exception("Cannot convert '" + (result ?? "(null)") + "' to boolean value");
        }

        public static int ToInt(object value)
        {
            if (value is int)
            {
                return (int) value;
            }

            int intValue;
            if (value == null || !int.TryParse(value.ToString(), out intValue))
            {
                throw new Exception("Expected integer value");
            }

            return intValue;
        }

        private static int GetOperationPriority(AtomType atomType)
        {
            switch (atomType)
            {
                case AtomType.And:
                case AtomType.Or:
                    return 10;
                case AtomType.Minus:
                case AtomType.Plus:
                    return 9;
                case AtomType.Equal:
                case AtomType.NotEqual:
                    return 8;
            }

            return -1;
        }

        private object EvaluateFlatOperations(List<ExpressionAtom> atoms, int from, int count)
        {
            if (count >= 3)
            {
                int to = from + count;
                int topPriority = -1;
                int index = -1;
                for (var i = from; i < to; i++)
                {
                    var priority = GetOperationPriority(atoms[i].Type);
                    if (priority > topPriority)
                    {
                        topPriority = priority;
                        index = i;
                    }
                }
                var leftCount = index-from;
                var rightCount = from + count - index - 1;

                var right = EvaluateFlatOperations(atoms, index + 1, rightCount);
                var left = EvaluateFlatOperations(atoms, 0, leftCount);

                return ExecuteBinaryOperator(atoms[index].Type, left, right);
            }

            if (count == 1)
            {
                return atoms[from].Value;
            }

            throw new Exception("Unexpected atoms count");
        }

        private object EvaluateGroup(int start, int count)
        {
            var end = start + count;
            var pos = start;
            var left = Evaluate(start, ref count);
            pos += count;
            if (pos == end)
            {
                return left;
            }

            var atoms = new List<ExpressionAtom>
            {
                new ExpressionAtom {Type = AtomType.Object, Value = left}
            };

            while (pos < end)
            {
                var op = _atoms[pos];
                pos++;
                var rest = end - pos;
                if (!IsBinaryOperator(op.Type))
                {
                    throw new Exception("Expected binary operator instead of '" + op.Type + "'");
                }
                if (rest == 0)
                {
                    throw new Exception("Expected binary operator right operand for '" + op.Type + "'");
                }
                atoms.Add(op);
                var result = Evaluate(pos, ref rest);
                atoms.Add(new ExpressionAtom { Type = AtomType.Object, Value = result});
                pos += rest;
            }

            return EvaluateFlatOperations(atoms, 0, atoms.Count);
        }

        private static bool IsBinaryOperator(AtomType atomType)
        {
            switch (atomType)
            {
                case AtomType.And:
                case AtomType.Or:
                case AtomType.Equal:
                case AtomType.Minus:
                case AtomType.Plus:
                case AtomType.NotEqual:
                    return true;
            }
            return false;
        }

        private static bool ExecuteEqual(object left, object right)
        {
            if (left == null)
            {
                return right == null;
            }
            if (right == null)
            {
                return false;
            }
            if (left.GetType() == right.GetType())
            {
                return Equals(left, right);
            }
            return left.ToString() == right.ToString();
        }

        private static object ExecuteBinaryOperator(AtomType atomType, object left, object right)
        {
            switch (atomType)
            {
                case AtomType.And:
                    return ToBoolean(left) && ToBoolean(right);
                case AtomType.Or:
                    return ToBoolean(left) || ToBoolean(right);
                case AtomType.Equal:
                    return ExecuteEqual(left, right);
                case AtomType.Minus:
                    return ToInt(left) - ToInt(right);
                case AtomType.Plus:
                    return ToInt(left) + ToInt(right);
                case AtomType.NotEqual:
                    return !ExecuteEqual(left, right);
            }

            throw new ArgumentOutOfRangeException("atomType");
        }

        private static bool IsUnaryOperator(AtomType atomType)
        {
            switch (atomType)
            {
                case AtomType.Inverse:
                    return true;
            }
            return false;
        }

        private static object ExecuteUnaryOperator(AtomType atomType, object operand)
        {
            switch (atomType)
            {
                case AtomType.Inverse:
                    return !ToBoolean(operand);
            }

            throw new ArgumentOutOfRangeException("atomType");
        }

        private object Evaluate(int start, ref int count)
        {
            if (count == 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            var atom = _atoms[start];

            switch (atom.Type)
            {
                case AtomType.Parameter:
                    var parameterName = (string) atom.Value;
                    object parameterValue;
                    if (!_properties.TryGetValue(parameterName, out parameterValue))
                    {
                        throw new Exception("Unknown parameter '" + parameterName + "'");
                    }
                    count = 1;
                    return parameterValue;
                case AtomType.Null:
                    count = 1;
                    return null;
                case AtomType.Object:
                    count = 1;
                    return atom.Value;
            }

            if (IsUnaryOperator(atom.Type))
            {
                if (count == 1)
                {
                    throw new Exception("Expected unary operator operand for '" + atom.Type + "'");
                }

                var nextCount = count - 1;
                var nextStart = start + 1;

                var result = Evaluate(nextStart, ref nextCount);

                count = nextCount + 1;

                return ExecuteUnaryOperator(atom.Type, result);
            }

            if (atom.Type == AtomType.Open)
            {
                var end = start + count;
                int level = 1;
                int i;
                for (i = start + 1; i < end && level > 0; i++)
                {
                    switch (_atoms[i].Type)
                    {
                        case AtomType.Close:
                            level--;
                            break;
                        case AtomType.Open:
                            level++;
                            break;
                    }
                }

                if (level > 0)
                {
                    throw new Exception("Encountered opening bracket without closing one");
                }

                i += 1;

                count = i - start;

                return EvaluateGroup(start, count);
            }

            throw new Exception("Unknown atom '" + atom.Type + "'");
        }
    }
}
