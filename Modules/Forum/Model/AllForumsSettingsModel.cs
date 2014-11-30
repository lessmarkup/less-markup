using LessMarkup.Interfaces.RecordModel;

namespace LessMarkup.Forum.Model
{
    [RecordModel]
    public class AllForumsSettingsModel
    {
        [InputField(InputFieldType.CheckBox, ForumTextIds.ShowStatistics, DefaultValue = true)]
        public bool ShowStatistics { get; set; }
    }
}
