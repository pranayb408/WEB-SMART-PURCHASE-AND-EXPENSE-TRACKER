using System;
using System.Collections.Generic;

namespace PersonalExpenseTracker.Models
{
    public static class SourceTypes
    {
        public const string Manual = "Manual";
        public const string Imported = "Imported";
        public const string Receipt = "Receipt";

        public static readonly List<string> All = new()
        {
            Manual,
            Imported,
            Receipt
        };

        public static bool IsValid(string? source)
        {
            if (string.IsNullOrWhiteSpace(source)) return false;
            return All.Contains(source.Trim(), StringComparer.OrdinalIgnoreCase);
        }
    }
}

