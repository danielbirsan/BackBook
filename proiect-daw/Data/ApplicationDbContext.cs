using proiect_daw.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using static proiect_daw.Models.PostBookmarks;

namespace proiect_daw.Data
{
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
        public DbSet<GroupMembership> GroupMemberships { get; set; }
        public DbSet<GroupMessage> GroupMessages { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<FollowRequest> FollowRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Define the many-to-many relationship between Post and Bookmark
            modelBuilder.Entity<PostBookmark>()
                .HasKey(ab => new { ab.Id, ab.PostId, ab.BookmarkId });

            modelBuilder.Entity<PostBookmark>()
                .HasOne(ab => ab.Post)
                .WithMany(ab => ab.PostBookmarks)
                .HasForeignKey(ab => ab.PostId);

            modelBuilder.Entity<PostBookmark>()
                .HasOne(ab => ab.Bookmark)
                .WithMany(ab => ab.PostBookmarks)
                .HasForeignKey(ab => ab.BookmarkId);

            modelBuilder.Entity<FollowRequest>()
                .HasOne(fr => fr.Sender)
                .WithMany(u => u.SentFollowRequests)
                .HasForeignKey(fr => fr.SenderId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<FollowRequest>()
                .HasOne(fr => fr.Receiver)
                .WithMany(u => u.ReceivedFollowRequests)
                .HasForeignKey(fr => fr.ReceiverId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
