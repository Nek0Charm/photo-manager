using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<PhotoTag> PhotoTags { get; set; }
        public DbSet<UserAiSetting> UserAiSettings { get; set; }
        public DbSet<AiTagSuggestion> AiTagSuggestions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 确保用户名和邮箱唯一
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<UserAiSetting>()
                .HasIndex(s => s.UserId)
                .IsUnique();

            modelBuilder.Entity<UserAiSetting>()
                .HasOne(s => s.User)
                .WithOne(u => u.AiSetting)
                .HasForeignKey<UserAiSetting>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Tag>()
                .HasIndex(t => new { t.Name, t.Type })
                .IsUnique();

            modelBuilder.Entity<PhotoTag>()
                .HasKey(pt => new { pt.PhotoId, pt.TagId });

            modelBuilder.Entity<PhotoTag>()
                .HasOne(pt => pt.Photo)
                .WithMany(p => p.PhotoTags)
                .HasForeignKey(pt => pt.PhotoId);

            modelBuilder.Entity<PhotoTag>()
                .HasOne(pt => pt.Tag)
                .WithMany(t => t.PhotoTags)
                .HasForeignKey(pt => pt.TagId);

            modelBuilder.Entity<AiTagSuggestion>()
                .HasIndex(s => new { s.UserId, s.Name })
                .IsUnique();

            modelBuilder.Entity<AiTagSuggestion>()
                .HasOne(s => s.Photo)
                .WithMany()
                .HasForeignKey(s => s.PhotoId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
