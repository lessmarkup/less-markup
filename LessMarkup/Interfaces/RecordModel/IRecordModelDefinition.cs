/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

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
        void ValidateInput(object objectToValidate, bool isNew, string properties);
        bool SubmitWithCaptcha { get; }
    }
}
