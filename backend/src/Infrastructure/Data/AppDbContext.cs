using Microsoft.EntityFrameworkCore;
using Core.Entities;

namespace Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Article> Articles => Set<Article>();
    public DbSet<ServicePrice> ServicePrices => Set<ServicePrice>();
    public DbSet<Callback> Callbacks => Set<Callback>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<ArticleMedia> ArticleMedia => Set<ArticleMedia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Article>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Slug).IsRequired();
            entity.Property(e => e.Content).IsRequired();
        });

        modelBuilder.Entity<ArticleMedia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Path).IsRequired();
            entity.Property(e => e.MediaType).IsRequired();
            entity.HasOne(e => e.Article)
                .WithMany(a => a.Media)
                .HasForeignKey(e => e.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ServicePrice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).IsRequired();
            entity.Property(e => e.Name).IsRequired();
        });

        modelBuilder.Entity<Callback>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Phone).IsRequired();
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Phone).IsRequired();
            entity.Property(e => e.Message).IsRequired();
        });
    }
}
