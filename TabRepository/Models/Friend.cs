using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TabRepository.Models;

namespace TabRespository.Models
{
    public class Friend
    {
        [Key, ForeignKey("User1")]
        public string User1Id { get; set; }

        [Key, ForeignKey("User2")]
        public string User2Id { get; set; }

        public int Status { get; set; }

        public string ActingUserId { get; set; }

        public virtual ApplicationUser User1 { get; set; }

        public virtual ApplicationUser User2 { get; set; }

        public virtual ApplicationUser ActingUser { get; set; }
    }
}