using LessMarkup.Framework;
using LessMarkup.Interfaces.RecordModel;

namespace LessMarkup.MainModule.Model
{
    [RecordModel(TitleTextId = MainModuleTextIds.EditPage)]
    public class HtmlPageModel
    {
        [InputField(InputFieldType.CodeText, MainModuleTextIds.Body)]
        public string Body { get; set; }

        [InputField(InputFieldType.CodeText, MainModuleTextIds.Code)]
        public string Code { get; set; }
    }
}
