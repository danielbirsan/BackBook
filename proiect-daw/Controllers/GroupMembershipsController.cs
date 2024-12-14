using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proiect_daw.Data;
using proiect_daw.Models;

namespace proiect_daw.Controllers
{
    public class GroupMembershipsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;


        public GroupMembershipsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            db = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }
        
        public IActionResult PendingApproval()
        {
            var userId = _userManager.GetUserId(User);

            // Get the groups where the current user is the moderator
            var moderatedGroups = db.Groups.Where(g => g.ModeratorId == userId).Select(g => g.Id).ToList();

            // Get the group memberships that are pending approval for the moderated groups
            ViewBag.groupMemberships = db.GroupMemberships
                .Where(gm => moderatedGroups.Contains(gm.GroupId) && gm.PendingApproval)
                .Include(gm => gm.User) // Include the user details
                .ToList();

            return View();
        }

        public async Task<bool> HasPendingApprovalRequestsAsync()
        {
            var userId = _userManager.GetUserId(User);

            // Get the groups where the current user is the moderator
            var moderatedGroups = await db.Groups
                .Where(g => g.ModeratorId == userId)
                .Select(g => g.Id)
                .ToListAsync();

            // Check if there are any pending approval requests for the moderated groups
            return await db.GroupMemberships
                .AnyAsync(gm => moderatedGroups.Contains(gm.GroupId) && gm.PendingApproval);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveMembership(int id)
        {
            var membership = await db.GroupMemberships.FindAsync(id);
            if (membership != null)
            {
                membership.PendingApproval = false;
                db.GroupMemberships.Update(membership);
                await db.SaveChangesAsync();
                TempData["SuccessMessage"] = "User approved successfully.";
            }
            return RedirectToAction("PendingApproval");
        }

        [HttpPost]
        public async Task<IActionResult> RejectMembership(int id)
        {
            var membership = await db.GroupMemberships.FindAsync(id);
            if (membership != null)
            {
                db.GroupMemberships.Remove(membership);
                await db.SaveChangesAsync();
                TempData["SuccessMessage"] = "User rejected successfully.";
            }
            return RedirectToAction("PendingApproval");
        }
    }
}
