using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Data;
using PersonalExpenseTracker.Models;
using PersonalExpenseTracker.Services;
using PersonalExpenseTracker.ViewModels;

namespace PersonalExpenseTracker.Controllers
{
    public class PurchaseOrdersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IBillCalculationService _billService;
        private readonly IPurchaseOrderCsvService _csvService;
        private readonly ILogger<PurchaseOrdersController> _logger;

        public PurchaseOrdersController(
            AppDbContext context,
            IBillCalculationService billService,
            IPurchaseOrderCsvService csvService,
            ILogger<PurchaseOrdersController> logger)
        {
            _context = context;
            _billService = billService;
            _csvService = csvService;
            _logger = logger;
        }

        // GET: /PurchaseOrders
        public async Task<IActionResult> Index(
            int? year, 
            int? month, 
            int? platformId, 
            int? categoryId, 
            string? status, 
            string? searchTerm)
        {
            // Build query with SQL-level filtering
            var query = _context.PurchaseOrders
                .AsNoTracking()
                .Include(p => p.Platform)
                .Include(p => p.Items)
                    .ThenInclude(i => i.Category)
                .AsQueryable();

            if (year.HasValue && year.Value >= 2000 && year.Value <= 2100)
            {
                query = query.Where(p => p.OrderDate.Year == year.Value);
            }

            if (month.HasValue && month.Value >= 1 && month.Value <= 12)
            {
                query = query.Where(p => p.OrderDate.Month == month.Value);
            }

            if (platformId.HasValue && platformId.Value > 0)
            {
                query = query.Where(p => p.PlatformId == platformId.Value);
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.Items.Any(i => i.CategoryId == categoryId.Value));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(p => p.OrderStatus == status);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string search = searchTerm.Trim();
                query = query.Where(p => 
                    (p.ExternalOrderNumber != null && p.ExternalOrderNumber.Contains(search)) ||
                    (p.Notes != null && p.Notes.Contains(search)) ||
                    p.Items.Any(i => i.ItemName.Contains(search)));
            }

            var orders = await query
                .OrderByDescending(p => p.OrderDate)
                .ThenByDescending(p => p.Id)
                .ToListAsync();

            // Calculate overall metrics
            int totalCount = orders.Count;
            int completedCount = orders.Count(o => o.OrderStatus == OrderStatus.Completed);
            int pendingCount = orders.Count(o => o.OrderStatus == OrderStatus.Pending);
            int cancelledCount = orders.Count(o => o.OrderStatus == OrderStatus.Cancelled);

            decimal totalFinalBills = orders
                .Where(o => o.OrderStatus == OrderStatus.Completed)
                .Sum(o => o.FinalBillAmount);

            decimal totalRefunds = orders
                .Where(o => o.OrderStatus == OrderStatus.Completed)
                .Sum(o => o.RefundAmount);

            decimal netSpending = orders
                .Where(o => o.OrderStatus == OrderStatus.Completed)
                .Sum(o => o.NetPurchaseSpending);

            var viewModel = new PurchaseOrderFilterViewModel
            {
                SelectedYear = year,
                SelectedMonth = month,
                SelectedPlatformId = platformId,
                SelectedCategoryId = categoryId,
                SelectedStatus = status,
                SearchTerm = searchTerm,
                Orders = orders,
                TotalOrdersCount = totalCount,
                CompletedOrdersCount = completedCount,
                PendingOrdersCount = pendingCount,
                CancelledOrdersCount = cancelledCount,
                TotalFinalBills = totalFinalBills,
                TotalRefunds = totalRefunds,
                NetSpending = netSpending,
                PlatformOptions = await GetPlatformFilterOptionsAsync(platformId),
                CategoryOptions = await GetCategoryFilterOptionsAsync(categoryId),
                StatusOptions = GetStatusFilterOptions(status),
                MonthOptions = GetMonthFilterOptions(month),
                YearOptions = GetYearFilterOptions(year)
            };

