using AuralIndex.Domain;
using Microsoft.EntityFrameworkCore;

namespace AuralIndex.Infrastructure;

public class AuralIndexDbContext : DbContext
{
    public AuralIndexDbContext(DbContextOptions<AuralIndexDbContext> options) : base(options) { }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<UserEvent> UserEvents => Set<UserEvent>();
    public DbSet<FavoriteBook> FavoriteBooks => Set<FavoriteBook>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(e =>
        {
            e.ToTable("Books");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.Author).HasMaxLength(200).IsRequired();
            e.Property(x => x.Genre).HasMaxLength(100).IsRequired();
            e.Property(x => x.Language).HasMaxLength(50).IsRequired();
            e.Property(x => x.Narrator).HasMaxLength(200);
            e.HasIndex(x => x.Title);
            e.HasIndex(x => x.Author);
            e.HasIndex(x => x.Genre);
            e.HasIndex(x => x.Language);
        });

        modelBuilder.Entity<UserEvent>(e =>
        {
            e.ToTable("UserEvents");
            e.HasKey(x => x.Id);
            e.Property(x => x.EventType).HasMaxLength(40).IsRequired();
            e.Property(x => x.QueryText).HasMaxLength(500);
            e.Property(x => x.BookTitle).HasMaxLength(300);
            e.Property(x => x.CreatedAtUtc).IsRequired();
            e.HasIndex(x => x.CreatedAtUtc);
            e.HasIndex(x => x.BookId);
        });

        modelBuilder.Entity<FavoriteBook>(e =>
        {
            e.ToTable("FavoriteBooks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.Author).HasMaxLength(200).IsRequired();
            e.Property(x => x.Genre).HasMaxLength(100).IsRequired();
            e.Property(x => x.Language).HasMaxLength(50).IsRequired();
            e.Property(x => x.CreatedAtUtc).IsRequired();
            e.HasIndex(x => x.BookId).IsUnique();
            e.HasIndex(x => x.CreatedAtUtc);
        });
    }
}
