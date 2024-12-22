using proiect_daw.Data;
using proiect_daw.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proiect_daw.Controllers
{
    // [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
            )
        {
            db = context;

            _userManager = userManager;

            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            // MOTOR DE CAUTARE

            var search = "";

            if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
            {
                search = Convert.ToString(HttpContext.Request.Query["search"]).Trim(); // eliminam spatiile libere 

                var searched_users = db.Users
                     .Where(u => (u.FirstName + " " + u.LastName).Contains(search) ||
                                 u.UserName.Contains(search) ||
                                 u.Email.Contains(search))
                     .OrderBy(u => u.UserName)
                     .ToList();

                ViewBag.SearchedUsersList = searched_users;
            }

            ViewBag.SearchString = search;


            var excludedUsers = new List<string> { "admin@test.com", "editor@test.com", "user@test.com" };
            var currentUserName = User.Identity.Name;

            if (!string.IsNullOrEmpty(currentUserName))
            {
                excludedUsers.Add(currentUserName);
            }
            var users = from user in db.Users
                        where !excludedUsers.Contains(user.UserName)
                        orderby user.UserName
                        select user;

            ViewBag.UsersList = users;

            return View();
        }

        public async Task<ActionResult> Show(string id)
        {
            ApplicationUser user = await db.Users
                                           .Include(u => u.Posts)
                                           .ThenInclude(p => p.Likes)
                                           .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            ViewBag.Roles = roles;
            ViewBag.UserCurent = await _userManager.GetUserAsync(User);
            ViewBag.UserPosts = user.Posts;

          
            // Get the current user's ID
            var currentUserId = _userManager.GetUserId(User);
            ViewBag.UserCurent = currentUserId;
            // Create a dictionary to store the like status for each post
            var postLikes = new Dictionary<int, bool>();
            // Check if the current user has already sent a follow request
            var hasSentFollowRequest = db.FollowRequests.Any(fr => fr.SenderId == currentUserId && fr.ReceiverId == id && fr.PendingApproval);
            ViewBag.HasSentFollowRequest = hasSentFollowRequest;

            ViewBag.IsFollowing = db.FollowRequests.Any(fr => fr.SenderId == currentUserId && fr.ReceiverId == id && !fr.PendingApproval);
            ViewBag.HasSentFollowRequest = db.FollowRequests.Any(fr => fr.SenderId == currentUserId && fr.ReceiverId == id && fr.PendingApproval);
            ViewBag.FollowersCount = db.FollowRequests.Count(fr => fr.ReceiverId == id && !fr.PendingApproval);
            ViewBag.FollowingCount = db.FollowRequests.Count(fr => fr.SenderId == id && !fr.PendingApproval);


            foreach (var post in user.Posts)
            {
                // Check if the current user has liked the post
                var hasLiked = post.Likes.Any(like => like.UserId == currentUserId);
                postLikes[post.Id] = hasLiked;
                ViewData[$"Likes_{post.Id}"] = hasLiked;
            }

            ViewBag.PostLikes = postLikes;

            return View(user);
        }

        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Edit(string id)
        {
            ApplicationUser user = db.Users.Find(id);

            ViewBag.AllRoles = GetAllRoles();

            var roleNames = await _userManager.GetRolesAsync(user); // Lista de nume de roluri

            // Cautam ID-ul rolului in baza de date
            ViewBag.UserRole = _roleManager.Roles
                                              .Where(r => roleNames.Contains(r.Name))
                                              .Select(r => r.Id)
                                              .First(); // Selectam 1 singur rol

            return View(user);
        }

        private List<SelectListItem> GetAllRoles()
        {
            var roles = _roleManager.Roles.ToList();
            var roleList = new List<SelectListItem>();

            foreach (var role in roles)
            {
                roleList.Add(new SelectListItem
                {
                    Value = role.Id,
                    Text = role.Name
                });
            }

            return roleList;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult> Edit(string id, ApplicationUser newData, [FromForm] string newRole)
        {
            ApplicationUser user = db.Users.Find(id);

            user.AllRoles = GetAllRoles();

            if (ModelState.IsValid)
            {
                user.UserName = newData.UserName;
                user.Email = newData.Email;
                user.FirstName = newData.FirstName;
                user.LastName = newData.LastName;
                user.PhoneNumber = newData.PhoneNumber;
                user.PrivateProfile = newData.PrivateProfile;
                user.ProfileDescription = newData.ProfileDescription;
                user.ProfilePhoto = newData.ProfilePhoto;

                // Cautam toate rolurile din baza de date
                var roles = db.Roles.ToList();

                foreach (var role in roles)
                {
                    // Scoatem userul din rolurile anterioare
                    await _userManager.RemoveFromRoleAsync(user, role.Name);
                }
                // Adaugam noul rol selectat
                var roleName = await _roleManager.FindByIdAsync(newRole);
                await _userManager.AddToRoleAsync(user, roleName.ToString());

                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Delete(string id)
        {
            var user = db.Users
                         .Include(u => u.Posts)
                         .Include(u => u.Comments)
                         .Include(u => u.Bookmarks)
                         .Include(u => u.SentFollowRequests)
                         .Include(u => u.ReceivedFollowRequests)
                         .FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Delete user comments
            if (user.Comments != null && user.Comments.Count > 0)
            {
                db.Comments.RemoveRange(user.Comments);
            }

            // Delete user bookmarks
            if (user.Bookmarks != null && user.Bookmarks.Count > 0)
            {
                db.Bookmarks.RemoveRange(user.Bookmarks);
            }

            // Delete user posts
            if (user.Posts != null && user.Posts.Count > 0)
            {
                db.Posts.RemoveRange(user.Posts);
            }

            // Delete follow requests where the user is the sender
            if (user.SentFollowRequests != null && user.SentFollowRequests.Count > 0)
            {
                db.FollowRequests.RemoveRange(user.SentFollowRequests);
            }

            // Delete follow requests where the user is the receiver
            if (user.ReceivedFollowRequests != null && user.ReceivedFollowRequests.Count > 0)
            {
                db.FollowRequests.RemoveRange(user.ReceivedFollowRequests);
            }

            db.ApplicationUsers.Remove(user);
            db.SaveChanges();

            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> SendFollowRequest(string receiverId)
        {
            var senderId = _userManager.GetUserId(User);
            var receiver = await db.Users.FindAsync(receiverId);

            if (receiver == null)
            {
                return NotFound();
            }

            var followRequest = new FollowRequest
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                PendingApproval = receiver.PrivateProfile
            };

            db.FollowRequests.Add(followRequest);
            await db.SaveChangesAsync();

            // If the profile is public, accept the follow request automatically
            if (!receiver.PrivateProfile)
            {
                followRequest.PendingApproval = false;
                await db.SaveChangesAsync();
            }

            return RedirectToAction("Show", new { id = receiverId });
        }


        [HttpPost]
        public async Task<IActionResult> AcceptFollowRequest(int requestId)
        {
            var followRequest = await db.FollowRequests.FindAsync(requestId);
            if (followRequest != null)
            {
                followRequest.PendingApproval = false;
                await db.SaveChangesAsync();
            }

            return RedirectToAction("Show", new { id = followRequest.ReceiverId });
        }

        [HttpPost]
        public async Task<IActionResult> DeclineFollowRequest(int requestId)
        {
            var followRequest = await db.FollowRequests.FindAsync(requestId);
            if (followRequest != null)
            {
                db.FollowRequests.Remove(followRequest);
                await db.SaveChangesAsync();
            }

            return RedirectToAction("Show", new { id = followRequest.ReceiverId });
        }
        [HttpPost]
        public async Task<IActionResult> UndoFollowRequest(string receiverId)
        {
            var senderId = _userManager.GetUserId(User);
            var followRequest = db.FollowRequests.FirstOrDefault(fr => fr.SenderId == senderId && fr.ReceiverId == receiverId && fr.PendingApproval);

            if (followRequest != null)
            {
                db.FollowRequests.Remove(followRequest);
                await db.SaveChangesAsync();
            }

            return RedirectToAction("Show", new { id = receiverId });
        }
        public async Task<IActionResult> PendingFollows()
        {
            var currentUserId = _userManager.GetUserId(User);

            // Fetch all pending follow requests for the logged-in user
            var pendingRequests = db.FollowRequests
                .Include(fr => fr.Sender)
                .Where(fr => fr.ReceiverId == currentUserId && fr.PendingApproval)
                .ToList();

            return View(pendingRequests);
        }
        [HttpPost]
        public async Task<IActionResult> Unfollow(string receiverId)
        {
            var senderId = _userManager.GetUserId(User);

            // Find the follow request where the current user is following the target user
            var followRequest = await db.FollowRequests
                                        .FirstOrDefaultAsync(fr => fr.SenderId == senderId && fr.ReceiverId == receiverId && !fr.PendingApproval);

            if (followRequest != null)
            {
                // Remove the follow request to unfollow the user
                db.FollowRequests.Remove(followRequest);
                await db.SaveChangesAsync();
            }

            return RedirectToAction("Show", new { id = receiverId });
        }


    }
}
