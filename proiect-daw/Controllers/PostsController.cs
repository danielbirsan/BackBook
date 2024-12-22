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
            var currentUserId = _userManager.GetUserId(User);

            // Get the IDs of users the current user is following
            var followingUserIds = db.FollowRequests
                .Where(fr => fr.SenderId == currentUserId && !fr.PendingApproval)
                .Select(fr => fr.ReceiverId)
                .ToList();

            // Include the current user in the following list to display their own posts
            followingUserIds.Add(currentUserId);

            // Fetch posts only from public profiles or followed users
            var postari = db.Posts.Include("Category")
                                  .Include("User")
                                  .Where(post => post.User.PrivateProfile == false || followingUserIds.Contains(post.User.Id))
                                  .OrderByDescending(a => a.Date);

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            // Search functionality
            var search = "";

            if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
            {
                search = Convert.ToString(HttpContext.Request.Query["search"]).Trim();

                List<int> postIds = db.Posts.Where(at => at.Title.Contains(search) || at.Content.Contains(search))
                                             .Select(a => a.Id)
                                             .ToList();

                List<int> postIdsOfCommentsWithSearchString = db.Comments
                                                                .Where(c => c.Content.Contains(search))
                                                                .Select(c => (int)c.PostId)
                                                                .ToList();

                List<int> mergedIds = postIds.Union(postIdsOfCommentsWithSearchString).ToList();

                postari = postari.Where(post => mergedIds.Contains(post.Id)).OrderByDescending(a => a.Date);
            }


            ViewBag.SearchString = search;

            // Pagination logic
            int _perPage = 3;
            int totalItems = postari.Count();
            var pageQuery = HttpContext.Request.Query["page"].ToString();
            int currentPage = 1;

            if (!string.IsNullOrEmpty(pageQuery) && int.TryParse(pageQuery, out int parsedPage))
            {
                currentPage = parsedPage;
            }

            var offset = (currentPage - 1) * _perPage;
            var paginatedPosts = postari.Skip(offset).Take(_perPage).ToList();
            if (paginatedPosts == null || !paginatedPosts.Any())
            {
                ViewBag.Message = "No posts available.";
                ViewBag.Posts = new List<Post>();
            }

            ViewBag.lastPage = Math.Ceiling((float)totalItems / (float)_perPage);
            ViewBag.Posts = paginatedPosts;

            ViewBag.PaginationBaseUrl = search != ""
                ? $"/Posts/Index/?search={search}&page"
                : "/Posts/Index/?page";

            // Likes data preparation
            var postsList = db.Posts.Include(p => p.Likes).ToList();
            var postLikes = new Dictionary<int, bool>();
            var likedPhotos = new List<string>();

            ViewBag.UserCurent = currentUserId;

            foreach (var post in postsList)
            {
                var hasLiked = post.Likes.Any(like => like.UserId == currentUserId);
                postLikes[post.Id] = hasLiked;
                ViewData[$"Likes_{post.Id}"] = hasLiked;

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

        public Category GetDefaultCategory()
        {
            return db.Categories.FirstOrDefault() ?? new Category { CategoryName = "Default" };
        }

        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult New(Post post)
        {
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedTags.Add("img");
            sanitizer.AllowedAttributes.Add("src");
            sanitizer.AllowedAttributes.Add("alt");


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


        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Edit(int id)
        {
            var post = db.Posts.Include("Category").FirstOrDefault(p => p.Id == id);

            if (post == null)
            {
                TempData["message"] = "Postarea nu a fost găsită.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            if (!IsAuthorizedToEditOrDelete(post))
            {
                TempData["message"] = "Nu aveți permisiunea să modificați această postare.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            post.Categ = GetAllCategories();
            return View(post);
        }

        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Edit(int id, Post updatedPost)
        {
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedTags.Add("img");
            sanitizer.AllowedAttributes.Add("src");
            sanitizer.AllowedAttributes.Add("alt");

            var post = db.Posts.Find(id);
            if (post == null)
            {
                TempData["message"] = "Postarea nu a fost găsită.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            if (!IsAuthorizedToEditOrDelete(post))
            {
                TempData["message"] = "Nu aveți permisiunea să modificați această postare.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                post.Title = updatedPost.Title;
                post.Content = sanitizer.Sanitize(updatedPost.Content);
                post.Date = DateTime.Now;
                post.CategoryId = updatedPost.CategoryId;

                db.SaveChanges();

                TempData["message"] = "Postarea a fost modificată cu succes.";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }

            updatedPost.Categ = GetAllCategories();
            return View(updatedPost);
        }

        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public ActionResult Delete(int id)
        {
            var post = db.Posts.Include("Comments").FirstOrDefault(p => p.Id == id);

            if (post == null)
            {
                TempData["message"] = "Postarea nu a fost găsită.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            if (!IsAuthorizedToEditOrDelete(post))
            {
                TempData["message"] = "Nu aveți permisiunea să ștergeți această postare.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            db.Posts.Remove(post);
            db.SaveChanges();

            TempData["message"] = "Postarea a fost ștearsă cu succes.";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Index");
        }

        private bool IsAuthorizedToEditOrDelete(Post post)
        {
            var currentUserId = _userManager.GetUserId(User);
            return post.UserId == currentUserId || User.IsInRole("Admin");
        }

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
  
            return selectList;
        }

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
        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public async Task<IActionResult> UploadImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest(new { error = "Invalid image file." });

            try
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                var imageUrl = Url.Content("~/uploads/" + uniqueFileName);
                return Json(new { imageUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while uploading the image.", details = ex.Message });
            }
        }


    }
}
