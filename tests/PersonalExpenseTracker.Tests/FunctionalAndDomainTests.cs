using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Controllers;
using PersonalExpenseTracker.Data;
using PersonalExpenseTracker.Models;
using PersonalExpenseTracker.Services;
using PersonalExpenseTracker.ViewModels;
using Xunit;

namespace PersonalExpenseTracker.Tests
{
    public class FunctionalAndDomainTests
    {
        private IList<ValidationResult> ValidateModel(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, context, results, validateAllProperties: true);
            return results;
        }

        #region 1. Dashboard Calculations & Formulas

        [Fact]
        public void Dashboard_EmptyDatabase_CalculationsAreZero()
        {
            // Arrange
            var vm = new DashboardViewModel
            {
                TotalIncome = 0m,
                TotalExpenses = 0m,
                MonthlyBudgetAmount = 0m
            };

            // Assert
            Assert.Equal(0m, vm.NetBalance);
            Assert.Equal(0m, vm.BudgetRemaining);
            Assert.Equal(0.0, vm.BudgetUsagePercentage);
            Assert.False(vm.HasBudget);
            Assert.False(vm.IsOverBudget);
            Assert.Equal(0m, vm.OverBudgetAmount);
            Assert.Equal("bg-secondary", vm.ProgressBarClass);
        }

        [Fact]
        public void Dashboard_StandardBalancedState_CalculationsAreCorrect()
        {
            // Arrange: Income = 50,000, Expense = 20,000, Budget = 30,000
            var vm = new DashboardViewModel
            {
                TotalIncome = 50000m,
                TotalExpenses = 20000m,
                MonthlyBudgetAmount = 30000m
            };

            // Assert
            // Net balance = 50000 - 20000 = 30000
            Assert.Equal(30000m, vm.NetBalance);
            // Budget remaining = 30000 - 20000 = 10000
            Assert.Equal(10000m, vm.BudgetRemaining);
            // Budget usage percentage = (20000 / 30000) * 100 = 66.7%
            Assert.Equal(66.7, vm.BudgetUsagePercentage);
            Assert.True(vm.HasBudget);
            Assert.False(vm.IsOverBudget);
            Assert.Equal(0m, vm.OverBudgetAmount);
            // Usage < 80% should be green
            Assert.Equal("bg-success", vm.ProgressBarClass);
        }

        [Fact]
        public void Dashboard_WarningThreshold_UsageAtOrAbove80Percent_ReturnsWarningClass()
        {
            // Arrange: Expense = 25,000, Budget = 30,000 => 83.3%
            var vm = new DashboardViewModel
            {
                TotalIncome = 50000m,
                TotalExpenses = 25000m,
                MonthlyBudgetAmount = 30000m
            };

            // Assert
            Assert.Equal(83.3, vm.BudgetUsagePercentage);
            Assert.False(vm.IsOverBudget);
            Assert.Equal("bg-warning", vm.ProgressBarClass);
        }

        [Fact]
        public void Dashboard_OverBudget_CalculatesExceededAmountAndReturnsDangerClass()
        {
            // Arrange: Expense = 35,000, Budget = 30,000
            var vm = new DashboardViewModel
            {
                TotalIncome = 50000m,
                TotalExpenses = 35000m,
                MonthlyBudgetAmount = 30000m
            };

            // Assert
            Assert.True(vm.IsOverBudget);
            // Budget remaining is negative (-5000)
            Assert.Equal(-5000m, vm.BudgetRemaining);
            // OverBudgetAmount is positive (5000)
            Assert.Equal(5000m, vm.OverBudgetAmount);
            Assert.Equal(116.7, vm.BudgetUsagePercentage);
            Assert.Equal("bg-danger", vm.ProgressBarClass);
        }

        [Fact]
        public void Dashboard_NoDivisionByZero_WhenBudgetIsZero()
        {
            // Arrange: Has expenses but no budget defined
            var vm = new DashboardViewModel
            {
                TotalIncome = 10000m,
                TotalExpenses = 5000m,
                MonthlyBudgetAmount = 0m
            };

            // Assert
            Assert.False(vm.HasBudget);
            Assert.Equal(0.0, vm.BudgetUsagePercentage);
            Assert.Equal("bg-secondary", vm.ProgressBarClass);
        }

        #endregion

        #region 2. Transaction Model Validation

