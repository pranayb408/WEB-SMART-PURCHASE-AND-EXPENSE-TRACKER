using System.Collections.Generic;
using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.Services
{
    public interface IBillCalculationService
    {
        /// <summary>
        /// Calculates the sum of line subtotals (Quantity × UnitPrice) across all items.
        /// </summary>
        decimal CalculateItemSubtotal(IEnumerable<OrderItem> items);

        /// <summary>
        /// Calculates FinalBillAmount = ItemSubtotal + DeliveryFee + PackagingFee + PlatformFee + Taxes + OtherCharges − Discount.
        /// </summary>
        decimal CalculateFinalBillAmount(
            decimal itemSubtotal, 
            decimal deliveryFee, 
            decimal packagingFee, 
            decimal platformFee, 
            decimal taxes, 
            decimal otherCharges, 
            decimal discount);

        /// <summary>
        /// Calculates NetPurchaseSpending = FinalBillAmount − RefundAmount for Completed orders.
        /// Cancelled and Pending orders contribute 0.00 to completed net spending.
        /// </summary>
        decimal CalculateNetPurchaseSpending(decimal finalBillAmount, decimal refundAmount, string orderStatus);

        /// <summary>
        /// Validates charges, taxes, discounts, refunds, items, quantities, statuses, and compares with original platform bills.
        /// </summary>
        BillCalculationResult CalculateAndValidate(PurchaseOrder order, decimal? originalPlatformBill = null);

        /// <summary>
        /// Recalculates and updates all monetary totals on the order entity in memory.
        /// </summary>
        void RecalculateOrder(PurchaseOrder order);
    }
}

