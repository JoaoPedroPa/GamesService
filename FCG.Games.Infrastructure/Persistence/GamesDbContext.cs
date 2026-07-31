using FCG.Games.Infrastructure.Events;
using FCG.Games.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace FCG.Games.Infrastructure.Persistence; 
public sealed class GamesDbContext(DbContextOptions<GamesDbContext> o) : DbContext(o) 
{ 
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<LibraryItem> Library => Set<LibraryItem>(); 
    public DbSet<StoredEvent> StoredEvents => Set<StoredEvent>();
    protected override void OnModelCreating(ModelBuilder b) 
    { 
        b.Entity<Game>().Property(x => x.Price).HasPrecision(18, 2);
        b.Entity<Purchase>().Property(x => x.OriginalAmount).HasPrecision(18, 2);
        b.Entity<Purchase>().Property(x => x.FinalAmount).HasPrecision(18, 2);
        b.Entity<LibraryItem>().HasIndex(x => new { x.UserId, x.GameId }).IsUnique();

        b.Entity<StoredEvent>(eventBuilder =>
        {
            eventBuilder.ToTable("StoredEvents");
            eventBuilder.HasKey(x => x.Id);
            eventBuilder.Property(x => x.AggregateType).HasMaxLength(150).IsRequired();
            eventBuilder.Property(x => x.AggregateId).HasMaxLength(150).IsRequired();
            eventBuilder.Property(x => x.EventType).HasMaxLength(200).IsRequired();
            eventBuilder.Property(x => x.Data).HasColumnType("nvarchar(max)").IsRequired();
            eventBuilder.Property(x => x.TraceId).HasMaxLength(64);
            eventBuilder.HasIndex(x => new { x.AggregateType, x.AggregateId, x.Version }).IsUnique();
            eventBuilder.HasIndex(x => x.OccurredAtUtc);
        });
    } 
}
