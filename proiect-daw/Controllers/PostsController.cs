using proiect_daw.Data;
using proiect_daw.Models;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using static proiect_daw.Models.PostBookmarks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;

namespace proiect_daw.Controllers
{
    [Authorize]
    public class PostsController : Controller
    {
        private readonly IStringLocalizer<HomeController> _localizer;

        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public PostsController(ApplicationDbContext context,
                                UserManager<ApplicationUser> userManager,
                                RoleManager<IdentityRole> roleManager,
                                IStringLocalizer<HomeController> localizer)
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _localizer = localizer;
        }

        // Se afiseaza lista tuturor postarilor impreuna cu categoria 
        // din care fac parte
        // Pentru fiecare articol se afiseaza si userul care a postat articolul respectiv
        // [HttpGet] care se executa implicit
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Index()
        {
            var postari = db.Posts.Include("Category")
                                      .Include("User")
                                      .OrderByDescending(a => a.Date);

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

                // Cautare in articol (Title si Content)

                List<int> postIds = db.Posts.Where
                                        (
                                         at => at.Title.Contains(search)
                                         || at.Content.Contains(search)
                                        ).Select(a => a.Id).ToList();

                // Cautare in comentarii (Content)
                List<int> postIdsOfCommentsWithSearchString = db.Comments
                                        .Where
                                        (
                                         c => c.Content.Contains(search)
                                        ).Select(c => (int)c.PostId).ToList();

                // Se formeaza o singura lista formata din toate id-urile selectate anterior
                List<int> mergedIds = postIds.Union(postIdsOfCommentsWithSearchString).ToList();


                // Lista postarilor care contin cuvantul cautat
                // fie in articol -> Title si Content
                // fie in comentarii -> Content
                postari = db.Posts.Where(post => mergedIds.Contains(post.Id))
                                      .Include("Category")
                                      .Include("User")
                                      .OrderByDescending(a => a.Date);

            }

            ViewBag.SearchString = search;

            // AFISARE PAGINATA

            // Alegem sa afisam 3 postari pe pagina
            int _perPage = 3;

            // Fiind un numar variabil de postari, verificam de fiecare data utilizand 
            // metoda Count()
            int totalItems = postari.Count();

            // Se preia pagina curenta din View-ul asociat
            // Numarul paginii este valoarea parametrului page din ruta
            // /Posts/Index?page=valoare

            var pageQuery = HttpContext.Request.Query["page"].ToString();
            int currentPage = 1; // Default to the first page if parsing fails

            if (!string.IsNullOrEmpty(pageQuery) && int.TryParse(pageQuery, out int parsedPage))
            {
                currentPage = parsedPage;
            }
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
            var paginatedPosts = postari.Skip(offset).Take(_perPage).ToList();

            // Preluam numarul ultimei pagini
            ViewBag.lastPage = Math.Ceiling((float)totalItems / (float)_perPage);

            // Trimitem postarile cu ajutorul unui ViewBag catre View-ul corespunzator
            ViewBag.Posts = paginatedPosts;

            // DACA AVEM AFISAREA PAGINATA IMPREUNA CU SEARCH

            if (search != "")
            {
                ViewBag.PaginationBaseUrl = "/Posts/Index/?search=" + search + "&page";
            }
            else
            {
                ViewBag.PaginationBaseUrl = "/Posts/Index/?page";
            }


            var postsList = db.Posts.Include(p => p.Likes).ToList();
            var currentUserId = _userManager.GetUserId(User);
            var postLikes = new Dictionary<int, bool>();
            var likedPhotos = new List<string>();

            ViewBag.UserCurent = currentUserId;

            foreach (var post in postsList)
            {
                // Check if the current user has liked the post
                var hasLiked = post.Likes.Any(like => like.UserId == currentUserId);
                postLikes[post.Id] = hasLiked;
                ViewData[$"Likes_{post.Id}"] = hasLiked;

                // If the post has been liked by the current user, add the photo to the likedPhotos list
                if (hasLiked && !string.IsNullOrEmpty(post.Id.ToString()))
                {
                    likedPhotos.Add(post.Id.ToString());
                }
            }


            ViewBag.LikedPhotos = likedPhotos;



