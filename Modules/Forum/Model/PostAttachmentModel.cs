using System.Linq;
using System.Web.Mvc;
using LessMarkup.Forum.DataObjects;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Forum.Model
{
    public class PostAttachmentModel
    {
        public string FileName { get; set; }
        public long Id { get; set; }
        public string Url { get; set; }

        public static ActionResult CreateResult(long threadId, long postId, long attachmentId, IDomainModelProvider domainModelProvider)
        {
            using (var domainModel = domainModelProvider.Create())
            {
                var attachment = domainModel.GetSiteCollection<PostAttachment>().FirstOrDefault(a => a.Id == attachmentId && a.PostId == postId && a.Post.ThreadId == threadId);

                if (attachment == null)
                {
                    return new HttpNotFoundResult();
                }

                return new FileContentResult(attachment.Data, attachment.ContentType);
            }
        }
    }
}
