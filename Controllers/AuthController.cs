using Microsoft.AspNetCore.Mvc;

namespace TextClassification.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Login()
        {
            return View(); // Views/Auth/Login.cshtml
        }

        public IActionResult Register()
        {
            return View(); // Views/Auth/Register.cshtml
        }
    }
}