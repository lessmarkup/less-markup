/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

namespace LessMarkup.Framework.Scripting
{
    public static class ScriptHelper
    {
        public static bool EvaluateExpression(string expression, object objectToEvaluate)
        {
            var parser = new ExpressionParser();
            parser.Parse(expression);
            return parser.Evaluate(objectToEvaluate);
        }
    }
}
