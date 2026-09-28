using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Claims;


namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        
        private readonly IPasswordHasher<Person> _passwordHasher;
        
        public HomeController(ILogger<HomeController> logger, IPasswordHasher<Person> passwordHasher)
        {
            _logger = logger;
            _passwordHasher = passwordHasher;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        public async Task<IActionResult> Login()
        {
            Person neuerNutzer = new Person();
            string pepper = "AlleMeineEntchen";
            string password = neuerNutzer.Password + pepper;
            string hash = _passwordHasher.HashPassword(neuerNutzer, password);

            if(true)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email, neuerNutzer.Email),
                    new Claim(ClaimTypes.Sid, neuerNutzer.ID.ToString()),
                    new Claim(ClaimTypes.Name, neuerNutzer.Username),
                    new Claim(ClaimTypes.Surname, neuerNutzer.Nachname),
                    new Claim(ClaimTypes.GivenName, neuerNutzer.Vorname)
                    //new Claim(ClaimTypes.Role, neuerNutzer.RolleID.ToString())
                };
                ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                await HttpContext.SignInAsync(claimsPrincipal);
            }

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
