using System.ComponentModel.DataAnnotations;

namespace proiect_daw.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Continutul comentariului este obligatoriu")]
        public string Content { get; set; }

        public DateTime Date { get; set; }

        // cheie externa (FK) - un comentariu apartine unui postare
        public int PostId { get; set; }

        // PASUL 6: useri si roluri 
        // cheie externa (FK) - un comentariu este postat de catre un user
        public string? UserId {  get; set; }

        // PASUL 6: useri si roluri 
        // proprietatea virtuala - un comentariu este postat de catre un user
        public virtual ApplicationUser? User { get; set; }

        // proprietatea virtuala - un comentariu apartine unui postare
        public virtual Post? Post { get; set; }
    }

}
