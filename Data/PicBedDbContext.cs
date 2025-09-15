using Microsoft.EntityFrameworkCore;
using PicBed.Models;

namespace PicBed.Data
{
    public class PicBedDbContext : DbContext
    {
        public PicBedDbContext(DbContextOptions<PicBedDbContext> options) : base(options)
        {
        }

        public DbSet<ImageRecord> Images { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ImageRecord>(entity =>
            {
                entity.HasIndex(e => e.FileName).IsUnique();
                entity.HasIndex(e => e.UploadTime);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.IsPublic);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email);
            });
        }
    }
}
