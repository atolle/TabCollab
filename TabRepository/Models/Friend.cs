using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TabRepository.Models
{
    public class Friend
    {
        [Key, ForeignKey("User1")]
        public string User1Id { get; set; }

        [Key, ForeignKey("User2")]
        public string User2Id { get; set; }

        public FriendStatus Status { get; set; }

        public string ActingUserId { get; set; }

        public virtual ApplicationUser User1 { get; set; }

        public virtual ApplicationUser User2 { get; set; }

        public virtual ApplicationUser ActingUser { get; set; }
    }

    public enum FriendStatus
    {
        None,
        Friends,
        Blocked,
        Requested
    }

    public enum Direction
    {
        From,
        To
    }
}