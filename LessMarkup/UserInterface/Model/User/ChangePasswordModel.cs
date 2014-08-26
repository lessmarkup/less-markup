using LessMarkup.Interfaces.RecordModel;

namespace LessMarkup.UserInterface.Model.User
{
    [RecordModel(TitleTextId = UserInterfaceTextIds.ChangePassword)]
    public class ChangePasswordModel
    {
        [InputField(InputFieldType.PasswordRepeat, UserInterfaceTextIds.Password)]
        public string Password { get; set; }
    }
}
