using LessMarkup.Interfaces.RecordModel;

namespace LessMarkup.UserInterface.Model.Common
{
    public class PropertyModel
    {
        public string Name { get; set; }
        public object Value { get; set; }
        public InputFieldType Type { get; set; }
    }
}
