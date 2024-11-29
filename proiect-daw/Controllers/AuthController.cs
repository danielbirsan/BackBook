using Microsoft.AspNetCore.Mvc;

namespace proiect_daw.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
