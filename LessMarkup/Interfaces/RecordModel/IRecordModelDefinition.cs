using System;
using System.Collections.Generic;

namespace LessMarkup.Interfaces.RecordModel
{
    public interface IRecordModelDefinition
    {
        Type CollectionType { get; }
        object TitleTextId { get; }
        string ModuleType { get; }
        Type DataType { get; }
        string Id { get; }
        IReadOnlyList<InputFieldDefinition> Fields { get; }
        IReadOnlyList<ColumnDefinition> Columns { get; }
        void ValidateInput(object objectToValidate, bool isNew);
    }
}