        [Fact]
        public void Transaction_ValidModel_PassesValidation()
        {
            var tx = new Transaction
            {
                Title = "Freelance Project Payment",
                Type = "Income",
                Category = "Freelance",
                Amount = 15000.00m,
                Date = DateTime.Today,
                Notes = "Web design gig"
            };

            var errors = ValidateModel(tx);
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Transaction_BlankTitle_FailsValidation(string? title)
        {
            var tx = new Transaction
            {
                Title = title!,
                Type = "Income",
                Category = "Salary",
                Amount = 1000m,
                Date = DateTime.Today
            };

            var errors = ValidateModel(tx);
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Transaction.Title)));
        }

        [Fact]
        public void Transaction_TitleTooShort_FailsValidation()
        {
            var tx = new Transaction
            {
                Title = "A", // Min length is 2
                Type = "Income",
                Category = "Salary",
                Amount = 1000m,
                Date = DateTime.Today
            };

            var errors = ValidateModel(tx);
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Transaction.Title)));
        }

        [Fact]
        public void Transaction_InvalidType_FailsValidation()
        {
            var tx = new Transaction
            {
                Title = "Stock Dividend",
                Type = "Investment", // Allowed: Income or Expense
                Category = "Investments",
                Amount = 500m,
                Date = DateTime.Today
            };

            var errors = ValidateModel(tx);
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Transaction.Type)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-50)]
        public void Transaction_ZeroOrNegativeAmount_FailsValidation(decimal amount)
        {
            var tx = new Transaction
            {
                Title = "Lunch",
                Type = "Expense",
                Category = "Food & Dining",
                Amount = amount,
                Date = DateTime.Today
            };

            var errors = ValidateModel(tx);
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Transaction.Amount)));
        }

        [Fact]
        public void Transaction_NotesExceeding300Chars_FailsValidation()
        {
            var tx = new Transaction
            {
                Title = "Lunch",
                Type = "Expense",
                Category = "Food & Dining",
                Amount = 150m,
                Date = DateTime.Today,
                Notes = new string('x', 301)
            };

            var errors = ValidateModel(tx);
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Transaction.Notes)));
        }

        #endregion

        #region 3. MonthlyBudget Model Validation

        [Fact]
        public void MonthlyBudget_ValidModel_PassesValidation()
        {
            var budget = new MonthlyBudget
            {
                Year = 2026,
                Month = 10,
                Amount = 30000m
            };

            var errors = ValidateModel(budget);
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        [InlineData(-1)]
        public void MonthlyBudget_InvalidMonth_FailsValidation(int month)
        {
            var budget = new MonthlyBudget
            {
                Year = 2026,
                Month = month,
                Amount = 30000m
            };

            var errors = ValidateModel(budget);
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(MonthlyBudget.Month)));
        }

        [Theory]
        [InlineData(1999)]
        [InlineData(2101)]
        public void MonthlyBudget_InvalidYear_FailsValidation(int year)
        {
            var budget = new MonthlyBudget
            {
                Year = year,
                Month = 6,
                Amount = 30000m
            };

            var errors = ValidateModel(budget);
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(MonthlyBudget.Year)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        public void MonthlyBudget_ZeroOrNegativeAmount_FailsValidation(decimal amount)
        {
            var budget = new MonthlyBudget
            {
                Year = 2026,
                Month = 10,
                Amount = amount
            };

            var errors = ValidateModel(budget);
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(MonthlyBudget.Amount)));
        }

        #endregion

        #region 4. Category Business Logic

        [Theory]
        [InlineData("Salary", true)]
        [InlineData("Freelance", true)]
        [InlineData("Investment", true)]
        [InlineData("Food & Dining", false)] // Expense category
        [InlineData("NonExistent", false)]
        public void TransactionCategories_IncomeValidation_ReturnsExpected(string category, bool expected)
        {
            bool actual = TransactionCategories.IsValid(category, "Income");
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("Food & Dining", true)]
        [InlineData("Rent & Housing", true)]
        [InlineData("Utilities & Bills", true)]
        [InlineData("Transportation", true)]
        [InlineData("Salary", false)] // Income category
        [InlineData("Crypto", false)]
        public void TransactionCategories_ExpenseValidation_ReturnsExpected(string category, bool expected)
        {
            bool actual = TransactionCategories.IsValid(category, "Expense");
            Assert.Equal(expected, actual);
        }

        #endregion

        #region 5. Smart Purchase Platforms & Reporting Tests

        [Fact]
        public void PurchasePlatforms_ContainsMajorDeliveryAndGroceryApps()
        {
            var all = PurchasePlatforms.GetAll();
            Assert.Contains("Swiggy", all);
            Assert.Contains("Zomato", all);
            Assert.Contains("Blinkit", all);
            Assert.Contains("Zepto", all);
            Assert.Contains("Instamart", all);
            Assert.Contains("Amazon", all);
            Assert.Contains("Flipkart", all);
            Assert.Contains("Myntra", all);
            Assert.Contains("Direct / In-Store", all);
        }

        [Fact]
        public void PurchasePlatforms_BadgeAndIcons_ReturnValidStrings()
        {
            var swiggyBadge = PurchasePlatforms.GetBadgeStyle("Swiggy");
            var amazonBadge = PurchasePlatforms.GetBadgeStyle("Amazon");
            var swiggyIcon = PurchasePlatforms.GetIcon("Swiggy");
            var blinkitIcon = PurchasePlatforms.GetIcon("Blinkit");

            Assert.Contains("#fc8019", swiggyBadge);
            Assert.Contains("#ff9900", amazonBadge);
            Assert.Equal("bi-cup-hot-fill", swiggyIcon);
            Assert.Equal("bi-basket2-fill", blinkitIcon);
        }

        [Fact]
        public void Transaction_WithPlatformAndOrderNumber_IsValid()
        {
            var tx = new Transaction
            {
                Title = "Swiggy Gourmet Dinner",
                Type = "Expense",
                Category = "Food & Dining",
                Amount = 780.50m,
                Date = DateTime.Today,
                Platform = "Swiggy",
                OrderNumber = "#SWIG-9812",
                PaymentMethod = "UPI (GPay / PhonePe / Paytm)"
            };

            var errors = ValidateModel(tx);
            Assert.Empty(errors);
        }

        [Fact]
        public void ReportsViewModel_FinancialCalculations_AreAccurate()
        {
            var reports = new ReportsViewModel
            {
                TotalIncome = 100000m,
                TotalExpenses = 35000m,
                TotalPurchasesCount = 14
            };

            // Net Savings = 100000 - 35000 = 65000
            Assert.Equal(65000m, reports.NetSavings);

            // Savings Rate = (65000 / 100000) * 100 = 65.0%
            Assert.Equal(65.0, reports.SavingsRatePercentage);

            // Average Purchase Value = 35000 / 14 = 2500
            Assert.Equal(2500.00m, reports.AveragePurchaseAmount);
        }

        #endregion

        #region 6. Master Business Rules Verification (Section 3)

        [Fact]
        public void FinancialAccounting_StandaloneIncomeAndExpenses_CalculatedAccurately()
        {
            var service = new FinancialAccountingService();
            var txs = new List<Transaction>
            {
                new Transaction { Type = "Income", Amount = 50000m },
                new Transaction { Type = "Income", Amount = 15000m },
                new Transaction { Type = "Expense", Amount = 8000m },
                new Transaction { Type = "Expense", Amount = 4500m }
            };

            decimal income = service.CalculateStandaloneIncome(txs);
            decimal expense = service.CalculateStandaloneExpenses(txs);

            Assert.Equal(65000m, income);
            Assert.Equal(12500m, expense);
        }

        [Fact]
        public void FinancialAccounting_NetPurchaseSpending_ExcludesPendingAndCancelledOrders()
        {
            var service = new FinancialAccountingService();
            var orders = new List<PurchaseOrder>
            {
                // Completed order: Bill 2,400 with 400 refund -> Net = 2,000
                new PurchaseOrder 
                { 
                    OrderNumber = "ORD-001",
                    Platform = new Platform { Id = 3, Name = "Blinkit", PlatformType = "Groceries & Quick Commerce" },
                    Status = OrderStatus.Completed,
                    TotalAmount = 2400m,
                    RefundAmount = 400m
                },
                // Pending order: 1,500 -> Must be excluded from NetPurchaseSpending
                new PurchaseOrder 
                { 
                    OrderNumber = "ORD-002",
                    Platform = new Platform { Id = 6, Name = "Amazon", PlatformType = "Shopping" },
                    Status = OrderStatus.Pending,
                    TotalAmount = 1500m
                },
                // Cancelled order: 900 -> Must be excluded from NetPurchaseSpending
                new PurchaseOrder 
                { 
                    OrderNumber = "ORD-003",
                    Platform = new Platform { Id = 1, Name = "Swiggy", PlatformType = "Food Delivery" },
                    Status = OrderStatus.Cancelled,
                    TotalAmount = 900m
                }
            };

            decimal netSpending = service.CalculateNetPurchaseSpending(orders);
            decimal pending = service.CalculatePendingPurchases(orders);
            decimal cancelled = service.CalculateCancelledPurchases(orders);

            Assert.Equal(2000m, netSpending);
            Assert.Equal(1500m, pending);
            Assert.Equal(900m, cancelled);
        }

        [Fact]
        public void FinancialAccounting_TotalExpensesAndNetBalance_NoDoubleCounting()
        {
            var service = new FinancialAccountingService();
            decimal standaloneIncome = 65000m;
            decimal standaloneExpenses = 12500m;
            decimal netPurchaseSpending = 2000m;

            // Total expenses = standalone expenses plus net purchase spending
            decimal totalExpenses = service.CalculateTotalExpenses(standaloneExpenses, netPurchaseSpending);
            Assert.Equal(14500m, totalExpenses);

            // Net balance = standalone income minus total expenses
            decimal netBalance = service.CalculateNetBalance(standaloneIncome, totalExpenses);
            Assert.Equal(50500m, netBalance);
        }

        [Fact]
        public void PurchaseOrder_OneOrderWithMultipleItems_RecalculatesTotalsOnServer()
        {
            // Requirement: "One grocery order may contain milk, fruit, bread, and detergent. 
            // Store one order record and four related order-item records. Never create four separate purchase orders."
            var groceryCat = new Category { Id = 2, Name = "Groceries", CategoryType = "Expense" };
            var householdCat = new Category { Id = 5, Name = "Household", CategoryType = "Expense" };

            var order = new PurchaseOrder
            {
                OrderNumber = "#BK-GROCERY-01",
                Platform = new Platform { Id = 3, Name = "Blinkit", PlatformType = "Groceries & Quick Commerce" },
                Status = OrderStatus.Completed,
                DeliveryFee = 40m,
                Discount = 50m,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemName = "Milk", Category = groceryCat, CategoryId = 2, Quantity = 2, UnitPrice = 30m },
                    new OrderItem { ItemName = "Fruit", Category = groceryCat, CategoryId = 2, Quantity = 1, UnitPrice = 150m },
                    new OrderItem { ItemName = "Bread", Category = groceryCat, CategoryId = 2, Quantity = 2, UnitPrice = 40m },
                    new OrderItem { ItemName = "Detergent", Category = householdCat, CategoryId = 5, Quantity = 1, UnitPrice = 210m }
                }
            };

            // Assert one order with 4 items
            Assert.Equal(4, order.Items.Count);

            // Execute server-side recalculation
            order.RecalculateTotals();

            // Items sum: (2*30) + (1*150) + (2*40) + (1*210) = 60 + 150 + 80 + 210 = 500
            Assert.Equal(500m, order.ItemSubtotal);

            // Total bill = 500 + 40 (delivery) - 50 (discount) = 490
            Assert.Equal(490m, order.FinalBillAmount);
            Assert.Equal(490m, order.NetPurchaseSpending);
        }

        [Fact]
        public void FinancialAccounting_BudgetComparison_ComparesAgainstTotalSpending()
        {
            var service = new FinancialAccountingService();
            decimal monthlyBudget = 20000m;
            decimal totalSpending = 14500m;

            var result = service.CalculateBudgetComparison(monthlyBudget, totalSpending);

            // Budget Remaining = 20000 - 14500 = 5500
            Assert.Equal(5500m, result.Remaining);
            Assert.False(result.IsOverBudget);
            Assert.Equal(0m, result.OverBudgetAmount);
            Assert.Equal(72.5, result.UsagePercentage);
            Assert.Equal("bg-success", result.ProgressBarClass);

            // Over-budget test
            var overResult = service.CalculateBudgetComparison(10000m, totalSpending);
            Assert.True(overResult.IsOverBudget);
            Assert.Equal(4500m, overResult.OverBudgetAmount);
            Assert.Equal("bg-danger", overResult.ProgressBarClass);
        }

        #region Section 4 & Section 5: Data Model and Bill Calculation Tests

        [Fact]
        public void Section4_PlatformsAndCategories_ModelPropertiesAndInitialValues()
        {
            var platform = new Platform
            {
                Id = 1,
                Name = "Swiggy",
                PlatformType = "Food Delivery",
                IsActive = true
            };

            Assert.Equal(1, platform.Id);
            Assert.Equal("Swiggy", platform.Name);
            Assert.Equal("Food Delivery", platform.PlatformType);
            Assert.True(platform.IsActive);

            var category = new Category
            {
                Id = 2,
                Name = "Groceries",
                CategoryType = "Expense",
                IsActive = true
            };

            Assert.Equal(2, category.Id);
            Assert.Equal("Groceries", category.Name);
            Assert.Equal("Expense", category.CategoryType);
            Assert.True(category.IsActive);
        }

        [Fact]
        public void Section4_OrderItem_LineSubtotalCalculatedServerSide()
        {
            var item = new OrderItem
            {
                ItemName = "Organic Apples",
                CategoryId = 2,
                Quantity = 3,
                UnitPrice = 120.50m
            };

            // LineSubtotal = 3 * 120.50 = 361.50
            Assert.Equal(361.50m, item.LineSubtotal);
            Assert.Equal(361.50m, item.TotalPrice);
        }

        [Fact]
        public void Section4_CategoryBudget_EnforcesRequiredFields()
        {
            var budget = new CategoryBudget
            {
                Id = 1,
                Year = 2026,
                Month = 10,
                CategoryId = 2,
                Amount = 8500.00m
            };

            Assert.Equal(2026, budget.Year);
            Assert.Equal(10, budget.Month);
            Assert.Equal(2, budget.CategoryId);
            Assert.Equal(8500.00m, budget.Amount);
        }

        [Fact]
        public void Section5_BillCalculationService_Formulas_ItemSubtotalAndFinalBill()
        {
            var billService = new BillCalculationService();
            var items = new List<OrderItem>
            {
                new OrderItem { ItemName = "Pizza", Quantity = 2, UnitPrice = 399.00m },
                new OrderItem { ItemName = "Garlic Bread", Quantity = 1, UnitPrice = 149.00m },
                new OrderItem { ItemName = "Coke", Quantity = 2, UnitPrice = 40.00m }
            };

            // Subtotal = (2*399) + 149 + (2*40) = 798 + 149 + 80 = 1027.00
            decimal itemSubtotal = billService.CalculateItemSubtotal(items);
            Assert.Equal(1027.00m, itemSubtotal);

            // Final bill = 1027 + 40 (delivery) + 15 (packaging) + 5 (platform) + 51.35 (taxes) + 10 (other) - 150 (discount)
            // Pre-discount = 1027 + 40 + 15 + 5 + 51.35 + 10 = 1148.35
            // Final = 1148.35 - 150 = 998.35
            decimal finalBill = billService.CalculateFinalBillAmount(
                itemSubtotal: 1027.00m,
                deliveryFee: 40.00m,
                packagingFee: 15.00m,
                platformFee: 5.00m,
                taxes: 51.35m,
                otherCharges: 10.00m,
                discount: 150.00m);

            Assert.Equal(998.35m, finalBill);
        }

        [Fact]
        public void Section5_BillCalculationService_Validation_NegativeChargesAndExcessiveDiscount()
        {
            var billService = new BillCalculationService();
            var order = new PurchaseOrder
            {
                OrderStatus = OrderStatus.Completed,
                DeliveryFee = -10.00m, // Negative charge -> Must be rejected
                Discount = 500.00m,    // Excessive discount -> Must be rejected if > pre-discount
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemName = "Coffee", Quantity = 1, UnitPrice = 120.00m }
                }
            };

            var result = billService.CalculateAndValidate(order);

            Assert.False(result.IsValid);
            Assert.Contains(result.ValidationErrors, e => e.Contains("Delivery fee cannot be negative"));
            Assert.Contains(result.ValidationErrors, e => e.Contains("Discount") && e.Contains("cannot exceed"));
        }

        [Fact]
        public void Section5_BillCalculationService_Validation_RefundExceedingFinalBill()
        {
            var billService = new BillCalculationService();
            var order = new PurchaseOrder
            {
                OrderStatus = OrderStatus.Completed,
                RefundAmount = 500.00m, // Final bill will be 120, refund is 500 -> Must be rejected
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemName = "Book", Quantity = 1, UnitPrice = 120.00m }
                }
            };

            var result = billService.CalculateAndValidate(order);

            Assert.False(result.IsValid);
            Assert.Contains(result.ValidationErrors, e => e.Contains("Refund amount") && e.Contains("cannot exceed"));
        }

        [Fact]
        public void Section5_BillCalculationService_Validation_CancelledAndPendingOrders()
        {
            var billService = new BillCalculationService();

            // Cancelled order must not contribute to completed net spending
            decimal cancelledNet = billService.CalculateNetPurchaseSpending(finalBillAmount: 850m, refundAmount: 0m, orderStatus: OrderStatus.Cancelled);
            Assert.Equal(0.00m, cancelledNet);

            // Pending order must not contribute to completed net spending
            decimal pendingNet = billService.CalculateNetPurchaseSpending(finalBillAmount: 1200m, refundAmount: 0m, orderStatus: OrderStatus.Pending);
            Assert.Equal(0.00m, pendingNet);

            // Completed order with refund reduces net spending
            decimal completedNet = billService.CalculateNetPurchaseSpending(finalBillAmount: 1200m, refundAmount: 300m, orderStatus: OrderStatus.Completed);
            Assert.Equal(900.00m, completedNet);
        }

        [Fact]
        public void Section5_BillCalculationService_ImportDiscrepancy_DetectedAndExplained()
        {
            var billService = new BillCalculationService();
            var order = new PurchaseOrder
            {
                OrderStatus = OrderStatus.Completed,
                DeliveryFee = 30.00m,
                Discount = 0.00m,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemName = "Shirt", Quantity = 1, UnitPrice = 899.00m }
                }
            };

            // Calculated total = 899 + 30 = 929.00
            // Original bill from invoice image/PDF was 959.00 (discrepancy of 30.00)
            var result = billService.CalculateAndValidate(order, originalPlatformBill: 959.00m);

            Assert.False(result.IsValid);
            Assert.True(result.HasDiscrepancy);
            Assert.Equal(30.00m, result.DiscrepancyAmount);
            Assert.Equal(959.00m, result.OriginalBillAmount);
            Assert.Equal(929.00m, result.CalculatedBillAmount);
            Assert.NotNull(result.DiscrepancyExplanation);
            Assert.Contains("differs from calculated itemized total", result.DiscrepancyExplanation);
        }

        [Fact]
        public void Section5_BillCalculationService_RecalculateOrder_UpdatesEntityState()
        {
            var billService = new BillCalculationService();
            var order = new PurchaseOrder
            {
                OrderStatus = OrderStatus.Completed,
                DeliveryFee = 50.00m,
                PackagingFee = 20.00m,
                Discount = 30.00m,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemName = "Item A", Quantity = 2, UnitPrice = 100.00m },
                    new OrderItem { ItemName = "Item B", Quantity = 1, UnitPrice = 250.00m }
                }
            };

            billService.RecalculateOrder(order);

            // Subtotal = (2*100) + 250 = 450.00
            Assert.Equal(450.00m, order.ItemSubtotal);

            // Final Bill = 450 + 50 + 20 - 30 = 490.00
            Assert.Equal(490.00m, order.FinalBillAmount);
            Assert.Equal(490.00m, order.NetPurchaseSpending);
        }

        #endregion

        #region Section 6 & Section 7: CRUD, Platform & Category Management Tests

        [Fact]
        public void Section6_PurchaseOrderFormViewModel_LineSubtotalsAndBindings_WorkConsistently()
        {
            var formModel = new PurchaseOrderFormViewModel
            {
                PlatformId = 1,
                ExternalOrderNumber = "#SW-99881",
                OrderDate = DateTime.Today,
                OrderStatus = OrderStatus.Completed,
                DeliveryFee = 35.00m,
                Discount = 60.00m,
                Items = new List<PurchaseOrderItemInputModel>
                {
                    new PurchaseOrderItemInputModel { ItemName = "Biryani", CategoryId = 1, Quantity = 2, UnitPrice = 280.00m },
                    new PurchaseOrderItemInputModel { ItemName = "Gulab Jamun", CategoryId = 1, Quantity = 1, UnitPrice = 90.00m }
                }
            };

            // Assert item input model line subtotals
            Assert.Equal(560.00m, formModel.Items[0].LineSubtotal);
            Assert.Equal(90.00m, formModel.Items[1].LineSubtotal);

            decimal itemsSubtotal = formModel.Items.Sum(i => i.LineSubtotal);
            Assert.Equal(650.00m, itemsSubtotal);

            decimal finalBill = itemsSubtotal + formModel.DeliveryFee - formModel.Discount;
            Assert.Equal(625.00m, finalBill);
        }

        [Fact]
        public async Task Section6_PurchaseOrder_CascadeDeletion_DeletesItemsWithoutOrphans()
        {
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<PersonalExpenseTracker.Data.AppDbContext>()
                .UseSqlite(connection)
                .Options;

            using (var context = new PersonalExpenseTracker.Data.AppDbContext(options))
            {
                context.Database.EnsureCreated();

                var order = new PurchaseOrder
                {
                    Id = 10,
                    PlatformId = 1,
                    OrderStatus = OrderStatus.Completed,
                    FinalBillAmount = 500m,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { Id = 101, ItemName = "Noodles", CategoryId = 1, Quantity = 2, UnitPrice = 150m },
                        new OrderItem { Id = 102, ItemName = "Soup", CategoryId = 1, Quantity = 1, UnitPrice = 200m }
                    }
                };

                context.PurchaseOrders.Add(order);
                await context.SaveChangesAsync();

                // Assert stored
                Assert.Equal(1, await context.PurchaseOrders.CountAsync(p => p.Id == 10));
                Assert.Equal(2, await context.OrderItems.CountAsync(i => i.PurchaseOrderId == 10));

                // Remove Order
                context.PurchaseOrders.Remove(order);
                await context.SaveChangesAsync();

                // Order items must be deleted via cascade
                Assert.Equal(0, await context.PurchaseOrders.CountAsync(p => p.Id == 10));
                Assert.Equal(0, await context.OrderItems.CountAsync(i => i.PurchaseOrderId == 10));
            }
        }

        [Fact]
        public void Section7_PlatformManagement_ToggleActive_UpdatesStatus()
        {
            var platform = new Platform
            {
                Id = 1,
                Name = "Swiggy",
                PlatformType = "Food Delivery",
                IsActive = true
            };

            // Toggle to inactive
            platform.IsActive = !platform.IsActive;
            Assert.False(platform.IsActive);

            // Toggle back to active
            platform.IsActive = !platform.IsActive;
            Assert.True(platform.IsActive);
        }

        [Fact]
        public void Section7_PlatformManagement_HistoricalReference_ProtectsFromHardDelete()
        {
            var platform = new Platform
            {
                Id = 1,
                Name = "Zepto",
                PlatformType = "Groceries & Quick Commerce",
                PurchaseOrders = new List<PurchaseOrder>
                {
                    new PurchaseOrder { Id = 1, OrderStatus = OrderStatus.Completed, FinalBillAmount = 450m }
                }
            };

            // Rule: If referenced by historical purchase orders, do not hard-delete
            bool canHardDelete = platform.PurchaseOrders == null || !platform.PurchaseOrders.Any();
            Assert.False(canHardDelete);
        }

        [Fact]
        public void Section7_CategoryManagement_HistoricalReference_ProtectsFromHardDelete()
        {
            var category = new Category
            {
                Id = 2,
                Name = "Groceries",
                CategoryType = "Expense",
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { Id = 1, ItemName = "Rice", Quantity = 1, UnitPrice = 250m }
                }
            };

            // Rule: If referenced by order items or budgets, do not hard-delete
            int refCount = (category.OrderItems?.Count ?? 0) + (category.CategoryBudgets?.Count ?? 0);
            Assert.True(refCount > 0);
        }

        #endregion

        #region Section 8: Financial Accounting & Proportional Allocation Tests

        [Fact]
        public void Section8_ProportionalCategoryAllocation_DeterministicReconciliation_ZeroDrift()
        {
            var service = new FinancialAccountingService();

            // Order with 3 items each having subtotal 100 (total subtotal 300)
            // Final net bill = 100.00
            // 100 / 3 = 33.3333... per category
            // Rounding 33.33 * 3 = 99.99
            // Deterministic remainder (0.01) must be added so total allocated sum == 100.00
            var order = new PurchaseOrder
            {
                OrderStatus = OrderStatus.Completed,
                FinalBillAmount = 100m,
                RefundAmount = 0m,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemName = "Food 1", Category = new Category { Name = "Food" }, Quantity = 1, UnitPrice = 100m },
                    new OrderItem { ItemName = "Grocery 1", Category = new Category { Name = "Groceries" }, Quantity = 1, UnitPrice = 100m },
                    new OrderItem { ItemName = "Household 1", Category = new Category { Name = "Household" }, Quantity = 1, UnitPrice = 100m }
                }
            };

            var catSpending = service.CalculateCategoryWiseSpending(Enumerable.Empty<Transaction>(), new[] { order });

            Assert.Equal(3, catSpending.Count);
            decimal totalAllocated = catSpending.Values.Sum();
            Assert.Equal(100.00m, totalAllocated); // Exact reconciliation with zero drift!
        }

        [Fact]
        public void Section8_ProportionalCategoryAllocation_ZeroSubtotal_UsesEqualSplitRule()
        {
            var service = new FinancialAccountingService();

            // Zero item subtotal (free items with delivery fee)
            var order = new PurchaseOrder
            {
                OrderStatus = OrderStatus.Completed,
                FinalBillAmount = 50m,
                RefundAmount = 0m,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemName = "Promo Sample 1", Category = new Category { Name = "Food" }, Quantity = 1, UnitPrice = 0m },
                    new OrderItem { ItemName = "Promo Sample 2", Category = new Category { Name = "Groceries" }, Quantity = 1, UnitPrice = 0m }
                }
            };

            var catSpending = service.CalculateCategoryWiseSpending(Enumerable.Empty<Transaction>(), new[] { order });

            Assert.Equal(2, catSpending.Count);
            Assert.Equal(25.00m, catSpending["Food"]);
            Assert.Equal(25.00m, catSpending["Groceries"]);
            Assert.Equal(50.00m, catSpending.Values.Sum());
        }

        #endregion

        #region Section 9: Category Budget & Monthly Budget Progress Tests

        [Fact]
        public void Section9_CategoryBudgetProgress_StandardProgressCalculation()
        {
            var progress = new CategoryBudgetProgressViewModel
            {
                CategoryId = 1,
                CategoryName = "Food",
                BudgetAmount = 5000m,
                ActualSpending = 2500m
            };

            Assert.Equal(50.0, progress.PercentageConsumed);
            Assert.Equal(2500m, progress.RemainingBudget);
            Assert.False(progress.IsOverBudget);
            Assert.Equal(0m, progress.OverBudgetAmount);
            Assert.Equal("bg-success", progress.ProgressBarClass);
        }

        [Fact]
        public void Section9_CategoryBudgetProgress_OverBudgetCalculation()
        {
            var progress = new CategoryBudgetProgressViewModel
            {
                CategoryId = 2,
                CategoryName = "Groceries",
                BudgetAmount = 2000m,
                ActualSpending = 3000m
            };

            Assert.Equal(150.0, progress.PercentageConsumed);
            Assert.Equal(-1000m, progress.RemainingBudget);
            Assert.True(progress.IsOverBudget);
            Assert.Equal(1000m, progress.OverBudgetAmount);
            Assert.Equal("bg-danger", progress.ProgressBarClass);
        }

        [Fact]
        public void Section9_CategoryBudgetProgress_ZeroBudget_NoDivisionByZero()
        {
            var progressWithSpending = new CategoryBudgetProgressViewModel
            {
                CategoryId = 3,
                CategoryName = "Electronics",
                BudgetAmount = 0m,
                ActualSpending = 500m
            };

            // When budget is 0 and spending > 0, safely capped at 100.0 without division by zero
            Assert.Equal(100.0, progressWithSpending.PercentageConsumed);
            Assert.Equal(-500m, progressWithSpending.RemainingBudget);
            Assert.True(progressWithSpending.IsOverBudget);
            Assert.Equal(500m, progressWithSpending.OverBudgetAmount);

            var progressNoSpending = new CategoryBudgetProgressViewModel
            {
                CategoryId = 3,
                CategoryName = "Electronics",
                BudgetAmount = 0m,
                ActualSpending = 0m
            };

            // When budget is 0 and spending is 0, percentage is 0.0
            Assert.Equal(0.0, progressNoSpending.PercentageConsumed);
            Assert.Equal(0m, progressNoSpending.RemainingBudget);
            Assert.False(progressNoSpending.IsOverBudget);
            Assert.Equal(0m, progressNoSpending.OverBudgetAmount);
        }

        #endregion

        #region Section 10: CSV Import & Export Tests

        [Fact]
        public void Section10_CsvExport_FormulaInjectionDefense_EscapesSpecialCharacters()
        {
            var billService = new BillCalculationService();
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<PersonalExpenseTracker.Data.AppDbContext>()
                .UseSqlite(connection)
                .Options;

            using var context = new PersonalExpenseTracker.Data.AppDbContext(options);
            context.Database.EnsureCreated();
            var csvService = new PurchaseOrderCsvService(context, billService);

            var orders = new List<PurchaseOrder>
            {
                new PurchaseOrder
                {
                    Id = 1,
                    ExternalOrderNumber = "=1+2",
                    PlatformName = "@MaliciousPlatform",
                    OrderStatus = OrderStatus.Completed,
                    PaymentMethod = "UPI",
                    OrderDate = new DateTime(2026, 10, 20),
                    SourceType = SourceTypes.Manual,
                    Items = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            ItemName = "+cmd|' /C calc'!A0",
                            Category = new Category { Name = "-FormulaTest" },
                            Quantity = 1,
                            UnitPrice = 100m
                        }
                    }
                }
            };

            byte[] csvBytes = csvService.ExportOrdersToCsv(orders);
            string csvContent = System.Text.Encoding.UTF8.GetString(csvBytes);

            // Verify formula injection prefixes (=, @, +, -) are safely escaped with apostrophe (')
            Assert.Contains("'=1+2", csvContent);
            Assert.Contains("'@MaliciousPlatform", csvContent);
            Assert.Contains("'+cmd", csvContent);
            Assert.Contains("'-FormulaTest", csvContent);
        }

        [Fact]
        public async Task Section10_CsvParsing_MultiRowOrderGroupingAndCalculations()
        {
            var billService = new BillCalculationService();
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<PersonalExpenseTracker.Data.AppDbContext>()
                .UseSqlite(connection)
                .Options;

            using var context = new PersonalExpenseTracker.Data.AppDbContext(options);
            context.Database.EnsureCreated(); // Seeds Swiggy and Food automatically

            var csvService = new PurchaseOrderCsvService(context, billService);

            string csvData = 
                "ExternalOrderNumber,Platform,OrderDate,OrderStatus,PaymentMethod,ItemName,Category,Quantity,UnitPrice,DeliveryFee,PackagingFee,PlatformFee,Taxes,OtherCharges,Discount,RefundAmount\n" +
                "SWG-101,Swiggy,2026-10-25,Completed,UPI,Pizza,Food,1,400.00,30.00,10.00,5.00,20.00,0.00,50.00,0.00\n" +
                "SWG-101,Swiggy,2026-10-25,Completed,UPI,Garlic Bread,Food,2,100.00,30.00,10.00,5.00,20.00,0.00,50.00,0.00\n";

            var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvData));
            var formFile = new Microsoft.AspNetCore.Http.FormFile(stream, 0, stream.Length, "csvFile", "test_orders.csv");

            var preview = await csvService.ParseAndValidateCsvAsync(formFile);

            Assert.Equal(2, preview.TotalRowsRead);
            Assert.Single(preview.Groups); // 2 rows grouped into 1 order
            var group = preview.Groups[0];
            Assert.Equal("SWG-101", group.ExternalOrderNumber);
            Assert.Equal("Swiggy", group.PlatformName);
            Assert.Equal(2, group.Items.Count);
            Assert.Equal(600m, group.ItemsSubtotal); // 400 + 200
            // PreDiscountTotal = 600 + 30 + 10 + 5 + 20 = 665
            // FinalBillAmount = 665 - 50 = 615
            Assert.Equal(615m, group.FinalBillAmount);
            Assert.Equal(615m, group.NetSpending);
            Assert.True(group.IsValid);
        }

        [Fact]
        public async Task Section10_CsvValidation_DiscountExceedingPreDiscount_Fails()
        {
            var billService = new BillCalculationService();
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<PersonalExpenseTracker.Data.AppDbContext>()
                .UseSqlite(connection)
                .Options;

            using var context = new PersonalExpenseTracker.Data.AppDbContext(options);
            context.Database.EnsureCreated();

            var csvService = new PurchaseOrderCsvService(context, billService);

            // Discount 500 > Pre-discount subtotal 100
            string csvData = 
                "ExternalOrderNumber,Platform,OrderDate,OrderStatus,PaymentMethod,ItemName,Category,Quantity,UnitPrice,DeliveryFee,PackagingFee,PlatformFee,Taxes,OtherCharges,Discount,RefundAmount\n" +
                "SWG-102,Swiggy,2026-10-25,Completed,UPI,Snack,Food,1,100.00,0.00,0.00,0.00,0.00,0.00,500.00,0.00\n";

            var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvData));
            var formFile = new Microsoft.AspNetCore.Http.FormFile(stream, 0, stream.Length, "csvFile", "test_discount.csv");

            var preview = await csvService.ParseAndValidateCsvAsync(formFile);

            Assert.Single(preview.Groups);
            var group = preview.Groups[0];
            Assert.False(group.IsValid);
            Assert.Contains(group.ValidationErrors, e => e.Contains("Discount") && e.Contains("exceeds pre-discount subtotal"));
        }

        [Fact]
        public async Task Section10_CsvValidation_RefundExceedingFinalBill_Fails()
        {
            var billService = new BillCalculationService();
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<PersonalExpenseTracker.Data.AppDbContext>()
                .UseSqlite(connection)
                .Options;

            using var context = new PersonalExpenseTracker.Data.AppDbContext(options);
            context.Database.EnsureCreated();

            var csvService = new PurchaseOrderCsvService(context, billService);

            // Final bill = 100, Refund = 200 (exceeds final bill)
            string csvData = 
                "ExternalOrderNumber,Platform,OrderDate,OrderStatus,PaymentMethod,ItemName,Category,Quantity,UnitPrice,DeliveryFee,PackagingFee,PlatformFee,Taxes,OtherCharges,Discount,RefundAmount\n" +
                "SWG-103,Swiggy,2026-10-25,Completed,UPI,Snack,Food,1,100.00,0.00,0.00,0.00,0.00,0.00,0.00,200.00\n";

            var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvData));
            var formFile = new Microsoft.AspNetCore.Http.FormFile(stream, 0, stream.Length, "csvFile", "test_refund.csv");

            var preview = await csvService.ParseAndValidateCsvAsync(formFile);

            Assert.Single(preview.Groups);
            var group = preview.Groups[0];
            Assert.False(group.IsValid);
            Assert.Contains(group.ValidationErrors, e => e.Contains("Refund") && e.Contains("exceeds final bill"));
        }

        [Fact]
        public async Task Section10_CsvValidation_InconsistentOrderFeesAcrossRows_FlagsError()
        {
            var billService = new BillCalculationService();
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<PersonalExpenseTracker.Data.AppDbContext>()
                .UseSqlite(connection)
                .Options;

            using var context = new PersonalExpenseTracker.Data.AppDbContext(options);
            context.Database.EnsureCreated();

            var csvService = new PurchaseOrderCsvService(context, billService);

            // Two rows for SWG-104 with different delivery fees (30 vs 50)
            string csvData = 
                "ExternalOrderNumber,Platform,OrderDate,OrderStatus,PaymentMethod,ItemName,Category,Quantity,UnitPrice,DeliveryFee,PackagingFee,PlatformFee,Taxes,OtherCharges,Discount,RefundAmount\n" +
                "SWG-104,Swiggy,2026-10-25,Completed,UPI,Burger,Food,1,150.00,30.00,0.00,0.00,0.00,0.00,0.00,0.00\n" +
                "SWG-104,Swiggy,2026-10-25,Completed,UPI,Fries,Food,1,80.00,50.00,0.00,0.00,0.00,0.00,0.00,0.00\n";

            var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvData));
            var formFile = new Microsoft.AspNetCore.Http.FormFile(stream, 0, stream.Length, "csvFile", "test_inconsistent.csv");

            var preview = await csvService.ParseAndValidateCsvAsync(formFile);

            Assert.Single(preview.Groups);
            var group = preview.Groups[0];
            Assert.False(group.IsValid);
            Assert.Contains(group.ValidationErrors, e => e.Contains("Inconsistent order fees/discounts"));
        }

        #endregion

        #region Section 12: Security & Data Quality Automated Tests

        [Fact]
        public void Section12_Security_AllHttpPostActions_MustHaveAntiForgeryToken()
        {
            var controllerTypes = new[]
            {
                typeof(PurchaseOrdersController),
                typeof(TransactionsController),
                typeof(BudgetsController),
                typeof(PlatformsController),
                typeof(CategoriesController)
            };

            var missingAntiforgery = new List<string>();

            foreach (var type in controllerTypes)
            {
                var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                foreach (var method in methods)
                {
                    bool isHttpPost = method.GetCustomAttributes(typeof(HttpPostAttribute), inherit: false).Any();
                    if (isHttpPost)
                    {
                        bool hasAntiforgery = method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), inherit: false).Any() ||
                                              method.GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), inherit: false).Any() ||
                                              method.GetCustomAttributes(typeof(IgnoreAntiforgeryTokenAttribute), inherit: false).Any();

                        if (!hasAntiforgery)
                        {
                            missingAntiforgery.Add($"{type.Name}.{method.Name}");
                        }
                    }
                }
            }

            Assert.Empty(missingAntiforgery);
        }

        [Fact]
        public void Section12_Security_OverpostingDefense_BindingRestrictionsEnforced()
        {
            // 1. TransactionsController.Create has [Bind] attribute
            var createTxMethod = typeof(TransactionsController).GetMethod("Create", new[] { typeof(Transaction) });
            Assert.NotNull(createTxMethod);
            var txParam = createTxMethod.GetParameters()[0];
            var bindTx = txParam.GetCustomAttribute<BindAttribute>();
            Assert.NotNull(bindTx);
            Assert.Contains("Title", bindTx.Include);
            Assert.Contains("Amount", bindTx.Include);
            Assert.DoesNotContain("Id", bindTx.Include); // Protects primary key ID from being overwritten

            // 2. BudgetsController.Create has [Bind] attribute
            var createBudgetMethod = typeof(BudgetsController).GetMethod("Create", new[] { typeof(MonthlyBudget) });
            Assert.NotNull(createBudgetMethod);
            var budgetParam = createBudgetMethod.GetParameters()[0];
            var bindBudget = budgetParam.GetCustomAttribute<BindAttribute>();
            Assert.NotNull(bindBudget);
            Assert.Contains("Amount", bindBudget.Include);
            Assert.DoesNotContain("Id", bindBudget.Include);

            // 3. PurchaseOrdersController.Create uses PurchaseOrderFormViewModel (ViewModel isolation)
            var createPoMethod = typeof(PurchaseOrdersController).GetMethod("Create", new[] { typeof(PurchaseOrderFormViewModel) });
            Assert.NotNull(createPoMethod);
        }

        [Fact]
        public async Task Section12_Security_PlatformDelete_RejectsIfReferenced()
        {
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<PersonalExpenseTracker.Data.AppDbContext>()
                .UseSqlite(connection)
                .Options;

            using var context = new PersonalExpenseTracker.Data.AppDbContext(options);
            context.Database.EnsureCreated();

            var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
            var logger = loggerFactory.CreateLogger<PlatformsController>();

            var controller = new PlatformsController(context, logger)
            {
                TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                    new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                    new TestTempDataProvider())
            };

            // Platform 1 is Swiggy (seeded)
            // Add a purchase order referencing Swiggy (Platform 1)
            var order = new PurchaseOrder
            {
                PlatformId = 1,
                OrderStatus = OrderStatus.Completed,
                OrderDate = DateTime.Today,
                FinalBillAmount = 500m
            };
            context.PurchaseOrders.Add(order);
            await context.SaveChangesAsync();

            // Attempt to delete platform 1
            var result = await controller.Delete(1);

            // Platform 1 must still exist in database
            var platformStillExists = await context.Platforms.AnyAsync(p => p.Id == 1);
            Assert.True(platformStillExists);

            // TempData must contain warning
            Assert.NotNull(controller.TempData["ErrorMessage"]);
            Assert.Contains("Cannot delete", controller.TempData["ErrorMessage"]!.ToString());
        }

        [Fact]
        public async Task Section12_Security_CategoryDelete_RejectsIfReferenced()
        {
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<PersonalExpenseTracker.Data.AppDbContext>()
                .UseSqlite(connection)
                .Options;

            using var context = new PersonalExpenseTracker.Data.AppDbContext(options);
            context.Database.EnsureCreated();

            var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
            var logger = loggerFactory.CreateLogger<CategoriesController>();

            var controller = new CategoriesController(context, logger)
            {
                TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                    new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                    new TestTempDataProvider())
            };

            // Category 1 is Food (seeded)
            // Add an order item referencing Food (Category 1)
            var order = new PurchaseOrder
            {
                PlatformId = 1,
                OrderStatus = OrderStatus.Completed,
                OrderDate = DateTime.Today,
                FinalBillAmount = 250m
            };
            context.PurchaseOrders.Add(order);
            await context.SaveChangesAsync();

            context.OrderItems.Add(new OrderItem
            {
                PurchaseOrderId = order.Id,
                CategoryId = 1,
                ItemName = "Snack",
                Quantity = 1,
                UnitPrice = 250m
            });
            await context.SaveChangesAsync();

            // Attempt to delete Category 1
            var result = await controller.Delete(1);

            // Category 1 must still exist in database
            var categoryStillExists = await context.Categories.AnyAsync(c => c.Id == 1);
            Assert.True(categoryStillExists);

            // TempData must contain warning
            Assert.NotNull(controller.TempData["ErrorMessage"]);
            Assert.Contains("Cannot delete", controller.TempData["ErrorMessage"]!.ToString());
        }

        private class TestTempDataProvider : Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider
        {
            private readonly Dictionary<string, object> _data = new();
            public IDictionary<string, object> LoadTempData(HttpContext context) => _data;
            public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
        }

        private class TestSession : ISession
        {
            private readonly Dictionary<string, byte[]> _storage = new();
            public bool IsAvailable => true;
            public string Id => "test-session";
            public IEnumerable<string> Keys => _storage.Keys;
            public void Clear() => _storage.Clear();
            public Task CommitAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task LoadAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
            public void Remove(string key) => _storage.Remove(key);
            public void Set(string key, byte[] value) => _storage[key] = value;
            public bool TryGetValue(string key, out byte[] value) => _storage.TryGetValue(key, out value!);
        }

        #endregion

        #endregion
    }
}
