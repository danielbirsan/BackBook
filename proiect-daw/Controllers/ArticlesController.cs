using ArticlesApp.Data;
using ArticlesApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ArticlesApp.Controllers
{
    public class ArticlesController : Controller
    {
        private readonly ApplicationDbContext db;
        public ArticlesController(ApplicationDbContext context)
        {
            db = context;
        }

        // Se afiseaza lista tuturor articolelor impreuna cu categoria 
        // din care fac parte
        // HttpGet implicit
        public IActionResult Index()
        {
            var articles = db.Articles.Include("Category");

            // ViewBag.OriceDenumireSugestiva
            ViewBag.Articles = articles;

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
            }

            return View();
        }

        // Se afiseaza un singur articol in functie de id-ul sau 
        // impreuna cu categoria din care face parte
        // In plus sunt preluate si toate comentariile asociate unui articol
        // HttpGet implicit
        public IActionResult Show(int id)
        {
            Article article = db.Articles.Include("Category").Include("Comments")
                              .Where(art => art.Id == id)
                              .First();

            return View(article);
        }


        // Se afiseaza formularul in care se vor completa datele unui articol
        // impreuna cu selectarea categoriei din care face parte
        // HttpGet implicit

        public IActionResult New()
        {
          
            Article article = new Article();

            article.Categ = GetAllCategories();

            return View(article);
        }

        // Se adauga articolul in baza de date
        [HttpPost]
        public IActionResult New(Article article)
        {
            article.Date = DateTime.Now;
            article.Categ = GetAllCategories();

            if(string.IsNullOrEmpty(article.Title))
            {
                ModelState.AddModelError(string.Empty, "Titlul este obligatoriu");
            }
            if(string.IsNullOrEmpty(article.Content))
            {
                ModelState.AddModelError(string.Empty, "Continutul articolului este obligatoriu");
            }
            if (article.CategoryId == 0)
            {
                ModelState.AddModelError(string.Empty, "Categoria este obligatorie");
            }

            Console.WriteLine(article.Content); 

            if (ModelState.IsValid)
            {
                db.Articles.Add(article);
                db.SaveChanges();
                TempData["message"] = "Articolul a fost adaugat";
                return RedirectToAction("Index");
            }
            else
            {
                return View(article);
            }
        }

        // Se editeaza un articol existent in baza de date impreuna cu categoria din care face parte
        // Categoria se selecteaza dintr-un dropdown
        // HttpGet implicit
        // Se afiseaza formularul impreuna cu datele aferente articolului din baza de date
        public IActionResult Edit(int id)
        {

            Article article = db.Articles.Include("Category")
                                         .Where(art => art.Id == id)
                                         .First();

            article.Categ = GetAllCategories();

            return View(article);

        }

        // Se adauga articolul modificat in baza de date
        [HttpPost]
        public IActionResult Edit(int id, Article requestArticle)
        {
            Article article = db.Articles.Find(id);

            if (string.IsNullOrEmpty(article.Title))
            {
                ModelState.AddModelError("", "Titlul este obligatoriu");
            }
            if (string.IsNullOrEmpty(article.Content))
            {
                ModelState.AddModelError("", "Continutul articolului este obligatoriu");
            }
            if(article.CategoryId == 0)
            {
                ModelState.AddModelError("", "Categoria este obligatorie");
            }

            if(ModelState.IsValid)
            {
                article.Title = requestArticle.Title;
                article.Content = requestArticle.Content;
                article.Date = DateTime.Now;
                article.CategoryId = requestArticle.CategoryId;
                db.SaveChanges();
                TempData["message"] = "Articolul a fost modificat";
                return RedirectToAction("Index");

            }
            else
            {
                requestArticle.Categ = GetAllCategories();
                return View(requestArticle);
            }
        }

        // Adaugarea unui comentariu asociat unui articol in baza de date
        [HttpPost]
        public IActionResult Show([FromForm] Comment comm)
        {
            comm.Date = DateTime.Now;

            if (string.IsNullOrEmpty(comm.Content))
            {
                ModelState.AddModelError("Content", "Mesajul nu poate fi gol.");
            }

            if (ModelState.IsValid)
            {
                db.Comments.Add(comm);
                db.SaveChanges();
                return Redirect("/Articles/Show/" + comm.ArticleId);
            }
            else
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }

                Article art = db.Articles.Include("Category").Include("Comments")
                              .Where(art => art.Id == comm.ArticleId)
                              .First();

                return View(art);
            }

        }


        // Se sterge un articol din baza de date 
        [HttpPost]
        public ActionResult Delete(int id)
        {
            Article article = db.Articles.Find(id);
            db.Articles.Remove(article);
            db.SaveChanges();
            TempData["message"] = "Articolul a fost sters";
            return RedirectToAction("Index");
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
    }
}
