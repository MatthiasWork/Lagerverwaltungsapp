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
        /// <param name="logger">Der Logger für den HomeController</param>
        /// <param name="context">Der Datenbankkontext der Lagerverwaltung</param>
        /// <param name="passwordService">Der Service zum Hashen und Prüfen von Passwörtern</param>
        public HomeController(ILogger<HomeController> logger, LagerverwaltungContext context, PasswordService passwordService)
        {
            _logger = logger;
            _context = context;
            _passwordService = passwordService;
        }

        /// <summary>
        /// Methode, die die Startseite anzeigt.
        /// </summary>
        /// <returns>Gibt ein IActionResult zurück</returns>
        // GET: Home
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Methode, die die Seite mit der Datenschutzerklärung anzeigt.
        /// </summary>
        /// <returns>Gibt ein IActionResult zurück</returns>
        // GET: Home/Privacy
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Methode, die die Login-Seite anzeigt.
        /// </summary>
        /// <param name="returnUrl">Die URL, zu der nach erfolgreichem Login weitergeleitet wird</param>
        /// <returns>Gibt ein IActionResult zurück</returns>
        // GET: Home/Login
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// Methode, die den Login eines Benutzers verarbeitet. Stimmen Benutzername und Passwort,
        /// wird der Benutzer per Cookie angemeldet und weitergeleitet.
        /// </summary>
        /// <param name="login">Das LoginViewModel mit Benutzername und Passwort aus dem Formular</param>
        /// <param name="returnUrl">Die URL, zu der nach erfolgreichem Login weitergeleitet wird</param>
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
        /// Methode, die die Registrierungsseite anzeigt. Bereits angemeldete Benutzer werden zur Startseite weitergeleitet.
        /// </summary>
        /// <returns>Gibt ein IActionResult zurück</returns>
        // GET: Home/Register
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Index));
            }
            return View();
        }

        /// <summary>
        /// Methode, die einen neuen Benutzer registriert, sofern der Benutzername noch nicht vergeben ist.
        /// Neue Benutzer bekommen automatisch die Rolle "LehrerIn" und werden danach direkt angemeldet.
        /// </summary>
        /// <param name="registrierung">Das RegisterViewModel mit den Daten aus dem Formular</param>
        /// <returns>Gibt eine Task zurück</returns>
        // POST: Home/Register
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel registrierung)
        {
            // Der Login sucht über den Benutzernamen, daher muss er eindeutig sein
            if (await _context.Person.AnyAsync(p => p.Username == registrierung.Username))
            {
                ModelState.AddModelError(nameof(RegisterViewModel.Username), "Dieser Benutzername ist bereits vergeben.");
            }

            if (!ModelState.IsValid)
            {
                return View(registrierung);
            }

            // Wer sich selbst registriert, darf nie Admin werden. Deshalb nur eine LehrerIn-Rolle ohne Admin-Rechte verwenden
            // und sie neu anlegen, falls sie gelöscht oder zu einer Admin-Rolle gemacht wurde.
            var lehrerInRolle = await _context.Rolle.FirstOrDefaultAsync(r => r.Name == Rolle.LehrerIn && !r.Admin)
                ?? new Rolle { Name = Rolle.LehrerIn, Admin = false };

            var person = new Person
            {
                Vorname = registrierung.Vorname,
                Nachname = registrierung.Nachname,
                Username = registrierung.Username,
                Email = registrierung.Email,
                Rolle = lehrerInRolle
            };
            person.Password = _passwordService.HashPassword(person, registrierung.Password);

            _context.Person.Add(person);
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, person.Email),
                new Claim(ClaimTypes.Sid, person.ID.ToString()),
                new Claim(ClaimTypes.Name, person.Username),
                new Claim(ClaimTypes.Surname, person.Nachname),
                new Claim(ClaimTypes.GivenName, person.Vorname),
                new Claim(ClaimTypes.Role, person.Rolle.Name)
            };

            // Keine Admin-Prüfung nötig, da neu registrierte Benutzer immer die LehrerIn-Rolle ohne Admin-Rechte bekommen
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

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
        // GET: Home/Error
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
