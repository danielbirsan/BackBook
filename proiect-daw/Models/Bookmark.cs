using static proiect_daw.Models.ArticleBookmarks;
using System.ComponentModel.DataAnnotations;

namespace proiect_daw.Models
{
    public class Bookmark
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Numele colectiei este obligatoriu")]
        public string Name { get; set; }

        // o colectie este creata de catre un user
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        // relatia many-to-many dintre Article si Bookmark
        public virtual ICollection<ArticleBookmark>? ArticleBookmarks { get; set; }
    }
}
