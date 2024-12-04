using System.ComponentModel.DataAnnotations;

namespace proiect_daw.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Numele categoriei este obligatoriu")]
        public string CategoryName { get; set; }

        // proprietatea virtuala - dintr-o categorie fac parte mai multe postari
        public virtual ICollection<Post>? Posts { get; set; }
    }

}
