using System.Collections.Generic;

namespace PersonalExpenseTracker.Models
{
    public static class OrderStatus
    {
        public const string Completed = "Completed";
        public const string Pending = "Pending";
        public const string Cancelled = "Cancelled";

        public static readonly List<string> All = new()
        {
            Completed,
            Pending,
            Cancelled
        };

        public static bool IsValid(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;
            return All.Contains(status.Trim());
        }
    }
}

