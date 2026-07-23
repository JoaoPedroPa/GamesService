using FCG.Games.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace FCG.Games.Infrastructure.Persistence; 
public sealed class GamesDbContext(DbContextOptions<GamesDbContext> o) : DbContext(o) 
{ 
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<LibraryItem> Library => Set<LibraryItem>(); 
    protected override void OnModelCreating(ModelBuilder b) 
    { 
        b.Entity<Game>().Property(x => x.Price).HasPrecision(18, 2);
        b.Entity<Purchase>().Property(x => x.OriginalAmount).HasPrecision(18, 2);
        b.Entity<Purchase>().Property(x => x.FinalAmount).HasPrecision(18, 2);
        b.Entity<LibraryItem>().HasIndex(x => new { x.UserId, x.GameId }).IsUnique();
    } 
}
