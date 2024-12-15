using System.ComponentModel.DataAnnotations;

namespace proiect_daw.Models
{
    public class GroupMessage
    {
        [Key]
        public int Id { get; set; }

        public string Content { get; set; }
        public string UserId { get; set; }
        public int GroupId { get; set; }
        public ApplicationUser User { get; set; }
        public Group Group { get; set; }
    }
}
