using TaskManagerSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace TaskManagerSystem.Data
{
    // Veritabaný ile etkileþim için kullanýlan DbContext sýnýfý
    public class AppDbContext : DbContext
    {
        // Constructor: DbContext seçeneklerini alýr ve base sýnýfa iletir
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Users tablosu için DbSet (kullanýcý verilerini tutar)
        public DbSet<User> Users { get; set; }

        // Tasks tablosu için DbSet (kullanýcý görevlerini tutar)
        public DbSet<UserTask> Tasks { get; set; }

        public DbSet<TaskAttachment> TaskAttachments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Ýliþki ayarlarý (Fluent API) - Opsiyonel ama saðlam olmasý için:
            modelBuilder.Entity<TaskAttachment>()
                .HasOne(a => a.Task)
                .WithMany(t => t.Attachments)
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.Cascade); // Görev silinince dosyalar da silinsin (Kural: 8.1)
        }
    }
}
