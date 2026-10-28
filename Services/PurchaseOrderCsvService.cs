using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Data;
using PersonalExpenseTracker.Models;
using PersonalExpenseTracker.ViewModels;

namespace PersonalExpenseTracker.Services
{
    public class PurchaseOrderCsvService : IPurchaseOrderCsvService
    {
        private readonly AppDbContext _context;
        private readonly IBillCalculationService _billService;

        public PurchaseOrderCsvService(AppDbContext context, IBillCalculationService billService)
        {
            _context = context;
            _billService = billService;
        }

        public byte[] GenerateTemplateCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("ExternalOrderNumber,Platform,OrderDate,OrderStatus,PaymentMethod,ItemName,Category,Quantity,UnitPrice,DeliveryFee,PackagingFee,PlatformFee,Taxes,OtherCharges,Discount,RefundAmount");
            // Realistic sample multi-item food delivery order
            sb.AppendLine("SWIGGY-1001,Swiggy,2026-10-25,Completed,UPI,Paneer Tikka,Food,2,220.00,30.00,10.00,5.00,15.00,0.00,50.00,0.00");
            sb.AppendLine("SWIGGY-1001,Swiggy,2026-10-25,Completed,UPI,Garlic Naan,Food,3,40.00,30.00,10.00,5.00,15.00,0.00,50.00,0.00");
            // Realistic sample grocery order
            sb.AppendLine("BLINKIT-2002,Blinkit,2026-10-26,Completed,Credit Card,Toned Milk 1L,Groceries,2,35.00,15.00,0.00,2.00,0.00,0.00,0.00,0.00");
            sb.AppendLine("BLINKIT-2002,Blinkit,2026-10-26,Completed,Credit Card,Whole Wheat Bread,Groceries,1,45.00,15.00,0.00,2.00,0.00,0.00,0.00,0.00");

            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        public async Task<ImportPreviewViewModel> ParseAndValidateCsvAsync(IFormFile file)
        {
            var result = new ImportPreviewViewModel();

            if (file == null || file.Length == 0)
            {
                result.FileErrors.Add("The uploaded file is empty.");
                return result;
            }

            // Limit file size (5 MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                result.FileErrors.Add("File size exceeds the allowed 5 MB limit.");
                return result;
            }

            // Validate extension
            string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".csv")
            {
                result.FileErrors.Add("Invalid file type. Only standard .csv files are supported.");
                return result;
            }

            // Load active platforms and categories for validation
            var platforms = await _context.Platforms.AsNoTracking().ToListAsync();
            var platformMap = platforms.ToDictionary(p => p.Name.Trim(), p => p, StringComparer.OrdinalIgnoreCase);

            var categories = await _context.Categories.AsNoTracking().ToListAsync();
            var categoryMap = categories.ToDictionary(c => c.Name.Trim(), c => c, StringComparer.OrdinalIgnoreCase);

            var rawRows = new List<(int rowNum, string[] columns)>();

