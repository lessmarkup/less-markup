using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LessMarkup.Framework.Helpers;
using LessMarkup.Framework.NodeHandlers;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.Model.Common;

namespace LessMarkup.UserInterface.NodeHandlers.Common
{
    public abstract class PropertiesNodeHandler : AbstractNodeHandler
    {
        class PropertyDefinition
        {
            public string Name { get; set; }
            public InputFieldType Type { get; set; }
            public object Value { get; set; }
        }

        private readonly IModuleProvider _moduleProvider;
        private readonly List<PropertyDefinition> _properties = new List<PropertyDefinition>();

        protected PropertiesNodeHandler(IModuleProvider moduleProvider)
        {
            _moduleProvider = moduleProvider;
        }

        protected void AddProperty(string name, InputFieldType type, object value)
        {
            _properties.Add(new PropertyDefinition
            {
                Name = name,
                Type = type,
                Value = value
            });
        }

        protected override string ViewType
        {
            get { return "Properties"; }
        }

        protected override Dictionary<string, object> GetViewData()
        {
            var moduleConfiguration = _moduleProvider.Modules.First(m => m.Assembly == GetType().Assembly);

            var properties = new List<PropertyModel>();

            foreach (var property in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var propertyAttribute = property.GetCustomAttribute<PropertyAttribute>();

                if (propertyAttribute == null)
                {
                    continue;
                }

                var value = property.GetValue(this);

                if (value == null)
                {
                    continue;
                }

                var model = new PropertyModel
                {
                    Name = LanguageHelper.GetText(moduleConfiguration.ModuleType, propertyAttribute.TextId),
                    Value = value,
                    Type = propertyAttribute.Type
                };

                switch (model.Type)
                {
                    case InputFieldType.Image:
                        var valueType = model.Value.GetType();
                        if (valueType == typeof(long?))
                        {
                            var imageId = (long?) model.Value;
                            model.Value = ImageHelper.ImageUrl(imageId.Value);
                        }
                        else if (valueType == typeof(long))
                        {
                            var imageId = (long) model.Value;
                            model.Value = ImageHelper.ImageUrl(imageId);
                        }
                        break;
                }

                properties.Add(model);
            }

            foreach (var item in _properties)
            {
                var model = new PropertyModel
                {
                    Name = item.Name,
                    Value = item.Value,
                    Type = item.Type
                };

                properties.Add(model);
            }

            return new Dictionary<string, object>
            {
                {"Properties", properties.Select(p => new
                {
                    name = p.Name, value = p.Value, type = p.Type
                }).ToList()}
            };
        }
    }
}
