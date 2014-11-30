using System;
using System.Collections.Generic;
using System.Data.Entity.Design.PluralizationServices;
using System.Globalization;
using System.Reflection;

namespace LessMarkup.Framework.Data
{
    class TableMetadata
    {
        private static readonly PluralizationService _pluralizationService = PluralizationService.CreateService(new CultureInfo("en-us"));

        public TableMetadata(Type sourceType)
        {
            Name = _pluralizationService.Pluralize(sourceType.Name);

            Columns = new Dictionary<string, PropertyInfo>();

            foreach (var property in sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Columns[property.Name] = property;
            }
        }

        public string Name { get; private set; }
        public Dictionary<string, PropertyInfo> Columns { get; private set; }
    }
}
