using System;
using System.Collections.Generic;
using System.Linq;
using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.Services
{
    public class BillCalculationService : IBillCalculationService
    {
        public decimal CalculateItemSubtotal(IEnumerable<OrderItem> items)
        {
            if (items == null) return 0m;
            decimal subtotal = items.Sum(i => i.Quantity * i.UnitPrice);
            return Math.Round(subtotal, 2, MidpointRounding.AwayFromZero);
        }

        public decimal CalculateFinalBillAmount(
            decimal itemSubtotal, 
            decimal deliveryFee, 
            decimal packagingFee, 
            decimal platformFee, 
            decimal taxes, 
            decimal otherCharges, 
            decimal discount)
        {
            decimal preDiscountTotal = itemSubtotal + deliveryFee + packagingFee + platformFee + taxes + otherCharges;
            decimal finalBill = preDiscountTotal - discount;
            return Math.Max(0m, Math.Round(finalBill, 2, MidpointRounding.AwayFromZero));
        }

        public decimal CalculateNetPurchaseSpending(decimal finalBillAmount, decimal refundAmount, string orderStatus)
        {
            if (!string.Equals(orderStatus, OrderStatus.Completed, StringComparison.OrdinalIgnoreCase))
            {
                // Cancelled and Pending orders do not contribute to completed net spending
                return 0m;
            }

            decimal net = finalBillAmount - refundAmount;
            return Math.Max(0m, Math.Round(net, 2, MidpointRounding.AwayFromZero));
        }

        public BillCalculationResult CalculateAndValidate(PurchaseOrder order, decimal? originalPlatformBill = null)
        {
            var result = new BillCalculationResult();

            if (order == null)
            {
                result.ValidationErrors.Add("Purchase order record cannot be null.");
                return result;
            }

            // 1. Order Status validation
            if (!OrderStatus.IsValid(order.OrderStatus))
            {
                result.ValidationErrors.Add($"Invalid or unsupported order status '{order.OrderStatus}'. Allowed statuses: {string.Join(", ", OrderStatus.All)}.");
            }

            // 2. Charges and taxes must not be negative
            if (order.DeliveryFee < 0) result.ValidationErrors.Add("Delivery fee cannot be negative.");
            if (order.PackagingFee < 0) result.ValidationErrors.Add("Packaging fee cannot be negative.");
            if (order.PlatformFee < 0) result.ValidationErrors.Add("Platform fee cannot be negative.");
            if (order.Taxes < 0) result.ValidationErrors.Add("Taxes & GST cannot be negative.");
            if (order.OtherCharges < 0) result.ValidationErrors.Add("Other charges cannot be negative.");

            // 3. Items validation: At least one valid item is required
            if (order.Items == null || !order.Items.Any())
            {
                result.ValidationErrors.Add("At least one valid item is required for a purchase order.");
            }
            else
            {
                int itemIndex = 1;
                foreach (var item in order.Items)
                {
                    if (string.IsNullOrWhiteSpace(item.ItemName))
                    {
                        result.ValidationErrors.Add($"Item #{itemIndex}: Product / Item name is required.");
                    }
                    if (item.Quantity < 1 || item.Quantity > 10000)
                    {
                        result.ValidationErrors.Add($"Item '{item.ItemName}': Quantity ({item.Quantity}) must be between 1 and 10,000.");
                    }
                    if (item.UnitPrice < 0m || item.UnitPrice > 10000000m)
                    {
                        result.ValidationErrors.Add($"Item '{item.ItemName}': Unit price (₹{item.UnitPrice:N2}) must be between 0.00 and 10,000,000.00.");
                    }
                    itemIndex++;
                }
            }

            // 4. Calculate Subtotal from valid items
            decimal itemSubtotal = CalculateItemSubtotal(order.Items ?? Enumerable.Empty<OrderItem>());
            decimal preDiscountTotal = itemSubtotal + order.DeliveryFee + order.PackagingFee + order.PlatformFee + order.Taxes + order.OtherCharges;
            preDiscountTotal = Math.Round(preDiscountTotal, 2, MidpointRounding.AwayFromZero);

            // 5. Discount validation: must not be negative or exceed pre-discount total
            if (order.Discount < 0)
            {
                result.ValidationErrors.Add("Discount cannot be negative.");
            }
            else if (order.Discount > preDiscountTotal)
            {
                result.ValidationErrors.Add($"Discount (₹{order.Discount:N2}) cannot exceed the permitted pre-discount total (₹{preDiscountTotal:N2}).");
            }

            decimal finalBillAmount = CalculateFinalBillAmount(
                itemSubtotal, 
                order.DeliveryFee, 
                order.PackagingFee, 
                order.PlatformFee, 
                order.Taxes, 
                order.OtherCharges, 
                order.Discount);

            // 6. Refund validation: must not be negative or exceed final bill
            if (order.RefundAmount < 0)
            {
                result.ValidationErrors.Add("Refund amount cannot be negative.");
            }
            else if (order.RefundAmount > finalBillAmount)
            {
                result.ValidationErrors.Add($"Refund amount (₹{order.RefundAmount:N2}) cannot exceed the final bill amount (₹{finalBillAmount:N2}).");
            }

            decimal netSpending = CalculateNetPurchaseSpending(finalBillAmount, order.RefundAmount, order.OrderStatus);

            // Populate calculation result
            result.ItemSubtotal = itemSubtotal;
            result.PreDiscountTotal = preDiscountTotal;
            result.FinalBillAmount = finalBillAmount;
            result.NetPurchaseSpending = netSpending;

            // 7. Imported historical order comparison check
            if (originalPlatformBill.HasValue)
            {
                decimal original = Math.Round(originalPlatformBill.Value, 2, MidpointRounding.AwayFromZero);
                result.OriginalBillAmount = original;

                if (Math.Abs(finalBillAmount - original) > 0.01m)
                {
                    result.HasDiscrepancy = true;
                    result.DiscrepancyAmount = Math.Abs(finalBillAmount - original);
                    result.DiscrepancyExplanation = $"Platform bill (₹{original:N2}) differs from calculated itemized total (₹{finalBillAmount:N2}) by ₹{result.DiscrepancyAmount:N2}. Please confirm or correct before saving.";
                }
            }

            return result;
        }

        public void RecalculateOrder(PurchaseOrder order)
        {
            if (order == null) return;

            order.ItemSubtotal = CalculateItemSubtotal(order.Items ?? Enumerable.Empty<OrderItem>());
            order.FinalBillAmount = CalculateFinalBillAmount(
                order.ItemSubtotal,
                order.DeliveryFee,
                order.PackagingFee,
                order.PlatformFee,
                order.Taxes,
                order.OtherCharges,
                order.Discount);

            // Ensure refund does not exceed final bill
            if (order.RefundAmount > order.FinalBillAmount)
            {
                order.RefundAmount = order.FinalBillAmount;
            }

            order.UpdatedAt = DateTime.UtcNow;
        }
    }
}

