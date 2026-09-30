using System.Data;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltungsapp_MatthiasUtrata.Services
{
    /// <summary>
    /// Kern der Geschäftslogik der Lagerverwaltung: Lagerbewegungen anlegen und bestätigen und dabei den Raumbestand führen.
    /// Wer eine Bewegung anlegen oder bestätigen darf, hängt nur von Raum.PersonID ab, nicht von der Admin-Rolle,
    /// der Raumart oder der Bewegungsart.
    ///
    /// Zuständigkeitsregel: Anlegen darf eine Bewegung nur die für den Von-Raum verantwortliche Person,
    /// bestätigen nur die für den Nach-Raum verantwortliche Person.
    ///
    /// Bestandsregel: Beim Anlegen wird im Von-Raum sofort abgebucht, beim Bestätigen im Nach-Raum zugebucht.
    /// Dazwischen ist die Menge "unterwegs" (offene Lagerbewegung, BestaetigtAm = null).
    /// Sonderfall Wareneingang (Von-Raum = Nach-Raum): Es wird nichts abgebucht, nur zugebucht.
    ///
    /// Seriennummerregel: Nur ein Gegenstand mit Seriennummer wird als einzelnes Gerät gebucht (immer Menge 1, nur an einem Ort).
    /// Alles ohne Seriennummer, auch ein einzelnes Gerät ohne Seriennummer, wird über die Menge geführt.
    ///
    /// Die Buchungsmethoden geben wie LoeschHindernisAsync in den Controllern eine Fehlermeldung zurück oder null, wenn alles geklappt hat.
    /// Gespeichert wird immer über BuchenAsync (eine Transaktion, ein SaveChangesAsync).
    /// </summary>
    public class LagerService
    {
        private readonly LagerverwaltungContext _context;

        /// <summary>
        /// Konstruktor für den LagerService.
        /// </summary>
        /// <param name="context">Der Datenbankkontext der Lagerverwaltung</param>
        public LagerService(LagerverwaltungContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Methode, die überprüft, ob eine Person für einen Raum verantwortlich ist.
        /// Es wird immer die aktuelle Zuständigkeit aus der Datenbank verwendet.
        /// </summary>
        /// <param name="raumID">Die ID des Raums</param>
        /// <param name="personID">Die ID der Person</param>
        /// <returns>Gibt True oder False zurück</returns>
        public async Task<bool> IstVerantwortlichAsync(string raumID, int personID)
        {
            return await _context.Raum.AnyAsync(r => r.ID == raumID && r.PersonID == personID);
        }

        /// <summary>
        /// Methode, die den Raum ermittelt, für den eine Person verantwortlich ist (z. B. für das Menü).
        /// Eine Person ist für höchstens einen Raum verantwortlich.
        /// </summary>
        /// <param name="personID">Die ID der Person</param>
        /// <returns>Die ID ihres Raums oder null, wenn sie für keinen Raum verantwortlich ist</returns>
        public async Task<string?> RaumDerPersonAsync(int personID)
        {
            return await _context.Raum
                .Where(r => r.PersonID == personID)
                .Select(r => r.ID)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Methode, die sucht, ob eine Person schon für einen anderen Raum verantwortlich ist.
        /// Wird vor dem Zuweisen eines Raums aufgerufen, da eine Person nur für einen Raum verantwortlich sein kann.
        /// </summary>
        /// <param name="personID">Die ID der Person</param>
        /// <param name="raumID">Die ID des Raums, der der Person zugewiesen werden soll</param>
        /// <returns>Die ID des anderen Raums oder null, wenn die Person noch keinen anderen Raum hat</returns>
        public async Task<string?> AndererRaumAsync(int personID, string raumID)
        {
            return await _context.Raum
                .Where(r => r.PersonID == personID && r.ID != raumID)
                .Select(r => r.ID)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Methode, die einen Wareneingang bucht: Gegenstände kommen von außen in einen Raum.
        /// Da VonRaumID in der Datenbank Pflicht ist, sind Von- und Nach-Raum derselbe Raum. Es wird daher nichts abgebucht,
        /// und die Lagerbewegung ist sofort bestätigt, da dieselbe Person für Von- und Nach-Raum verantwortlich ist.
        /// </summary>
        /// <param name="gegenstandID">Die ID des Gegenstands, der eingeht</param>
        /// <param name="menge">Die Menge, die eingeht (bei einem Gerät mit Seriennummer immer 1)</param>
        /// <param name="raumID">Die ID des Raums, in den der Gegenstand eingeht</param>
        /// <param name="bewegungsartID">Die ID der Bewegungsart, die die Lagerbewegung beschreibt</param>
        /// <param name="personID">Die ID der angemeldeten Person, die den Wareneingang bucht</param>
        /// <returns>Die Fehlermeldung oder null, wenn der Wareneingang gebucht wurde</returns>
        public async Task<string?> WareneingangAsync(int gegenstandID, int menge, string raumID, int bewegungsartID, int personID)
        {
            return await BewegungAnlegenAsync(gegenstandID, menge, raumID, raumID, bewegungsartID, personID);
        }

        /// <summary>
        /// Methode, die eine Lagerbewegung anlegt und die Menge sofort im Von-Raum abbucht.
        /// Die Bewegung bleibt offen, bis die für den Nach-Raum verantwortliche Person sie bestätigt (siehe BestaetigenAsync).
        /// Ist dieselbe Person für beide Räume verantwortlich, gilt sie sofort als bestätigt.
        /// Sind Von- und Nach-Raum derselbe Raum, ist es ein Wareneingang (siehe WareneingangAsync).
        /// </summary>
        /// <param name="gegenstandID">Die ID des Gegenstands, der bewegt wird</param>
        /// <param name="menge">Die Menge, die bewegt wird (bei einem Gerät mit Seriennummer immer 1)</param>
        /// <param name="vonRaumID">Die ID des Raums, aus dem der Gegenstand kommt</param>
        /// <param name="nachRaumID">Die ID des Raums, in den der Gegenstand kommt</param>
        /// <param name="bewegungsartID">Die ID der Bewegungsart, die die Lagerbewegung beschreibt</param>
        /// <param name="personID">Die ID der angemeldeten Person, die die Bewegung anlegt</param>
        /// <returns>Die Fehlermeldung oder null, wenn die Lagerbewegung angelegt wurde</returns>
        public async Task<string?> BewegungAnlegenAsync(int gegenstandID, int menge, string vonRaumID, string nachRaumID, int bewegungsartID, int personID)
        {
            return await BuchenAsync(async () =>
            {
                if (menge < 1)
                {
                    return "Die Menge muss mindestens 1 sein.";
                }

                var gegenstand = await _context.Gegenstand.FindAsync(gegenstandID);
                if (gegenstand == null)
                {
                    return "Diesen Gegenstand gibt es nicht.";
                }

                // Ein Gerät mit Seriennummer gibt es genau einmal, daher wird es immer einzeln gebucht
                if (gegenstand.Seriennummer != null && menge != 1)
                {
                    return $"\"{gegenstand.Name}\" ({gegenstand.Seriennummer}) ist ein einzelnes Gerät und kann nur mit der Menge 1 gebucht werden.";
                }

                if (!await _context.Bewegungsart.AnyAsync(b => b.ID == bewegungsartID))
                {
                    return "Bitte eine Bewegungsart auswählen.";
                }

                var vonRaum = await _context.Raum.FindAsync(vonRaumID);
                var nachRaum = await _context.Raum.FindAsync(nachRaumID);
                if (vonRaum == null || nachRaum == null)
                {
                    return "Diesen Raum gibt es nicht.";
                }

                // Zuständigkeitsregel: Anlegen darf nur die Person, die für den Von-Raum verantwortlich ist (auch ein Admin nicht)
                if (vonRaum.PersonID != personID)
                {
                    return $"Nur die für den Raum {vonRaum.ID} verantwortliche Person darf Gegenstände aus diesem Raum buchen.";
                }

                // Ohne verantwortliche Person könnte niemand die Übernahme bestätigen und die Menge bliebe für immer unterwegs
                if (nachRaum.PersonID == null)
                {
                    return $"Für den Raum {nachRaum.ID} ist niemand verantwortlich, der die Übernahme bestätigen könnte. Weise dem Raum zuerst eine verantwortliche Person zu.";
                }

                // Die IDs der geladenen Räume verwenden, da SQL Server bei der Suche nicht auf Groß-/Kleinschreibung achtet
                if (vonRaum.ID == nachRaum.ID)
                {
                    // Wareneingang: nichts abbuchen, aber ein Gerät mit Seriennummer darf es danach nicht zweimal geben
                    if (gegenstand.Seriennummer != null)
                    {
                        var hindernis = await GeraetVorhandenAsync(gegenstand);
                        if (hindernis != null)
                        {
                            return hindernis;
                        }
                    }
                }
                else
                {
                    // Sofort abbuchen, damit dieselbe Menge nicht ein zweites Mal gebucht werden kann, solange sie unterwegs ist.
                    // Das ist die letzte Prüfung, danach wird nur noch gespeichert
                    var fehler = await AbbuchenAsync(gegenstand, vonRaum.ID, menge);
                    if (fehler != null)
                    {
                        return fehler;
                    }
                }

                var jetzt = DateTime.Now;
                var lagerbewegung = new Lagerbewegung
                {
                    Menge = menge,
                    ErstelltAm = jetzt,
                    BewegungsartID = bewegungsartID,
                    VonRaumID = vonRaum.ID,
                    NachRaumID = nachRaum.ID,
                    GegenstandID = gegenstand.ID,
                    PersonID = personID
                };
                _context.Lagerbewegung.Add(lagerbewegung);

                // Ist die Person auch für den Nach-Raum verantwortlich (beim Wareneingang immer), müsste sie sich die Bewegung
                // selbst bestätigen. Daher gilt sie sofort als bestätigt und wird gleich im Nach-Raum zugebucht
                if (nachRaum.PersonID == personID)
                {
                    lagerbewegung.BestaetigtAm = jetzt;
                    await ZubuchenAsync(gegenstand.ID, nachRaum.ID, menge);
                }

                return null;
            });
        }

        /// <summary>
        /// Methode, mit der die für den Nach-Raum verantwortliche Person eine offene Lagerbewegung bestätigt (digitale Übernahme).
        /// Erst jetzt wird die Menge im Nach-Raum zugebucht.
        /// </summary>
        /// <param name="lagerbewegungID">Die ID der Lagerbewegung, die bestätigt werden soll</param>
        /// <param name="personID">Die ID der angemeldeten Person, die bestätigt</param>
        /// <returns>Die Fehlermeldung oder null, wenn die Lagerbewegung bestätigt wurde</returns>
        public async Task<string?> BestaetigenAsync(int lagerbewegungID, int personID)
        {
            return await BuchenAsync(async () =>
            {
                var lagerbewegung = await _context.Lagerbewegung
                    .Include(l => l.NachRaum)
                    .FirstOrDefaultAsync(l => l.ID == lagerbewegungID);
                if (lagerbewegung == null)
                {
                    return "Diese Lagerbewegung gibt es nicht.";
                }

                // Sonst würde dieselbe Menge ein zweites Mal zugebucht
                if (lagerbewegung.BestaetigtAm != null)
                {
                    return $"Diese Lagerbewegung wurde bereits am {lagerbewegung.BestaetigtAm.Value:dd.MM.yyyy HH:mm} bestätigt.";
                }

                // Zuständigkeitsregel: Bestätigen darf nur die Person, die jetzt für den Nach-Raum verantwortlich ist.
                // Wechselt die Zuständigkeit, während die Bewegung offen ist, bestätigt also die neue Person
                if (lagerbewegung.NachRaum.PersonID != personID)
                {
                    return $"Nur die für den Raum {lagerbewegung.NachRaumID} verantwortliche Person darf diese Lagerbewegung bestätigen.";
                }

                lagerbewegung.BestaetigtAm = DateTime.Now;
                await ZubuchenAsync(lagerbewegung.GegenstandID, lagerbewegung.NachRaumID, lagerbewegung.Menge);

                return null;
            });
        }

        /// <summary>
        /// Methode, die eine Menge im Raumbestand eines Raums abbucht, sofern dort genug vorhanden ist.
        /// Ist danach nichts mehr übrig, wird die Zeile gelöscht, damit der Raumbestand nur zeigt, was wirklich im Raum ist.
        /// </summary>
        /// <param name="gegenstand">Der Gegenstand, der abgebucht wird</param>
        /// <param name="raumID">Die ID des Raums, aus dem abgebucht wird</param>
        /// <param name="menge">Die Menge, die abgebucht wird</param>
        /// <returns>Die Fehlermeldung oder null, wenn abgebucht wurde</returns>
        private async Task<string?> AbbuchenAsync(Gegenstand gegenstand, string raumID, int menge)
        {
            var bestand = await _context.Raumbestand.FindAsync(gegenstand.ID, raumID);
            if (bestand == null || bestand.Menge < menge)
            {
                if (gegenstand.Seriennummer != null)
                {
                    return $"\"{gegenstand.Name}\" ({gegenstand.Seriennummer}) ist nicht im Raum {raumID}.";
                }
                return $"Im Raum {raumID} sind nur {bestand?.Menge ?? 0} Stück von \"{gegenstand.Name}\" vorhanden, {menge} Stück können daher nicht gebucht werden.";
            }

            bestand.Menge -= menge;
            if (bestand.Menge == 0)
            {
                _context.Raumbestand.Remove(bestand);
            }
            return null;
        }

        /// <summary>
        /// Methode, die eine Menge im Raumbestand eines Raums zubucht. Gibt es für den Gegenstand in diesem Raum
        /// noch keine Zeile, wird sie angelegt.
        /// </summary>
        /// <param name="gegenstandID">Die ID des Gegenstands, der zugebucht wird</param>
        /// <param name="raumID">Die ID des Raums, in den zugebucht wird</param>
        /// <param name="menge">Die Menge, die zugebucht wird</param>
        /// <returns>Gibt eine Task zurück</returns>
        private async Task ZubuchenAsync(int gegenstandID, string raumID, int menge)
        {
            var bestand = await _context.Raumbestand.FindAsync(gegenstandID, raumID);
            if (bestand == null)
            {
                _context.Raumbestand.Add(new Raumbestand { GegenstandID = gegenstandID, RaumID = raumID, Menge = menge });
            }
            else
            {
                bestand.Menge += menge;
            }
        }

        /// <summary>
        /// Methode, die überprüft, ob es ein Gerät mit Seriennummer schon gibt, also ob es in einem Raum liegt oder unterwegs ist.
        /// Ein Gerät mit Seriennummer gibt es nur einmal, daher darf es dann nicht noch einmal eingebucht werden.
        /// </summary>
        /// <param name="gegenstand">Das Gerät mit Seriennummer, das eingebucht werden soll</param>
        /// <returns>Die Fehlermeldung oder null, wenn es das Gerät noch nicht gibt</returns>
        private async Task<string?> GeraetVorhandenAsync(Gegenstand gegenstand)
        {
            var raumID = await _context.Raumbestand
                .Where(r => r.GegenstandID == gegenstand.ID && r.Menge > 0)
                .Select(r => r.RaumID)
                .FirstOrDefaultAsync();
            if (raumID != null)
            {
                return $"\"{gegenstand.Name}\" ({gegenstand.Seriennummer}) ist bereits im Raum {raumID}. Ein Gerät mit Seriennummer gibt es nur einmal.";
            }

            // Eine offene Lagerbewegung ist schon aus dem Von-Raum abgebucht, aber noch nicht im Nach-Raum zugebucht
            var nachRaumID = await _context.Lagerbewegung
                .Where(l => l.GegenstandID == gegenstand.ID && l.BestaetigtAm == null)
                .Select(l => l.NachRaumID)
                .FirstOrDefaultAsync();
            if (nachRaumID != null)
            {
                return $"\"{gegenstand.Name}\" ({gegenstand.Seriennummer}) ist gerade unterwegs in den Raum {nachRaumID}. Ein Gerät mit Seriennummer gibt es nur einmal.";
            }

            return null;
        }

        /// <summary>
        /// Methode, die eine Buchung in einer Transaktion ausführt und speichert, sofern die Buchung keine Fehlermeldung liefert.
        /// Gespeichert wird mit genau einem SaveChangesAsync, daher werden Abbuchung, Zubuchung und Lagerbewegung ganz oder gar nicht gespeichert.
        ///
        /// Isolationsstufe Serializable: Was die Buchung gelesen hat (z. B. den Bestand oder ob eine Bewegung noch offen ist),
        /// kann bis zum Ende der Transaktion niemand anderer ändern. Laufen zwei Buchungen gleichzeitig auf dieselben Daten
        /// (z. B. durch einen Doppelklick), bricht SQL Server eine davon ab, statt dieselbe Menge doppelt zu buchen.
        /// Die abgebrochene Buchung wird nicht wiederholt, sondern liefert eine Fehlermeldung.
        ///
        /// Achtung: Objekte, die vorher über denselben Kontext geladen wurden, werden dabei verworfen (ChangeTracker.Clear).
        /// Nach einer Buchung daher neu laden, was angezeigt werden soll.
        /// </summary>
        /// <param name="buchung">Prüft und ändert die Daten (ohne zu speichern) und liefert eine Fehlermeldung oder null</param>
        /// <returns>Die Fehlermeldung oder null, wenn die Buchung gespeichert wurde</returns>
        private async Task<string?> BuchenAsync(Func<Task<string?>> buchung)
        {
            // Alles verwerfen, was der Kontext schon geladen hat: Sonst liefert z. B. FindAsync einen veralteten Bestand
            // aus einer früheren Abfrage, statt ihn innerhalb der Transaktion neu aus der Datenbank zu lesen
            _context.ChangeTracker.Clear();

            try
            {
                await using var transaktion = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                // Liefert die Buchung eine Fehlermeldung, wird die Transaktion ohne Speichern beendet
                var fehler = await buchung();
                if (fehler != null)
                {
                    return fehler;
                }

                await _context.SaveChangesAsync();
                await transaktion.CommitAsync();
                return null;
            }
            catch (Exception)
            {
                // Die Änderungen der abgebrochenen Buchung verwerfen, damit sie nicht bei einem späteren SaveChangesAsync doch gespeichert werden
                _context.ChangeTracker.Clear();
                return "Die Buchung konnte nicht gespeichert werden, da gleichzeitig andere Buchungen dieselben Daten geändert haben. Bitte versuche es noch einmal.";
            }
        }
    }
}
