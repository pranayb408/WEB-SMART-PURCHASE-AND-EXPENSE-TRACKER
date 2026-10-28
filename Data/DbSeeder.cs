using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.Data
{
    public static class DbSeeder
    {
        public static void SeedSampleData(AppDbContext db)
        {
            try
            {
                // 1. Seed Platforms
                if (!db.Platforms.Any())
                {
                    db.Platforms.AddRange(
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
                    db.SaveChanges();
                }

                // 2. Seed Categories
                if (!db.Categories.Any())
                {
                    db.Categories.AddRange(
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
                    db.SaveChanges();
                }

                int currentYear = DateTime.Today.Year;
                int currentMonth = DateTime.Today.Month;

                // 3. Seed Monthly Budgets
                if (!db.MonthlyBudgets.Any(b => b.Year == currentYear && b.Month == currentMonth))
                {
                    db.MonthlyBudgets.Add(new MonthlyBudget
                    {
                        Year = currentYear,
                        Month = currentMonth,
                        Amount = 45000m
                    });
                }
                int prevMonth = currentMonth == 1 ? 12 : currentMonth - 1;
                int prevYear = currentMonth == 1 ? currentYear - 1 : currentYear;
                if (!db.MonthlyBudgets.Any(b => b.Year == prevYear && b.Month == prevMonth))
                {
                    db.MonthlyBudgets.Add(new MonthlyBudget
                    {
                        Year = prevYear,
                        Month = prevMonth,
                        Amount = 42000m
                    });
                }
                int nextMonth = currentMonth == 12 ? 1 : currentMonth + 1;
                int nextYear = currentMonth == 12 ? currentYear + 1 : currentYear;
                if (!db.MonthlyBudgets.Any(b => b.Year == nextYear && b.Month == nextMonth))
                {
                    db.MonthlyBudgets.Add(new MonthlyBudget
                    {
                        Year = nextYear,
                        Month = nextMonth,
                        Amount = 40000m
                    });
                }
                db.SaveChanges();

                // 4. Seed Category Budgets
                var catMap = db.Categories.AsNoTracking().ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);
                var categoryBudgetsToAdd = new List<(string CatName, decimal Amount)>
                {
                    ("Food", 10000m),
                    ("Groceries", 8000m),
                    ("Electronics", 5000m),
                    ("Clothing", 4000m),
                    ("Household", 25000m),
                    ("Health", 5000m),
                    ("Subscriptions", 2000m),
                    ("Travel", 3000m)
                };

                foreach (var (catName, amt) in categoryBudgetsToAdd)
                {
                    if (catMap.TryGetValue(catName, out int catId))
                    {
                        if (!db.CategoryBudgets.Any(cb => cb.Year == currentYear && cb.Month == currentMonth && cb.CategoryId == catId))
                        {
                            db.CategoryBudgets.Add(new CategoryBudget
                            {
                                Year = currentYear,
                                Month = currentMonth,
                                CategoryId = catId,
                                Amount = amt
                            });
                        }
                    }
                }
                db.SaveChanges();

                // 5. Seed Standalone Transactions
                if (db.Transactions.Count() < 5)
                {
                    db.Transactions.AddRange(
                        new Transaction
                        {
                            Title = "Monthly Software Engineer Salary",
                            Type = "Income",
                            Category = "Salary",
                            Amount = 85000m,
                            Date = new DateTime(currentYear, currentMonth, 1),
                            Platform = "Direct / In-Store",
                            PaymentMethod = "Net Banking / NEFT",
                            Notes = "Base monthly tech salary credited via direct deposit."
                        },
                        new Transaction
                        {
                            Title = "Freelance Mobile App Development",
                            Type = "Income",
                            Category = "Freelance / Bonus",
                            Amount = 25000m,
                            Date = new DateTime(currentYear, currentMonth, 15),
                            Platform = "Direct / In-Store",
                            PaymentMethod = "UPI (GPay / PhonePe / Paytm)",
                            Notes = "Milestone payment for React Native & ASP.NET backend integration."
                        },
                        new Transaction
                        {
                            Title = "Mutual Fund Dividend Payout",
                            Type = "Income",
                            Category = "Investments",
                            Amount = 3500m,
                            Date = new DateTime(currentYear, currentMonth, 20),
                            Platform = "Direct / In-Store",
                            PaymentMethod = "Net Banking / NEFT",
                            Notes = "Quarterly equity dividend payout."
                        },
                        new Transaction
                        {
                            Title = "Apartment Rent Payment",
                            Type = "Expense",
                            Category = "Household",
                            Amount = 18000m,
                            Date = new DateTime(currentYear, currentMonth, 5),
                            Platform = "Direct / In-Store",
                            PaymentMethod = "Net Banking / NEFT",
                            Notes = "Monthly flat rental maintenance transfer."
                        },
                        new Transaction
                        {
                            Title = "Electricity & Water Utility Bill",
                            Type = "Expense",
                            Category = "Household",
                            Amount = 2450m,
                            Date = new DateTime(currentYear, currentMonth, 10),
                            Platform = "Direct / In-Store",
                            PaymentMethod = "UPI (GPay / PhonePe / Paytm)",
                            Notes = "State electricity and municipal water utility payment."
                        },
                        new Transaction
                        {
                            Title = "Fiber High-Speed Broadband Bill",
                            Type = "Expense",
                            Category = "Subscriptions",
                            Amount = 999m,
                            Date = new DateTime(currentYear, currentMonth, 12),
                            Platform = "Direct / In-Store",
                            PaymentMethod = "Credit / Debit Card",
                            Notes = "Airtel Xstream 200 Mbps fiber unlimited broadband."
                        },
                        new Transaction
                        {
                            Title = "Metro Monthly Commuter Pass",
                            Type = "Expense",
                            Category = "Travel",
                            Amount = 1200m,
                            Date = new DateTime(currentYear, currentMonth, 8),
                            Platform = "Direct / In-Store",
                            PaymentMethod = "UPI (GPay / PhonePe / Paytm)",
                            Notes = "Smart card monthly transit recharge."
                        },
                        new Transaction
                        {
                            Title = "Routine Medical Checkup & Pharmacy",
                            Type = "Expense",
                            Category = "Health",
                            Amount = 1850m,
                            Date = new DateTime(currentYear, currentMonth, 18),
                            Platform = "Direct / In-Store",
                            PaymentMethod = "Credit / Debit Card",
                            Notes = "Clinic consultation fee and vitamin supplements."
                        }
                    );
                    db.SaveChanges();
                }

                // 6. Seed Purchase Orders with itemized multi-row line items
                var platMap = db.Platforms.AsNoTracking().ToDictionary(p => p.Name, p => p.Id, StringComparer.OrdinalIgnoreCase);

                if (db.PurchaseOrders.Count() < 5)
                {
                    // Order 1: Swiggy (Food Delivery)
                    int swiggyId = platMap.TryGetValue("Swiggy", out int sId) ? sId : 1;
                    int foodCatId = catMap.TryGetValue("Food", out int fId) ? fId : 1;
                    var order1 = new PurchaseOrder
                    {
                        PlatformId = swiggyId,
                        ExternalOrderNumber = "SWG-849201",
                        OrderDate = new DateTime(currentYear, currentMonth, 25),
                        OrderStatus = OrderStatus.Completed,
                        PaymentMethod = "UPI (GPay / PhonePe / Paytm)",
                        ItemSubtotal = 620m,
                        DeliveryFee = 35m,
                        PackagingFee = 15m,
                        PlatformFee = 5m,
                        Taxes = 31m,
                        OtherCharges = 0m,
                        Discount = 60m,
                        FinalBillAmount = 646m,
                        RefundAmount = 0m,
                        Notes = "Dinner order from Punjab Grill with family combo.",
                        SourceType = SourceTypes.Manual,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    db.PurchaseOrders.Add(order1);
                    db.SaveChanges();

                    db.OrderItems.AddRange(
                        new OrderItem { PurchaseOrderId = order1.Id, CategoryId = foodCatId, ItemName = "Paneer Butter Masala", Quantity = 1, UnitPrice = 320m, TotalPrice = 320m },
                        new OrderItem { PurchaseOrderId = order1.Id, CategoryId = foodCatId, ItemName = "Butter Naan", Quantity = 3, UnitPrice = 60m, TotalPrice = 180m },
                        new OrderItem { PurchaseOrderId = order1.Id, CategoryId = foodCatId, ItemName = "Gulab Jamun (2 Pcs)", Quantity = 1, UnitPrice = 120m, TotalPrice = 120m }
                    );
                    db.SaveChanges();

                    // Order 2: Blinkit (Groceries & Quick Commerce)
                    int blinkitId = platMap.TryGetValue("Blinkit", out int bId) ? bId : 3;
                    int grocCatId = catMap.TryGetValue("Groceries", out int gId) ? gId : 2;
                    int houseCatId = catMap.TryGetValue("Household", out int hId) ? hId : 5;
                    int healthCatId = catMap.TryGetValue("Health", out int hlId) ? hlId : 6;

                    var order2 = new PurchaseOrder
                    {
                        PlatformId = blinkitId,
                        ExternalOrderNumber = "BLK-932148",
                        OrderDate = new DateTime(currentYear, currentMonth, 26),
                        OrderStatus = OrderStatus.Completed,
                        PaymentMethod = "Credit / Debit Card",
                        ItemSubtotal = 547m,
                        DeliveryFee = 15m,
                        PackagingFee = 0m,
                        PlatformFee = 5m,
                        Taxes = 27m,
                        OtherCharges = 0m,
                        Discount = 30m,
                        FinalBillAmount = 564m,
                        RefundAmount = 0m,
                        Notes = "Quick-commerce 10-minute grocery restock.",
                        SourceType = SourceTypes.Manual,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    db.PurchaseOrders.Add(order2);
                    db.SaveChanges();

                    db.OrderItems.AddRange(
                        new OrderItem { PurchaseOrderId = order2.Id, CategoryId = grocCatId, ItemName = "Amul Taaza Toned Milk 1L", Quantity = 2, UnitPrice = 54m, TotalPrice = 108m },
                        new OrderItem { PurchaseOrderId = order2.Id, CategoryId = grocCatId, ItemName = "Whole Wheat Brown Bread 400g", Quantity = 1, UnitPrice = 50m, TotalPrice = 50m },
                        new OrderItem { PurchaseOrderId = order2.Id, CategoryId = houseCatId, ItemName = "Surf Excel Matic Front Load 1kg", Quantity = 1, UnitPrice = 245m, TotalPrice = 245m },
                        new OrderItem { PurchaseOrderId = order2.Id, CategoryId = healthCatId, ItemName = "Dettol Liquid Handwash Refill 750ml", Quantity = 1, UnitPrice = 144m, TotalPrice = 144m }
                    );
                    db.SaveChanges();

                    // Order 3: Amazon (Shopping & Electronics)
                    int amazonId = platMap.TryGetValue("Amazon", out int aId) ? aId : 6;
                    int elecCatId = catMap.TryGetValue("Electronics", out int eId) ? eId : 3;

                    var order3 = new PurchaseOrder
                    {
                        PlatformId = amazonId,
                        ExternalOrderNumber = "AMZ-402-918237",
                        OrderDate = new DateTime(currentYear, currentMonth, 27),
                        OrderStatus = OrderStatus.Completed,
                        PaymentMethod = "Amazon Pay / Wallets",
                        ItemSubtotal = 2046m,
                        DeliveryFee = 0m,
                        PackagingFee = 0m,
                        PlatformFee = 0m,
                        Taxes = 184m,
                        OtherCharges = 0m,
                        Discount = 150m,
                        FinalBillAmount = 2080m,
                        RefundAmount = 0m,
                        Notes = "Work-from-home desk setup accessories with Prime delivery.",
                        SourceType = SourceTypes.Manual,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    db.PurchaseOrders.Add(order3);
                    db.SaveChanges();

                    db.OrderItems.AddRange(
                        new OrderItem { PurchaseOrderId = order3.Id, CategoryId = elecCatId, ItemName = "Logitech M221 Silent Wireless Mouse", Quantity = 1, UnitPrice = 799m, TotalPrice = 799m },
                        new OrderItem { PurchaseOrderId = order3.Id, CategoryId = elecCatId, ItemName = "Portronics Type-C Fast Braided Cable 1.5m", Quantity = 2, UnitPrice = 199m, TotalPrice = 398m },
                        new OrderItem { PurchaseOrderId = order3.Id, CategoryId = houseCatId, ItemName = "Ergonomic Aluminium Laptop Riser Stand", Quantity = 1, UnitPrice = 849m, TotalPrice = 849m }
                    );
                    db.SaveChanges();

                    // Order 4: Zepto (Quick Commerce Fresh Produce)
                    int zeptoId = platMap.TryGetValue("Zepto", out int zId) ? zId : 4;

                    var order4 = new PurchaseOrder
                    {
                        PlatformId = zeptoId,
                        ExternalOrderNumber = "ZPT-551029",
                        OrderDate = new DateTime(currentYear, currentMonth, 28),
                        OrderStatus = OrderStatus.Completed,
                        PaymentMethod = "UPI (GPay / PhonePe / Paytm)",
                        ItemSubtotal = 322m,
                        DeliveryFee = 0m,
                        PackagingFee = 0m,
                        PlatformFee = 4m,
                        Taxes = 16m,
                        OtherCharges = 0m,
                        Discount = 20m,
                        FinalBillAmount = 322m,
                        RefundAmount = 0m,
                        Notes = "Fresh morning breakfast ingredients via Zepto Express.",
                        SourceType = SourceTypes.Manual,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    db.PurchaseOrders.Add(order4);
                    db.SaveChanges();

                    db.OrderItems.AddRange(
                        new OrderItem { PurchaseOrderId = order4.Id, CategoryId = grocCatId, ItemName = "Robusta Fresh Bananas 1kg", Quantity = 1, UnitPrice = 65m, TotalPrice = 65m },
                        new OrderItem { PurchaseOrderId = order4.Id, CategoryId = foodCatId, ItemName = "Farm Fresh Organic Eggs (6 Pcs)", Quantity = 1, UnitPrice = 72m, TotalPrice = 72m },
                        new OrderItem { PurchaseOrderId = order4.Id, CategoryId = grocCatId, ItemName = "Nescafe Classic Instant Coffee Jar 50g", Quantity = 1, UnitPrice = 185m, TotalPrice = 185m }
                    );
                    db.SaveChanges();

                    // Order 5: Myntra (Fashion & Clothing)
                    int myntraId = platMap.TryGetValue("Myntra", out int mId) ? mId : 8;
                    int clothCatId = catMap.TryGetValue("Clothing", out int cId) ? cId : 4;

                    var order5 = new PurchaseOrder
                    {
                        PlatformId = myntraId,
                        ExternalOrderNumber = "MYN-778219",
                        OrderDate = new DateTime(currentYear, currentMonth, 24),
                        OrderStatus = OrderStatus.Completed,
                        PaymentMethod = "Credit / Debit Card",
                        ItemSubtotal = 1997m,
                        DeliveryFee = 49m,
                        PackagingFee = 0m,
                        PlatformFee = 0m,
                        Taxes = 100m,
                        OtherCharges = 0m,
                        Discount = 300m,
                        FinalBillAmount = 1846m,
                        RefundAmount = 0m,
                        Notes = "Festival sale apparel order.",
                        SourceType = SourceTypes.Manual,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    db.PurchaseOrders.Add(order5);
                    db.SaveChanges();

                    db.OrderItems.AddRange(
                        new OrderItem { PurchaseOrderId = order5.Id, CategoryId = clothCatId, ItemName = "Roadster Solid Crew Neck Cotton T-Shirt", Quantity = 2, UnitPrice = 399m, TotalPrice = 798m },
                        new OrderItem { PurchaseOrderId = order5.Id, CategoryId = clothCatId, ItemName = "Highlander Slim Fit Stretch Denim Jeans", Quantity = 1, UnitPrice = 1199m, TotalPrice = 1199m }
                    );
                    db.SaveChanges();

                    // Order 6: Zomato (Food Delivery with partial refund recorded)
                    int zomatoId = platMap.TryGetValue("Zomato", out int zmId) ? zmId : 2;

                    var order6 = new PurchaseOrder
                    {
                        PlatformId = zomatoId,
                        ExternalOrderNumber = "ZOM-332190",
                        OrderDate = new DateTime(currentYear, currentMonth, 23),
                        OrderStatus = OrderStatus.Completed,
                        PaymentMethod = "Net Banking / NEFT",
                        ItemSubtotal = 598m,
                        DeliveryFee = 40m,
                        PackagingFee = 15m,
                        PlatformFee = 5m,
                        Taxes = 30m,
                        OtherCharges = 0m,
                        Discount = 50m,
                        FinalBillAmount = 638m,
                        RefundAmount = 100m,
                        Notes = "Domino's Pizza order. ₹100 refunded due to missing beverage.",
                        SourceType = SourceTypes.Manual,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    db.PurchaseOrders.Add(order6);
                    db.SaveChanges();

                    db.OrderItems.AddRange(
                        new OrderItem { PurchaseOrderId = order6.Id, CategoryId = foodCatId, ItemName = "Farmhouse Medium Hand-Tossed Pizza", Quantity = 1, UnitPrice = 449m, TotalPrice = 449m },
                        new OrderItem { PurchaseOrderId = order6.Id, CategoryId = foodCatId, ItemName = "Cheesy Garlic Breadsticks", Quantity = 1, UnitPrice = 149m, TotalPrice = 149m }
                    );
                    db.SaveChanges();

                    // Order 7: Flipkart (Pending delivery order)
                    int flipkartId = platMap.TryGetValue("Flipkart", out int fkId) ? fkId : 7;

                    var order7 = new PurchaseOrder
                    {
                        PlatformId = flipkartId,
                        ExternalOrderNumber = "FK-9018274",
                        OrderDate = new DateTime(currentYear, currentMonth, 28),
                        OrderStatus = OrderStatus.Pending,
                        PaymentMethod = "Cash on Delivery (COD)",
                        ItemSubtotal = 1499m,
                        DeliveryFee = 40m,
                        PackagingFee = 0m,
                        PlatformFee = 0m,
                        Taxes = 135m,
                        OtherCharges = 0m,
                        Discount = 100m,
                        FinalBillAmount = 1574m,
                        RefundAmount = 0m,
                        Notes = "Pending shipment order. Expected delivery in 2 business days.",
                        SourceType = SourceTypes.Manual,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    db.PurchaseOrders.Add(order7);
                    db.SaveChanges();

                    db.OrderItems.AddRange(
                        new OrderItem { PurchaseOrderId = order7.Id, CategoryId = elecCatId, ItemName = "Noise ColorFit Pulse 2 Bluetooth Smartwatch", Quantity = 1, UnitPrice = 1499m, TotalPrice = 1499m }
                    );
                    db.SaveChanges();
                }
            }
            catch
            {
                // Graceful fallback if tables already populated or in migration
            }
        }
    }
}
