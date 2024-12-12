using System.ComponentModel.DataAnnotations;

namespace proiect_daw.Models
{
    public class Group
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }


        public string? Moderator { get; set; }

        public DateTime Date { get; set; }

        public virtual ICollection<ApplicationUser>? Users { get; set; }

    }
}
