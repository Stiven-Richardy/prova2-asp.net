using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Prova2.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Creditos()
        {
            return View();
        }
    }
}