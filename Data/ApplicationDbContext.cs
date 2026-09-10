using ElectricalBilling.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectricalBilling.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<BusinessSetting> BusinessSettings { get; set; } = null!;
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Service> Services { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<InvoiceItem> InvoiceItems { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
            });

            modelBuilder.Entity<BusinessSetting>(entity =>
            {
                entity.Property(b => b.DefaultGstPercent).HasColumnType("decimal(5,2)");
                entity.Property(b => b.CgstPercent).HasColumnType("decimal(5,2)");
                entity.Property(b => b.SgstPercent).HasColumnType("decimal(5,2)");
            });

            modelBuilder.Entity<Customer>(entity =>
            {
                // Not unique: a family/business may share one mobile number
                // across multiple customer records. Indexed for fast search.
                entity.HasIndex(c => c.Mobile);
                entity.HasIndex(c => c.CustomerName);
            });

            modelBuilder.Entity<Service>(entity =>
            {
                entity.HasIndex(s => s.ServiceName);

                // Seed the common services mentioned on the original bill so
                // the invoice screen has useful options from day one.
                entity.HasData(
                    new Service { ServiceId = 1, ServiceName = "Power Factor Maintenance", IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
                    new Service { ServiceId = 2, ServiceName = "Electric Work", IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
                    new Service { ServiceId = 3, ServiceName = "HT/LT Cabling", IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
                    new Service { ServiceId = 4, ServiceName = "Earth Testing", IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
                    new Service { ServiceId = 5, ServiceName = "Transformer Oil Testing", IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
                    new Service { ServiceId = 6, ServiceName = "APFC Panel", IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
                    new Service { ServiceId = 7, ServiceName = "Electrical Guidance", IsActive = true, CreatedAt = new DateTime(2026, 1, 1) }
                );
            });

            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasIndex(i => i.InvoiceNumber).IsUnique();
                entity.HasIndex(i => i.InvoiceDate);
                entity.HasIndex(i => i.CustomerId);

                entity.Property(i => i.CgstPercent).HasColumnType("decimal(5,2)");
                entity.Property(i => i.SgstPercent).HasColumnType("decimal(5,2)");
                entity.Property(i => i.SubTotal).HasColumnType("decimal(12,2)");
                entity.Property(i => i.CgstAmount).HasColumnType("decimal(12,2)");
                entity.Property(i => i.SgstAmount).HasColumnType("decimal(12,2)");
                entity.Property(i => i.GrandTotal).HasColumnType("decimal(12,2)");

                // Stored as text rather than an int so the raw table is
                // readable without joining against the enum definition.
                entity.Property(i => i.Status)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.HasOne(i => i.Customer)
                    .WithMany()
                    .HasForeignKey(i => i.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<InvoiceItem>(entity =>
            {
                entity.Property(x => x.Qty).HasColumnType("decimal(10,2)");
                entity.Property(x => x.Rate).HasColumnType("decimal(10,2)");
                entity.Property(x => x.Amount).HasColumnType("decimal(12,2)");

                entity.HasOne(x => x.Invoice)
                    .WithMany(i => i.Items)
                    .HasForeignKey(x => x.InvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Service)
                    .WithMany()
                    .HasForeignKey(x => x.ServiceId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.Property(p => p.Amount).HasColumnType("decimal(12,2)");

                // Stored as text like Invoice.Status so the raw table stays
                // human-readable without joining against the enum definition.
                entity.Property(p => p.PaymentMode)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.HasIndex(p => p.InvoiceId);
                entity.HasIndex(p => p.PaymentDate);

                entity.HasOne(p => p.Invoice)
                    .WithMany(i => i.Payments)
                    .HasForeignKey(p => p.InvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
