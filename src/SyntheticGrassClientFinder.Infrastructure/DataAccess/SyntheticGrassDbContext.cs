using Microsoft.EntityFrameworkCore;
using SyntheticGrassClientFinder.Domain.Entities;
using SyntheticGrassClientFinder.Domain.ValueObjects;

namespace SyntheticGrassClientFinder.Infrastructure.DataAccess;

public class SyntheticGrassDbContext : DbContext
{
    public SyntheticGrassDbContext(DbContextOptions<SyntheticGrassDbContext> options) : base(options) { }

    public DbSet<Client> Clients { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(entity =>
        {
            // Chave primária
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasConversion(
                    id => id.Value,
                    value => new ClientId(value))
                .ValueGeneratedNever();

            // Propriedades básicas
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Type)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.GooglePlaceId)
                .HasMaxLength(255);

            entity.Property(e => e.Rating)
                .HasPrecision(3, 2);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Address Value Object
            entity.OwnsOne(e => e.Address, address =>
            {
                address.Property(a => a.Street).HasColumnName("Street").HasMaxLength(500);
                address.Property(a => a.City).HasColumnName("City").HasMaxLength(100);
                address.Property(a => a.State).HasColumnName("State").HasMaxLength(50);
                address.Property(a => a.PostalCode).HasColumnName("PostalCode").HasMaxLength(20);
                address.Property(a => a.Country).HasColumnName("Country").HasMaxLength(50);
            });

            // Location Value Object
            entity.OwnsOne(e => e.Location, location =>
            {
                location.Property(l => l.Latitude).HasColumnName("Latitude").HasPrecision(10, 8);
                location.Property(l => l.Longitude).HasColumnName("Longitude").HasPrecision(11, 8);
            });

            // ContactInfo Value Object (opcional)
            entity.OwnsOne(e => e.ContactInfo, contact =>
            {
                contact.Property(c => c.Phone).HasColumnName("Phone").HasMaxLength(50);
                contact.Property(c => c.Email).HasColumnName("Email").HasMaxLength(255);
                contact.Property(c => c.Website).HasColumnName("Website").HasMaxLength(500);
                contact.Property(c => c.SocialMedia).HasColumnName("SocialMedia").HasMaxLength(500);
            });

            // Índices simples (não compostos)
            entity.HasIndex(e => e.Name).HasDatabaseName("IX_Clients_Name");
            entity.HasIndex(e => e.Type).HasDatabaseName("IX_Clients_Type");
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_Clients_Status");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_Clients_CreatedAt");
            entity.HasIndex(e => e.GooglePlaceId).HasDatabaseName("IX_Clients_GooglePlaceId");
        });

        base.OnModelCreating(modelBuilder);
    }
}
