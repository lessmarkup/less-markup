using LessMarkup.Interfaces.RecordModel;

namespace LessMarkup.Forum.Model
{
    [RecordModel(TitleTextId = ForumTextIds.ForumConfiguration)]
    public class ForumConfigurationModel
    {
        public ForumConfigurationModel()
        {
            HasThreads = true;
        }

        [InputField(InputFieldType.CheckBox, ForumTextIds.HasThreads, DefaultValue = true)]
        public bool HasThreads { get; set; }
    }
}
