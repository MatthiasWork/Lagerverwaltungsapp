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
        /// <param name="ansicht">"Default" für die Seitenleiste (Desktop), "TabLeiste" für die Tab-Leiste unten (Mobil)</param>
        /// <returns>Gibt eine Task zurück</returns>
        public async Task<IViewComponentResult> InvokeAsync(string ansicht = "Default")
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
                // Offene Bewegungen, die die Person freigeben muss: Transfers in den eigenen Raum und aus ihm geholte Geräte
                menue.OffeneUebernahmen = await _context.Lagerbewegung
                    .Where(Lagerbewegung.IstOffen)
                    .Where(Lagerbewegung.FreigabeFuer(raumID))
                    .CountAsync();
            }

            return View(ansicht, menue);
        }
    }
}
