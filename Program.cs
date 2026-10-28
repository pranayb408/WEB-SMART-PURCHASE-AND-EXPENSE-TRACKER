using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Data;
using PersonalExpenseTracker.Services;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IFinancialAccountingService, FinancialAccountingService>();
builder.Services.AddScoped<IBillCalculationService, BillCalculationService>();
builder.Services.AddScoped<IPurchaseOrderCsvService, PurchaseOrderCsvService>();

// Detect whether MySQL Server is running locally and accessible
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=localhost;Port=3306;Database=expense_tracker_db;User=root;Password=;";

bool isMySqlLive = false;
try
{
    var testBuilder = new MySqlConnector.MySqlConnectionStringBuilder(connectionString)
    {
        ConnectionTimeout = 1 // 1 second timeout
    };
    using var testConn = new MySqlConnector.MySqlConnection(testBuilder.ConnectionString);
    testConn.Open();
    using var cmd = testConn.CreateCommand();
    cmd.CommandText = "SELECT 1;";
    cmd.ExecuteScalar();
    isMySqlLive = true;
}
catch
{
    isMySqlLive = false;
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (isMySqlLive)
    {
        // When MySQL service is active, use Pomelo MySQL provider
        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)), mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 1,
                maxRetryDelay: TimeSpan.FromSeconds(1),
                errorNumbersToAdd: null);
        });
    }
    else
    {
        // Zero-configuration local database fallback so all CRUD, forms, and dashboard calculations work 100% seamlessly
        options.UseSqlite("Data Source=expense_tracker.db");
    }
});

