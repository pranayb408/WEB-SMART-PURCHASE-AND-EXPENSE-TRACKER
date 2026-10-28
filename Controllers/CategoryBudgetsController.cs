using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace PersonalExpenseTracker.Controllers
{
    /// <summary>
    /// Dedicated controller for CategoryBudgets routing (Section 13).
    /// Bridges direct /CategoryBudgets requests to the unified Budget management module.
    /// </summary>
    public class CategoryBudgetsController : Controller
    {
        // GET: /CategoryBudgets
        public IActionResult Index(int? year, int? month)
        {
            return RedirectToAction("Index", "Budgets", new { year, month });
        }

        // GET: /CategoryBudgets/Create
        public IActionResult Create(int? year, int? month)
        {
            return RedirectToAction("CreateCategoryBudget", "Budgets", new { year, month });
        }

        // GET: /CategoryBudgets/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null) return NotFound();
            return RedirectToAction("EditCategoryBudget", "Budgets", new { id });
        }
    }
}

