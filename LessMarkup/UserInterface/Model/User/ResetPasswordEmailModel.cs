using LessMarkup.Interfaces.System;

namespace LessMarkup.UserInterface.Model.User
{
    public class ResetPasswordEmailModel : MailTemplateModel
    {
        public string ResetUrl { get; set; }

        public string SiteName { get; set; }

        public string HostName { get; set; }
    }
}
