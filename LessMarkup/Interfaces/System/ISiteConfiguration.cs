using System.ComponentModel;
using LessMarkup.Interfaces.Cache;

namespace LessMarkup.Interfaces.System
{
    public interface ISiteConfiguration : ICacheHandler
    {
        [DefaultValue("Site")]
        string SiteName { get; }
        [DefaultValue(10)]
        int RecordsPerPage { get; }
        string NoReplyEmail { get; }
        string NoReplyName { get; }
        [DefaultValue("Registered")]
        string DefaultUserGroup { get; }
        [DefaultValue(1024 * 1024 * 10)]
        int MaximumFileSize { get; }
        [DefaultValue(75)]
        int ThumbnailWidth { get; }
        [DefaultValue(75)]
        int ThumbnailHeight { get; }
        [DefaultValue(800)]
        int MaximumImageWidth { get; }
        [DefaultValue(600)]
        int MaximumImageHeight { get; }
        [DefaultValue(false)]
        bool HasUsers { get; }
        [DefaultValue(false)]
        bool HasNavigationBar { get; }
        [DefaultValue(false)]
        bool HasSearch { get; }
        [DefaultValue(false)]
        bool UseLanguages { get; }
        [DefaultValue(false)]
        bool UseCurrencies { get; }
        [DefaultValue("Default")]
        string DefaultCronJobId { get; }
        string AdminLoginPage { get; }
        [DefaultValue(false)]
        bool AdminNotifyNewUsers { get; }
        [DefaultValue(false)]
        bool AdminApproveNewUsers { get; }
        string UserAgreement { get; }
        string GoogleAnalyticsResource { get; }
        [DefaultValue("image/*,text/*,application/vnd.ms-excel,application/msword")]
        string ValidFileType { get; }
        [DefaultValue("txt,jpg,png,gif,bmp,doc,xls")]
        string ValidFileExtension { get; }
    }
}
