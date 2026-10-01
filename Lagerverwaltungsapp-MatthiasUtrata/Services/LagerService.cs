using System.Data;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltungsapp_MatthiasUtrata.Services
{
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
        /// <param name="personID">Die ID der Person oder null, wenn niemand angemeldet ist (z. B. User.GetPersonID())</param>
        /// <returns>Die ID ihres Raums oder null, wenn sie für keinen Raum verantwortlich ist</returns>
        public async Task<string?> RaumDerPersonAsync(int? personID)
        {
            if (personID == null)
            {
                return null;
            }

            return await _context.Raum
                .Where(r => r.PersonID == personID)
                .Select(r => r.ID)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Methode, die die Bewegungsarten liefert, die man beim Buchen auswählen kann (für die Auswahllisten in den Formularen).
        /// "Storniert" und "Korrektur" fehlen, da nur der LagerService sie vergibt.
        /// </summary>
        /// <returns>Die Bewegungsarten nach Namen sortiert</returns>
        public async Task<List<Bewegungsart>> WaehlbareBewegungsartenAsync()
        {
            return await _context.Bewegungsart
                .Where(b => b.Name != Bewegungsart.Storniert && b.Name != Bewegungsart.Korrektur)
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Methode, die sucht, ob eine Person schon für einen anderen Raum verantwortlich ist.
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
            return await BuchenAsync(() => BewegungBuchenAsync(gegenstandID, menge, vonRaumID, nachRaumID, bewegungsartID, personID));
        }

        /// <summary>
        /// Methode, mit der die für einen Raum verantwortliche Person mehrere Gegenstände von einem Raum in einen anderen bucht:
        /// aus ihrem Raum an einen anderen Raum (Transfer, Ausleihe) oder aus einem Lager in ihren Raum (holen).
        /// Alles oder nichts in einer Transaktion; die Regeln je Gegenstand stehen in BewegungBuchenAsync.
        /// </summary>
        /// <param name="positionen">Die Gegenstände, die gebucht werden (Schlüssel = GegenstandID, Wert = Menge)</param>
        /// <param name="vonRaumID">Die ID des Raums, aus dem gebucht wird (der eigene Raum oder ein Lager)</param>
        /// <param name="nachRaumID">Die ID des Raums, in den gebucht wird (ein anderer Raum oder der eigene)</param>
        /// <param name="bewegungsartID">Die ID der Bewegungsart, die die Lagerbewegungen beschreibt (normalerweise "Ausgabe")</param>
        /// <param name="personID">Die ID der angemeldeten Person, die bucht</param>
        /// <returns>Die Fehlermeldung oder null, wenn alle Gegenstände gebucht wurden</returns>
        public async Task<string?> AusborgenAsync(IReadOnlyDictionary<int, int> positionen, string vonRaumID, string nachRaumID, int bewegungsartID, int personID)
        {
            return await BuchenAsync(async () =>
            {
                if (positionen.Count == 0)
                {
                    return "Bitte mindestens einen Gegenstand auswählen.";
                }

                // Derselbe Raum wäre ein Wareneingang, dabei wird nichts abgebucht und der Bestand würde sich verdoppeln.
                // Die IDs der geladenen Räume vergleichen, da SQL Server bei der Suche nicht auf Groß-/Kleinschreibung achtet
                var vonRaum = await _context.Raum.FindAsync(vonRaumID);
                var nachRaum = await _context.Raum.FindAsync(nachRaumID);
                if (vonRaum != null && nachRaum != null && vonRaum.ID == nachRaum.ID)
                {
                    return "Bitte einen anderen Raum als den eigenen auswählen.";
                }

                foreach (var (gegenstandID, menge) in positionen)
                {
                    var fehler = await BewegungBuchenAsync(gegenstandID, menge, vonRaumID, nachRaumID, bewegungsartID, personID);
                    if (fehler != null)
                    {
                        return fehler;
                    }
                }

                return null;
            });
        }

        /// <summary>
        /// Methode, mit der die Person, die freigeben muss, eine offene Lagerbewegung bestätigt (digitale Übernahme):
        /// die Person des Nach-Raums, bei Geräten, die aus einem Lager geholt werden, die des Lagers (Lagerbewegung.FreigabeRaum).
        /// </summary>
        /// <param name="lagerbewegungID">Die ID der Lagerbewegung, die bestätigt werden soll</param>
        /// <param name="personID">Die ID der angemeldeten Person, die bestätigt</param>
        /// <returns>Die Fehlermeldung oder null, wenn die Lagerbewegung bestätigt wurde</returns>
        public async Task<string?> BestaetigenAsync(int lagerbewegungID, int personID)
        {
            return await BuchenAsync(() => BewegungBestaetigenAsync(lagerbewegungID, personID));
        }

        /// <summary>
        /// Methode, mit der die Person, die freigeben muss, mehrere offene Lagerbewegungen auf einmal bestätigt (siehe BestaetigenAsync)
        /// </summary>
        /// <param name="lagerbewegungIDs">Die IDs der Lagerbewegungen, die bestätigt werden sollen</param>
        /// <param name="personID">Die ID der angemeldeten Person, die bestätigt</param>
        /// <returns>Die Fehlermeldung oder null, wenn alle Lagerbewegungen bestätigt wurden</returns>
        public async Task<string?> BestaetigenAsync(IEnumerable<int> lagerbewegungIDs, int personID)
        {
            return await BuchenAsync(async () =>
            {
                // Doppelte IDs nur einmal bestätigen, sonst würde die zweite als "bereits bestätigt" die ganze Buchung abbrechen
                var ids = lagerbewegungIDs.Distinct().ToList();
                if (ids.Count == 0)
                {
                    return "Bitte mindestens eine Übernahme zum Bestätigen auswählen.";
                }

                foreach (var id in ids)
                {
                    var fehler = await BewegungBestaetigenAsync(id, personID);
                    if (fehler != null)
                    {
                        return fehler;
                    }
                }

                return null;
            });
        }

        /// <summary>
        /// Methode, mit der die Person, die freigeben muss (Lagerbewegung.FreigabeRaum), eine offene Lagerbewegung ablehnt.
        /// Die Menge wird wieder im Von-Raum zugebucht, die Lagerbewegung bleibt als storniert in der Historie.
        /// </summary>
        /// <param name="lagerbewegungID">Die ID der Lagerbewegung, die abgelehnt werden soll</param>
        /// <param name="personID">Die ID der angemeldeten Person, die ablehnt</param>
        /// <returns>Die Fehlermeldung oder null, wenn die Lagerbewegung abgelehnt wurde</returns>
        public async Task<string?> AblehnenAsync(int lagerbewegungID, int personID)
        {
            return await BuchenAsync(async () =>
            {
                var lagerbewegung = await LagerbewegungLadenAsync(lagerbewegungID);
                var fehler = NichtOffenFehler(lagerbewegung);
                if (fehler != null)
                {
                    return fehler;
                }

                // Zuständigkeitsregel wie beim Bestätigen: nur die Person, die jetzt für den freigebenden Raum verantwortlich ist
                if (lagerbewegung!.FreigabeRaum.PersonID != personID)
                {
                    return $"Nur die für den Raum {lagerbewegung.FreigabeRaum.ID} verantwortliche Person darf diese Lagerbewegung ablehnen.";
                }

                return await StornierenAsync(lagerbewegung);
            });
        }

        /// <summary>
        /// Methode, mit der die Person, die angefragt hat (Lagerbewegung.AnfrageRaum), eine offene Lagerbewegung zurückzieht,
        /// solange sie noch nicht bestätigt ist: bei einem Transfer die Person des Von-Raums, bei Geräten, die aus einem Lager
        /// geholt werden, die des Nach-Raums. Die Menge wird wieder im Von-Raum zugebucht, die Lagerbewegung bleibt als storniert in der Historie.
        /// </summary>
        /// <param name="lagerbewegungID">Die ID der Lagerbewegung, die zurückgezogen werden soll</param>
        /// <param name="personID">Die ID der angemeldeten Person, die zurückzieht</param>
        /// <returns>Die Fehlermeldung oder null, wenn die Lagerbewegung zurückgezogen wurde</returns>
        public async Task<string?> ZurueckziehenAsync(int lagerbewegungID, int personID)
        {
            return await BuchenAsync(async () =>
            {
                var lagerbewegung = await LagerbewegungLadenAsync(lagerbewegungID);
                var fehler = NichtOffenFehler(lagerbewegung);
                if (fehler != null)
                {
                    return fehler;
                }

                // Zurückziehen darf, wer angefragt hat, nach der aktuellen Zuständigkeit für den anfragenden Raum
                if (lagerbewegung!.AnfrageRaum.PersonID != personID)
                {
                    return $"Nur die für den Raum {lagerbewegung.AnfrageRaum.ID} verantwortliche Person darf diese Lagerbewegung zurückziehen.";
                }

                return await StornierenAsync(lagerbewegung);
            });
        }

        /// <summary>
        /// Methode, mit der ein Admin den Bestand eines Raums korrigiert, z. B. nach einer Inventur. Für jeden geänderten Gegenstand
        /// entsteht eine sofort bestätigte Lagerbewegung mit der Bewegungsart "Korrektur" (siehe Lagerbewegung).
        /// Alles oder nichts in einer Transaktion; die Regeln je Gegenstand stehen in KorrekturBuchenAsync.
        /// </summary>
        /// <param name="raumID">Die ID des Raums, dessen Bestand korrigiert wird</param>
        /// <param name="positionen">Die geänderten Gegenstände (Schlüssel = GegenstandID) mit der Menge, die beim Anzeigen im Raum war, und der neuen Menge</param>
        /// <param name="personID">Die ID des angemeldeten Admins</param>
        /// <returns>Die Fehlermeldung oder null, wenn alle Korrekturen gebucht wurden</returns>
        public async Task<string?> KorrigierenAsync(string raumID, IReadOnlyDictionary<int, (int Bisher, int Neu)> positionen, int personID)
        {
            return await BuchenAsync(async () =>
            {
                // Zuständigkeitsregel: Korrigieren darf nur ein Admin, in jedem Raum
                if (!await _context.Person.AnyAsync(p => p.ID == personID && p.Rolle.Admin))
                {
                    return "Nur ein Admin darf den Bestand korrigieren.";
                }

                var raum = await _context.Raum.FindAsync(raumID);
                if (raum == null)
                {
                    return "Diesen Raum gibt es nicht.";
                }

                // Die Ersteinrichtung legt die Bewegungsart bei jedem Start an, falls sie fehlt
                var korrektur = await _context.Bewegungsart.FirstOrDefaultAsync(b => b.Name == Bewegungsart.Korrektur);
                if (korrektur == null)
                {
                    return $"Die Bewegungsart \"{Bewegungsart.Korrektur}\" fehlt. Bitte die Anwendung neu starten, dann wird sie angelegt.";
                }

                var geaendert = positionen.Where(p => p.Value.Neu != p.Value.Bisher).ToList();
                if (geaendert.Count == 0)
                {
                    return "Es wurde keine Menge geändert.";
                }

                var jetzt = DateTime.Now;
                foreach (var (gegenstandID, (bisher, neu)) in geaendert)
                {
                    var fehler = await KorrekturBuchenAsync(gegenstandID, bisher, neu, raum.ID, korrektur.ID, personID, jetzt);
                    if (fehler != null)
                    {
                        return fehler;
                    }
                }

                return null;
            });
        }

        /// <summary>
        /// Methode, die die Korrektur eines Gegenstands in einem Raum prüft, den Raumbestand auf die neue Menge setzt und die
        /// Lagerbewegung dazu anlegt, ohne zu speichern.
        /// </summary>
        /// <param name="gegenstandID">Die ID des Gegenstands, dessen Bestand korrigiert wird</param>
        /// <param name="bisher">Die Menge, die beim Anzeigen im Raum war</param>
        /// <param name="neu">Die neue Menge im Raum (bei einem Gerät mit Seriennummer 0 oder 1)</param>
        /// <param name="raumID">Die ID des Raums, schon so geschrieben wie in der Datenbank</param>
        /// <param name="bewegungsartID">Die ID der Bewegungsart "Korrektur"</param>
        /// <param name="personID">Die ID des Admins, der korrigiert</param>
        /// <param name="jetzt">Der Zeitpunkt der Korrektur (für alle Gegenstände derselbe)</param>
        /// <returns>Die Fehlermeldung oder null, wenn die Korrektur gebucht wurde</returns>
        private async Task<string?> KorrekturBuchenAsync(int gegenstandID, int bisher, int neu, string raumID, int bewegungsartID, int personID, DateTime jetzt)
        {
            var gegenstand = await _context.Gegenstand.FindAsync(gegenstandID);
            if (gegenstand == null)
            {
                return "Diesen Gegenstand gibt es nicht.";
            }

            if (neu < 0)
            {
                return $"Die Menge von \"{gegenstand.Name}\" darf nicht negativ sein.";
            }

            // Ein Gerät mit Seriennummer gibt es genau einmal, im Raum ist es also einmal oder gar nicht
            if (gegenstand.Seriennummer != null && neu > 1)
            {
                return $"\"{gegenstand.Name}\" ({gegenstand.Seriennummer}) ist ein einzelnes Gerät und kann nur einmal oder gar nicht im Raum sein.";
            }

            // Hat sich der Bestand geändert, seit der Admin ihn gesehen hat (z. B. durch einen Transfer), stimmt die gezählte Menge
            // vielleicht nicht mehr. Dann lieber abbrechen und neu prüfen lassen
            var bestand = await _context.Raumbestand.FindAsync(gegenstand.ID, raumID);
            var aktuell = bestand?.Menge ?? 0;
            if (aktuell != bisher)
            {
                return $"Der Bestand von \"{gegenstand.Name}\" in Raum {raumID} hat sich inzwischen geändert (jetzt {aktuell} Stück). Bitte die Mengen prüfen und noch einmal speichern.";
            }

            // Ein Gerät mit Seriennummer darf danach nicht zweimal da sein: Liegt es in einem anderen Raum oder ist es unterwegs,
            // muss es dort zuerst ausgebucht oder die Bewegung abgeschlossen werden
            if (gegenstand.Seriennummer != null && neu == 1)
            {
                var hindernis = await GeraetVorhandenAsync(gegenstand);
                if (hindernis != null)
                {
                    return hindernis;
                }
            }

            if (bestand == null)
            {
                _context.Raumbestand.Add(new Raumbestand { GegenstandID = gegenstand.ID, RaumID = raumID, Menge = neu });
            }
            else if (neu == 0)
            {
                _context.Raumbestand.Remove(bestand);
            }
            else
            {
                bestand.Menge = neu;
            }

            _context.Lagerbewegung.Add(new Lagerbewegung
            {
                Menge = neu - aktuell,
                ErstelltAm = jetzt,
                BestaetigtAm = jetzt,
                BewegungsartID = bewegungsartID,
                VonRaumID = raumID,
                NachRaumID = raumID,
                GegenstandID = gegenstand.ID,
                PersonID = personID
            });
            return null;
        }

        /// <summary>
        /// Methode, die eine Lagerbewegung prüft, anlegt und im Raumbestand bucht, ohne zu speichern.
        /// </summary>
        /// <param name="gegenstandID">Die ID des Gegenstands, der bewegt wird</param>
        /// <param name="menge">Die Menge, die bewegt wird (bei einem Gerät mit Seriennummer immer 1)</param>
        /// <param name="vonRaumID">Die ID des Raums, aus dem der Gegenstand kommt</param>
        /// <param name="nachRaumID">Die ID des Raums, in den der Gegenstand kommt</param>
        /// <param name="bewegungsartID">Die ID der Bewegungsart, die die Lagerbewegung beschreibt</param>
        /// <param name="personID">Die ID der angemeldeten Person, die die Bewegung anlegt</param>
        /// <returns>Die Fehlermeldung oder null, wenn die Lagerbewegung angelegt wurde</returns>
        private async Task<string?> BewegungBuchenAsync(int gegenstandID, int menge, string vonRaumID, string nachRaumID, int bewegungsartID, int personID)
        {
            var gegenstand = await _context.Gegenstand.FindAsync(gegenstandID);
            if (gegenstand == null)
            {
                return "Diesen Gegenstand gibt es nicht.";
            }

            // Mit Namen, da bei einer Ausleihe mehrere Gegenstände auf einmal gebucht werden
            if (menge < 1)
            {
                return $"Die Menge von \"{gegenstand.Name}\" muss mindestens 1 sein.";
            }

            // Ein Gerät mit Seriennummer gibt es genau einmal, daher wird es immer einzeln gebucht
            if (gegenstand.Seriennummer != null && menge != 1)
            {
                return $"\"{gegenstand.Name}\" ({gegenstand.Seriennummer}) ist ein einzelnes Gerät und kann nur mit der Menge 1 gebucht werden.";
            }

            var bewegungsart = await _context.Bewegungsart.FindAsync(bewegungsartID);
            if (bewegungsart == null)
            {
                return "Bitte eine Bewegungsart auswählen.";
            }

            // Sonst wäre die Bewegung z. B. schon beim Anlegen storniert, aber trotzdem offen
            var nurVergebenFuer = Bewegungsart.NurVergebenFuer(bewegungsart.Name);
            if (nurVergebenFuer != null)
            {
                return $"Die Bewegungsart \"{bewegungsart.Name}\" bekommen nur {nurVergebenFuer}. Bitte eine andere auswählen.";
            }

            var vonRaum = await _context.Raum.Include(r => r.Raumart).FirstOrDefaultAsync(r => r.ID == vonRaumID);
            var nachRaum = await _context.Raum.FindAsync(nachRaumID);
            if (vonRaum == null || nachRaum == null)
            {
                return "Diesen Raum gibt es nicht.";
            }

            // Zuständigkeitsregel: Anlegen darf die Person, die für den Von-Raum verantwortlich ist, und aus einem Lager auch die
            // Person des Nach-Raums (holen). Sonst niemand, auch ein Admin nicht
            var ausLagerGeholt = vonRaum.PersonID != personID && nachRaum.PersonID == personID && vonRaum.Raumart.IstLager;
            if (vonRaum.PersonID != personID && !ausLagerGeholt)
            {
                if (nachRaum.PersonID == personID)
                {
                    return $"Raum {vonRaum.ID} ist kein Lager. Geräte holen kann man nur aus einem Lager, aus anderen Räumen muss die dort verantwortliche Person sie verlegen.";
                }
                return $"Nur die für den Raum {vonRaum.ID} verantwortliche Person darf Gegenstände aus diesem Raum buchen.";
            }

            // Freigeben muss die andere Seite. Ohne verantwortliche Person könnte das niemand und die Menge bliebe für immer unterwegs
            Raum freigabeRaum;
            if (ausLagerGeholt)
            {
                freigabeRaum = vonRaum;
            }
            else
            {
                freigabeRaum = nachRaum;
            }

            if (freigabeRaum.PersonID == null)
            {
                return $"Für den Raum {freigabeRaum.ID} ist niemand verantwortlich, der die Lagerbewegung freigeben könnte. Weise dem Raum zuerst eine verantwortliche Person zu.";
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

            // Müsste die Person die Bewegung selbst freigeben (beim Wareneingang immer), gilt sie sofort als bestätigt
            // und wird gleich im Nach-Raum zugebucht
            if (freigabeRaum.PersonID == personID)
            {
                lagerbewegung.BestaetigtAm = jetzt;
                await ZubuchenAsync(gegenstand.ID, nachRaum.ID, menge);
            }

            return null;
        }

        /// <summary>
        /// Methode, die eine offene Lagerbewegung prüft, bestätigt und im Nach-Raum zubucht, ohne zu speichern.
        /// </summary>
        /// <param name="lagerbewegungID">Die ID der Lagerbewegung, die bestätigt werden soll</param>
        /// <param name="personID">Die ID der angemeldeten Person, die bestätigt</param>
        /// <returns>Die Fehlermeldung oder null, wenn die Lagerbewegung bestätigt wurde</returns>
        private async Task<string?> BewegungBestaetigenAsync(int lagerbewegungID, int personID)
        {
            var lagerbewegung = await LagerbewegungLadenAsync(lagerbewegungID);
            var fehler = NichtOffenFehler(lagerbewegung);
            if (fehler != null)
            {
                return fehler;
            }

            // Zuständigkeitsregel: Bestätigen darf nur die Person, die jetzt für den freigebenden Raum verantwortlich ist
            // (Nach-Raum, beim Holen aus einem Lager das Lager). Wechselt die Zuständigkeit, während die Bewegung offen ist,
            // bestätigt also die neue Person
            if (lagerbewegung!.FreigabeRaum.PersonID != personID)
            {
                return $"Nur die für den Raum {lagerbewegung.FreigabeRaum.ID} verantwortliche Person darf diese Lagerbewegung bestätigen.";
            }

            lagerbewegung.BestaetigtAm = DateTime.Now;
            await ZubuchenAsync(lagerbewegung.GegenstandID, lagerbewegung.NachRaumID, lagerbewegung.Menge);

            return null;
        }

        /// <summary>
        /// Methode, die eine offene Lagerbewegung storniert, ohne zu speichern: Sie bekommt die Bewegungsart "Storniert",
        /// BestaetigtAm den Zeitpunkt der Stornierung, und die Menge wird wieder im Von-Raum zugebucht.
        /// Die ursprüngliche Bewegungsart wird dabei überschrieben.
        /// </summary>
        /// <param name="lagerbewegung">Die offene Lagerbewegung, deren Zuständigkeit schon geprüft ist</param>
        /// <returns>Die Fehlermeldung oder null, wenn die Lagerbewegung storniert wurde</returns>
        private async Task<string?> StornierenAsync(Lagerbewegung lagerbewegung)
        {
            // Die Ersteinrichtung legt die Bewegungsart bei jedem Start an, falls sie fehlt
            var storniert = await _context.Bewegungsart.FirstOrDefaultAsync(b => b.Name == Bewegungsart.Storniert);
            if (storniert == null)
            {
                return $"Die Bewegungsart \"{Bewegungsart.Storniert}\" fehlt. Bitte die Anwendung neu starten, dann wird sie angelegt.";
            }

            lagerbewegung.Bewegungsart = storniert;
            lagerbewegung.BestaetigtAm = DateTime.Now;
            await ZubuchenAsync(lagerbewegung.GegenstandID, lagerbewegung.VonRaumID, lagerbewegung.Menge);
            return null;
        }

        /// <summary>
        /// Methode, die eine Lagerbewegung mit allem lädt, was zum Bestätigen, Ablehnen oder Zurückziehen nötig ist:
        /// die Bewegungsart (für NichtOffenFehler) sowie Von- und Nach-Raum (für FreigabeRaum und AnfrageRaum).
        /// </summary>
        /// <param name="lagerbewegungID">Die ID der Lagerbewegung</param>
        /// <returns>Die Lagerbewegung oder null, wenn es sie nicht gibt</returns>
        private async Task<Lagerbewegung?> LagerbewegungLadenAsync(int lagerbewegungID)
        {
            return await _context.Lagerbewegung
                .Include(l => l.Bewegungsart)
                .Include(l => l.VonRaum)
                .Include(l => l.NachRaum)
                .FirstOrDefaultAsync(l => l.ID == lagerbewegungID);
        }

        /// <summary>
        /// Methode, die überprüft, ob es eine Lagerbewegung gibt und ob sie noch offen ist. Eine abgeschlossene Lagerbewegung
        /// darf nicht noch einmal abgeschlossen werden, sonst würde dieselbe Menge ein zweites Mal zugebucht.
        /// Die Bewegungsart muss dafür geladen sein.
        /// </summary>
        /// <param name="lagerbewegung">Die geladene Lagerbewegung oder null, wenn es sie nicht gibt</param>
        /// <returns>Die Fehlermeldung oder null, wenn die Lagerbewegung offen ist</returns>
        private static string? NichtOffenFehler(Lagerbewegung? lagerbewegung)
        {
            if (lagerbewegung == null)
            {
                return "Diese Lagerbewegung gibt es nicht.";
            }

            if (lagerbewegung.BestaetigtAm == null)
            {
                return null;
            }

            var zeitpunkt = lagerbewegung.BestaetigtAm.Value.ToString("dd.MM.yyyy HH:mm");
            if (lagerbewegung.Storniert)
            {
                return $"Diese Lagerbewegung wurde bereits am {zeitpunkt} storniert.";
            }
            return $"Diese Lagerbewegung wurde bereits am {zeitpunkt} bestätigt.";
        }

        /// <summary>
        /// Methode, die eine Menge im Raumbestand eines Raums abbucht, sofern dort genug vorhanden ist.
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
        /// Methode, die eine Menge im Raumbestand eines Raums zubucht. 
        /// Gibt es für den Gegenstand in diesem Raum noch keine Zeile, wird sie angelegt.
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
                .Where(Lagerbewegung.IstOffen)
                .Where(l => l.GegenstandID == gegenstand.ID)
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

                // Liefert die Buchung eine Fehlermeldung, wird die Transaktion ohne Speichern beendet. Was sie bis dahin geändert hat
                // (z. B. bei einer Ausleihe die schon abgebuchten Positionen), wird verworfen, sonst zeigt das Formular danach
                // den verringerten Bestand an
                var fehler = await buchung();
                if (fehler != null)
                {
                    _context.ChangeTracker.Clear();
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
