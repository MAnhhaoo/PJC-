using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class SystemLog
    {
        [Key]
        public int LogId { get; set; }

        [ForeignKey("User")]
        public int? UserId { get; set; }
        public User User { get; set; }

        public string Action { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
