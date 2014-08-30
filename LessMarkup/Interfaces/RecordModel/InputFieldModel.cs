using System.Collections.Generic;

namespace LessMarkup.Interfaces.RecordModel
{
    public class InputFieldModel
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
        public List<SelectValueModel> SelectValues { get; set; }
        public object DefaultValue { get; set; }
    }
}
