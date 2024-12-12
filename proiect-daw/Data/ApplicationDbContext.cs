using proiect_daw.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using static proiect_daw.Models.PostBookmarks;
using Microsoft.Identity.Client;

namespace proiect_daw.Data
{
    // PASUL 3: useri si roluri
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Bookmark> Bookmarks { get; set; }
        public DbSet<PostBookmark> PostBookmarks { get; set; }

        public DbSet<Group> Groups { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // definirea relatiei many-to-many dintre Post si Bookmark

            base.OnModelCreating(modelBuilder);

            // definire primary key compus
            modelBuilder.Entity<PostBookmark>()
                .HasKey(ab => new { ab.Id, ab.PostId, ab.BookmarkId });


            // definire relatii cu modelele Bookmark si Post (FK)

            modelBuilder.Entity<PostBookmark>()
                .HasOne(ab => ab.Post)
                .WithMany(ab => ab.PostBookmarks)
                .HasForeignKey(ab => ab.PostId);

            modelBuilder.Entity<PostBookmark>()
                .HasOne(ab => ab.Bookmark)
                .WithMany(ab => ab.PostBookmarks)
                .HasForeignKey(ab => ab.BookmarkId);
        }
    }
}
