using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace PhoStudioMVC.Controllers
{
    public class HomeController : Controller
    {
        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Services()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Portfolio()
        {
            return View();
        }
    }
}
