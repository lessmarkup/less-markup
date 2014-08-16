using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LessMarkup.Framework.Helpers;
using LessMarkup.Framework.NodeHandlers;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.UserInterface.NodeHandlers.Common
{
    public abstract class PropertiesNodeHandler : AbstractNodeHandler
    {
        private readonly IModuleProvider _moduleProvider;
        private readonly List<Tuple<string, object>> _properties = new List<Tuple<string, object>>();

        protected PropertiesNodeHandler(IModuleProvider moduleProvider)
        {
            _moduleProvider = moduleProvider;
        }

        protected void AddProperty(string name, object textId)
        {
            _properties.Add(Tuple.Create(name, textId));
        }

        protected override string ViewType
        {
            get { return "Properties"; }
        }

        protected override Dictionary<string, object> GetViewData()
        {
            var moduleConfiguration = _moduleProvider.Modules.First(m => m.Assembly == GetType().Assembly);

            var properties = new List<Tuple<string, string>>();

            foreach (var property in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var propertyAttribute = property.GetCustomAttribute<PropertyAttribute>();

                if (propertyAttribute == null)
                {
                    continue;
                }

                properties.Add(Tuple.Create(LanguageHelper.GetText(moduleConfiguration.ModuleType, propertyAttribute.TextId), property.GetValue(this).ToString()));
            }

            foreach (var item in _properties)
            {
                var property = GetType().GetProperty(item.Item1);

                properties.Add(Tuple.Create(LanguageHelper.GetText(moduleConfiguration.ModuleType, item.Item2), property.GetValue(this).ToString()));
            }

            return new Dictionary<string, object>()
            {
                {"Properties", properties.Select(p => new {name = p.Item1, value = p.Item2}).ToList()}
            };
        }
    }
}
