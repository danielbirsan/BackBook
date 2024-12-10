using proiect_daw.Data;
using proiect_daw.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace proiect_daw.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public HomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<HomeController> logger

            )
        {
            db = context;

            _userManager = userManager;

            _roleManager = roleManager;

            _logger = logger;

        }

        public IActionResult Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Posts");
            }

            var posts = from post in db.Posts
                        select post;

            var firstPost = posts.FirstOrDefault();
            if (firstPost != null)
            {
                ViewBag.FirstPost = firstPost;
                ViewBag.Posts = posts.OrderBy(o => o.Date).Skip(1).Take(2);
            }
            else
            {
                ViewBag.FirstPost = null;
                ViewBag.Posts = Enumerable.Empty<Post>();
            }

            return View();
        }

        public IActionResult Welcome()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}