            using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
            {
                int lineIndex = 0;
                string? line;
                string[]? headers = null;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lineIndex++;
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var cols = ParseCsvLine(line);
                    if (lineIndex == 1)
                    {
                        headers = cols.Select(h => h.Trim().Replace(" ", "").Replace("_", "").ToLowerInvariant()).ToArray();
                        continue;
                    }

                    rawRows.Add((lineIndex, cols));
                }
            }

            result.TotalRowsRead = rawRows.Count;
            if (!rawRows.Any())
            {
                result.FileErrors.Add("No data rows found in the CSV file.");
                return result;
            }

            // Group rows by (Platform, ExternalOrderNumber)
            var groupDict = new Dictionary<string, ImportOrderGroupPreview>(StringComparer.OrdinalIgnoreCase);

            foreach (var (rowNum, cols) in rawRows)
            {
                if (cols.Length < 9)
                {
                    result.FileErrors.Add($"Row {rowNum}: Insufficient columns. Minimum 9 columns required.");
                    continue;
                }

                string orderNum = GetCol(cols, 0).Trim();
                string platName = GetCol(cols, 1).Trim();
                string dateStr = GetCol(cols, 2).Trim();
                string statusStr = GetCol(cols, 3).Trim();
                string payMethod = GetCol(cols, 4).Trim();
                string itemName = GetCol(cols, 5).Trim();
                string catName = GetCol(cols, 6).Trim();
                string qtyStr = GetCol(cols, 7).Trim();
                string priceStr = GetCol(cols, 8).Trim();
                string deliveryStr = GetCol(cols, 9).Trim();
                string packagingStr = GetCol(cols, 10).Trim();
                string platformFeeStr = GetCol(cols, 11).Trim();
                string taxesStr = GetCol(cols, 12).Trim();
                string otherStr = GetCol(cols, 13).Trim();
                string discountStr = GetCol(cols, 14).Trim();
                string refundStr = GetCol(cols, 15).Trim();

                if (string.IsNullOrWhiteSpace(orderNum))
                {
                    result.FileErrors.Add($"Row {rowNum}: ExternalOrderNumber is required for CSV import to group item rows.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(platName))
                {
                    result.FileErrors.Add($"Row {rowNum}: Platform is required.");
                    continue;
                }

                string groupKey = $"{platName}|{orderNum}";

                if (!groupDict.TryGetValue(groupKey, out var group))
                {
                    group = new ImportOrderGroupPreview
                    {
                        GroupKey = groupKey,
                        ExternalOrderNumber = orderNum,
                        PlatformName = platName,
                        OrderStatus = string.IsNullOrWhiteSpace(statusStr) ? OrderStatus.Completed : statusStr,
                        PaymentMethod = string.IsNullOrWhiteSpace(payMethod) ? "UPI" : payMethod
                    };

                    // Match Platform
                    if (platformMap.TryGetValue(platName, out var pMatch))
                    {
                        group.PlatformId = pMatch.Id;
                    }
                    else
                    {
                        group.IsValid = false;
                        group.ValidationErrors.Add($"Platform '{platName}' is not registered in the system.");
                    }

                    // Parse Date
                    if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate) ||
                        DateTime.TryParse(dateStr, out parsedDate))
                    {
                        group.OrderDate = parsedDate;
                    }
                    else
                    {
                        group.IsValid = false;
                        group.ValidationErrors.Add($"Invalid OrderDate '{dateStr}'. Use YYYY-MM-DD.");
                    }

                    // Parse Order Fees
                    group.DeliveryFee = ParseDecimal(deliveryStr, 0m);
                    group.PackagingFee = ParseDecimal(packagingStr, 0m);
                    group.PlatformFee = ParseDecimal(platformFeeStr, 0m);
                    group.Taxes = ParseDecimal(taxesStr, 0m);
                    group.OtherCharges = ParseDecimal(otherStr, 0m);
                    group.Discount = ParseDecimal(discountStr, 0m);
                    group.RefundAmount = ParseDecimal(refundStr, 0m);

                    groupDict[groupKey] = group;
                }
                else
                {
                    // Order-level fields consistency check across rows of the same order (Section 10)
                    if (ParseDecimal(deliveryStr, 0m) != group.DeliveryFee ||
                        ParseDecimal(discountStr, 0m) != group.Discount ||
                        ParseDecimal(refundStr, 0m) != group.RefundAmount)
                    {
                        group.IsValid = false;
                        group.ValidationErrors.Add($"Row {rowNum}: Inconsistent order fees/discounts. Order-level charges must match across all rows in order '{orderNum}'.");
                    }
                }

                // Child Item validation
                var itemPreview = new ImportItemPreview
                {
                    RowNumber = rowNum,
                    ItemName = itemName,
                    CategoryName = catName
                };

                if (string.IsNullOrWhiteSpace(itemName))
                {
                    itemPreview.IsValid = false;
                    itemPreview.Errors.Add("ItemName cannot be empty.");
                }

                if (categoryMap.TryGetValue(catName, out var cMatch))
                {
                    itemPreview.CategoryId = cMatch.Id;
                }
                else
                {
                    itemPreview.IsValid = false;
                    itemPreview.Errors.Add($"Category '{catName}' does not exist.");
                }

                if (int.TryParse(qtyStr, out int qty) && qty >= 1 && qty <= 10000)
                {
                    itemPreview.Quantity = qty;
                }
                else
                {
                    itemPreview.IsValid = false;
                    itemPreview.Errors.Add($"Invalid Quantity '{qtyStr}'. Must be an integer between 1 and 10000.");
                }

                itemPreview.UnitPrice = ParseDecimal(priceStr, -1m);
                if (itemPreview.UnitPrice < 0)
                {
                    itemPreview.IsValid = false;
                    itemPreview.Errors.Add($"Invalid UnitPrice '{priceStr}'. Must be non-negative.");
                }

                if (!itemPreview.IsValid)
                {
                    group.IsValid = false;
                    group.ValidationErrors.AddRange(itemPreview.Errors.Select(e => $"Row {rowNum}: {e}"));
                }

                group.Items.Add(itemPreview);
            }

            // Check Duplicate Orders against database and final bill rules
            foreach (var group in groupDict.Values)
            {
                if (group.PlatformId.HasValue)
                {
                    bool exists = await _context.PurchaseOrders
                        .AnyAsync(p => p.PlatformId == group.PlatformId.Value && p.ExternalOrderNumber == group.ExternalOrderNumber);

                    if (exists)
                    {
                        group.IsDuplicate = true;
                        group.ValidationErrors.Add($"Duplicate order: '{group.ExternalOrderNumber}' already exists for platform '{group.PlatformName}'.");
                    }
                }

                // Check Discount cap
                if (group.Discount > group.PreDiscountTotal)
                {
                    group.IsValid = false;
                    group.ValidationErrors.Add($"Discount (₹{group.Discount:N2}) exceeds pre-discount subtotal (₹{group.PreDiscountTotal:N2}).");
                }

                // Check Refund cap
                if (group.RefundAmount > group.FinalBillAmount)
                {
                    group.IsValid = false;
                    group.ValidationErrors.Add($"Refund (₹{group.RefundAmount:N2}) exceeds final bill amount (₹{group.FinalBillAmount:N2}).");
                }
            }

            result.Groups = groupDict.Values.ToList();
            return result;
        }

        public async Task<(int importedCount, int rejectedCount)> CommitImportedOrdersAsync(List<ImportOrderGroupPreview> validGroups)
        {
            int imported = 0;
            int rejected = 0;

            foreach (var group in validGroups.Where(g => g.IsValid && !g.IsDuplicate))
            {
                if (!group.PlatformId.HasValue || !group.Items.Any())
                {
                    rejected++;
                    continue;
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var order = new PurchaseOrder
                    {
                        PlatformId = group.PlatformId.Value,
                        ExternalOrderNumber = group.ExternalOrderNumber,
                        OrderDate = group.OrderDate,
                        OrderStatus = group.OrderStatus,
                        PaymentMethod = group.PaymentMethod,
                        DeliveryFee = group.DeliveryFee,
                        PackagingFee = group.PackagingFee,
                        PlatformFee = group.PlatformFee,
                        Taxes = group.Taxes,
                        OtherCharges = group.OtherCharges,
                        Discount = group.Discount,
                        RefundAmount = group.RefundAmount,
                        SourceType = SourceTypes.Imported,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Notes = "Imported from CSV"
                    };

                    foreach (var item in group.Items.Where(i => i.IsValid && i.CategoryId.HasValue))
                    {
                        order.Items.Add(new OrderItem
                        {
                            CategoryId = item.CategoryId!.Value,
                            ItemName = item.ItemName,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            TotalPrice = item.Quantity * item.UnitPrice
                        });
                    }

                    order.RecalculateTotals();

                    _context.PurchaseOrders.Add(order);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    imported++;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    rejected++;
                }
            }

            return (imported, rejected);
        }

        public byte[] ExportOrdersToCsv(IEnumerable<PurchaseOrder> orders)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ExternalOrderNumber,Platform,OrderDate,OrderStatus,PaymentMethod,ItemName,Category,Quantity,UnitPrice,LineSubtotal,DeliveryFee,PackagingFee,PlatformFee,Taxes,OtherCharges,Discount,RefundAmount,FinalBillAmount,NetSpending,SourceType");

            foreach (var order in orders)
            {
                string platName = order.Platform?.Name ?? order.PlatformName;
                string orderNum = order.ExternalOrderNumber ?? $"ORD-{order.Id:D4}";

                if (order.Items != null && order.Items.Any())
                {
                    foreach (var item in order.Items)
                    {
                        string catName = item.Category?.Name ?? item.CategoryName;
                        sb.AppendLine(string.Join(",",
                            EscapeCsv(orderNum),
                            EscapeCsv(platName),
                            order.OrderDate.ToString("yyyy-MM-dd"),
                            EscapeCsv(order.OrderStatus),
                            EscapeCsv(order.PaymentMethod),
                            EscapeCsv(item.ItemName),
                            EscapeCsv(catName),
                            item.Quantity.ToString(),
                            item.UnitPrice.ToString("F2", CultureInfo.InvariantCulture),
                            item.LineSubtotal.ToString("F2", CultureInfo.InvariantCulture),
                            order.DeliveryFee.ToString("F2", CultureInfo.InvariantCulture),
                            order.PackagingFee.ToString("F2", CultureInfo.InvariantCulture),
                            order.PlatformFee.ToString("F2", CultureInfo.InvariantCulture),
                            order.Taxes.ToString("F2", CultureInfo.InvariantCulture),
                            order.OtherCharges.ToString("F2", CultureInfo.InvariantCulture),
                            order.Discount.ToString("F2", CultureInfo.InvariantCulture),
                            order.RefundAmount.ToString("F2", CultureInfo.InvariantCulture),
                            order.FinalBillAmount.ToString("F2", CultureInfo.InvariantCulture),
                            order.NetPurchaseSpending.ToString("F2", CultureInfo.InvariantCulture),
                            EscapeCsv(order.SourceType)
                        ));
                    }
                }
                else
                {
                    sb.AppendLine(string.Join(",",
                        EscapeCsv(orderNum),
                        EscapeCsv(platName),
                        order.OrderDate.ToString("yyyy-MM-dd"),
                        EscapeCsv(order.OrderStatus),
                        EscapeCsv(order.PaymentMethod),
                        "All Items",
                        "Shopping",
                        "1",
                        order.ItemSubtotal.ToString("F2", CultureInfo.InvariantCulture),
                        order.ItemSubtotal.ToString("F2", CultureInfo.InvariantCulture),
                        order.DeliveryFee.ToString("F2", CultureInfo.InvariantCulture),
                        order.PackagingFee.ToString("F2", CultureInfo.InvariantCulture),
                        order.PlatformFee.ToString("F2", CultureInfo.InvariantCulture),
                        order.Taxes.ToString("F2", CultureInfo.InvariantCulture),
                        order.OtherCharges.ToString("F2", CultureInfo.InvariantCulture),
                        order.Discount.ToString("F2", CultureInfo.InvariantCulture),
                        order.RefundAmount.ToString("F2", CultureInfo.InvariantCulture),
                        order.FinalBillAmount.ToString("F2", CultureInfo.InvariantCulture),
                        order.NetPurchaseSpending.ToString("F2", CultureInfo.InvariantCulture),
                        EscapeCsv(order.SourceType)
                    ));
                }
            }

            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        #region Helpers

        /// <summary>
        /// Prevents CSV formula injection by prepending apostrophe if field starts with =, +, -, @
        /// </summary>
        private static string EscapeCsv(string? field)
        {
            if (string.IsNullOrEmpty(field)) return "\"\"";

            string clean = field;
            // Formula injection defense
            if (clean.StartsWith('=') || clean.StartsWith('+') || clean.StartsWith('-') || clean.StartsWith('@'))
            {
                clean = "'" + clean;
            }

            if (clean.Contains('"') || clean.Contains(',') || clean.Contains('\n') || clean.Contains('\r'))
            {
                clean = "\"" + clean.Replace("\"", "\"\"") + "\"";
            }

            return clean;
        }

        private static string[] ParseCsvLine(string line)
        {
            var values = new List<string>();
            var curVal = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        curVal.Append('"');
                        i++; // skip escaped quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(curVal.ToString());
                    curVal.Clear();
                }
                else
                {
                    curVal.Append(c);
                }
            }
            values.Add(curVal.ToString());
            return values.ToArray();
        }

        private static string GetCol(string[] cols, int index)
        {
            return index < cols.Length ? cols[index] : string.Empty;
        }

        private static decimal ParseDecimal(string val, decimal defaultVal)
        {
            if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d))
                return d;
            if (decimal.TryParse(val, out d))
                return d;
            return defaultVal;
        }

        #endregion
    }
}

