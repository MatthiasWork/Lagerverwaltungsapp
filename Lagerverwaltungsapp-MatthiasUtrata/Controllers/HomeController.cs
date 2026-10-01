using Lagerverwaltungsapp_MatthiasUtrata.Extensions;
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

        private readonly LagerService _lagerService;

        /// <summary>
        /// Konstruktor der HomeController-Klasse.
        /// </summary>
        /// <param name="logger">Der Logger für den HomeController</param>
        /// <param name="context">Der Datenbankkontext der Lagerverwaltung</param>
        /// <param name="passwordService">Der Service zum Hashen und Prüfen von Passwörtern</param>
        /// <param name="lagerService">Der Service, der die Zuständigkeiten für Räume kennt</param>
        public HomeController(ILogger<HomeController> logger, LagerverwaltungContext context, PasswordService passwordService, LagerService lagerService)
        {
            _logger = logger;
            _context = context;
            _passwordService = passwordService;
            _lagerService = lagerService;
        }

        /// <summary>
        /// Methode, die die Übersicht (Startseite) mit Kennzahlen und den letzten Buchungen anzeigt.
        /// Admins sehen alle Buchungen, alle anderen nur ihre eigenen und die ihres Raums.
        /// </summary>
        /// <returns>Gibt eine Task zurück</returns>
        // GET: Home
        public async Task<IActionResult> Index()
        {
            var personID = User.GetPersonID();
            var istAdmin = User.IsInRole("Admin");
            var raumID = personID == null ? null : await _lagerService.RaumDerPersonAsync(personID.Value);

            var offen = _context.Lagerbewegung.Where(Lagerbewegung.IstOffen);
            var uebersicht = new UebersichtViewModel
            {
                Verfuegbar = await _context.Raumbestand.SumAsync(r => r.Menge),
                OffeneFreigaben = istAdmin ? await offen.CountAsync()
                    : raumID != null ? await offen.CountAsync(l => l.NachRaumID == raumID)
                    : 0
            };
            // Unterwegs ist, was schon abgebucht, aber noch nicht übernommen wurde
            uebersicht.Gesamt = uebersicht.Verfuegbar + await offen.SumAsync(l => l.Menge);

            var bewegungen = _context.Lagerbewegung.AsQueryable();
            if (!istAdmin)
            {
                bewegungen = bewegungen.Where(l => l.PersonID == personID || l.VonRaumID == raumID || l.NachRaumID == raumID);
            }

            // Jede Bewegung ergibt bis zu zwei Einträge (angefragt, dann übernommen oder storniert). Die 5 neuesten Einträge
            // stammen daher sicher aus den 5 Bewegungen mit dem neuesten Zeitpunkt
            var letzte = await bewegungen
                .Include(l => l.Person)
                .Include(l => l.Gegenstand)
                .Include(l => l.Bewegungsart)
                .OrderByDescending(l => l.BestaetigtAm ?? l.ErstelltAm)
                .Take(5)
                .ToListAsync();

            foreach (var l in letzte)
            {
                var person = $"{l.Person.Vorname} {l.Person.Nachname}";
                var was = l.Menge == 1 ? l.Gegenstand.Name : $"{l.Menge} × {l.Gegenstand.Name}";

                if (!l.Storniert && l.BestaetigtAm == l.ErstelltAm)
                {
                    // Sofort bestätigt: Wareneingang oder Umbuchung zwischen zwei eigenen Räumen
                    var text = l.VonRaumID == l.NachRaumID
                        ? $"{person} hat {was} in {l.NachRaumID} eingebucht"
                        : $"{person} hat {was} von {l.VonRaumID} nach {l.NachRaumID} umgebucht";
                    uebersicht.Aktivitaeten.Add(new Aktivitaet { Zeitpunkt = l.ErstelltAm, Text = text });
                    continue;
                }

                uebersicht.Aktivitaeten.Add(new Aktivitaet { Zeitpunkt = l.ErstelltAm, Text = $"{person} fragt Transfer von {was} nach {l.NachRaumID} an" });
                if (l.Storniert)
                {
                    uebersicht.Aktivitaeten.Add(new Aktivitaet { Zeitpunkt = l.BestaetigtAm!.Value, Text = $"Transfer von {was} nach {l.NachRaumID} storniert, zurück in {l.VonRaumID}" });
                }
                else if (l.BestaetigtAm != null)
                {
                    uebersicht.Aktivitaeten.Add(new Aktivitaet { Zeitpunkt = l.BestaetigtAm.Value, Text = $"{was} in {l.NachRaumID} übernommen" });
                }
            }

            uebersicht.Aktivitaeten = uebersicht.Aktivitaeten.OrderByDescending(a => a.Zeitpunkt).Take(5).ToList();

            return View(uebersicht);
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
