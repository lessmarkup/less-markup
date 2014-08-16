using System;
using System.Collections.Generic;

namespace LessMarkup.Forum.Model
{
    public class ForumStatisticsModel
    {
        internal ForumStatisticsModel Parent { get; set; }
        public List<ForumStatisticsModel> Children { get; set; }

        public string Title { get; set; }
        public long Id { get; set; }
        public int Level { get; set; }
        public string Path { get; set; }
        public int Posts { get; set; }
        public long? LastAuthorId { get; set; }
        public string LastAuthorName { get; set; }
        public string LastAuthorUrl { get; set; }
        public DateTime? LastCreated { get; set; }
        public long? LastPostId { get; set; }
        public long? LastThreadId { get; set; }
        public string LastThreadTitle { get; set; }
        public bool IsHeader { get; set; }
    }
}
