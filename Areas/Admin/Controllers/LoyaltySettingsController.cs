using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HamroGrocery.Data;
using HamroGrocery.Models.Entities;
using System.Text.Json;

namespace HamroGrocery.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class LoyaltySettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LoyaltySettingsController> _logger;

        public LoyaltySettingsController(ApplicationDbContext context, ILogger<LoyaltySettingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/LoyaltySettings
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get or create loyalty settings
                var settings = await _context.LoyaltySettings.FirstOrDefaultAsync();

                if (settings == null)
                {
                    // Create default settings
                    settings = new LoyaltySettings
                    {
                        Id = 1,
                        IsLoyaltyProgramEnabled = true,
                        IsReviewSystemEnabled = true,
                        IsComplaintSystemEnabled = true,
                        PointsPerDollarSpent = 1,
                        PointsForReview = 50,
                        PointsForReferral = 100,
                        PointsRedemptionValue = 0.01m,
                        BronzeRequirement = 0,
                        SilverRequirement = 10,
                        GoldRequirement = 30,
                        PlatinumRequirement = 50,
                        DiamondRequirement = 100,
                        BronzeDiscount = 0,
                        SilverDiscount = 5,
                        GoldDiscount = 10,
                        PlatinumDiscount = 15,
                        DiamondDiscount = 20,
                        EnableMilestoneRewards = true,
                        MilestoneRewardsJson = "{\"5\":100,\"10\":250,\"25\":500,\"50\":1000,\"100\":2500}",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.LoyaltySettings.Add(settings);
                    await _context.SaveChangesAsync();
                }

                // Parse milestone rewards
                var milestones = ParseMilestoneRewards(settings.MilestoneRewardsJson);
                ViewBag.Milestones = milestones;

                // Get statistics
                ViewBag.Statistics = new
                {
                    TotalUsers = await _context.Users.CountAsync(u => u.Role == UserRole.Customer),
                    ActiveLoyaltyMembers = await _context.Users.CountAsync(u => u.LoyaltyPoints > 0),
                    TotalPointsIssued = await _context.Users.SumAsync(u => u.LoyaltyPoints),
                    BronzeMembers = await _context.Users.CountAsync(u => u.MembershipTier == MembershipTier.Bronze),
                    SilverMembers = await _context.Users.CountAsync(u => u.MembershipTier == MembershipTier.Silver),
                    GoldMembers = await _context.Users.CountAsync(u => u.MembershipTier == MembershipTier.Gold),
                    PlatinumMembers = await _context.Users.CountAsync(u => u.MembershipTier == MembershipTier.Platinum),
                    DiamondMembers = await _context.Users.CountAsync(u => u.MembershipTier == MembershipTier.Diamond),
                    TotalReviews = await _context.Reviews.CountAsync(),
                    PendingReviews = await _context.Reviews.CountAsync(r => !r.IsApproved && !r.IsDeleted),
                    TotalComplaints = await _context.Complaints.CountAsync(),
                    PendingComplaints = await _context.Complaints.CountAsync(c => c.Status == ComplaintStatus.Pending)
                };

                return View(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading loyalty settings");
                TempData["ErrorMessage"] = "Error loading loyalty settings";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: Admin/LoyaltySettings/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(LoyaltySettings model, string milestonesJson)
        {
            try
            {
                var settings = await _context.LoyaltySettings.FirstOrDefaultAsync();

                if (settings == null)
                {
                    return NotFound();
                }

                // Update all settings
                settings.IsLoyaltyProgramEnabled = model.IsLoyaltyProgramEnabled;
                settings.IsReviewSystemEnabled = model.IsReviewSystemEnabled;
                settings.IsComplaintSystemEnabled = model.IsComplaintSystemEnabled;
                settings.PointsPerDollarSpent = model.PointsPerDollarSpent;
                settings.PointsForReview = model.PointsForReview;
                settings.PointsForReferral = model.PointsForReferral;
                settings.PointsRedemptionValue = model.PointsRedemptionValue;
                settings.BronzeRequirement = model.BronzeRequirement;
                settings.SilverRequirement = model.SilverRequirement;
                settings.GoldRequirement = model.GoldRequirement;
                settings.PlatinumRequirement = model.PlatinumRequirement;
                settings.DiamondRequirement = model.DiamondRequirement;
                settings.BronzeDiscount = model.BronzeDiscount;
                settings.SilverDiscount = model.SilverDiscount;
                settings.GoldDiscount = model.GoldDiscount;
                settings.PlatinumDiscount = model.PlatinumDiscount;
                settings.DiamondDiscount = model.DiamondDiscount;
                settings.EnableMilestoneRewards = model.EnableMilestoneRewards;

                // Update milestones if provided
                if (!string.IsNullOrEmpty(milestonesJson))
                {
                    settings.MilestoneRewardsJson = milestonesJson;
                }

                settings.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Loyalty settings updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating loyalty settings");
                TempData["ErrorMessage"] = "Error updating loyalty settings";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/LoyaltySettings/ToggleFeature
        [HttpPost]
        public async Task<IActionResult> ToggleFeature(string feature)
        {
            try
            {
                var settings = await _context.LoyaltySettings.FirstOrDefaultAsync();
                if (settings == null)
                {
                    return Json(new { success = false, message = "Settings not found" });
                }

                switch (feature.ToLower())
                {
                    case "loyalty":
                        settings.IsLoyaltyProgramEnabled = !settings.IsLoyaltyProgramEnabled;
                        break;
                    case "reviews":
                        settings.IsReviewSystemEnabled = !settings.IsReviewSystemEnabled;
                        break;
                    case "complaints":
                        settings.IsComplaintSystemEnabled = !settings.IsComplaintSystemEnabled;
                        break;
                    case "milestones":
                        settings.EnableMilestoneRewards = !settings.EnableMilestoneRewards;
                        break;
                    default:
                        return Json(new { success = false, message = "Invalid feature" });
                }

                settings.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Feature toggled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling feature");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Helper method to parse milestone rewards
        private Dictionary<int, int> ParseMilestoneRewards(string json)
        {
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
                return dict?.ToDictionary(
                    kvp => int.Parse(kvp.Key),
                    kvp => kvp.Value
                ) ?? new Dictionary<int, int>();
            }
            catch
            {
                return new Dictionary<int, int>();
            }
        }
    }
}