/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.System;

namespace LessMarkup.UserInterface.Model.RecordModel
{
    public class InputFormDefinitionModel
    {
        private readonly IDataCache _dataCache;
        private readonly IEngineConfiguration _engineConfiguration;

        public InputFormDefinitionModel(IDataCache dataCache, IEngineConfiguration engineConfiguration)
        {
            _dataCache = dataCache;
            _engineConfiguration = engineConfiguration;
        }

        public List<InputFieldModel> Fields { get; set; }
        public string Title { get; set; }
        public bool SubmitWithCaptcha { get; set; }

        public void Initialize(Type modelType)
        {
            var cache = _dataCache.Get<IRecordModelCache>();
            var definition = cache.GetDefinition(modelType);
            Initialize(definition);
        }

        public void Initialize(string id)
        {
            var cache = _dataCache.Get<IRecordModelCache>();
            var definition = cache.GetDefinition(id);
            Initialize(definition);
        }

        private void Initialize(IRecordModelDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            Fields = new List<InputFieldModel>();

            if (definition.TitleTextId != null)
            {
                Title = LanguageHelper.GetText(definition.ModuleType, definition.TitleTextId);
            }

            if (definition.SubmitWithCaptcha)
            {
                if (!string.IsNullOrWhiteSpace(_engineConfiguration.RecaptchaPrivateKey) &&
                    !string.IsNullOrWhiteSpace(_engineConfiguration.RecaptchaPublicKey))
                {
                    SubmitWithCaptcha = true;
                }
            }

            IInputSource inputSource = null;

            foreach (var source in definition.Fields)
            {
                var target = new InputFieldModel
                {
                    Id = source.Id,
                    ReadOnly = source.ReadOnly,
                    ReadOnlyCondition = source.ReadOnlyCondition,
                    Required = source.Required,
                    Text = source.TextId == null ? null : LanguageHelper.GetText(definition.ModuleType, source.TextId),
                    Type = source.Type,
                    VisibleCondition = source.VisibleCondition,
                    Width = source.Width,
                    Property = source.Property.ToJsonCase(),
                    DefaultValue = source.DefaultValue
                };

                if (source.EnumValues != null && source.EnumValues.Count > 0)
                {
                    target.SelectValues = new List<SelectValueModel>();
                    foreach (var value in source.EnumValues)
                    {
                        target.SelectValues.Add(new SelectValueModel
                        {
                            Value = value.Value,
                            Text = value.TextId != null ? LanguageHelper.GetText(definition.ModuleType, value.TextId) : value.Value
                        });
                    }
                }
                else if (source.Type == InputFieldType.Select || source.Type == InputFieldType.SelectText || source.Type == InputFieldType.MultiSelect)
                {
                    if (inputSource == null && typeof(IInputSource).IsAssignableFrom(definition.ModelType))
                    {
                        inputSource = (IInputSource) DependencyResolver.Resolve(definition.ModelType);
                    }

                    if (inputSource != null)
                    {
                        target.SelectValues = inputSource.GetEnumValues(target.Property).Select(v => new SelectValueModel
                        {
                            Text = v.Text, 
                            Value = v.Value
                        }).ToList();
                    }
                }

                Fields.Add(target);
            }
        }
    }
}
