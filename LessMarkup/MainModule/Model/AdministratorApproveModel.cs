using LessMarkup.Interfaces.System;

namespace LessMarkup.MainModule.Model
{
    public class AdministratorApproveModel : MailTemplateModel
    {
        public string ConfirmLink { get; set; }
        public string BlockLink { get; set; }
        public string Email { get; set; }
        public long UserId { get; set; }
    }
}
