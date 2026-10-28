using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PersonalExpenseTracker.Models;
using PersonalExpenseTracker.ViewModels;

namespace PersonalExpenseTracker.Services
{
    public interface IPurchaseOrderCsvService
    {
        /// <summary>
        /// Generates the standard CSV template bytes for download.
        /// </summary>
        byte[] GenerateTemplateCsv();

        /// <summary>
        /// Safely parses and validates an uploaded CSV file, grouping item rows into orders.
        /// Identifies duplicates against the database.
        /// </summary>
        Task<ImportPreviewViewModel> ParseAndValidateCsvAsync(IFormFile file);

        /// <summary>
        /// Transactionally commits valid imported order groups into the database.
        /// </summary>
        Task<(int importedCount, int rejectedCount)> CommitImportedOrdersAsync(List<ImportOrderGroupPreview> validGroups);

        /// <summary>
        /// Exports purchase orders with items into CSV format with formula injection protection.
        /// </summary>
        byte[] ExportOrdersToCsv(IEnumerable<PurchaseOrder> orders);
    }
}