            return View(viewModel);
        }

        // GET: /PurchaseOrders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.PurchaseOrders
                .AsNoTracking()
                .Include(p => p.Platform)
                .Include(p => p.Items)
                    .ThenInclude(i => i.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // GET: /PurchaseOrders/Create
        public async Task<IActionResult> Create()
        {
            var model = new PurchaseOrderFormViewModel
            {
                OrderDate = DateTime.Today,
                OrderStatus = OrderStatus.Completed,
                PaymentMethod = "UPI (GPay / PhonePe / Paytm)",
                SourceType = SourceTypes.Manual,
                Items = new List<PurchaseOrderItemInputModel>
                {
                    // Default initial row
                    new PurchaseOrderItemInputModel { Quantity = 1, UnitPrice = 0.00m }
                }
            };

            await PopulateFormOptionsAsync(model);
            return View(model);
        }

        // POST: /PurchaseOrders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseOrderFormViewModel model)
        {
            // Filter out empty rows if user added and left blank
            if (model.Items != null)
            {
                model.Items = model.Items
                    .Where(i => !string.IsNullOrWhiteSpace(i.ItemName) || i.UnitPrice > 0)
                    .ToList();
            }

            if (model.Items == null || !model.Items.Any())
            {
                ModelState.AddModelError("", "At least one item with a valid name is required.");
            }

            if (ModelState.IsValid)
            {
                // Map to domain entity
                var order = new PurchaseOrder
                {
                    PlatformId = model.PlatformId,
                    ExternalOrderNumber = string.IsNullOrWhiteSpace(model.ExternalOrderNumber) ? null : model.ExternalOrderNumber.Trim(),
                    OrderDate = model.OrderDate,
                    OrderStatus = model.OrderStatus,
                    PaymentMethod = model.PaymentMethod,
                    DeliveryFee = model.DeliveryFee,
                    PackagingFee = model.PackagingFee,
                    PlatformFee = model.PlatformFee,
                    Taxes = model.Taxes,
                    OtherCharges = model.OtherCharges,
                    Discount = model.Discount,
                    RefundAmount = model.RefundAmount,
                    Notes = model.Notes,
                    SourceType = model.SourceType,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Items = model.Items != null 
                        ? model.Items.Select(i => new OrderItem
                        {
                            ItemName = i.ItemName.Trim(),
                            CategoryId = i.CategoryId,
                            Quantity = i.Quantity,
                            UnitPrice = i.UnitPrice
                        }).ToList()
                        : new List<OrderItem>()
                };

                // Validate order business rules using IBillCalculationService
                var calcResult = _billService.CalculateAndValidate(order);
                if (!calcResult.IsValid)
                {
                    foreach (var error in calcResult.ValidationErrors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }
                else
                {
                    // Check uniqueness constraint for PlatformId + ExternalOrderNumber
                    if (!string.IsNullOrWhiteSpace(order.ExternalOrderNumber))
                    {
                        bool exists = await _context.PurchaseOrders.AnyAsync(p => 
                            p.PlatformId == order.PlatformId && 
                            p.ExternalOrderNumber == order.ExternalOrderNumber);

                        if (exists)
                        {
                            ModelState.AddModelError("ExternalOrderNumber", 
                                $"An order with number '{order.ExternalOrderNumber}' already exists for this platform.");
                        }
                    }

                    if (ModelState.IsValid)
                    {
                        // Ensure all monetary fields are updated correctly
                        _billService.RecalculateOrder(order);

                        // Transactional persistence
                        await using var transaction = await _context.Database.BeginTransactionAsync();
                        try
                        {
                            _context.PurchaseOrders.Add(order);
                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();

                            TempData["SuccessMessage"] = $"Purchase Order #{order.Id} ({(string.IsNullOrEmpty(order.ExternalOrderNumber) ? "Direct" : order.ExternalOrderNumber)}) saved successfully!";
                            return RedirectToAction(nameof(Index));
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            _logger.LogError(ex, "Failed to save purchase order.");
                            ModelState.AddModelError("", "Database error saving order. Please verify input data and try again.");
                        }
                    }
                }
            }

            await PopulateFormOptionsAsync(model);
            return View(model);
        }

        // GET: /PurchaseOrders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.PurchaseOrders
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (order == null) return NotFound();

            var model = new PurchaseOrderFormViewModel
            {
                Id = order.Id,
                PlatformId = order.PlatformId,
                ExternalOrderNumber = order.ExternalOrderNumber,
                OrderDate = order.OrderDate,
                OrderStatus = order.OrderStatus,
                PaymentMethod = order.PaymentMethod,
                DeliveryFee = order.DeliveryFee,
                PackagingFee = order.PackagingFee,
                PlatformFee = order.PlatformFee,
                Taxes = order.Taxes,
                OtherCharges = order.OtherCharges,
                Discount = order.Discount,
                RefundAmount = order.RefundAmount,
                Notes = order.Notes,
                SourceType = order.SourceType,
                ItemSubtotal = order.ItemSubtotal,
                FinalBillAmount = order.FinalBillAmount,
                NetPurchaseSpending = order.NetPurchaseSpending,
                Items = order.Items.Select(i => new PurchaseOrderItemInputModel
                {
                    Id = i.Id,
                    ItemName = i.ItemName,
                    CategoryId = i.CategoryId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            await PopulateFormOptionsAsync(model);
            return View(model);
        }

        // POST: /PurchaseOrders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PurchaseOrderFormViewModel model)
        {
            if (id != model.Id) return BadRequest();

            // Filter out blank rows
            if (model.Items != null)
            {
                model.Items = model.Items
                    .Where(i => !string.IsNullOrWhiteSpace(i.ItemName) || i.UnitPrice > 0)
                    .ToList();
            }

            if (model.Items == null || !model.Items.Any())
            {
                ModelState.AddModelError("", "At least one item with a valid name is required.");
            }

            if (ModelState.IsValid)
            {
                var existingOrder = await _context.PurchaseOrders
                    .Include(p => p.Items)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (existingOrder == null) return NotFound();

                // Check uniqueness constraint on PlatformId + ExternalOrderNumber (excluding self)
                string? cleanExtNum = string.IsNullOrWhiteSpace(model.ExternalOrderNumber) ? null : model.ExternalOrderNumber.Trim();
                if (!string.IsNullOrWhiteSpace(cleanExtNum))
                {
                    bool exists = await _context.PurchaseOrders.AnyAsync(p => 
                        p.Id != id &&
                        p.PlatformId == model.PlatformId && 
                        p.ExternalOrderNumber == cleanExtNum);

                    if (exists)
                    {
                        ModelState.AddModelError("ExternalOrderNumber", 
                            $"An order with number '{cleanExtNum}' already exists for this platform.");
                    }
                }

                if (ModelState.IsValid)
                {
                    // Update header fields
                    existingOrder.PlatformId = model.PlatformId;
                    existingOrder.ExternalOrderNumber = cleanExtNum;
                    existingOrder.OrderDate = model.OrderDate;
                    existingOrder.OrderStatus = model.OrderStatus;
                    existingOrder.PaymentMethod = model.PaymentMethod;
                    existingOrder.DeliveryFee = model.DeliveryFee;
                    existingOrder.PackagingFee = model.PackagingFee;
                    existingOrder.PlatformFee = model.PlatformFee;
                    existingOrder.Taxes = model.Taxes;
                    existingOrder.OtherCharges = model.OtherCharges;
                    existingOrder.Discount = model.Discount;
                    existingOrder.RefundAmount = model.RefundAmount;
                    existingOrder.Notes = model.Notes;
                    existingOrder.SourceType = model.SourceType;

                    // Child item reconciliation:
                    // 1. Remove items that were deleted in the UI
                    var submittedItemIds = (model.Items ?? new List<PurchaseOrderItemInputModel>())
                        .Where(i => i.Id > 0)
                        .Select(i => i.Id)
                        .ToHashSet();
                    var itemsToRemove = existingOrder.Items.Where(i => !submittedItemIds.Contains(i.Id)).ToList();
                    foreach (var itemToRemove in itemsToRemove)
                    {
                        _context.OrderItems.Remove(itemToRemove);
                    }

                    // 2. Update existing items
                    foreach (var submittedItem in (model.Items ?? new List<PurchaseOrderItemInputModel>()).Where(i => i.Id > 0))
                    {
                        var target = existingOrder.Items.FirstOrDefault(i => i.Id == submittedItem.Id);
                        if (target != null)
                        {
                            target.ItemName = submittedItem.ItemName.Trim();
                            target.CategoryId = submittedItem.CategoryId;
                            target.Quantity = submittedItem.Quantity;
                            target.UnitPrice = submittedItem.UnitPrice;
                        }
                    }

                    // 3. Add newly added items
                    foreach (var newItem in (model.Items ?? new List<PurchaseOrderItemInputModel>()).Where(i => i.Id == 0))
                    {
                        existingOrder.Items.Add(new OrderItem
                        {
                            ItemName = newItem.ItemName.Trim(),
                            CategoryId = newItem.CategoryId,
                            Quantity = newItem.Quantity,
                            UnitPrice = newItem.UnitPrice
                        });
                    }

                    // Validate recalculated order
                    var calcResult = _billService.CalculateAndValidate(existingOrder);
                    if (!calcResult.IsValid)
                    {
                        foreach (var err in calcResult.ValidationErrors)
                        {
                            ModelState.AddModelError("", err);
                        }
                    }
                    else
                    {
                        _billService.RecalculateOrder(existingOrder);

                        await using var transaction = await _context.Database.BeginTransactionAsync();
                        try
                        {
                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();

                            TempData["SuccessMessage"] = $"Purchase Order #{existingOrder.Id} updated successfully!";
                            return RedirectToAction(nameof(Index));
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            _logger.LogError(ex, "Failed to update purchase order #{OrderId}", id);
                            ModelState.AddModelError("", "Database error updating order. Please try again.");
                        }
                    }
                }
            }

            await PopulateFormOptionsAsync(model);
            return View(model);
        }

        // GET: /PurchaseOrders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.PurchaseOrders
                .AsNoTracking()
                .Include(p => p.Platform)
                .Include(p => p.Items)
                    .ThenInclude(i => i.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // POST: /PurchaseOrders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.PurchaseOrders
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (order == null) return NotFound();

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.PurchaseOrders.Remove(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Purchase Order #{id} deleted successfully.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to delete purchase order #{OrderId}", id);
                TempData["ErrorMessage"] = "Could not delete order due to an unexpected database error.";
            }

            return RedirectToAction(nameof(Index));
        }

        #region Helper Methods for Dropdowns & Options

        private async Task PopulateFormOptionsAsync(PurchaseOrderFormViewModel model)
        {
            var platforms = await _context.Platforms
                .AsNoTracking()
                .OrderBy(p => p.PlatformType)
                .ThenBy(p => p.Name)
                .ToListAsync();

            model.PlatformOptions = platforms.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.IsActive ? $"{p.Name} ({p.PlatformType})" : $"{p.Name} [Inactive]",
                Selected = p.Id == model.PlatformId
            }).ToList();

            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            model.CategoryOptions = categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.IsActive ? c.Name : $"{c.Name} [Inactive]"
            }).ToList();

            model.StatusOptions = OrderStatus.All.Select(s => new SelectListItem
            {
                Value = s,
                Text = s,
                Selected = s == model.OrderStatus
            }).ToList();

            model.PaymentMethodOptions = PaymentMethods.All.Select(m => new SelectListItem
            {
                Value = m,
                Text = m,
                Selected = m == model.PaymentMethod
            }).ToList();

            model.SourceTypeOptions = SourceTypes.All.Select(st => new SelectListItem
            {
                Value = st,
                Text = st,
                Selected = st == model.SourceType
            }).ToList();
        }

        #region CSV Import & Export (Section 10)

        // GET: /PurchaseOrders/DownloadTemplate
        public IActionResult DownloadTemplate()
        {
            byte[] fileBytes = _csvService.GenerateTemplateCsv();
            return File(fileBytes, "text/csv", "purchase_orders_template.csv");
        }

        // GET: /PurchaseOrders/ExportCsv
        public async Task<IActionResult> ExportCsv(
            int? year, 
            int? month, 
            int? platformId, 
            int? categoryId, 
            string? status, 
            string? searchTerm)
        {
            var query = _context.PurchaseOrders
                .AsNoTracking()
                .Include(p => p.Platform)
                .Include(p => p.Items)
                    .ThenInclude(i => i.Category)
                .AsQueryable();

            if (year.HasValue && year.Value >= 2000 && year.Value <= 2100)
            {
                query = query.Where(p => p.OrderDate.Year == year.Value);
            }

            if (month.HasValue && month.Value >= 1 && month.Value <= 12)
            {
                query = query.Where(p => p.OrderDate.Month == month.Value);
            }

            if (platformId.HasValue && platformId.Value > 0)
            {
                query = query.Where(p => p.PlatformId == platformId.Value);
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.Items.Any(i => i.CategoryId == categoryId.Value));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(p => p.OrderStatus == status);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string term = searchTerm.Trim().ToLower();
                query = query.Where(p => 
                    (p.ExternalOrderNumber != null && p.ExternalOrderNumber.ToLower().Contains(term)) ||
                    (p.Notes != null && p.Notes.ToLower().Contains(term)) ||
                    p.Items.Any(i => i.ItemName.ToLower().Contains(term)));
            }

            var orders = await query.OrderByDescending(p => p.OrderDate).ThenByDescending(p => p.Id).ToListAsync();
            byte[] csvBytes = _csvService.ExportOrdersToCsv(orders);

            string fileName = $"purchase_orders_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(csvBytes, "text/csv", fileName);
        }

        // GET: /PurchaseOrders/Import
        public IActionResult Import()
        {
            return View();
        }

        // POST: /PurchaseOrders/PreviewImport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PreviewImport(IFormFile? csvFile)
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                ModelState.AddModelError("", "Please select a valid CSV file to upload.");
                return View("Import");
            }

            var preview = await _csvService.ParseAndValidateCsvAsync(csvFile);

            if (preview.FileErrors.Any())
            {
                foreach (var err in preview.FileErrors)
                {
                    ModelState.AddModelError("", err);
                }
                return View("Import");
            }

            // Save previewed groups in session for confirmation
            var options = new System.Text.Json.JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles };
            HttpContext.Session.SetString("CsvImportPreviewJson", System.Text.Json.JsonSerializer.Serialize(preview.Groups, options));

            return View("ImportPreview", preview);
        }

        // POST: /PurchaseOrders/CommitImport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CommitImport()
        {
            string? json = HttpContext.Session.GetString("CsvImportPreviewJson");
            if (string.IsNullOrEmpty(json))
            {
                TempData["ErrorMessage"] = "No pending CSV import session found. Please re-upload your file.";
                return RedirectToAction(nameof(Import));
            }

            var groups = System.Text.Json.JsonSerializer.Deserialize<List<ImportOrderGroupPreview>>(json);
            if (groups == null || !groups.Any())
            {
                TempData["ErrorMessage"] = "No valid records to import.";
                return RedirectToAction(nameof(Import));
            }

            var (importedCount, rejectedCount) = await _csvService.CommitImportedOrdersAsync(groups);

            HttpContext.Session.Remove("CsvImportPreviewJson");

            TempData["SuccessMessage"] = $"CSV Import complete: {importedCount} order(s) successfully recorded. ({rejectedCount} skipped/duplicates).";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        private async Task<List<SelectListItem>> GetPlatformFilterOptionsAsync(int? selectedPlatformId)
        {
            var list = new List<SelectListItem> { new() { Value = "", Text = "All Platforms" } };
            var platforms = await _context.Platforms.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
            list.AddRange(platforms.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.Name,
                Selected = p.Id == selectedPlatformId
            }));
            return list;
        }

        private async Task<List<SelectListItem>> GetCategoryFilterOptionsAsync(int? selectedCategoryId)
        {
            var list = new List<SelectListItem> { new() { Value = "", Text = "All Categories" } };
            var categories = await _context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            list.AddRange(categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name,
                Selected = c.Id == selectedCategoryId
            }));
            return list;
        }

        private static List<SelectListItem> GetStatusFilterOptions(string? selectedStatus)
        {
            var list = new List<SelectListItem> { new() { Value = "", Text = "All Statuses" } };
            list.AddRange(OrderStatus.All.Select(s => new SelectListItem
            {
                Value = s,
                Text = s,
                Selected = s == selectedStatus
            }));
            return list;
        }

        private static List<SelectListItem> GetMonthFilterOptions(int? selectedMonth)
        {
            var list = new List<SelectListItem> { new() { Value = "", Text = "All Months" } };
            for (int m = 1; m <= 12; m++)
            {
                list.Add(new SelectListItem
                {
                    Value = m.ToString(),
                    Text = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m),
                    Selected = m == selectedMonth
                });
            }
            return list;
        }

        private static List<SelectListItem> GetYearFilterOptions(int? selectedYear)
        {
            var list = new List<SelectListItem> { new() { Value = "", Text = "All Years" } };
            int curYear = DateTime.Today.Year;
            for (int y = curYear - 4; y <= curYear + 4; y++)
            {
                list.Add(new SelectListItem
                {
                    Value = y.ToString(),
                    Text = y.ToString(),
                    Selected = y == selectedYear
                });
            }
            return list;
        }

        #endregion
    }
}
