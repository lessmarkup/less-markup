/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Reflection;

namespace LessMarkup.Interfaces.RecordModel
{
    public class InputFieldDefinition
    {
        public object TextId { get; set; }
        public InputFieldType Type { get; set; }
        public bool ReadOnly { get; set; }
        public string Id { get; set; }
        public bool Required { get; set; }
        public double? Width { get; set; }
        public string ReadOnlyCondition { get; set; }
        public string VisibleCondition { get; set; }
        public string Property { get; set; }
        public object DefaultValue { get; set; }
        public List<InputFieldEnum> EnumValues { get; set; }

        public void Initialize(InputFieldAttribute configuration, PropertyInfo property, string moduleType)
        {
            Id = configuration.Id;
            ReadOnly = configuration.ReadOnly;
            ReadOnlyCondition = configuration.ReadOnlyCondition;
            Required = configuration.Required;
            TextId = configuration.TextId;
            Type = configuration.Type;
            VisibleCondition = configuration.VisibleCondition;
            Width = configuration.Width;
            Property = property.Name;
            DefaultValue = configuration.DefaultValue;

            if (Type == InputFieldType.Select || Type == InputFieldType.SelectText || Type == InputFieldType.MultiSelect)
            {
                var propertyType = property.PropertyType;
                if (propertyType.IsEnum)
                {
                    InitializeEnum(propertyType, configuration.EnumTextIdBase);
                }
            }
        }

        private void InitializeEnum(Type enumType, object textIdBaseObj)
        {
            string textIdBase = null;

            if (textIdBaseObj != null)
            {
                textIdBase = textIdBaseObj as string ?? textIdBaseObj.ToString();
            }

            var values = Enum.GetValues(enumType);

            EnumValues = new List<InputFieldEnum>();

            foreach (var value in values)
            {
                var enumValue = new InputFieldEnum
                {
                    Value = value.ToString()
                };

                if (textIdBase != null)
                {
                    enumValue.TextId = textIdBase + enumValue.Value;
                }

                EnumValues.Add(enumValue);
            }
        }
    }
}
