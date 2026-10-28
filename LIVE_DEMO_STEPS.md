# 🚀 Smart Purchase & Expense Tracker — Live Demonstration Guide

A complete step-by-step walkthrough for evaluators, examiners, and developers to run, inspect, and test every live page and feature in the application.

---

## ⚡ Quick Launch Instructions

### 1. Build and Run
Open your terminal in the project directory (`c:\Users\Lenovo\OneDrive\Documents\WEB_NEW`):

```powershell
# Build application (verifies 0 warnings, 0 errors)
dotnet build PersonalExpenseTracker.csproj

# Run automated tests (verifies 70/70 passing tests)
dotnet test tests/PersonalExpenseTracker.Tests/PersonalExpenseTracker.Tests.csproj

# Start the web server
dotnet run --launch-profile http
```

### 2. Open in Browser
Open your browser and navigate to:  
👉 **`http://localhost:5197`**

> **Note on Database Storage:**  
> The application uses a **Resilient Dual-Engine Bootloader** ([`Program.cs`](file:///C:/Users/Lenovo/OneDrive/Documents/WEB_NEW/Program.cs)). If MySQL (`expense_tracker_db` on port 3306) is active in XAMPP, it connects to MySQL. If MySQL is offline, it seamlessly activates local SQLite (`expense_tracker.db`) with automatic table creation and rich sample data seeding. The active storage engine is displayed in the footer.

---

## 🧭 Step-by-Step Live Page Tour

---

### Step 1: Central Financial Dashboard
* **URL:** `http://localhost:5197/Dashboard/Index` (or click **Dashboard** in navbar)
* **What to Observe:**
  * **Summary KPI Cards**:
    * **Net Balance**: ₹37,561.00 (Income − Total Expenses)
    * **Standalone Income**: +₹150,000.00 (Eligible direct income)
    * **Monthly Spending**: ₹112,439.00 (Direct expenses + net purchase spending)
    * **Yearly Spending (2026)**: ₹112,439.00 (Annual cumulative spending)
    * **Net Purchases**: ₹1,520.00 (Completed order bills minus refunds)
    * **Standalone Expenses**: ₹110,919.00 (Direct non-order expenses)
    * **Refunds Credited**: ₹100.00 (Excluded from net spending)
    * **Pending Orders**: ₹1,574.00 (In-transit orders)
  * **Monthly Budget Cap Indicator**:
    * Dynamic progress bar showing consumption against the monthly limit with over-budget warning badge.
  * **Interactive Chart.js Visualizations**:
    * **Expenses by Category Doughnut Chart**: Proportional multi-color distribution across categories.
    * **6-Month Trend Overview Bar Chart**: Green (Income) vs Red (Realized Expenses) from May 2026 to Oct 2026.
  * **Recent Orders & Transactions Table**: Displays recent orders from Swiggy, Blinkit, and Amazon with direct action links.
  * **Filter Bar**: Select different months, platforms, categories, or order statuses and click **Apply**.

---

### Step 2: Standalone Transactions
* **URL:** `http://localhost:5197/Transactions` (or click **Transactions** in navbar)
* **What to Observe:**
  * Displays direct earnings and non-order living expenses (Salary, Rent, Utilities, Broadband, Travel pass).
  * Filter bar supporting:
    * **Year & Month**
    * **Type**: `All Types`, `Income Only`, `Expense Only`
    * **Category & Platform**
    * **Search Term**: Instant full-text search matching titles and notes.
  * Table rows with color-coded badges (`+₹` in green, `-₹` in red) and payment method chips.
  * Direct **Edit** and **Delete** buttons for each transaction.

---

### Step 3: Add Standalone Transaction
* **URL:** `http://localhost:5197/Transactions/Create` (or click **+ Add Transaction** in navbar)
* **What to Test:**
  1. **Radio Toggle**: Click between **Income** and **Expense** cards to see form presets dynamically adapt.
  2. **Try Submitting Empty**: Click **Save Transaction** with empty fields to observe server-side and client-side validation errors in red.
  3. **Add a Valid Expense**:
     * Title: `Weekend Coffee & Snacks`
     * Type: `Expense`
     * Category: `Food`
     * Amount: `350`
     * Date: Today's date
     * Platform: Click quick preset `Direct / In-Store`
     * Payment Method: `UPI (GPay / PhonePe / Paytm)`
     * Click **Save Transaction**.
  4. Observe the green flash alert (`TempData`) and the newly inserted transaction on the list.

---

### Step 4: Purchase Orders Subsystem
* **URL:** `http://localhost:5197/PurchaseOrders` (or click **Orders** in navbar)
* **What to Observe:**
  * Centralized quick-commerce and e-commerce orders:
    * `SWG-849201` (Swiggy) — ₹646.00 (Completed)
    * `BLK-932148` (Blinkit) — ₹564.00 (Completed)
    * `AMZ-402-918237` (Amazon) — ₹2,080.00 (Completed)
    * `ZPT-551029` (Zepto) — ₹322.00 (Completed)
    * `MYN-778219` (Myntra) — ₹1,846.00 (Completed)
    * `ZOM-332190` (Zomato) — ₹638.00 (Completed, ₹100 refund)
    * `FK-9018274` (Flipkart) — ₹1,574.00 (Pending)
  * Multi-dimensional filtering by Platform, Category, Status, Month, and Year.
  * Summary metric strip at the top showing filtered totals.

---

### Step 5: Add Itemized Purchase Order (Dynamic JS & Bill Preview)
* **URL:** `http://localhost:5197/PurchaseOrders/Create` (or click **+ Add Order** in navbar)
* **What to Test:**
  1. Select **Platform**: `Blinkit`.
  2. Order Number: `BLK-TEST-101`.
  3. Date: Today. Status: `Completed`. Payment: `UPI`.
  4. **Multi-Row Item Management**:
     * Line 1: Item Name: `Organic Milk 1L`, Category: `Groceries`, Qty: `2`, Unit Price: `55` (Subtotal automatically becomes `110.00`).
     * Click **+ Add Another Item**:
     * Line 2: Item Name: `Dark Chocolate Bar`, Category: `Food`, Qty: `1`, Unit Price: `90`.
     * Click **+ Add Another Item**:
     * Line 3: Item Name: `Dishwashing Liquid`, Category: `Household`, Qty: `1`, Unit Price: `120`.
  5. **Dynamic Deletion & Reindexing**:
     * Delete Line 2. Notice that the JavaScript cleanly reindexes the DOM inputs (`Items[0]`, `Items[1]`) so model binding never misses a row.
  6. **Live Bill Preview Box**:
     * Enter Delivery Fee: `15`, Platform Fee: `5`, Taxes: `18`, Discount: `25`.
     * Observe the right-hand **Live Bill Summary** card recalculating in real-time before form submission.
  7. Click **Save Purchase Order**. It validates all line items on the server and redirects to the order list.

---

### Step 6: Purchase Order Details & Proportional Category Allocation
* **URL:** `http://localhost:5197/PurchaseOrders/Details/1` (or click **Details** on any order)
* **What to Observe:**
  * Comprehensive order header (Platform, External Receipt ID, Status badge, Payment method).
  * Line items breakdown table with quantities and unit prices.
  * Fee summary strip (Delivery, Packaging, Platform, Taxes, Discount, Final Bill, Refund).
  * **Proportional Category Allocation Table**:
    * Demonstrates the mathematical distribution of order charges and discounts across the order's item categories.
    * Highlights **deterministic zero-drift rounding**: the sum of category allocations reconciles to the exact cent with the final net bill.

---

### Step 7: Edit Purchase Order
* **URL:** `http://localhost:5197/PurchaseOrders/Edit/1` (or click **Edit** on any order)
* **What to Test:**
  * Pre-populated line items and charges.
  * Change status from `Completed` to `Cancelled` or add a refund amount.
  * Click **Save Changes** and verify the dashboard and reports update accordingly.

---

### Step 8: Delete Purchase Order
* **URL:** `http://localhost:5197/PurchaseOrders/Delete/1` (or click **Delete** on an order)
* **What to Observe:**
  * Displays a full preview of the order and associated line items that will be removed.
  * Protected POST confirmation with Anti-Forgery validation.

---

### Step 9: Monthly Budgets
* **URL:** `http://localhost:5197/Budgets` (or click **Budgets &rarr; Monthly Budgets** in navbar)
* **What to Observe:**
  * Lists configured monthly spending limits (e.g. October 2026: ₹45,000.00, September: ₹42,000.00, November: ₹40,000.00).
  * Visual progress bars transitioning from **Green (<80%)** &rarr; **Amber (80-100%)** &rarr; **Red (>100% Danger)**.
  * Test duplicate prevention: Navigate to `/Budgets/Create` and try adding another budget for October 2026. The system rejects duplicates with a clear validation message.

---

### Step 10: Category Budgets
* **URL:** `http://localhost:5197/CategoryBudgets` (or click **Budgets &rarr; Category Budgets** in navbar)
* **What to Observe:**
  * Dedicated breakdown of budgets per category for the selected month (Food, Groceries, Electronics, Clothing, Household, Health, Subscriptions, Travel).
  * Displays: **Category Name**, **Allocated Budget**, **Realized Spending**, **Remaining Allowance**, and a color-coded **Progress Bar**.

---

### Step 11: Platforms Management
* **URL:** `http://localhost:5197/Platforms` (or click **Metadata &rarr; Platforms** in navbar)
* **What to Observe:**
  * Registry of pre-seeded delivery and shopping apps: Swiggy, Zomato, Blinkit, Zepto, BigBasket, Amazon, Flipkart, Myntra.
  * **Active Status Toggle**: Easily deactivate a platform without deleting historical records.
  * **Safe Deletion Protection**: Try deleting `Swiggy` or `Amazon`. The application blocks hard deletion because existing orders reference it, preserving database integrity.

---

### Step 12: Categories Management
* **URL:** `http://localhost:5197/Categories` (or click **Metadata &rarr; Categories** in navbar)
* **What to Observe:**
  * Pre-seeded categories: Food, Groceries, Electronics, Clothing, Household, Health, Education, Travel, Subscriptions, Other.
  * Same foreign-key reference protection preventing deletion of categories linked to orders or transactions.

---

### Step 13: Financial Reports & Analytics
* **URL:** `http://localhost:5197/Reports` (or click **Reports** in navbar)
* **What to Observe:**
  * **Monthly Mode**:
    * Platform-wise breakdown (Spending share %, order count, top category per app).
    * Category-wise breakdown with percentage of total spending.
    * Interactive Chart.js charts.
    * Highlights strip: Biggest purchase, top spending platform.
  * **Yearly Mode**:
    * Full 12-month summary table (Jan – Dec) with monthly budgets, net savings, and peak spend month highlights.
    * 12-month Income vs Expense comparative bar chart.
  * **Export / Print**:
    * Click **Print Statement** for a print-optimized, clean report view.

---

### Step 14: CSV Bulk Import
* **URL:** `http://localhost:5197/PurchaseOrders/Import` (or click **Import / Export &rarr; Import CSV** in navbar)
* **What to Test:**
  1. Click **Download Sample CSV Template** to download `purchase_orders_template.csv`.
  2. Inspect the template: multi-item orders are grouped by `(Platform, ExternalOrderNumber)` with identical header fees.
  3. Try uploading a non-CSV file or a file > 5 MB to verify security guards.
  4. Upload a valid CSV file to review the preview table and perform transactional batch commit.

---

### Step 15: CSV Export
* **URL:** `http://localhost:5197/PurchaseOrders/ExportCsv` (or click **Import / Export &rarr; Export CSV** in navbar)
* **What to Observe:**
  * Automatically downloads a complete CSV file of all purchase orders.
  * **Formula Injection Defense**: Any cell starting with `=`, `+`, `-`, or `@` is safely sanitized with a leading single quote (`'`), protecting users opening the file in Microsoft Excel.

---

## 🛡️ Security & Integrity Checklist

| Security Feature | Implementation | Verification |
| :--- | :--- | :---: |
| **Anti-Forgery Protection** | `[ValidateAntiForgeryToken]` on all POST actions | Verified across 70 tests |
| **Overposting Defense** | `[Bind("Prop1,Prop2...")]` on controller action arguments | Verified |
| **Safe Deletion** | Foreign key reference check blocks deleting active entities | Verified |
| **SQL Injection Prevention** | Pure EF Core LINQ parameterized queries (0 string concatenations) | Verified |
| **Formula Injection Defense** | Sanitizes dangerous leading characters (`=`, `+`, `-`, `@`) on CSV export | Verified |
| **File Upload Security** | 5 MB maximum size limit + `.csv` extension verification | Verified |

---

## 🏁 Summary

The **Smart Purchase & Expense Tracker** provides a seamless, robust, and professional platform for managing modern quick-commerce and digital lifestyle spending. Every page is fully responsive, loaded with rich test data, and backed by a 70-test automated verification suite.
