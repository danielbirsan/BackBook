using Ganss.Xss;
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
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Index()
        {
            var grupuri = db.Groups.AsQueryable();

            // ViewBag.OriceDenumireSugestiva
            // ViewBag.Posts = posts;

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            // MOTOR DE CAUTARE

            var search = "";

            if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
            {
                search = Convert.ToString(HttpContext.Request.Query["search"]).Trim(); // eliminam spatiile libere 

                grupuri = grupuri.Where(g => g.Name.Contains(search) || g.Description.Contains(search));

            }

            ViewBag.SearchString = search;

            // AFISARE PAGINATA

            // Alegem sa afisam 3 postari pe pagina
            int _perPage = 3;

            // Fiind un numar variabil de postari, verificam de fiecare data utilizand 
            // metoda Count()
            int totalItems = grupuri.Count();

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
            var paginatedPosts = grupuri.Skip(offset).Take(_perPage).ToList();

            // Preluam numarul ultimei pagini
            ViewBag.lastPage = Math.Ceiling((float)totalItems / (float)_perPage);

            // Trimitem postarile cu ajutorul unui ViewBag catre View-ul corespunzator
            ViewBag.Groups = paginatedPosts;

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

            if (group == null)
            {
                // Handle the case when the post is not found
                return NotFound();
            }

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            return View(group);
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
