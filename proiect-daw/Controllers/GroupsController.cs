﻿using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proiect_daw.Data;
using proiect_daw.Models;

namespace proiect_daw.Controllers
{
    public class GroupsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly GroupMembership _groupMembership;
       
        public GroupsController(ApplicationDbContext context,
                                UserManager<ApplicationUser> userManager)
        {
            db = context;
            _userManager = userManager;
        }

        public IActionResult New()
        {
            return View();
        }


        // Se afiseaza lista tuturor postarilor impreuna cu categoria 
        // din care fac parte
        // Pentru fiecare articol se afiseaza si userul care a postat articolul respectiv
        // [HttpGet] care se executa implicit
        public IActionResult Index()
        {
            var userId = _userManager.GetUserId(User);

            var groupMemberships = db.GroupMemberships.Where(gm => gm.UserId == userId);

            // ViewBag.OriceDenumireSugestiva
            // ViewBag.Posts = posts;

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            // MOTOR DE CAUTARE

            var search = "";
            var grupuri = db.Groups.AsQueryable();

            if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
            {
                search = Convert.ToString(HttpContext.Request.Query["search"]).Trim(); // eliminam spatiile libere 

                grupuri = grupuri.Where(g => g.Name.Contains(search) || g.Description.Contains(search));
            }

            var join = from g in grupuri
                       join gm in groupMemberships on g.Id equals gm.GroupId
                       select g;

            ViewBag.SearchString = search;

            // AFISARE PAGINATA

            // Alegem sa afisam 3 postari pe pagina
            int _perPage = 3;

            // Fiind un numar variabil de postari, verificam de fiecare data utilizand 
            // metoda Count()
            int totalItems = join.Count();

            // Se preia pagina curenta din View-ul asociat
            // Numarul paginii este valoarea parametrului page din ruta
            // /Posts/Index?page=valoare

            var currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);

            // Pentru prima pagina offsetul o sa fie zero
            // Pentru pagina 2 o sa fie 3 
            // Asadar offsetul este egal cu numarul de postari care au fost deja afisate pe paginile anterioare
            var offset = 0;

            // Se calculeaza offsetul in functie de numarul paginii la care suntem
            if (!currentPage.Equals(0))
            {
                offset = (currentPage - 1) * _perPage;
            }

            // Se preiau postarile corespunzatoare pentru fiecare pagina la care ne aflam 
            // in functie de offset
            var paginatedPosts = join.Skip(offset).Take(_perPage).ToList();

            // Preluam numarul ultimei pagini
            ViewBag.lastPage = Math.Ceiling((float)totalItems / (float)_perPage);

            // Trimitem postarile cu ajutorul unui ViewBag catre View-ul corespunzator
            ViewBag.OwnGroups = paginatedPosts;
            ViewBag.Groups = grupuri.ToList();

            // DACA AVEM AFISAREA PAGINATA IMPREUNA CU SEARCH

            if (search != "")
            {
                ViewBag.PaginationBaseUrl = "/Groups/Index/?search=" + search + "&page";
            }
            else
            {
                ViewBag.PaginationBaseUrl = "/Groups/Index/?page";
            }

            return View();
        }


        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Show(int id)
        {
            var group = db.Groups.FirstOrDefault(g => g.Id == id);
            var userId = _userManager.GetUserId(User);

            if (group == null)
            {
                // Handle the case when the post is not found
                return NotFound();
            }

            var ceva = db.GroupMemberships.FirstOrDefault(gm => gm.GroupId == id && gm.UserId == userId);

            if(ceva == null)
            {
                ViewBag.isPendingApproval = null;
            }
            else
            {
                ViewBag.isPendingApproval = ceva.PendingApproval;
            }

            var users = db.GroupMemberships
                            .Where(gm => gm.GroupId == id && !gm.PendingApproval)
                            .Include(gm => gm.User)
                            .Select(gm => gm.User)
                            .ToList();

            ViewBag.Users = users;

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            return View(group);
        }

        [HttpPost]
        public IActionResult Join(int groupId)
        {
            var userId = _userManager.GetUserId(User);

            GroupMembership groupMembership = new GroupMembership
            {
                GroupId = groupId,
                UserId = userId,
                PendingApproval = true
            };

            db.GroupMemberships.Add(groupMembership);
            db.SaveChanges();

            return RedirectToAction("Index");
        }


        // Se adauga articolul in baza de date
        // Doar utilizatorii cu rolul Editor si Admin pot adauga postari in platforma
        [HttpPost]
        public async Task<IActionResult> New(Group group)
        {
            var sanitizer = new HtmlSanitizer();

            group.Date = DateTime.Now;
            group.ModeratorId = _userManager.GetUserId(User);

            var user = await _userManager.GetUserAsync(User);

            group.ModeratorName = user.LastName + " " + user.FirstName;


            if (ModelState.IsValid)
            {
                group.Name = sanitizer.Sanitize(group.Name);
                group.Description = sanitizer.Sanitize(group.Description);

                db.Groups.Add(group);
                db.SaveChanges();

                db.GroupMemberships.Add(new GroupMembership
                {
                    GroupId = group.Id,
                    UserId = group.ModeratorId,
                    PendingApproval = false
                });
                db.SaveChanges();

                TempData["message"] = "Grupul a fost adaugat";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }
            else
            {
                return View(group);
            }

            return View(group);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveUser(int groupId, string userId)
        {
            var group = await db.Groups.FindAsync(groupId);
            var currentUserId = _userManager.GetUserId(User);

            if (group == null || group.ModeratorId != currentUserId)
            {
                TempData["message"] = "You are not authorized to remove users from this group.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = groupId });
            }

            var groupMembership = await db.GroupMemberships
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

            if (groupMembership != null)
            {
                db.GroupMemberships.Remove(groupMembership);
                await db.SaveChangesAsync();
                TempData["message"] = "User removed from the group successfully.";
                TempData["messageType"] = "alert-success";
            }
            else
            {
                TempData["message"] = "User not found in the group.";
                TempData["messageType"] = "alert-danger";
            }

            return RedirectToAction("Show", new { id = groupId });
        }

        public IActionResult GroupInfo()
        {
            var groups = GetGroups();
            ViewBag.Groups = groups ?? new List<Group>();
            ViewBag.SearchString = ""; // Initialize other ViewBag properties as needed
            return View();
        }

        private List<Group> GetGroups()
        {
            return db.Groups.OrderByDescending(a => a.Date).ToList();
        }
    }  
}
