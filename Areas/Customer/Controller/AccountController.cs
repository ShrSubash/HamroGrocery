using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;                    // ← This gives you TempData, RedirectToAction, NotFound, ModelState
using Microsoft.EntityFrameworkCore;
using HamroGrocery.Models.Entities;
using HamroGrocery.Models.ViewModels;
using HamroGrocery.Data;

namespace HamroGrocery.Areas.Customer.Controllers    // ← Correct namespace
{
    [Area("Customer")]           // ← This tells ASP.NET Core it's in Customer area
    [Authorize]
    public class AccountController : Controller   // ← Must inherit from Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<User> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: /Customer/Account/Profile
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Load city & state
            await _context.Entry(user)
                .Reference(u => u.City)
                .LoadAsync();
            await _context.Entry(user)
                .Reference(u => u.StateProvince)
                .LoadAsync();

            var orders = await _context.Orders
                .Where(o => o.CustomerId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToListAsync();

            var viewModel = new CustomerProfileViewModel
            {
                User = user,
                TotalOrders = await _context.Orders.CountAsync(o => o.CustomerId == user.Id),
                RecentOrders = orders
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(CustomerProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View("Profile", model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.FirstName = model.User.FirstName;
            user.LastName = model.User.LastName;
            user.PhoneNumber = model.User.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "Profile updated successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to update profile.";
            }

            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitComplaint(ComplaintViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fill all required fields.";
                return RedirectToAction(nameof(Profile));
            }

            var user = await _userManager.GetUserAsync(User);

            var complaint = new Complaint
            {
                UserId = user.Id,
                Type = ComplaintType.Other,
                Subject = model.Subject,
                Description = model.Message,
                Status = ComplaintStatus.Pending,
                Priority = ComplaintPriority.Medium,
                CreatedAt = DateTime.UtcNow
            };

            _context.Complaints.Add(complaint);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thank you! Your complaint has been submitted.";
            return RedirectToAction(nameof(Profile));
        }
    }
}