using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Constants;
using SweetCakeShop.Data;
using SweetCakeShop.Models;

namespace SweetCakeShop.Controllers
{
    [Authorize(Roles = nameof(Roles.Admin))]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        #region Category Management
        [HttpGet]
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.CategoryId)
                .ToListAsync();

            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["Error"] = "Tên danh mục không được để trống";
                return RedirectToAction(nameof(Categories));
            }

            var exists = await _context.Categories
                .AnyAsync(c => c.CategoryName == categoryName.Trim());

            if (exists)
            {
                TempData["Error"] = "Danh mục đã tồn tại";
                return RedirectToAction(nameof(Categories));
            }

            _context.Categories.Add(new Category { CategoryName = categoryName.Trim() });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm danh mục thành công";
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCategory(int categoryId, string categoryName)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null)
            {
                TempData["Error"] = "Không tìm thấy danh mục";
                return RedirectToAction(nameof(Categories));
            }

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["Error"] = "Tên danh mục không được để trống";
                return RedirectToAction(nameof(Categories));
            }

            category.CategoryName = categoryName.Trim();
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật danh mục thành công";
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int categoryId)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

            if (category == null)
            {
                TempData["Error"] = "Không tìm thấy danh mục";
                return RedirectToAction(nameof(Categories));
            }

            if (category.Products.Any())
            {
                TempData["Error"] = "Không thể xóa danh mục đang có sản phẩm";
                return RedirectToAction(nameof(Categories));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa danh mục thành công";
            return RedirectToAction(nameof(Categories));
        }
        #endregion

        #region Product Management

        #endregion
    }
}
