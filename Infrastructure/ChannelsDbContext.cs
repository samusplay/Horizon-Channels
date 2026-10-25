using Horizon.Channels.Domain;
using Microsoft.EntityFrameworkCore;

namespace Horizon.Channels.Infrastructure;

public sealed class ChannelsDbContext(DbContextOptions<ChannelsDbContext> options)
    : DbContext(options)
{
    // la tabla Channels
    public DbSet<Channel> Channels => Set<Channel>();

    // aquí se describe cómo se guarda Channel en SQL Server
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Channel>(e =>
        {
            e.ToTable("Channels");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).ValueGeneratedNever(); // el Id lo genera Channel.Create

            e.Property(c => c.Name).HasMaxLength(50).IsRequired();
            e.Property(c => c.Handle).HasMaxLength(30).IsRequired();
            e.Property(c => c.Description).HasMaxLength(500);
            e.Property(c => c.BannerUrl).HasMaxLength(500);
            e.Property(c => c.Status).HasConversion<string>().HasMaxLength(30); // guarda el texto, no un número
            e.Property(c => c.ConfirmationTokenHash).HasMaxLength(64);

            e.HasIndex(c => c.OwnerId).IsUnique();    // un canal por usuario
            e.HasIndex(c => c.Handle).IsUnique();     // la URL del canal no se repite
            e.HasIndex(c => c.ConfirmationTokenHash); // para encontrar el canal al confirmar
        });
    }
}