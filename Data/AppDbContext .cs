using Microsoft.EntityFrameworkCore;
using DRB_HMI_3D.Models;

namespace DRB_HMI_3D.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Workshop> Workshops { get; set; }
        public DbSet<PressGroup> PressGroups { get; set; }
        public DbSet<PressItem> PressItems { get; set; }
        public DbSet<PressTag> PressTags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PressGroup>()
                .HasOne(g => g.Workshop)
                .WithMany(w => w.PressGroups)
                .HasForeignKey(g => g.WorkshopId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PressItem>()
                .HasOne(p => p.PressGroup)
                .WithMany(g => g.PressItems)
                .HasForeignKey(p => p.PressGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PressTag>()
                .HasOne(t => t.PressItem)
                .WithMany(p => p.Tags)
                .HasForeignKey(t => t.PressItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Workshop>().Property(w => w.Name).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<PressGroup>().Property(g => g.Label).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<PressItem>().Property(p => p.Name).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<PressTag>().Property(t => t.Name).IsRequired().HasMaxLength(50);
        }
    }
}