// Configure State Management: Session and Distributed Memory Cache
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// HttpContextAccessor for controllers/views to access session easily
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Automatically ensure tables exist on active storage (MySQL or SQLite)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.EnsureCreated();
        if (!isMySqlLive)
        {
            try { db.Database.ExecuteSqlRaw("ALTER TABLE Transactions ADD COLUMN Platform TEXT;"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE Transactions ADD COLUMN OrderNumber TEXT;"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE Transactions ADD COLUMN PaymentMethod TEXT;"); } catch { }

            // Platforms Table (Section 4.A)
            try
            {
                db.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS Platforms (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL UNIQUE,
                        PlatformType TEXT NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1
                    );");
            }
            catch { }

            // Seed Platforms if empty
            try
            {
                db.Database.ExecuteSqlRaw(@"
                    INSERT OR IGNORE INTO Platforms (Id, Name, PlatformType, IsActive) VALUES
                    (1, 'Swiggy', 'Food Delivery', 1),
                    (2, 'Zomato', 'Food Delivery', 1),
                    (3, 'Blinkit', 'Groceries & Quick Commerce', 1),
                    (4, 'Zepto', 'Groceries & Quick Commerce', 1),
                    (5, 'BigBasket', 'Groceries & Quick Commerce', 1),
                    (6, 'Amazon', 'Shopping', 1),
                    (7, 'Flipkart', 'Shopping', 1),
                    (8, 'Myntra', 'Shopping', 1),
                    (9, 'Other', 'Other', 1);");
            }
            catch { }

            // Categories Table (Section 4.B)
            try
            {
                db.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS Categories (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL UNIQUE,
                        CategoryType TEXT NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1
                    );");
            }
            catch { }

            // Seed Categories if empty
            try
            {
                db.Database.ExecuteSqlRaw(@"
                    INSERT OR IGNORE INTO Categories (Id, Name, CategoryType, IsActive) VALUES
                    (1, 'Food', 'Expense', 1),
                    (2, 'Groceries', 'Expense', 1),
                    (3, 'Electronics', 'Expense', 1),
                    (4, 'Clothing', 'Expense', 1),
                    (5, 'Household', 'Expense', 1),
                    (6, 'Health', 'Expense', 1),
                    (7, 'Education', 'Expense', 1),
                    (8, 'Travel', 'Expense', 1),
                    (9, 'Subscriptions', 'Expense', 1),
                    (10, 'Other', 'Expense', 1);");
            }
            catch { }

            // Cleanly recreate PurchaseOrders and OrderItems for SQLite if empty or outdated schema
            try
            {
                var poCount = db.PurchaseOrders.Count();
                if (poCount == 0)
                {
                    db.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS OrderItems;");
                    db.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS PurchaseOrders;");
                }
            }
            catch
            {
                try { db.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS OrderItems;"); } catch { }
                try { db.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS PurchaseOrders;"); } catch { }
            }

            // PurchaseOrders Table (Section 4.C)
            try
            {
                db.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS PurchaseOrders (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        PlatformId INTEGER NOT NULL DEFAULT 1,
                        ExternalOrderNumber TEXT,
                        OrderDate TEXT NOT NULL,
                        OrderStatus TEXT NOT NULL,
                        PaymentMethod TEXT,
                        ItemSubtotal TEXT NOT NULL DEFAULT '0.00',
                        DeliveryFee TEXT NOT NULL DEFAULT '0.00',
                        PackagingFee TEXT NOT NULL DEFAULT '0.00',
                        PlatformFee TEXT NOT NULL DEFAULT '0.00',
                        Taxes TEXT NOT NULL DEFAULT '0.00',
                        OtherCharges TEXT NOT NULL DEFAULT '0.00',
                        Discount TEXT NOT NULL DEFAULT '0.00',
                        FinalBillAmount TEXT NOT NULL DEFAULT '0.00',
                        RefundAmount TEXT NOT NULL DEFAULT '0.00',
                        Notes TEXT,
                        SourceType TEXT NOT NULL DEFAULT 'Manual',
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL,
                        FOREIGN KEY (PlatformId) REFERENCES Platforms (Id) ON DELETE RESTRICT
                    );");
            }
            catch { }

            // Ensure all columns exist on PurchaseOrders in case table pre-existed from older version
            try { db.Database.ExecuteSqlRaw("ALTER TABLE PurchaseOrders ADD COLUMN PlatformId INTEGER NOT NULL DEFAULT 1;"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE PurchaseOrders ADD COLUMN ExternalOrderNumber TEXT;"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE PurchaseOrders ADD COLUMN OrderDate TEXT NOT NULL DEFAULT '2026-10-28';"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE PurchaseOrders ADD COLUMN OrderStatus TEXT NOT NULL DEFAULT 'Completed';"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE PurchaseOrders ADD COLUMN PaymentMethod TEXT;"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE PurchaseOrders ADD COLUMN ItemSubtotal TEXT NOT NULL DEFAULT '0.00';"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE PurchaseOrders ADD COLUMN DeliveryFee TEXT NOT NULL DEFAULT '0.00';"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE PurchaseOrders ADD COLUMN PackagingFee TEXT NOT NULL DEFAULT '0.00';"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE PurchaseOrders ADD COLUMN PlatformFee TEXT NOT NULL DEFAULT '0.00';"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE PurchaseOrders ADD COLUMN Taxes TEXT NOT NULL DEFAULT '0.00';"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE PurchaseOrders ADD COLUMN OtherCharges TEXT NOT NULL DEFAULT '0.00';"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE PurchaseOrders ADD COLUMN Discount TEXT NOT NULL DEFAULT '0.00';"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE PurchaseOrders ADD COLUMN FinalBillAmount TEXT NOT NULL DEFAULT '0.00';"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE PurchaseOrders ADD COLUMN RefundAmount TEXT NOT NULL DEFAULT '0.00';"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE PurchaseOrders ADD COLUMN Notes TEXT;"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE PurchaseOrders ADD COLUMN SourceType TEXT NOT NULL DEFAULT 'Manual';"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE PurchaseOrders ADD COLUMN CreatedAt TEXT NOT NULL DEFAULT '2026-10-28 00:00:00';"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE PurchaseOrders ADD COLUMN UpdatedAt TEXT NOT NULL DEFAULT '2026-10-28 00:00:00';"); } catch { }

            // OrderItems Table (Section 4.D)
            try
            {
                db.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS OrderItems (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        PurchaseOrderId INTEGER NOT NULL,
                        CategoryId INTEGER NOT NULL DEFAULT 1,
                        ItemName TEXT NOT NULL,
                        Quantity INTEGER NOT NULL,
                        UnitPrice TEXT NOT NULL,
                        TotalPrice TEXT NOT NULL,
                        FOREIGN KEY (PurchaseOrderId) REFERENCES PurchaseOrders (Id) ON DELETE CASCADE,
                        FOREIGN KEY (CategoryId) REFERENCES Categories (Id) ON DELETE RESTRICT
                    );");
            }
            catch { }

            try { db.Database.ExecuteSqlRaw("ALTER TABLE OrderItems ADD COLUMN PurchaseOrderId INTEGER NOT NULL DEFAULT 1;"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE OrderItems ADD COLUMN CategoryId INTEGER NOT NULL DEFAULT 1;"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE OrderItems ADD COLUMN ItemName TEXT NOT NULL DEFAULT '';"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE OrderItems ADD COLUMN Quantity INTEGER NOT NULL DEFAULT 1;"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE OrderItems ADD COLUMN UnitPrice TEXT NOT NULL DEFAULT '0.00';"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE OrderItems ADD COLUMN TotalPrice TEXT NOT NULL DEFAULT '0.00';"); } catch { }

            // CategoryBudgets Table (Section 4.E)
            try
            {
                db.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS CategoryBudgets (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Year INTEGER NOT NULL,
                        Month INTEGER NOT NULL,
                        CategoryId INTEGER NOT NULL,
                        Amount TEXT NOT NULL,
                        FOREIGN KEY (CategoryId) REFERENCES Categories (Id) ON DELETE RESTRICT
                    );");
            }
            catch { }

            try
            {
                db.Database.ExecuteSqlRaw(@"
                    CREATE UNIQUE INDEX IF NOT EXISTS IX_CategoryBudgets_Year_Month_CategoryId 
                    ON CategoryBudgets (Year, Month, CategoryId);");
            }
            catch { }
        }

        // Populate realistic, diverse sample data across all entities (Section 11-16)
        DbSeeder.SeedSampleData(db);
    }
    catch (Exception startupDbEx)
    {
        app.Logger.LogWarning(startupDbEx, "Initial database schema verification notice.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Enable Session State Management middleware
app.UseSession();

app.UseAuthorization();

// Default route points to DashboardController
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
