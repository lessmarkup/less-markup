/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataFramework;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.RecordModel;

namespace LessMarkup.UserInterface.Model.RecordModel
{
    public class InputFormDefinitionModel
    {
        private readonly IDataCache _dataCache;

        public InputFormDefinitionModel(IDataCache dataCache)
        {
            _dataCache = dataCache;
        }

        public class SelectValue
        {
            public string Text { get; set; }
            public string Value { get; set; }
        }

        public class FieldModel
        {
            public string Text { get; set; }
            public InputFieldType Type { get; set; }
            public bool ReadOnly { get; set; }
            public string Id { get; set; }
            public bool Required { get; set; }
            public double? Width { get; set; }
            public string ReadOnlyCondition { get; set; }
            public string VisibleCondition { get; set; }
            public string Property { get; set; }
            public string HelpText { get; set; }
            public List<SelectValue> SelectValues { get; set; }
            public object DefaultValue { get; set; }
        }

        public List<FieldModel> Fields { get; set; }
        public string Title { get; set; }

        public void Initialize(Type modelType)
        {
            var cache = _dataCache.Get<RecordModelCache>();
            var definition = cache.GetDefinition(modelType);
            Initialize(definition);
        }

        public void Initialize(string id)
        {
            var cache = _dataCache.Get<RecordModelCache>();
            var definition = cache.GetDefinition(id);
            Initialize(definition);
        }

        private void Initialize(RecordModelDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            Fields = new List<FieldModel>();

            if (definition.TitleTextId != null)
            {
                Title = LanguageHelper.GetText(definition.ModuleType, definition.TitleTextId);
            }

            IInputSource inputSource = null;

            foreach (var source in definition.Fields)
            {
                var target = new FieldModel
                {
                    Id = source.Id,
                    ReadOnly = source.ReadOnly,
                    ReadOnlyCondition = source.ReadOnlyCondition,
                    Required = source.Required,
                    Text = LanguageHelper.GetText(definition.ModuleType, source.TextId),
                    Type = source.Type,
                    VisibleCondition = source.VisibleCondition,
                    Width = source.Width,
                    Property = source.Property,
                    DefaultValue = source.DefaultValue
                };

                if (source.EnumValues != null && source.EnumValues.Count > 0)
                {
                    target.SelectValues = new List<SelectValue>();
                    foreach (var value in source.EnumValues)
                    {
                        target.SelectValues.Add(new SelectValue
                        {
                            Value = value.Value,
                            Text = value.TextId != null ? LanguageHelper.GetText(definition.ModuleType, value.TextId) : value.Value
                        });
                    }
                }
                else if (source.Type == InputFieldType.Select || source.Type == InputFieldType.SelectText || source.Type == InputFieldType.MultiSelect)
                {
                    if (inputSource == null && typeof(IInputSource).IsAssignableFrom(definition.DataType))
                    {
                        inputSource = (IInputSource) DependencyResolver.Resolve(definition.DataType);
                    }

                    if (inputSource != null)
                    {
                        target.SelectValues = inputSource.GetEnumValues(target.Property).Select(v => new SelectValue { Text = v.Text, Value = v.Value }).ToList();
                    }
                }

                Fields.Add(target);
            }
        }
    }
}
