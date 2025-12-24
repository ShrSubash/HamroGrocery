using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HamroGrocery.Data;
using HamroGrocery.Models.Entities;
using HamroGrocery.Models.ViewModels;
using HamroGrocery.Services;

namespace HamroGrocery.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<UsersController> _logger;
        private readonly IUserStatisticsService _userStatisticsService;

        public UsersController(
        ApplicationDbContext context,
        UserManager<User> userManager,
        ILogger<UsersController> logger,
        IUserStatisticsService userStatisticsService) 
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _userStatisticsService = userStatisticsService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecalculateStatistics()
        {
            try
            {
                await _userStatisticsService.RecalculateAllUserStatisticsAsync();
                TempData["SuccessMessage"] = "User statistics recalculated successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating user statistics");
                TempData["ErrorMessage"] = "Error recalculating statistics. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
        // GET: Admin/Users
        public async Task<IActionResult> Index(string searchTerm = "", string role = "", string status = "", string tier = "")
        {
            try
            {
                ViewData["Title"] = "User Management";

                var usersQuery = _context.Users
                    .Include(u => u.Orders)
                    .Include(u => u.Reviews)
                    .Include(u => u.Complaints)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    usersQuery = usersQuery.Where(u =>
                        u.FirstName.Contains(searchTerm) ||
                        u.LastName.Contains(searchTerm) ||
                        (u.UserName != null && u.UserName.Contains(searchTerm)) ||
                        u.Email.Contains(searchTerm) ||
                        (u.PhoneNumber != null && u.PhoneNumber.Contains(searchTerm)));
                }

                if (!string.IsNullOrEmpty(role) && Enum.TryParse<UserRole>(role, true, out var userRole))
                {
                    usersQuery = usersQuery.Where(u => u.Role == userRole);
                }

                if (!string.IsNullOrEmpty(status) && bool.TryParse(status, out var isActive))
                {
                    usersQuery = usersQuery.Where(u => u.IsActive == isActive);
                }

                if (!string.IsNullOrEmpty(tier) && Enum.TryParse<MembershipTier>(tier, true, out var membershipTier))
                {
                    usersQuery = usersQuery.Where(u => u.MembershipTier == membershipTier);
                }

                var users = await usersQuery
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();

                // Build ViewModel
                var viewModel = new UserManagementViewModel
                {
                    Users = users.Select(u => new UserCardDto
                    {
                        Id = u.Id,
                        FullName = u.FullName,
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber,
                        ProfilePictureUrl = u.ProfilePictureUrl,
                        Role = u.Role.ToString(),
                        MembershipTier = u.MembershipTier.ToString(),
                        LoyaltyPoints = u.LoyaltyPoints,
                        TotalPurchases = u.TotalPurchases,
                        TotalSpent = u.TotalSpent,
                        ReviewCount = u.ReviewCount,
                        ComplaintCount = u.ComplaintCount,
                        LastPurchaseDate = u.Orders.OrderByDescending(o => o.OrderDate).FirstOrDefault()?.OrderDate,
                        CreatedAt = u.CreatedAt,
                        LastLoginAt = u.LastLoginAt,
                        IsActive = u.IsActive
                    }).ToList(),

                    Statistics = new UserStatistics
                    {
                        TotalUsers = users.Count,
                        ActiveUsers = users.Count(u => u.IsActive),
                        NewUsersThisMonth = users.Count(u => u.CreatedAt.Month == DateTime.Now.Month && u.CreatedAt.Year == DateTime.Now.Year),
                        TotalRevenue = await _context.Orders
                            .Where(o => o.Status == OrderStatus.Delivered)
                            .SumAsync(o => o.TotalAmount),
                        TotalOrders = await _context.Orders.CountAsync(),
                        PendingComplaints = await _context.Complaints
                            .CountAsync(c => c.Status == ComplaintStatus.Pending),
                        PendingReviews = await _context.Reviews
                            .CountAsync(r => !r.IsApproved && !r.IsDeleted),
                        BronzeUsers = users.Count(u => u.MembershipTier == MembershipTier.Bronze),
                        SilverUsers = users.Count(u => u.MembershipTier == MembershipTier.Silver),
                        GoldUsers = users.Count(u => u.MembershipTier == MembershipTier.Gold),
                        PlatinumUsers = users.Count(u => u.MembershipTier == MembershipTier.Platinum),
                        DiamondUsers = users.Count(u => u.MembershipTier == MembershipTier.Diamond)
                    },

                    LoyaltySettings = await _context.LoyaltySettings.FirstOrDefaultAsync()
                };

                // Pass filter values to maintain state
                ViewData["SearchTerm"] = searchTerm;
                ViewData["Role"] = role;
                ViewData["Status"] = status;
                ViewData["Tier"] = tier;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users");
                TempData["ErrorMessage"] = "Error loading users";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Admin/Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderItems)
                .Include(u => u.Reviews)
                    .ThenInclude(r => r.Product)
                .Include(u => u.Complaints)
                .Include(u => u.Country)
                .Include(u => u.StateProvince)
                .Include(u => u.City)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (user == null) return NotFound();

            ViewData["Title"] = $"User Details - {user.FullName}";

            // Calculate analytics
            var weeklyOrders = user.Orders
                .Where(o => o.OrderDate >= DateTime.Now.AddDays(-7))
                .ToList();

            var monthlyOrders = user.Orders
                .Where(o => o.OrderDate >= DateTime.Now.AddMonths(-1))
                .ToList();

            var yearlyOrders = user.Orders
                .Where(o => o.OrderDate >= DateTime.Now.AddYears(-1))
                .ToList();

            ViewBag.WeeklyStats = new
            {
                OrderCount = weeklyOrders.Count,
                TotalAmount = weeklyOrders.Sum(o => o.TotalAmount),
                AverageOrderValue = weeklyOrders.Any() ? weeklyOrders.Average(o => o.TotalAmount) : 0
            };

            ViewBag.MonthlyStats = new
            {
                OrderCount = monthlyOrders.Count,
                TotalAmount = monthlyOrders.Sum(o => o.TotalAmount),
                AverageOrderValue = monthlyOrders.Any() ? monthlyOrders.Average(o => o.TotalAmount) : 0
            };

            ViewBag.YearlyStats = new
            {
                OrderCount = yearlyOrders.Count,
                TotalAmount = yearlyOrders.Sum(o => o.TotalAmount),
                AverageOrderValue = yearlyOrders.Any() ? yearlyOrders.Average(o => o.TotalAmount) : 0
            };

            return View(user);
        }

        // GET: Admin/Users/Create
        public IActionResult Create()
        {
            ViewData["Title"] = "Create New User";
            ViewBag.Countries = _context.Countries.OrderBy(c => c.CountryName).ToList();
            ViewBag.Roles = Enum.GetValues<UserRole>().ToList();
            ViewBag.Tiers = Enum.GetValues<MembershipTier>().ToList();
            return View();
        }

        // POST: Admin/Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user, string password)
        {
            if (ModelState.IsValid && !string.IsNullOrEmpty(password))
            {
                // Set default values
                user.CreatedAt = DateTime.UtcNow;
                user.IsActive = true;
                user.IsApproved = true;
                user.EmailConfirmed = true;
                user.UserName = user.Email;

                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = $"User {user.FullName} created successfully!";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Reload dropdowns on error
            ViewBag.Countries = _context.Countries.OrderBy(c => c.CountryName).ToList();
            ViewBag.Roles = Enum.GetValues<UserRole>().ToList();
            ViewBag.Tiers = Enum.GetValues<MembershipTier>().ToList();
            return View(user);
        }

        // GET: Admin/Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            ViewData["Title"] = $"Edit User - {user.FullName}";
            ViewBag.Countries = _context.Countries.OrderBy(c => c.CountryName).ToList();
            ViewBag.StateProvinces = user.CountryId.HasValue ?
                _context.StateProvinces.Where(s => s.CountryId == user.CountryId).OrderBy(s => s.StateName).ToList() :
                new List<StateProvince>();
            ViewBag.Cities = user.StateProvinceId.HasValue ?
                _context.Cities.Where(c => c.StateProvinceId == user.StateProvinceId).OrderBy(c => c.CityName).ToList() :
                new List<City>();
            ViewBag.Roles = Enum.GetValues<UserRole>().ToList();
            ViewBag.Tiers = Enum.GetValues<MembershipTier>().ToList();

            return View(user);
        }

        // POST: Admin/Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User user, string newPassword = "")
        {
            if (id != user.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Users.FindAsync(id);
                    if (existingUser == null) return NotFound();

                    // Update user properties
                    existingUser.FirstName = user.FirstName;
                    existingUser.LastName = user.LastName;
                    existingUser.MiddleName = user.MiddleName;
                    existingUser.Email = user.Email;
                    existingUser.PhoneNumber = user.PhoneNumber;
                    existingUser.Role = user.Role;
                    existingUser.IsActive = user.IsActive;
                    existingUser.IsApproved = user.IsApproved;
                    existingUser.DateOfBirth = user.DateOfBirth;
                    existingUser.Gender = user.Gender;
                    existingUser.AddressLineOne = user.AddressLineOne;
                    existingUser.AddressLineTwo = user.AddressLineTwo;
                    existingUser.PostalCode = user.PostalCode;
                    existingUser.CountryId = user.CountryId;
                    existingUser.StateProvinceId = user.StateProvinceId;
                    existingUser.CityId = user.CityId;
                    existingUser.MembershipTier = user.MembershipTier;
                    existingUser.LoyaltyPoints = user.LoyaltyPoints;
                    existingUser.UpdatedAt = DateTime.UtcNow;

                    var result = await _userManager.UpdateAsync(existingUser);

                    // Update password if provided
                    if (!string.IsNullOrEmpty(newPassword))
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                        var passwordResult = await _userManager.ResetPasswordAsync(existingUser, token, newPassword);

                        if (!passwordResult.Succeeded)
                        {
                            foreach (var error in passwordResult.Errors)
                            {
                                ModelState.AddModelError(string.Empty, $"Password Error: {error.Description}");
                            }
                            return View(user);
                        }
                    }

                    if (result.Succeeded)
                    {
                        TempData["SuccessMessage"] = $"User {existingUser.FullName} updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating user");
                    TempData["ErrorMessage"] = $"Error updating user: {ex.Message}";
                }
            }

            // Reload dropdowns on error
            ViewBag.Countries = _context.Countries.OrderBy(c => c.CountryName).ToList();
            ViewBag.StateProvinces = user.CountryId.HasValue ?
                _context.StateProvinces.Where(s => s.CountryId == user.CountryId).OrderBy(s => s.StateName).ToList() :
                new List<StateProvince>();
            ViewBag.Cities = user.StateProvinceId.HasValue ?
                _context.Cities.Where(c => c.StateProvinceId == user.StateProvinceId).OrderBy(c => c.CityName).ToList() :
                new List<City>();
            ViewBag.Roles = Enum.GetValues<UserRole>().ToList();
            ViewBag.Tiers = Enum.GetValues<MembershipTier>().ToList();

            return View(user);
        }

        // POST: Admin/Users/ToggleStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                var status = user.IsActive ? "activated" : "deactivated";
                TempData["SuccessMessage"] = $"User {user.FullName} has been {status} successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update user status.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Users/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users
                .Include(u => u.Orders)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user != null)
            {
                // Check if user has orders - if so, deactivate instead of delete
                if (user.Orders.Any())
                {
                    user.IsActive = false;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    TempData["InfoMessage"] = $"User {user.FullName} has been deactivated instead of deleted because they have existing orders.";
                }
                else
                {
                    var result = await _userManager.DeleteAsync(user);
                    if (result.Succeeded)
                    {
                        TempData["SuccessMessage"] = $"User {user.FullName} deleted successfully!";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to delete user.";
                    }
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // API endpoints for AJAX calls
        [HttpGet]
        public async Task<IActionResult> GetStateProvinces(int countryId)
        {
            var states = await _context.StateProvinces
                .Where(s => s.CountryId == countryId)
                .OrderBy(s => s.StateName)
                .Select(s => new { s.Id, s.StateName })
                .ToListAsync();

            return Json(states);
        }

        [HttpGet]
        public async Task<IActionResult> GetCities(int stateProvinceId)
        {
            var cities = await _context.Cities
                .Where(c => c.StateProvinceId == stateProvinceId)
                .OrderBy(c => c.CityName)
                .Select(c => new { c.Id, c.CityName })
                .ToListAsync();

            return Json(cities);
        }
    }
}