using Microsoft.EntityFrameworkCore;
using MarcaAsistencia.Models;

namespace MarcaAsistencia.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Asistencia> Asistencias { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                "Server=DESKTOP-5OD0LL7;Database=MarcaAsistenciaDB;Trusted_Connection=True;TrustServerCertificate=True;"
            );
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración simple
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.Password).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.FechaCreacion).HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<Asistencia>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NombreEmpleado).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => new { e.NombreEmpleado, e.Fecha }).IsUnique();
            });
        }
    }
}