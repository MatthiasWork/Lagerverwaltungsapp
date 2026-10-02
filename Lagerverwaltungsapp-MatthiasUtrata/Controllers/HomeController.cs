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
        /// Methode, die die Übersicht (Startseite) mit Kennzahlen, dem Bestand je Kategorie und je Raum und den letzten Buchungen anzeigt.
        /// Admins sehen alle Buchungen, alle anderen nur ihre eigenen und die ihres Raums.
        /// </summary>
        /// <returns>Gibt eine Task zurück</returns>
        // GET: Home
        public async Task<IActionResult> Index()
        {
            var personID = User.GetPersonID();
            var istAdmin = User.IsInRole("Admin");
            var raumID = await _lagerService.RaumDerPersonAsync(personID);

            var offen = _context.Lagerbewegung.Where(Lagerbewegung.IstOffen);
            var uebersicht = new UebersichtViewModel
            {
                Verfuegbar = await _context.Raumbestand.SumAsync(r => r.Menge)
            };
            // Unterwegs ist, was schon abgebucht, aber noch nicht übernommen wurde
            uebersicht.Gesamt = uebersicht.Verfuegbar + await offen.SumAsync(l => l.Menge);

            // Statistiken: die verfügbaren Stück aus "Verfügbar" nach Kategorie und nach Raum aufgeteilt, wie die Kennzahl für alle gleich
            uebersicht.BestandJeKategorie = await _context.Raumbestand
                .GroupBy(r => new { r.Gegenstand.KategorieID, r.Gegenstand.Kategorie.Name })
                .Where(g => g.Sum(r => r.Menge) > 0)
                .OrderByDescending(g => g.Sum(r => r.Menge))
                .ThenBy(g => g.Key.Name)
                .Select(g => new KategorieStatistik { KategorieID = g.Key.KategorieID, Name = g.Key.Name, Stueck = g.Sum(r => r.Menge) })
                .ToListAsync();

            uebersicht.BestandJeRaum = await _context.Raumbestand
                .GroupBy(r => new { r.RaumID, Raumart = r.Raum.Raumart.Name })
                .Where(g => g.Sum(r => r.Menge) > 0)
                .OrderByDescending(g => g.Sum(r => r.Menge))
                .ThenBy(g => g.Key.RaumID)
                .Select(g => new RaumStatistik { RaumID = g.Key.RaumID, Raumart = g.Key.Raumart, Stueck = g.Sum(r => r.Menge) })
                .ToListAsync();

            // Admins sehen alle offenen Freigaben, alle anderen nur die für ihren Raum (ohne Raum keine)
            if (istAdmin)
            {
                uebersicht.OffeneFreigaben = await offen.CountAsync();
            }
            else if (raumID != null)
            {
                uebersicht.OffeneFreigaben = await offen.Where(Lagerbewegung.FreigabeFuer(raumID)).CountAsync();
            }

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
                var person = l.Person.VollerName;
                // Bei einer Korrektur ist die Menge die Änderung und kann negativ sein, die Richtung steht im Text
                var menge = Math.Abs(l.Menge);
                string was;
                if (menge == 1)
                {
                    was = l.Gegenstand.Name;
                }
                else
                {
                    was = $"{menge} × {l.Gegenstand.Name}";
                }

                if (l.IstKorrektur)
                {
                    string text;
                    if (l.Menge > 0)
                    {
                        text = $"{person} hat {was} in {l.NachRaumID} eingebucht (Korrektur)";
                    }
                    else
                    {
                        text = $"{person} hat {was} aus {l.NachRaumID} ausgebucht (Korrektur)";
                    }
                    uebersicht.Aktivitaeten.Add(new Aktivitaet { Zeitpunkt = l.ErstelltAm, Text = text });
                    continue;
                }

                // Zuerst prüfen: Eine stornierte Bewegung hat in BestaetigtAm den Zeitpunkt der Stornierung
                if (!l.Storniert && l.BestaetigtAm == l.ErstelltAm)
                {
                    // Sofort bestätigt: Wareneingang oder Umbuchung zwischen zwei eigenen Räumen
                    string text;
                    if (l.VonRaumID == l.NachRaumID)
                    {
                        text = $"{person} hat {was} in {l.NachRaumID} eingebucht";
                    }
                    else
                    {
                        text = $"{person} hat {was} von {l.VonRaumID} nach {l.NachRaumID} umgebucht";
                    }
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
        /// Methode, die die Login-Seite anzeigt. Gibt es noch keinen Benutzer, wird stattdessen zur Registrierung
        /// weitergeleitet, damit sich der erste Benutzer als Admin registrieren kann.
        /// </summary>
        /// <param name="returnUrl">Die URL, zu der nach erfolgreichem Login weitergeleitet wird</param>
        /// <returns>Gibt eine Task zurück</returns>
        // GET: Home/Login
        [AllowAnonymous]
        public async Task<IActionResult> Login(string? returnUrl)
        {
            if (!await _context.Person.AnyAsync())
            {
                return RedirectToAction(nameof(Register));
            }

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

            await AnmeldenAsync(person);

            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Methode, die die Registrierungsseite anzeigt. Registrieren kann sich nur der erste Benutzer, solange es noch keinen gibt;
        /// danach legt ein Admin alle weiteren Benutzer an und es wird zum Login weitergeleitet.
        /// </summary>
        /// <returns>Gibt eine Task zurück</returns>
        // GET: Home/Register
        [AllowAnonymous]
        public async Task<IActionResult> Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Index));
            }

            if (await _context.Person.AnyAsync())
            {
                return RedirectToAction(nameof(Login));
            }
            return View();
        }

        /// <summary>
        /// Methode, die den ersten Benutzer als Admin registriert und danach direkt anmeldet.
        /// Gibt es schon einen Benutzer, wird nichts angelegt und zum Login weitergeleitet.
        /// </summary>
        /// <param name="registrierung">Das RegisterViewModel mit den Daten aus dem Formular</param>
        /// <returns>Gibt eine Task zurück</returns>
        // POST: Home/Register
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel registrierung)
        {
            // Auch hier prüfen, sonst könnte man sich mit einem direkt abgeschickten Formular später noch als Admin registrieren
            if (await _context.Person.AnyAsync())
            {
                return RedirectToAction(nameof(Login));
            }

            if (!ModelState.IsValid)
            {
                return View(registrierung);
            }

            // Der erste Benutzer wird Admin, damit er alle weiteren Benutzer anlegen kann. Gibt es noch keine Admin-Rolle, wird sie angelegt
            var adminRolle = await _context.Rolle.FirstOrDefaultAsync(r => r.Admin)
                ?? new Rolle { Name = "Administrator", Admin = true };

            var person = new Person
            {
                Vorname = registrierung.Vorname,
                Nachname = registrierung.Nachname,
                Username = registrierung.Username,
                Email = registrierung.Email,
                Rolle = adminRolle
            };
            person.Password = _passwordService.HashPassword(person, registrierung.Password);

            _context.Person.Add(person);
            await _context.SaveChangesAsync();

            await AnmeldenAsync(person);

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
        /// Methode, die eine Person per Cookie anmeldet (nach dem Login und nach der Registrierung).
        /// Die ID steht im Sid-Claim (siehe ClaimsPrincipalExtensions.GetPersonID); wer eine Admin-Rolle hat,
        /// bekommt zusätzlich die Rolle "Admin".
        /// </summary>
        /// <param name="person">Die Person, die angemeldet wird; ihre Rolle muss geladen sein</param>
        /// <returns>Gibt eine Task zurück</returns>
        private async Task AnmeldenAsync(Person person)
        {
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

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);
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
