using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<MonthlyBudget> MonthlyBudgets { get; set; }
        public DbSet<Platform> Platforms { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<CategoryBudget> CategoryBudgets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -------------------------------------------------------------
            // 1. Transaction Entity
            // -------------------------------------------------------------
            modelBuilder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasColumnType("decimal(12,2)");

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Platform)
                .HasMaxLength(50);

            modelBuilder.Entity<Transaction>()
                .Property(t => t.OrderNumber)
                .HasMaxLength(60);

            modelBuilder.Entity<Transaction>()
                .Property(t => t.PaymentMethod)
                .HasMaxLength(50);

            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.Platform);

            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.Date);

            // -------------------------------------------------------------
            // 2. MonthlyBudget Entity
            // -------------------------------------------------------------
            modelBuilder.Entity<MonthlyBudget>()
                .Property(b => b.Amount)
                .HasColumnType("decimal(12,2)");

            modelBuilder.Entity<MonthlyBudget>()
                .HasIndex(b => new { b.Year, b.Month })
                .IsUnique();

            // -------------------------------------------------------------
            // 3. Platform Entity (Section 4.A)
            // -------------------------------------------------------------
            modelBuilder.Entity<Platform>()
                .Property(p => p.Name)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Platform>()
                .Property(p => p.PlatformType)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<Platform>()
                .HasIndex(p => p.Name)
                .IsUnique();

            // Seed initial platforms
            modelBuilder.Entity<Platform>().HasData(
                new Platform { Id = 1, Name = "Swiggy", PlatformType = "Food Delivery", IsActive = true },
                new Platform { Id = 2, Name = "Zomato", PlatformType = "Food Delivery", IsActive = true },
                new Platform { Id = 3, Name = "Blinkit", PlatformType = "Groceries & Quick Commerce", IsActive = true },
                new Platform { Id = 4, Name = "Zepto", PlatformType = "Groceries & Quick Commerce", IsActive = true },
                new Platform { Id = 5, Name = "BigBasket", PlatformType = "Groceries & Quick Commerce", IsActive = true },
                new Platform { Id = 6, Name = "Amazon", PlatformType = "Shopping", IsActive = true },
                new Platform { Id = 7, Name = "Flipkart", PlatformType = "Shopping", IsActive = true },
                new Platform { Id = 8, Name = "Myntra", PlatformType = "Shopping", IsActive = true },
                new Platform { Id = 9, Name = "Other", PlatformType = "Other", IsActive = true }
            );

            // -------------------------------------------------------------
            // 4. Category Entity (Section 4.B)
            // -------------------------------------------------------------
            modelBuilder.Entity<Category>()
                .Property(c => c.Name)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Category>()
                .Property(c => c.CategoryType)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            // Seed initial categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Food", CategoryType = "Expense", IsActive = true },
                new Category { Id = 2, Name = "Groceries", CategoryType = "Expense", IsActive = true },
                new Category { Id = 3, Name = "Electronics", CategoryType = "Expense", IsActive = true },
                new Category { Id = 4, Name = "Clothing", CategoryType = "Expense", IsActive = true },
                new Category { Id = 5, Name = "Household", CategoryType = "Expense", IsActive = true },
                new Category { Id = 6, Name = "Health", CategoryType = "Expense", IsActive = true },
                new Category { Id = 7, Name = "Education", CategoryType = "Expense", IsActive = true },
                new Category { Id = 8, Name = "Travel", CategoryType = "Expense", IsActive = true },
                new Category { Id = 9, Name = "Subscriptions", CategoryType = "Expense", IsActive = true },
                new Category { Id = 10, Name = "Other", CategoryType = "Expense", IsActive = true }
            );

            // -------------------------------------------------------------
            // 5. PurchaseOrder Entity (Section 4.C)
            // -------------------------------------------------------------
            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.ItemSubtotal)
                .HasColumnType("decimal(12,2)");

            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.DeliveryFee)
                .HasColumnType("decimal(12,2)");

            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.PackagingFee)
                .HasColumnType("decimal(12,2)");

            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.PlatformFee)
                .HasColumnType("decimal(12,2)");

            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.Taxes)
                .HasColumnType("decimal(12,2)");

            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.OtherCharges)
                .HasColumnType("decimal(12,2)");

            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.Discount)
                .HasColumnType("decimal(12,2)");

            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.FinalBillAmount)
                .HasColumnType("decimal(12,2)");

            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.RefundAmount)
                .HasColumnType("decimal(12,2)");

            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.ExternalOrderNumber)
                .HasMaxLength(100);

            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.OrderStatus)
                .HasMaxLength(30)
                .IsRequired();

            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.PaymentMethod)
                .HasMaxLength(50);

            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.SourceType)
                .HasMaxLength(50);

            // Unique constraint on (PlatformId, ExternalOrderNumber) when order number is present
            modelBuilder.Entity<PurchaseOrder>()
                .HasIndex(p => new { p.PlatformId, p.ExternalOrderNumber })
                .IsUnique();

            modelBuilder.Entity<PurchaseOrder>()
                .HasIndex(p => p.OrderDate);

            modelBuilder.Entity<PurchaseOrder>()
                .HasIndex(p => p.OrderStatus);

            // Relationship: Platform -> PurchaseOrders (Restrict delete to preserve historical records)
            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(p => p.Platform)
                .WithMany(pl => pl.PurchaseOrders)
                .HasForeignKey(p => p.PlatformId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship: PurchaseOrder -> OrderItems (Cascade delete so order deletion leaves no orphaned items)
            modelBuilder.Entity<PurchaseOrder>()
                .HasMany(p => p.Items)
                .WithOne(i => i.PurchaseOrder)
                .HasForeignKey(i => i.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // -------------------------------------------------------------
            // 6. OrderItem Entity (Section 4.D)
            // -------------------------------------------------------------
            modelBuilder.Entity<OrderItem>()
                .Property(i => i.ItemName)
                .HasMaxLength(150)
                .IsRequired();

            modelBuilder.Entity<OrderItem>()
                .Property(i => i.UnitPrice)
                .HasColumnType("decimal(12,2)");

            modelBuilder.Entity<OrderItem>()
                .Property(i => i.TotalPrice)
                .HasColumnType("decimal(12,2)");

            // Relationship: Category -> OrderItems (Restrict delete to preserve historical item references)
            modelBuilder.Entity<OrderItem>()
                .HasOne(i => i.Category)
                .WithMany(c => c.OrderItems)
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // -------------------------------------------------------------
            // 7. CategoryBudget Entity (Section 4.E)
            // -------------------------------------------------------------
            modelBuilder.Entity<CategoryBudget>()
                .Property(b => b.Amount)
                .HasColumnType("decimal(12,2)");

            // Unique index: (Year, Month, CategoryId)
            modelBuilder.Entity<CategoryBudget>()
                .HasIndex(b => new { b.Year, b.Month, b.CategoryId })
                .IsUnique();

            // Relationship: Category -> CategoryBudgets (Restrict delete)
            modelBuilder.Entity<CategoryBudget>()
                .HasOne(b => b.Category)
                .WithMany(c => c.CategoryBudgets)
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
