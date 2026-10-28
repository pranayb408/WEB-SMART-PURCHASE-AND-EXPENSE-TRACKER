using System.Collections.Generic;

namespace PersonalExpenseTracker.Models
{
    public static class PurchasePlatforms
    {
        public const string CategoryFoodDelivery = "Food Delivery";
        public const string CategoryGrocery = "Grocery & Quick Commerce";
        public const string CategoryShopping = "Online Shopping";
        public const string CategoryDirect = "Direct / Offline & Other";

        public static readonly Dictionary<string, List<string>> GroupedPlatforms = new()
        {
            [CategoryFoodDelivery] = new List<string>
            {
                "Swiggy",
                "Zomato",
                "EatSure",
                "Uber Eats"
            },
            [CategoryGrocery] = new List<string>
            {
                "Blinkit",
                "Zepto",
                "Instamart",
                "BigBasket",
                "DMart Ready"
            },
            [CategoryShopping] = new List<string>
            {
                "Amazon",
                "Flipkart",
                "Myntra",
                "Meesho",
                "Nykaa",
                "Ajio",
                "Tata CLiQ"
            },
            [CategoryDirect] = new List<string>
            {
                "Direct / In-Store",
                "Local Market / Kirana",
                "Other Platform"
            }
        };

        public static List<string> GetAll()
        {
            var all = new List<string>();
            foreach (var group in GroupedPlatforms.Values)
            {
                all.AddRange(group);
            }
            return all;
        }

        public static string GetBadgeStyle(string? platform)
        {
            return platform?.Trim() switch
            {
                "Swiggy" => "background-color: #fc8019; color: #fff;",
                "Zomato" => "background-color: #cb202d; color: #fff;",
                "Blinkit" => "background-color: #f8cb46; color: #222;",
                "Zepto" => "background-color: #57007b; color: #fff;",
                "Instamart" => "background-color: #ff5200; color: #fff;",
                "BigBasket" => "background-color: #84c225; color: #fff;",
                "DMart Ready" => "background-color: #0b723b; color: #fff;",
                "Amazon" => "background-color: #232f3e; color: #ff9900;",
                "Flipkart" => "background-color: #2874f0; color: #fff;",
                "Myntra" => "background-color: #ff3f6c; color: #fff;",
                "Meesho" => "background-color: #9b27b0; color: #fff;",
                "Nykaa" => "background-color: #fc2779; color: #fff;",
                "Ajio" => "background-color: #2c4152; color: #fff;",
                "Direct / In-Store" => "background-color: #64748b; color: #fff;",
                _ => "background-color: #475569; color: #fff;"
            };
        }

        public static string GetIcon(string? platform)
        {
            return platform?.Trim() switch
            {
                "Swiggy" or "Zomato" or "EatSure" or "Uber Eats" => "bi-cup-hot-fill",
                "Blinkit" or "Zepto" or "Instamart" or "BigBasket" or "DMart Ready" => "bi-basket2-fill",
                "Amazon" or "Flipkart" or "Myntra" or "Meesho" or "Nykaa" or "Ajio" or "Tata CLiQ" => "bi-bag-check-fill",
                "Direct / In-Store" or "Local Market / Kirana" => "bi-shop",
                _ => "bi-tag-fill"
            };
        }
    }
}