            return View();
        }

        // Se afiseaza un singur articol in functie de id-ul sau 
        // impreuna cu categoria din care face parte
        // In plus sunt preluate si toate comentariile asociate unui articol
        // Se afiseaza si userul care a postat articolul respectiv
        // [HttpGet] se executa implicit implicit
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Show(int id)
        {
            Post post = db.Posts.Include("Category")
                                .Include("Comments")
                                .Include("User")
                                .Include("Comments.User")
                                .Where(art => art.Id == id)
                                .FirstOrDefault();

            if (post == null)
            {
                // Handle the case when the post is not found
                return NotFound();
            }

            // Adaugam bookmark-urile utilizatorului pentru dropdown
            ViewBag.UserBookmarks = db.Bookmarks
                                      .Where(b => b.UserId == _userManager.GetUserId(User))
                                      .ToList();

            SetAccessRights();

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            var postsList = db.Posts.Include(p => p.Likes).ToList();
            var currentUserId = _userManager.GetUserId(User);
            var postLikes = new Dictionary<int, bool>();
            var likedPhotos = new List<string>();

            ViewBag.UserCurent = currentUserId;

            foreach (var postt in postsList)
            {
                // Check if the current user has liked the post
                var hasLiked = postt.Likes.Any(like => like.UserId == currentUserId);
                postLikes[postt.Id] = hasLiked;
                ViewData[$"Likes_{postt.Id}"] = hasLiked;

                // If the post has been liked by the current user, add the photo to the likedPhotos list
                if (hasLiked && !string.IsNullOrEmpty(postt.Id.ToString()))
                {
                    likedPhotos.Add(postt.Id.ToString());
                }
            }


            ViewBag.LikedPhotos = likedPhotos;


            return View(post);
        }

        // Se afiseaza formularul in care se vor completa datele unui articol
        // impreuna cu selectarea categoriei din care face parte
        // HttpGet implicit


        // Adaugarea unui comentariu asociat unui articol in baza de date
        // Toate rolurile pot adauga comentarii in baza de date
        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Show([FromForm] Comment comment)
        {
            comment.Date = DateTime.Now;

            // preluam Id-ul utilizatorului care posteaza comentariul
            comment.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                db.Comments.Add(comment);
                db.SaveChanges();
                return Redirect("/Posts/Show?id=" + comment.PostId);
            }
            else 
            {
                Post art = db.Posts.Include("Category")
                                         .Include("User")
                                         .Include("Comments")
                                         .Include("Comments.User")
                                         .Where(art => art.Id == comment.PostId)
                                         .First();

                //return Redirect("/Posts/Show/" + comm.PostId);

                // Adaugam bookmark-urile utilizatorului pentru dropdown
                ViewBag.UserBookmarks = db.Bookmarks
                                          .Where(b => b.UserId == _userManager.GetUserId(User))
                                          .ToList();

                SetAccessRights();

                return View(art);
            }
        }

        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult AddBookmark([FromForm] PostBookmark postBookmark)
        {
            // Daca modelul este valid
            if (ModelState.IsValid)
            {
                // Verificam daca avem deja articolul in colectie
                if (db.PostBookmarks
                    .Where(ab => ab.PostId == postBookmark.PostId)
                    .Where(ab => ab.BookmarkId == postBookmark.BookmarkId)
                    .Count() > 0)
                {
                    TempData["message"] = "Aceasta postare este deja adaugata in colectie";
                    TempData["messageType"] = "alert-danger";
                }
                else
                {
                    // Adaugam asocierea intre articol si bookmark 
                    db.PostBookmarks.Add(postBookmark);
                    // Salvam modificarile
                    db.SaveChanges();

                    // Adaugam un mesaj de succes
                    TempData["message"] = "Postarea a fost adaugata in colectia selectata";
                    TempData["messageType"] = "alert-success";
                }

            }
            else
            {
                TempData["message"] = "Nu s-a putut adauga postarea in colectie";
                TempData["messageType"] = "alert-danger";
            }

            // Ne intoarcem la pagina articolului
            return Redirect("/Posts/Show/" + postBookmark.PostId);
        }


        // Se afiseaza formularul in care se vor completa datele unui articol
        // impreuna cu selectarea categoriei din care face parte
        // Doar utilizatorii cu rolul de Editor si Admin pot adauga postari in platforma
        // [HttpGet] - care se executa implicit

        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult New()
        {
            Post post = new Post();

            post.Categ = GetAllCategories();

            return View(post);
        }

        // Se adauga articolul in baza de date
        // Doar utilizatorii cu rolul Editor si Admin pot adauga postari in platforma
        // GetDefaultCategory
        public Category GetDefaultCategory()
        {
            return db.Categories.FirstOrDefault() ?? new Category { CategoryName = "Default" };
        }

        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult New(Post post)
        {
            var sanitizer = new HtmlSanitizer();

            post.Date = DateTime.Now;
            post.LikesCount = 0;
            if (post.Category == null)
            {
                post.Category = GetDefaultCategory(); 
            }

            // preluam Id-ul utilizatorului care posteaza articolul
            post.UserId = _userManager.GetUserId(User);

            if(ModelState.IsValid)
            {
                post.Content = sanitizer.Sanitize(post.Content);

                db.Posts.Add(post);
                db.SaveChanges();
                TempData["message"] = "Postarea a fost adaugata";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }
            else
            {
                post.Categ = GetAllCategories();
                return View(post);
            }
        }

        // Se editeaza un articol existent in baza de date impreuna cu categoria din care face parte
        // Categoria se selecteaza dintr-un dropdown
        // Se afiseaza formularul impreuna cu datele aferente articolului din baza de date
        // Doar utilizatorii cu rolul de Editor si Admin pot edita postari
        // Adminii pot edita orice articol din baza de date
        // Editorii pot edita doar postarile proprii (cele pe care ei le-au postat)
        // [HttpGet] - se executa implicit

        [Authorize(Roles = "Editor,Admin,User")]
        public IActionResult Edit(int id)
        {

            Post post = db.Posts.Include("Category")
                                         .Where(art => art.Id == id)
                                         .First();

            post.Categ = GetAllCategories();

            if ((post.UserId == _userManager.GetUserId(User)) || 
                User.IsInRole("Admin"))
            {
                return View(post);
            }
            else
            {    
                
                TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unui post care nu va apartine";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }  
        }
        [HttpPost]
        

        // Se adauga articolul modificat in baza de date
        // Se verifica rolul utilizatorilor care au dreptul sa editeze (Editor si Admin)
        [HttpPost]
        [Authorize(Roles = "Editor,Admin")]
        public IActionResult Edit(int id, Post requestPost)
        {
            var sanitizer = new HtmlSanitizer();

            Post post = db.Posts.Find(id);

            if(ModelState.IsValid)
            {
                if(post.UserId == _userManager.GetUserId(User)
                    || User.IsInRole("Admin") )
                {
                    post.Title = requestPost.Title;

                    requestPost.Content = sanitizer.Sanitize(requestPost.Content);

                    post.Content = requestPost.Content;

                    post.Date = DateTime.Now;
                    post.CategoryId = requestPost.CategoryId;
                    TempData["message"] = "Postarea a fost modificata";
                    TempData["messageType"] = "alert-success";
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {                    
                    TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unui post care nu va apartine";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                requestPost.Categ = GetAllCategories();
                return View(requestPost);
            }
        }


        // Se sterge un articol din baza de date 
        // Utilizatorii cu rolul de Editor sau Admin pot sterge postari
        // Editorii pot sterge doar postarile publicate de ei
        // Adminii pot sterge orice articol de baza de date

        [HttpPost]
        [Authorize(Roles = "Editor,Admin, User")]
        public ActionResult Delete(int id)
        {
            // Post post = db.Posts.Find(id);

            Post post = db.Posts.Include("Comments")
                                         .Where(art => art.Id == id)
                                         .First();

            if ((post.UserId == _userManager.GetUserId(User))
                    || User.IsInRole("Admin"))
            {
                db.Posts.Remove(post);
                db.SaveChanges();
                TempData["message"] = "Postarea a fost stearsa";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["message"] = "Nu aveti dreptul sa stergeti o postare care nu va apartine";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }    
        }
        


        // Conditiile de afisare pentru butoanele de editare si stergere
        // butoanele aflate in view-uri
        private void SetAccessRights()
        {
            ViewBag.AfisareButoane = false;

            if (User.IsInRole("Editor"))
            {
                ViewBag.AfisareButoane = true;
            }

            ViewBag.UserCurent = _userManager.GetUserId(User);

            ViewBag.EsteAdmin = User.IsInRole("Admin");
        }

        [NonAction]
        public IEnumerable<SelectListItem> GetAllCategories()
        {
            // generam o lista de tipul SelectListItem fara elemente
            var selectList = new List<SelectListItem>();

            // extragem toate categoriile din baza de date
            var categories = from cat in db.Categories
                             select cat;

            // iteram prin categorii
            foreach (var category in categories)
            {
                // adaugam in lista elementele necesare pentru dropdown
                // id-ul categoriei si denumirea acesteia
                selectList.Add(new SelectListItem
                {
                    Value = category.Id.ToString(),
                    Text = category.CategoryName
                });
            }
            /* Sau se poate implementa astfel: 
             * 
            foreach (var category in categories)
            {
                var listItem = new SelectListItem();
                listItem.Value = category.Id.ToString();
                listItem.Text = category.CategoryName;

                selectList.Add(listItem);
             }*/


            // returnam lista de categorii
            return selectList;
        }

        // Metoda utilizata pentru exemplificarea Layout-ului
        // Am adaugat un nou Layout in Views -> Shared -> numit _LayoutNou.cshtml
        // Aceasta metoda are un View asociat care utilizeaza noul layout creat
        // in locul celui default generat de framework numit _Layout.cshtml
        public IActionResult IndexNou()
        {
            return View();
        }

        [HttpPost]
        [Route("Posts/ToggleLike")]
        public IActionResult ToggleLike(string userID, int postID)
        {
            var post = db.Posts.Include(p => p.Likes).FirstOrDefault(p => p.Id == postID);
            var user = db.Users.Find(userID);

            if (post == null || user == null)
            {
                return NotFound();
            }

            var existingLike = post.Likes.FirstOrDefault(l => l.UserId == userID);
            if (existingLike != null)
            {
                post.Likes.Remove(existingLike);
                post.LikesCount--;
            }
            else
            {
                post.Likes.Add(new Like
                {
                    UserId = userID,
                    PostId = postID,
                    Date = DateTime.Now
                });
                post.LikesCount++;
            }

            db.SaveChanges();

            return Redirect(Request.Headers["Referer"].ToString());
        }



    }
}
