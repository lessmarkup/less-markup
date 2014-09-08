/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Web.Mvc;
using LessMarkup.Forum.Model;
using LessMarkup.Framework.NodeHandlers;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Forum.Module.NodeHandlers
{
    public class PostAttachmentsNodeHandler : AbstractNodeHandler
    {
        private long _threadId;
        private long _postId;
        private long _attachmentId;
        private readonly IDomainModelProvider _domainModelProvider;

        public PostAttachmentsNodeHandler(IDomainModelProvider domainModelProvider)
        {
            _domainModelProvider = domainModelProvider;
        }

        public void Initialize(long threadId, long postId, long attachmentId)
        {
            _threadId = threadId;
            _postId = postId;
            _attachmentId = attachmentId;
        }

        protected override ActionResult CreateResult(string path)
        {
            if (path == null)
            {
                return PostAttachmentModel.CreateResult(_threadId, _postId, _attachmentId, _domainModelProvider);
            }

            return null;
        }
    }
}
