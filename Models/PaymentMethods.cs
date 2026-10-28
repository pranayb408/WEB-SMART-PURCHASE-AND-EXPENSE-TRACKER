using System.Collections.Generic;

namespace PersonalExpenseTracker.Models
{
    public static class PaymentMethods
    {
        public static readonly List<string> All = new()
        {
            "UPI (GPay / PhonePe / Paytm)",
            "Credit Card",
            "Debit Card",
            "Net Banking",
            "Cash on Delivery (COD)",
            "Wallet (Amazon Pay / Paytm)",
            "Cash"
        };
    }
}

