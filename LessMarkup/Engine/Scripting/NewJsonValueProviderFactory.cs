/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace LessMarkup.Engine.Scripting
{
    public class NewJsonValueProviderFactory : ValueProviderFactory
    {
        public static void Initialize()
        {
            foreach (var factory in ValueProviderFactories.Factories)
            {
                if (factory is JsonValueProviderFactory)
                {
                    ValueProviderFactories.Factories.Remove(factory);
                    break;
                }
            }

            ValueProviderFactories.Factories.Add(new NewJsonValueProviderFactory());
        }

        private static void AddToBackingStore(EntryLimitedDictionary backingStore, string prefix, object value)
        {
            var dictionary = value as IDictionary<string, object>;
            if (dictionary != null)
            {
                foreach (KeyValuePair<string, object> keyValuePair in dictionary)
                {
                    AddToBackingStore(backingStore, MakePropertyKey(prefix, keyValuePair.Key), keyValuePair.Value);
                }
            }
            else
            {
                var list = value as IList;
                if (list != null)
                {
                    for (int index = 0; index < list.Count; ++index)
                    {
                        AddToBackingStore(backingStore, MakeArrayKey(prefix, index), list[index]);
                    }
                }
                else
                {
                    backingStore.Add(prefix, value);
                }
            }
        }

        private static object GetDeserializedObject(ControllerContext controllerContext)
        {
            if (!controllerContext.HttpContext.Request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            string input = new StreamReader(controllerContext.HttpContext.Request.InputStream).ReadToEnd();
            return string.IsNullOrEmpty(input) ? null : JsonConvert.DeserializeObject(input);
        }

        public override IValueProvider GetValueProvider(ControllerContext controllerContext)
        {
            if (controllerContext == null)
            {
                throw new ArgumentNullException("controllerContext");
            }

            var deserializedObject = GetDeserializedObject(controllerContext);

            if (deserializedObject == null)
            {
                return null;
            }

            var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            AddToBackingStore(new EntryLimitedDictionary(dictionary), string.Empty, deserializedObject);
            return new DictionaryValueProvider<object>(dictionary, CultureInfo.CurrentCulture);
        }

        private static string MakeArrayKey(string prefix, int index)
        {
            return prefix + "[" + index.ToString(CultureInfo.InvariantCulture) + "]";
        }

        private static string MakePropertyKey(string prefix, string propertyName)
        {
            return !string.IsNullOrEmpty(prefix) ? prefix + "." + propertyName : propertyName;
        }

        private class EntryLimitedDictionary
        {
            private static readonly int _maximumDepth = GetMaximumDepth();
            private readonly IDictionary<string, object> _innerDictionary;
            private int _itemCount;

            static EntryLimitedDictionary()
            {
            }

            public EntryLimitedDictionary(IDictionary<string, object> innerDictionary)
            {
                _innerDictionary = innerDictionary;
            }

            public void Add(string key, object value)
            {
                if (++_itemCount > _maximumDepth)
                {
                    throw new InvalidOperationException();
                }

                _innerDictionary.Add(key, value);
            }

            private static int GetMaximumDepth()
            {
                var appSettings = ConfigurationManager.AppSettings;
                string[] values = appSettings.GetValues("aspnet:MaxJsonDeserializerMembers");
                int result;
                if (values != null && values.Length > 0 && int.TryParse(values[0], out result))
                {
                    return result;
                }
                return 1000;
            }
        }
    }
}
