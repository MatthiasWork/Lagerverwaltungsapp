using Lagerverwaltungsapp_MatthiasUtrata.Extensions;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Lagerverwaltungsapp_MatthiasUtrata.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltungsapp_MatthiasUtrata.ViewComponents
{
    // Hauptmenü im Layout: welche Menüpunkte jemand sieht, hängt von der Rolle (Admin) und davon ab, ob die Person einen Raum hat
    public class MenueViewComponent : ViewComponent
    {
        private readonly LagerverwaltungContext _context;
        private readonly LagerService _lagerService;

        /// <summary>
        /// Konstruktor der MenueViewComponent-Klasse.
        /// </summary>
        /// <param name="context">Der Datenbankkontext der Lagerverwaltung</param>
        /// <param name="lagerService">Der Service, der die Zuständigkeiten für Räume kennt</param>
        public MenueViewComponent(LagerverwaltungContext context, LagerService lagerService)
        {
            _context = context;
            _lagerService = lagerService;
        }

        /// <summary>
        /// Methode, die die Menüpunkte und Badge-Zahlen für die angemeldete Person ermittelt.
        /// Die Zuständigkeit wird bei jedem Seitenaufruf neu gelesen, damit eine geänderte Zuständigkeit sofort gilt.
        /// </summary>
        /// <returns>Gibt eine Task zurück</returns>
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var personID = UserClaimsPrincipal.GetPersonID();
            if (personID == null)
            {
                // Nicht angemeldet (z. B. auf der Login-Seite): kein Menü
                return Content(string.Empty);
            }

            var raumID = await _lagerService.RaumDerPersonAsync(personID.Value);

            var menue = new MenueViewModel
            {
                IstAdmin = UserClaimsPrincipal.IsInRole("Admin"),
                RaumID = raumID
            };

            if (raumID != null)
            {
                // Offene Bewegungen in den eigenen Raum, die die Person daher bestätigen muss
                menue.OffeneUebernahmen = await _context.Lagerbewegung
                    .CountAsync(l => l.BestaetigtAm == null && l.NachRaumID == raumID);
            }

            return View(menue);
        }
    }
}
