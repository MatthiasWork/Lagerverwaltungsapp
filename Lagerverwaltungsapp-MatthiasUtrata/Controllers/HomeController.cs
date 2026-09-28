using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Lagerverwaltungsapp_MatthiasUtrata.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;


namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly LagerverwaltungContext _context;

        private readonly PasswordService _passwordService;

        /// <summary>
        /// Konstruktor der HomeController-Klasse.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        /// <param name="passwordService"></param>
        public HomeController(ILogger<HomeController> logger, LagerverwaltungContext context, PasswordService passwordService)
        {
            _logger = logger;
            _context = context;
            _passwordService = passwordService;
        }

        /// <summary>
        /// Methode, die die Login-Seite anzeigt.
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Methode, 
        /// </summary>
        /// <returns>Gibt ein IActionResult zurück</returns>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Methode, die überprüft, ob das eingegebene Passwort mit dem gespeicherten Passwort übereinstimmt.
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns>Gibt ein IActionResult zurück</returns>
        // GET: Home/Login
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// Methode, die den Login eines Benutzers verarbeitet.
        /// </summary>
        /// <param name="login"></param>
        /// <param name="returnUrl"></param>
        /// <returns>Gibt eine Task zurück</returns>
        // POST: Home/Login
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel login, string? returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(login);
            }

            var person = await _context.Person
                .Include(p => p.Rolle)
                .FirstOrDefaultAsync(p => p.Username == login.Username);

            if (person == null || !_passwordService.VerifyPassword(person, login.Password))
            {
                ModelState.AddModelError(string.Empty, "Benutzername oder Passwort ist falsch.");
                return View(login);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, person.Email),
                new Claim(ClaimTypes.Sid, person.ID.ToString()),
                new Claim(ClaimTypes.Name, person.Username),
                new Claim(ClaimTypes.Surname, person.Nachname),
                new Claim(ClaimTypes.GivenName, person.Vorname),
                new Claim(ClaimTypes.Role, person.Rolle.Name)
            };

            if (person.Rolle.Admin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Methode, die den Logout eines Benutzers verarbeitet.
        /// </summary>
        /// <returns>Gibt eine Task zurück</returns>
        // POST: Home/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        /// <summary>
        /// Methode, die die View für Zugriffsverweigerung zurückgibt.
        /// </summary>
        /// <returns>Gibt ein IActionResult zurück</returns>
        // GET: Home/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }

        /// <summary>
        /// Methode, die die View für Fehlermeldungen zurückgibt.
        /// </summary>
        /// <returns>Gibt ein IActionResult zurück</returns>
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
