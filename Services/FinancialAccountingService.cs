using System;
using System.Collections.Generic;
using System.Linq;
using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.Services
{
    public class FinancialAccountingService : IFinancialAccountingService
    {
        public decimal CalculateStandaloneIncome(IEnumerable<Transaction> transactions)
        {
            if (transactions == null) return 0m;
            return Math.Round(
                transactions
                    .Where(t => string.Equals(t.Type, "Income", StringComparison.OrdinalIgnoreCase))
                    .Sum(t => t.Amount), 
                2, MidpointRounding.AwayFromZero);
        }

        public decimal CalculateStandaloneExpenses(IEnumerable<Transaction> transactions)
        {
            if (transactions == null) return 0m;
            return Math.Round(
                transactions
                    .Where(t => string.Equals(t.Type, "Expense", StringComparison.OrdinalIgnoreCase))
                    .Sum(t => t.Amount), 
                2, MidpointRounding.AwayFromZero);
        }

        public decimal CalculateNetPurchaseSpending(IEnumerable<PurchaseOrder> orders)
        {
            if (orders == null) return 0m;
            return Math.Round(
                orders
                    .Where(o => string.Equals(o.OrderStatus, OrderStatus.Completed, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(o.Status, OrderStatus.Completed, StringComparison.OrdinalIgnoreCase))
                    .Sum(o => o.NetPurchaseSpending),
                2, MidpointRounding.AwayFromZero);
        }

        public decimal CalculatePendingPurchases(IEnumerable<PurchaseOrder> orders)
        {
            if (orders == null) return 0m;
            return Math.Round(
                orders
                    .Where(o => string.Equals(o.OrderStatus, OrderStatus.Pending, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(o.Status, OrderStatus.Pending, StringComparison.OrdinalIgnoreCase))
                    .Sum(o => o.FinalBillAmount),
                2, MidpointRounding.AwayFromZero);
        }

        public decimal CalculateCancelledPurchases(IEnumerable<PurchaseOrder> orders)
        {
            if (orders == null) return 0m;
            return Math.Round(
                orders
                    .Where(o => string.Equals(o.OrderStatus, OrderStatus.Cancelled, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(o.Status, OrderStatus.Cancelled, StringComparison.OrdinalIgnoreCase))
                    .Sum(o => o.FinalBillAmount),
                2, MidpointRounding.AwayFromZero);
        }

        public decimal CalculateTotalExpenses(decimal standaloneExpenses, decimal netPurchaseSpending)
        {
            return Math.Round(standaloneExpenses + netPurchaseSpending, 2, MidpointRounding.AwayFromZero);
        }

        public decimal CalculateNetBalance(decimal standaloneIncome, decimal totalExpenses)
        {
            return Math.Round(standaloneIncome - totalExpenses, 2, MidpointRounding.AwayFromZero);
        }

        public Dictionary<string, decimal> CalculateCategoryWiseSpending(
            IEnumerable<Transaction> standaloneExpenses, 
            IEnumerable<PurchaseOrder> completedOrders)
        {
            var dict = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            // 1. Add standalone expenses
            if (standaloneExpenses != null)
            {
                foreach (var tx in standaloneExpenses.Where(t => string.Equals(t.Type, "Expense", StringComparison.OrdinalIgnoreCase)))
                {
                    string cat = string.IsNullOrWhiteSpace(tx.Category) ? "Other Expense" : tx.Category.Trim();
                    dict.TryGetValue(cat, out decimal current);
                    dict[cat] = current + tx.Amount;
                }
            }

            // 2. Add itemized purchase order spending (completed orders only)
            // Allocates order-level charges, discounts, taxes, and refunds proportionally by item category
            if (completedOrders != null)
            {
                foreach (var order in completedOrders.Where(o => string.Equals(o.OrderStatus, OrderStatus.Completed, StringComparison.OrdinalIgnoreCase) ||
                                                                string.Equals(o.Status, OrderStatus.Completed, StringComparison.OrdinalIgnoreCase)))
                {
                    decimal orderNet = order.NetPurchaseSpending;

                    if (order.Items != null && order.Items.Any())
                    {
                        var categoryGroups = order.Items
                            .GroupBy(i => !string.IsNullOrWhiteSpace(i.Category?.Name) 
                                ? i.Category.Name 
                                : (!string.IsNullOrWhiteSpace(i.CategoryName) ? i.CategoryName : "Other"))
                            .Select(g => new { Category = g.Key, Subtotal = g.Sum(i => i.LineSubtotal) })
                            .ToList();

                        decimal totalSubtotal = categoryGroups.Sum(g => g.Subtotal);
                        var allocations = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                        decimal sumAllocated = 0m;
                        string? largestCategory = null;
                        decimal largestSubtotal = -1m;

                        if (totalSubtotal > 0)
                        {
                            foreach (var group in categoryGroups)
                            {
                                decimal share = Math.Round(orderNet * (group.Subtotal / totalSubtotal), 2, MidpointRounding.AwayFromZero);
                                allocations[group.Category] = share;
                                sumAllocated += share;

                                if (group.Subtotal > largestSubtotal)
                                {
                                    largestSubtotal = group.Subtotal;
                                    largestCategory = group.Category;
                                }
                            }

                            // Deterministic rounding remainder assignment so category sum reconciles exactly with order net spending
                            decimal remainder = orderNet - sumAllocated;
                            if (remainder != 0m && largestCategory != null)
                            {
                                allocations[largestCategory] += remainder;
                            }
                        }
                        else
                        {
                            // Sensible alternative allocation rule when items subtotal is zero
                            int count = categoryGroups.Count;
                            decimal equalShare = count > 0 ? Math.Round(orderNet / count, 2, MidpointRounding.AwayFromZero) : orderNet;
                            foreach (var group in categoryGroups)
                            {
                                allocations[group.Category] = equalShare;
                                sumAllocated += equalShare;
                            }
                            decimal remainder = orderNet - sumAllocated;
                            if (remainder != 0m && categoryGroups.Any())
                            {
                                allocations[categoryGroups[0].Category] += remainder;
                            }
                        }

                        foreach (var kvp in allocations)
                        {
                            dict.TryGetValue(kvp.Key, out decimal current);
                            dict[kvp.Key] = current + kvp.Value;
                        }
                    }
                    else
                    {
                        // Fallback when order has no itemized sub-items
                        string cat = "Shopping";
                        dict.TryGetValue(cat, out decimal current);
                        dict[cat] = current + orderNet;
                    }
                }
            }

            // Round all final dictionary values to 2 decimal places
            return dict
                .OrderByDescending(kv => kv.Value)
                .ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value, 2, MidpointRounding.AwayFromZero));
        }

        public Dictionary<string, decimal> CalculatePlatformWiseSpending(IEnumerable<PurchaseOrder> completedOrders)
        {
            var dict = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            if (completedOrders != null)
            {
                foreach (var order in completedOrders.Where(o => string.Equals(o.OrderStatus, OrderStatus.Completed, StringComparison.OrdinalIgnoreCase) ||
                                                                string.Equals(o.Status, OrderStatus.Completed, StringComparison.OrdinalIgnoreCase)))
                {
                    string plat = !string.IsNullOrWhiteSpace(order.Platform?.Name) 
                        ? order.Platform.Name 
                        : (!string.IsNullOrWhiteSpace(order.PlatformName) ? order.PlatformName : "Direct / In-Store");

                    dict.TryGetValue(plat, out decimal current);
                    dict[plat] = current + order.NetPurchaseSpending;
                }
            }

            return dict
                .OrderByDescending(kv => kv.Value)
                .ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value, 2, MidpointRounding.AwayFromZero));
        }

        public BudgetComparisonResult CalculateBudgetComparison(decimal budgetAmount, decimal totalSpending)
        {
            return new BudgetComparisonResult
            {
                BudgetAmount = Math.Round(budgetAmount, 2, MidpointRounding.AwayFromZero),
                TotalSpending = Math.Round(totalSpending, 2, MidpointRounding.AwayFromZero)
            };
        }
    }
}

