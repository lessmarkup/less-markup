/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using Newtonsoft.Json;

namespace LessMarkup.Framework.Helpers
{
    public static class JsonHelper
    {
        public static T Get<T>(this Dictionary<string, string> jsonObject, string name)
        {
            string str;
            if (!jsonObject.TryGetValue(name, out str))
            {
                return default(T);
            }
            return JsonConvert.DeserializeObject<T>(str);
        }

        public static string Get(this Dictionary<string, string> jsonObject, string name)
        {
            string str;
            if (!jsonObject.TryGetValue(name, out str))
            {
                return null;
            }
            return str;
        }
    }
}
