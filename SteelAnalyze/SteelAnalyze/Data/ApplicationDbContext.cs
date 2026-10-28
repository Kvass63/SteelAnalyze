using Microsoft.EntityFrameworkCore;
using SteelAnalyze.Auth;
using SteelAnalyze.Models;

namespace SteelAnalyze.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<AppUser> Users => Set<AppUser>();
        public DbSet<DefectClass> DefectClasses => Set<DefectClass>();
        public DbSet<ReferenceSample> ReferenceSamples => Set<ReferenceSample>();
        public DbSet<Inspection> Inspections => Set<Inspection>();
        public DbSet<InspectionNeighbor> InspectionNeighbors => Set<InspectionNeighbor>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppUser>().HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<DefectClass>().HasIndex(c => c.Code).IsUnique();

            // Класс и пользователя нельзя удалить, пока на них ссылаются эталоны или журнал
            modelBuilder.Entity<ReferenceSample>(e =>
            {
                e.HasOne(r => r.DefectClass).WithMany().HasForeignKey(r => r.DefectClassId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(r => r.AddedBy).WithMany().HasForeignKey(r => r.AddedByUserId).OnDelete(DeleteBehavior.Restrict);

                // Быстрый поиск повторно загружаемого снимка по хешу содержимого
                e.HasIndex(r => r.ContentHash);
            });

            modelBuilder.Entity<Inspection>(e =>
            {
                e.HasOne(i => i.PredictedClass).WithMany().HasForeignKey(i => i.PredictedClassId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(i => i.Operator).WithMany().HasForeignKey(i => i.OperatorId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(i => i.ReviewedBy).WithMany().HasForeignKey(i => i.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);

                // Вердикты хранятся в базе словами, а не числами
                e.Property(i => i.Verdict).HasConversion<string>();
                e.Property(i => i.FinalVerdict).HasConversion<string>();

                // Ближайшие эталоны удаляются вместе с записью журнала
                e.HasMany(i => i.Neighbors).WithOne().HasForeignKey(n => n.InspectionId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<InspectionNeighbor>(e =>
            {
                e.HasOne(n => n.DefectClass).WithMany().HasForeignKey(n => n.DefectClassId).OnDelete(DeleteBehavior.Restrict);

                // При удалении эталона строка журнала остаётся, ссылка на эталон очищается
                e.HasOne(n => n.ReferenceSample).WithMany().HasForeignKey(n => n.ReferenceSampleId).OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
