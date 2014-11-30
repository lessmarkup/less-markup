using LessMarkup.Framework;
using LessMarkup.Interfaces.RecordModel;

namespace LessMarkup.MainModule.Model
{
    [RecordModel(TitleTextId = MainModuleTextIds.EditPage)]
    public class HtmlPageModel
    {
        [InputField(InputFieldType.CodeText, MainModuleTextIds.Body, Required = true)]
        public string Body { get; set; }
    }
}
