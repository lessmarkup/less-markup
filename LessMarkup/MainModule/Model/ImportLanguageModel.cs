using LessMarkup.Engine.Language;
using LessMarkup.Interfaces.RecordModel;

namespace LessMarkup.MainModule.Model
{
    [RecordModel(TitleTextId = MainModuleTextIds.Import)]
    public class ImportLanguageModel
    {
        [InputField(InputFieldType.File, MainModuleTextIds.FileToImport, Required = true)]
        public InputFile ImportFile { get; set; }
    }
}
