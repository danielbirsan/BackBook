using System.ComponentModel.DataAnnotations.Schema;

namespace proiect_daw.Models
{
    public class PostBookmarks
    {
        // tabelul asociativ care face legatura intre Post si Bookmark
        // un articol are mai multe colectii din care face parte
        // iar o colectie contine mai multe postari in cadrul ei
        public class PostBookmark
        {
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            // cheie primara compusa (Id, PostId, BookmarkId)
            public int Id { get; set; }
            public int? PostId { get; set; }
            public int? BookmarkId { get; set; }

            public virtual Post? Post { get; set; }
            public virtual Bookmark? Bookmark { get; set; }

            public DateTime BookmarkDate { get; set; }
        }
    }
}
