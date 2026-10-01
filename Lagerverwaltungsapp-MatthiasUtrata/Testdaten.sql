USE [Lagerverwaltung];
GO

SET NOCOUNT ON;
-- Der gefilterte Index UX_Raum_PersonID verlangt diese Einstellungen (in SSMS Standard, in sqlcmd nur mit -I)
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
-- Falls bei der Ausführung ein Fehler auftritt, wird die Transaktion abgebrochen und der Fortschritt zurückgesetzt.
SET XACT_ABORT ON;

BEGIN TRANSACTION;

/* ---------- Rolle ---------- */
-- "LehrerIn" muss genau so heißen, da neu registrierte Benutzer diese Rolle bekommen (Rolle.LehrerIn)
INSERT INTO dbo.Rolle (Name, [Admin])
SELECT v.Name, v.[Admin]
FROM (VALUES
    ('Administrator', 1),
    ('LehrerIn',      0),
    ('Laborvorstand', 0),
    ('KustodIn',      0),
    ('SchulwartIn',   0)
) AS v(Name, [Admin])
WHERE NOT EXISTS (SELECT 1 FROM dbo.Rolle r WHERE r.Name = v.Name);

/* ---------- Raumart (höchstens 20 Zeichen) ---------- */
INSERT INTO dbo.Raumart (Name)
SELECT v.Name
FROM (VALUES
    ('Hauptlager'),
    ('Umbuchungslager'),
    ('Labor'),
    ('Werkstatt'),
    ('Klassenzimmer'),
    ('Fachraum'),
    ('Turnsaal'),
    ('Bibliothek'),
    ('Konferenzzimmer'),
    ('Verwaltung')
) AS v(Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Raumart r WHERE r.Name = v.Name);

/* ---------- Bewegungsart (höchstens 20 Zeichen) ---------- */
-- "Storniert" muss genau so heißen, da abgelehnte und zurückgezogene Transfers diese Bewegungsart bekommen (Bewegungsart.Storniert)
INSERT INTO dbo.Bewegungsart (Name)
SELECT v.Name
FROM (VALUES
    ('Wareneingang'),
    ('Ausgabe'),
    ('Rueckgabe'),
    ('Einlagerung'),
    ('Reparatur'),
    ('Storniert')
) AS v(Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Bewegungsart b WHERE b.Name = v.Name);

/* ---------- Kategorie (höchstens 30 Zeichen) ---------- */
INSERT INTO dbo.Kategorie (Name)
SELECT v.Name
FROM (VALUES
    ('Monitore'),
    ('Notebooks'),
    ('Desktop-PCs'),
    ('Tablets'),
    ('Netzwerktechnik'),
    ('Kabel und Adapter'),
    ('Eingabegeräte'),
    ('Messgeräte'),
    ('Präsentationstechnik'),
    ('Drucker und Kopierer'),
    ('Audiotechnik'),
    ('Schulmöbel'),
    ('Tafeln und Zubehör'),
    ('Büromaterial'),
    ('Lehrmittel'),
    ('Mikrocontroller und Robotik'),
    ('Werkzeug'),
    ('Sportgeräte'),
    ('Sicherheit und Erste Hilfe'),
    ('Bücher')
) AS v(Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Kategorie k WHERE k.Name = v.Name);

/* ---------- Hersteller (höchstens 50 Zeichen) ---------- */
INSERT INTO dbo.Hersteller (Name)
SELECT v.Name
FROM (VALUES
    -- IT
    ('Dell'),
    ('HP'),
    ('Lenovo'),
    ('Apple'),
    ('Cisco'),
    ('Ubiquiti'),
    ('APC'),
    ('Logitech'),
    ('Delock'),
    ('Brennenstuhl'),
    -- Präsentation, Druck und Audio
    ('Epson'),
    ('BenQ'),
    ('Elmo'),
    ('Celexon'),
    ('Kyocera'),
    ('Brother'),
    ('JBL'),
    ('Sennheiser'),
    -- Möbel, Tafeln und Büro
    ('VS Vereinigte Spezialmöbelfabriken'),
    ('Bisley'),
    ('Legamaster'),
    ('edding'),
    ('Robercolor'),
    ('Leitz'),
    ('Navigator'),
    ('PARAT'),
    -- Lehrmittel und Bücher
    ('Casio'),
    ('Texas Instruments'),
    ('Columbus'),
    ('3B Scientific'),
    ('Bresser'),
    ('ÖBV'),
    ('PONS'),
    -- Elektronik, Messtechnik und Werkzeug
    ('Raspberry Pi'),
    ('Arduino'),
    ('LEGO Education'),
    ('Joy-IT'),
    ('Fluke'),
    ('Rigol'),
    ('PeakTech'),
    ('Weller'),
    ('Bosch'),
    ('Knipex'),
    ('Wera'),
    -- Sport und Sicherheit
    ('Molten'),
    ('Kübler Sport'),
    ('Söhngen'),
    ('Gloria'),
    ('uvex')
) AS v(Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Hersteller h WHERE h.Name = v.Name);

/* ---------- Person (Benutzername höchstens 20 Zeichen) ---------- */
-- Die Passwörter sind wie im PasswordService samt Pepper aus der appsettings.json ("AlleMeineEntchen") gehasht.
-- Wird der Pepper geändert, passen diese Hashes nicht mehr und die Konten können sich nicht mehr anmelden.
-- Die Startkonten legt sonst die Ersteinrichtung an (Passwort = Benutzername), mit ihnen kann das Skript auch vor dem ersten Start laufen.
-- Alle anderen Konten haben das Passwort "Test1234".
DECLARE @Test1234 varchar(max) = 'AQAAAAIAAYagAAAAEJnadFfrRR04RxK+beY0E2wLIrxiY3Xnij7Brc7ggfkMZkWost2mV4enzJlR7KKPQw==';

INSERT INTO dbo.Person (Vorname, Nachname, Username, Password, Email, RolleID)
SELECT v.Vorname, v.Nachname, v.Username, v.Password, v.Email, r.ID
FROM (VALUES
    -- Startkonten
    ('Admin',     'Admin',           'admin',           'AQAAAAIAAYagAAAAECwy1wqfPxqlVjPvCA6QHCcHx6JoT3XSmvtihIU4eszuJsm8o0MGhmG++xR+vzqT4g==', 'admin@lagerverwaltung.local',           'Administrator'),
    ('Lehrer',    'Hauptlager',      'hauptlager',      'AQAAAAIAAYagAAAAEBMzkETmS8S6jKneiDBFDI6TgtkXP3yW7D7ttOhlf0uTvf73QKkkeMmOF6/WJhRtpw==', 'hauptlager@lagerverwaltung.local',      'LehrerIn'),
    ('Lehrer',    'Umbuchungslager', 'umbuchungslager', 'AQAAAAIAAYagAAAAENUEVxB5yMOZnLGW8L+F+R+43AT10I3e+Hs7qEc7Lwsq9fEpG8VzQp7U4ANRj57CRQ==', 'umbuchungslager@lagerverwaltung.local', 'LehrerIn'),
    -- Testkonten
    ('Wolfgang',  'Fuchs',           'wfuchs',          @Test1234, 'wolfgang.fuchs@lagerverwaltung.local',    'Administrator'), -- Direktor
    ('Martina',   'Eder',            'meder',           @Test1234, 'martina.eder@lagerverwaltung.local',      'Administrator'), -- Sekretariat
    ('Anna',      'Huber',           'ahuber',          @Test1234, 'anna.huber@lagerverwaltung.local',        'LehrerIn'),
    ('Thomas',    'Gruber',          'tgruber',         @Test1234, 'thomas.gruber@lagerverwaltung.local',     'LehrerIn'),
    ('Sabine',    'Wagner',          'swagner',         @Test1234, 'sabine.wagner@lagerverwaltung.local',     'LehrerIn'),
    ('Julia',     'Pichler',         'jpichler',        @Test1234, 'julia.pichler@lagerverwaltung.local',     'LehrerIn'),
    ('Eva',       'Berger',          'eberger',         @Test1234, 'eva.berger@lagerverwaltung.local',        'LehrerIn'),
    ('Katharina', 'Wimmer',          'kwimmer',         @Test1234, 'katharina.wimmer@lagerverwaltung.local',  'LehrerIn'),
    ('Michael',   'Bauer',           'mbauer',          @Test1234, 'michael.bauer@lagerverwaltung.local',     'Laborvorstand'),
    ('Stefan',    'Moser',           'smoser',          @Test1234, 'stefan.moser@lagerverwaltung.local',      'Laborvorstand'),
    ('Andreas',   'Steiner',         'asteiner',        @Test1234, 'andreas.steiner@lagerverwaltung.local',   'Laborvorstand'),
    ('Claudia',   'Mayer',           'cmayer',          @Test1234, 'claudia.mayer@lagerverwaltung.local',     'KustodIn'),      -- Physik und Biologie
    ('Lisa',      'Leitner',         'lleitner',        @Test1234, 'lisa.leitner@lagerverwaltung.local',      'KustodIn'),      -- Bewegung und Sport
    ('Franz',     'Schmid',          'fschmid',         @Test1234, 'franz.schmid@lagerverwaltung.local',      'SchulwartIn'),
    ('Johann',    'Brunner',         'jbrunner',        @Test1234, 'johann.brunner@lagerverwaltung.local',    'SchulwartIn')
) AS v(Vorname, Nachname, Username, Password, Email, Rolle)
JOIN dbo.Rolle r ON r.Name = v.Rolle
WHERE NOT EXISTS (SELECT 1 FROM dbo.Person p WHERE p.Username = v.Username);

/* ---------- Raum (Raumnummer höchstens 8 Zeichen) ---------- */
INSERT INTO dbo.Raum (ID, RaumartID)
SELECT v.ID, ra.ID
FROM (VALUES
    ('HL',   'Hauptlager'),
    ('UL',   'Umbuchungslager'),
    ('101',  'Klassenzimmer'),
    ('102',  'Klassenzimmer'),
    ('103',  'Klassenzimmer'),
    ('205',  'Fachraum'),        -- Physik und Biologie
    ('309',  'Labor'),           -- EDV-Labor
    ('310',  'Labor'),           -- EDV-Labor
    ('311',  'Labor'),           -- Netzwerklabor
    ('312',  'Labor'),           -- Elektroniklabor
    ('W01',  'Werkstatt'),
    ('TS1',  'Turnsaal'),
    ('BIB',  'Bibliothek'),
    ('KONF', 'Konferenzzimmer'),
    ('SEK',  'Verwaltung')       -- Sekretariat
) AS v(ID, Raumart)
JOIN dbo.Raumart ra ON ra.Name = v.Raumart
WHERE NOT EXISTS (SELECT 1 FROM dbo.Raum r WHERE r.ID = v.ID);

/* ---------- Verantwortliche Person je Raum ---------- */
-- Auch Räume, die schon ohne verantwortliche Person angelegt waren (z. B. von einer früheren Version dieses Skripts), bekommen eine.
-- Ein Raum, der schon eine verantwortliche Person hat, bleibt unverändert. Eine Person ist für höchstens einen Raum verantwortlich
-- (Index UX_Raum_PersonID), daher wird sie nur eingetragen, wenn sie noch keinen Raum hat.
-- Ohne Raum bleiben admin (Administrator), kwimmer (LehrerIn) und jbrunner (SchulwartIn).
UPDATE r
SET PersonID = p.ID
FROM (VALUES
    ('HL',   'hauptlager'),
    ('UL',   'umbuchungslager'),
    ('101',  'ahuber'),
    ('102',  'tgruber'),
    ('103',  'swagner'),
    ('205',  'cmayer'),
    ('309',  'mbauer'),
    ('310',  'jpichler'),
    ('311',  'smoser'),
    ('312',  'asteiner'),
    ('W01',  'fschmid'),
    ('TS1',  'lleitner'),
    ('BIB',  'eberger'),
    ('KONF', 'wfuchs'),
    ('SEK',  'meder')
) AS v(RaumID, Username)
JOIN dbo.Raum r ON r.ID = v.RaumID
JOIN dbo.Person p ON p.Username = v.Username
WHERE r.PersonID IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.Raum x WHERE x.PersonID = p.ID);

/* ---------- Gegenstand ---------- */
INSERT INTO dbo.Gegenstand (Name, Seriennummer, KategorieID, HerstellerID)
SELECT v.Name, v.Seriennummer, k.ID, h.ID
FROM (VALUES
    /* Geräte mit Seriennummer */
    -- Monitore
    ('Dell P2422H 24" Monitor',               'DL-P2422H-001',   'Monitore',                    'Dell'),
    ('Dell P2422H 24" Monitor',               'DL-P2422H-002',   'Monitore',                    'Dell'),
    ('Dell P2422H 24" Monitor',               'DL-P2422H-003',   'Monitore',                    'Dell'),
    ('Dell P2422H 24" Monitor',               'DL-P2422H-004',   'Monitore',                    'Dell'),
    ('HP E24 G5 24" Monitor',                 'HP-E24G5-001',    'Monitore',                    'HP'),
    -- Notebooks
    ('Lenovo ThinkPad L14 Gen 4',             'LN-L14G4-001',    'Notebooks',                   'Lenovo'),
    ('Lenovo ThinkPad L14 Gen 4',             'LN-L14G4-002',    'Notebooks',                   'Lenovo'),
    ('Lenovo ThinkPad L14 Gen 4',             'LN-L14G4-003',    'Notebooks',                   'Lenovo'),
    ('HP ProBook 450 G10',                    'HP-PB450-001',    'Notebooks',                   'HP'),
    -- Desktop-PCs
    ('Dell OptiPlex 7010',                    'DL-OPT7010-001',  'Desktop-PCs',                 'Dell'),
    ('Dell OptiPlex 7010',                    'DL-OPT7010-002',  'Desktop-PCs',                 'Dell'),
    ('Dell OptiPlex 7010',                    'DL-OPT7010-003',  'Desktop-PCs',                 'Dell'),
    ('Lenovo ThinkCentre M70q',               'LN-M70Q-001',     'Desktop-PCs',                 'Lenovo'),
    -- Tablets
    ('Apple iPad (10. Generation)',           'AP-IPAD10-001',   'Tablets',                     'Apple'),
    ('Apple iPad (10. Generation)',           'AP-IPAD10-002',   'Tablets',                     'Apple'),
    ('Apple iPad (10. Generation)',           'AP-IPAD10-003',   'Tablets',                     'Apple'),
    ('Apple iPad (10. Generation)',           'AP-IPAD10-004',   'Tablets',                     'Apple'),
    ('PARAT Tablet-Ladekoffer für 16 Geräte', 'PA-TC16-001',     'Tablets',                     'PARAT'),
    -- Netzwerktechnik
    ('Cisco Catalyst 2960-X Switch',          'CS-C2960X-001',   'Netzwerktechnik',             'Cisco'),
    ('Cisco ISR 1100 Router',                 'CS-ISR1100-001',  'Netzwerktechnik',             'Cisco'),
    ('Ubiquiti UniFi U6 Pro Access Point',    'UB-U6PRO-001',    'Netzwerktechnik',             'Ubiquiti'),
    ('Ubiquiti UniFi U6 Pro Access Point',    'UB-U6PRO-002',    'Netzwerktechnik',             'Ubiquiti'),
    ('APC Back-UPS 950 USV',                  'APC-BX950-001',   'Netzwerktechnik',             'APC'),
    -- Präsentationstechnik
    ('Epson EB-W49 Beamer',                   'EP-EBW49-001',    'Präsentationstechnik',        'Epson'),
    ('Epson EB-W49 Beamer',                   'EP-EBW49-002',    'Präsentationstechnik',        'Epson'),
    ('Epson EB-W49 Beamer',                   'EP-EBW49-003',    'Präsentationstechnik',        'Epson'),
    ('Epson EB-W49 Beamer',                   'EP-EBW49-004',    'Präsentationstechnik',        'Epson'),
    ('Epson EB-W49 Beamer',                   'EP-EBW49-005',    'Präsentationstechnik',        'Epson'),
    ('Epson EB-W49 Beamer',                   'EP-EBW49-006',    'Präsentationstechnik',        'Epson'),
    ('BenQ RP6502 Interaktives Display 65"',  'BQ-RP6502-001',   'Präsentationstechnik',        'BenQ'),
    ('BenQ RP6502 Interaktives Display 65"',  'BQ-RP6502-002',   'Präsentationstechnik',        'BenQ'),
    ('Elmo MX-P3 Dokumentenkamera',           'EL-MXP3-001',     'Präsentationstechnik',        'Elmo'),
    ('Elmo MX-P3 Dokumentenkamera',           'EL-MXP3-002',     'Präsentationstechnik',        'Elmo'),
    -- Drucker und Kopierer
    ('HP LaserJet Pro M404dn',                'HP-M404DN-001',   'Drucker und Kopierer',        'HP'),
    ('HP LaserJet Pro M404dn',                'HP-M404DN-002',   'Drucker und Kopierer',        'HP'),
    ('Kyocera TASKalfa 2554ci Kopierer',      'KY-TA2554-001',   'Drucker und Kopierer',        'Kyocera'),
    ('Brother P-touch D610BT Beschriftungsgerät', 'BR-D610BT-001', 'Drucker und Kopierer',      'Brother'),
    -- Audiotechnik
    ('JBL Charge 5 Bluetooth-Lautsprecher',   'JB-CHG5-001',     'Audiotechnik',                'JBL'),
    ('JBL Charge 5 Bluetooth-Lautsprecher',   'JB-CHG5-002',     'Audiotechnik',                'JBL'),
    ('Sennheiser XSW 1 Funkmikrofon-Set',     'SE-XSW1-001',     'Audiotechnik',                'Sennheiser'),
    -- Messgeräte
    ('Fluke 117 Multimeter',                  'FL-117-001',      'Messgeräte',                  'Fluke'),
    ('Fluke LinkIQ Kabeltester',              'FL-LIQ-001',      'Messgeräte',                  'Fluke'),
    ('Rigol DS1054Z Oszilloskop',             'RG-DS1054Z-001',  'Messgeräte',                  'Rigol'),
    ('Rigol DS1054Z Oszilloskop',             'RG-DS1054Z-002',  'Messgeräte',                  'Rigol'),
    ('PeakTech 6225 A Labornetzgerät',        'PT-6225A-001',    'Messgeräte',                  'PeakTech'),
    ('PeakTech 6225 A Labornetzgerät',        'PT-6225A-002',    'Messgeräte',                  'PeakTech'),
    ('PeakTech 6225 A Labornetzgerät',        'PT-6225A-003',    'Messgeräte',                  'PeakTech'),
    -- Werkzeug
    ('Weller WE 1010 Lötstation',             'WL-WE1010-001',   'Werkzeug',                    'Weller'),
    ('Weller WE 1010 Lötstation',             'WL-WE1010-002',   'Werkzeug',                    'Weller'),
    ('Bosch GSR 12V-15 Akkuschrauber',        'BO-GSR12V-001',   'Werkzeug',                    'Bosch'),
    -- Sicherheit
    ('Gloria PD 6 GA Feuerlöscher 6 kg',      'GL-PD6-001',      'Sicherheit und Erste Hilfe',  'Gloria'),
    ('Gloria PD 6 GA Feuerlöscher 6 kg',      'GL-PD6-002',      'Sicherheit und Erste Hilfe',  'Gloria'),
    ('Gloria PD 6 GA Feuerlöscher 6 kg',      'GL-PD6-003',      'Sicherheit und Erste Hilfe',  'Gloria'),

    /* Artikel ohne Seriennummer */
    -- Kabel und Adapter
    ('Patchkabel Cat6 2 m',                   NULL,               'Kabel und Adapter',           'Delock'),
    ('Patchkabel Cat6 5 m',                   NULL,               'Kabel und Adapter',           'Delock'),
    ('HDMI-Kabel 2 m',                        NULL,               'Kabel und Adapter',           'Delock'),
    ('USB-C auf HDMI Adapter',                NULL,               'Kabel und Adapter',           'Delock'),
    ('Kaltgerätekabel 1,8 m',                 NULL,               'Kabel und Adapter',           'Delock'),
    ('RJ45-Stecker Cat6',                     NULL,               'Kabel und Adapter',           'Delock'),
    ('Steckdosenleiste 6-fach mit Schalter',  NULL,               'Kabel und Adapter',           'Brennenstuhl'),
    ('Kabeltrommel 25 m',                     NULL,               'Kabel und Adapter',           'Brennenstuhl'),
    -- Eingabegeräte
    ('Logitech K120 Tastatur',                NULL,               'Eingabegeräte',               'Logitech'),
    ('Logitech B100 Maus',                    NULL,               'Eingabegeräte',               'Logitech'),
    ('Logitech H390 Headset',                 NULL,               'Eingabegeräte',               'Logitech'),
    -- Präsentationstechnik
    ('Logitech R400 Presenter',               NULL,               'Präsentationstechnik',        'Logitech'),
    ('Rollo-Leinwand 200 x 150 cm',           NULL,               'Präsentationstechnik',        'Celexon'),
    -- Drucker und Kopierer
    ('HP 59A Tonerkartusche schwarz',         NULL,               'Drucker und Kopierer',        'HP'),
    -- Schulmöbel
    ('Schülertisch 130 x 50 cm',              NULL,               'Schulmöbel',                  'VS Vereinigte Spezialmöbelfabriken'),
    ('Schülersessel Größe 6',                 NULL,               'Schulmöbel',                  'VS Vereinigte Spezialmöbelfabriken'),
    ('Lehrertisch 140 x 70 cm',               NULL,               'Schulmöbel',                  'VS Vereinigte Spezialmöbelfabriken'),
    ('Drehstuhl für EDV-Räume',               NULL,               'Schulmöbel',                  'VS Vereinigte Spezialmöbelfabriken'),
    ('Aktenkasten abschließbar',              NULL,               'Schulmöbel',                  'Bisley'),
    -- Tafeln und Zubehör
    ('Whiteboard 180 x 120 cm',               NULL,               'Tafeln und Zubehör',          'Legamaster'),
    ('Flipchart mobil',                       NULL,               'Tafeln und Zubehör',          'Legamaster'),
    ('Whiteboard-Löscher magnetisch',         NULL,               'Tafeln und Zubehör',          'Legamaster'),
    ('edding 360 Boardmarker 4er-Set',        NULL,               'Tafeln und Zubehör',          'edding'),
    ('Tafelkreide weiß (100 Stück)',          NULL,               'Tafeln und Zubehör',          'Robercolor'),
    -- Büromaterial
    ('Kopierpapier A4 (500 Blatt)',           NULL,               'Büromaterial',                'Navigator'),
    ('Leitz Ordner 180° A4 8 cm',             NULL,               'Büromaterial',                'Leitz'),
    -- Lehrmittel
    ('Casio fx-991DE X Schulrechner',         NULL,               'Lehrmittel',                  'Casio'),
    ('TI-Nspire CX II-T CAS',                 NULL,               'Lehrmittel',                  'Texas Instruments'),
    ('Globus 30 cm beleuchtet',               NULL,               'Lehrmittel',                  'Columbus'),
    ('Skelettmodell Mensch 1:1',              NULL,               'Lehrmittel',                  '3B Scientific'),
    ('Bresser Erudit DLX Mikroskop',          NULL,               'Lehrmittel',                  'Bresser'),
    -- Mikrocontroller und Robotik
    ('Raspberry Pi 5 (8 GB)',                 NULL,               'Mikrocontroller und Robotik', 'Raspberry Pi'),
    ('Arduino Uno R4 WiFi',                   NULL,               'Mikrocontroller und Robotik', 'Arduino'),
    ('LEGO Education SPIKE Prime Set',        NULL,               'Mikrocontroller und Robotik', 'LEGO Education'),
    ('Breadboard 830 Kontakte',               NULL,               'Mikrocontroller und Robotik', 'Joy-IT'),
    -- Werkzeug
    ('Knipex Seitenschneider 160 mm',         NULL,               'Werkzeug',                    'Knipex'),
    ('Knipex Crimpzange für RJ45',            NULL,               'Werkzeug',                    'Knipex'),
    ('Wera Kraftform Schraubendreher-Set',    NULL,               'Werkzeug',                    'Wera'),
    -- Sportgeräte
    ('Fußball Größe 5',                       NULL,               'Sportgeräte',                 'Molten'),
    ('Volleyball V5M',                        NULL,               'Sportgeräte',                 'Molten'),
    ('Basketball Größe 7',                    NULL,               'Sportgeräte',                 'Molten'),
    ('Springschnur',                          NULL,               'Sportgeräte',                 'Kübler Sport'),
    ('Gymnastikmatte',                        NULL,               'Sportgeräte',                 'Kübler Sport'),
    ('Markierungshütchen (10er-Set)',         NULL,               'Sportgeräte',                 'Kübler Sport'),
    ('Weichbodenmatte 300 x 200 cm',          NULL,               'Sportgeräte',                 'Kübler Sport'),
    ('Casio HS-3V Stoppuhr',                  NULL,               'Sportgeräte',                 'Casio'),
    -- Sicherheit und Erste Hilfe
    ('Erste-Hilfe-Koffer',                    NULL,               N'Sicherheit und Erste Hilfe',  N'Söhngen'),
    ('Schutzbrille',                          NULL,               N'Sicherheit und Erste Hilfe',  N'uvex'),
    -- Bücher
    ('Österreichisches Wörterbuch',           NULL,               N'Bücher',                      N'ÖBV'),
    ('PONS Schulwörterbuch Englisch',         NULL,               N'Bücher',                      N'PONS')
) AS v(Name, Seriennummer, Kategorie, Hersteller)
JOIN dbo.Kategorie k ON k.Name = v.Kategorie
JOIN dbo.Hersteller h ON h.Name = v.Hersteller
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.Gegenstand g
    WHERE g.Seriennummer = v.Seriennummer
       OR (v.Seriennummer IS NULL AND g.Seriennummer IS NULL AND g.Name = v.Name));

/* ---------- Raumbestand ---------- */
-- Der Gegenstand wird über seine Seriennummer gefunden, bei Artikeln ohne Seriennummer über die Bezeichnung.
INSERT INTO dbo.Raumbestand (GegenstandID, RaumID, Menge)
SELECT g.ID, v.RaumID, v.Menge
FROM (VALUES
    -- Hauptlager
    ('DL-P2422H-003',                         'HL',   1),
    ('HP-E24G5-001',                          'HL',   1),
    ('LN-L14G4-002',                          'HL',   1),
    ('LN-M70Q-001',                           'HL',   1),
    ('AP-IPAD10-004',                         'HL',   1),
    ('UB-U6PRO-002',                          'HL',   1),
    ('EP-EBW49-006',                          'HL',   1),
    ('BQ-RP6502-002',                         'HL',   1),
    ('BR-D610BT-001',                         'HL',   1),
    ('JB-CHG5-002',                           'HL',   1),
    ('PT-6225A-003',                          'HL',   1),
    ('Patchkabel Cat6 2 m',                   'HL',   50),
    ('Patchkabel Cat6 5 m',                   'HL',   25),
    ('HDMI-Kabel 2 m',                        'HL',   15),
    ('USB-C auf HDMI Adapter',                'HL',   8),
    ('Kaltgerätekabel 1,8 m',                 'HL',   30),
    ('RJ45-Stecker Cat6',                     'HL',   100),
    ('Steckdosenleiste 6-fach mit Schalter',  'HL',   20),
    ('Kabeltrommel 25 m',                     'HL',   2),
    ('Logitech K120 Tastatur',                'HL',   12),
    ('Logitech B100 Maus',                    'HL',   12),
    ('Logitech H390 Headset',                 'HL',   10),
    ('Logitech R400 Presenter',               'HL',   5),
    ('Rollo-Leinwand 200 x 150 cm',           'HL',   1),
    ('HP 59A Tonerkartusche schwarz',         'HL',   4),
    ('Schülertisch 130 x 50 cm',              'HL',   5),
    ('Schülersessel Größe 6',                 'HL',   10),
    ('Whiteboard 180 x 120 cm',               'HL',   1),
    ('Flipchart mobil',                       'HL',   2),
    ('Whiteboard-Löscher magnetisch',         'HL',   10),
    ('edding 360 Boardmarker 4er-Set',        'HL',   30),
    ('Tafelkreide weiß (100 Stück)',          'HL',   20),
    ('Kopierpapier A4 (500 Blatt)',           'HL',   40),
    ('Leitz Ordner 180° A4 8 cm',             'HL',   50),
    ('Casio fx-991DE X Schulrechner',         'HL',   60),
    ('TI-Nspire CX II-T CAS',                 'HL',   25),
    ('Raspberry Pi 5 (8 GB)',                 'HL',   10),
    ('Arduino Uno R4 WiFi',                   'HL',   8),
    ('Breadboard 830 Kontakte',               'HL',   30),
    ('Erste-Hilfe-Koffer',                    'HL',   2),
    -- Umbuchungslager (zurückgegeben, noch nicht wieder eingelagert)
    ('HP-PB450-001',                          'UL',   1),
    ('USB-C auf HDMI Adapter',                'UL',   2),
    ('Patchkabel Cat6 2 m',                   'UL',   3),
    ('Casio fx-991DE X Schulrechner',         'UL',   4),
    ('Gymnastikmatte',                        'UL',   2),
    -- Klassenzimmer 101 (Tablet-Klasse)
    ('EP-EBW49-001',                          '101',  1),
    ('PA-TC16-001',                           '101',  1),
    ('AP-IPAD10-001',                         '101',  1),
    ('AP-IPAD10-002',                         '101',  1),
    ('AP-IPAD10-003',                         '101',  1),
    ('HDMI-Kabel 2 m',                        '101',  1),
    ('Schülertisch 130 x 50 cm',              '101',  15),
    ('Schülersessel Größe 6',                 '101',  30),
    ('Lehrertisch 140 x 70 cm',               '101',  1),
    ('Tafelkreide weiß (100 Stück)',          '101',  1),
    -- Klassenzimmer 102
    ('EP-EBW49-002',                          '102',  1),
    ('BQ-RP6502-001',                         '102',  1),
    ('HDMI-Kabel 2 m',                        '102',  1),
    ('Schülertisch 130 x 50 cm',              '102',  15),
    ('Schülersessel Größe 6',                 '102',  30),
    ('Lehrertisch 140 x 70 cm',               '102',  1),
    ('Tafelkreide weiß (100 Stück)',          '102',  1),
    -- Klassenzimmer 103
    ('EP-EBW49-003',                          '103',  1),
    ('EL-MXP3-001',                           '103',  1),
    ('HDMI-Kabel 2 m',                        '103',  1),
    ('Rollo-Leinwand 200 x 150 cm',           '103',  1),
    ('Schülertisch 130 x 50 cm',              '103',  15),
    ('Schülersessel Größe 6',                 '103',  30),
    ('Lehrertisch 140 x 70 cm',               '103',  1),
    ('Tafelkreide weiß (100 Stück)',          '103',  1),
    -- Fachraum 205 (Physik und Biologie)
    ('EP-EBW49-004',                          '205',  1),
    ('EL-MXP3-002',                           '205',  1),
    ('Globus 30 cm beleuchtet',               '205',  1),
    ('Skelettmodell Mensch 1:1',              '205',  1),
    ('Bresser Erudit DLX Mikroskop',          '205',  15),
    ('Schutzbrille',                          '205',  20),
    ('Erste-Hilfe-Koffer',                    '205',  1),
    -- EDV-Labor 309
    ('DL-P2422H-001',                         '309',  1),
    ('DL-P2422H-002',                         '309',  1),
    ('DL-OPT7010-001',                        '309',  1),
    ('Patchkabel Cat6 2 m',                   '309',  10),
    ('HDMI-Kabel 2 m',                        '309',  4),
    ('Logitech K120 Tastatur',                '309',  16),
    ('Logitech B100 Maus',                    '309',  16),
    ('Steckdosenleiste 6-fach mit Schalter',  '309',  8),
    ('Drehstuhl für EDV-Räume',               '309',  16),
    ('Whiteboard 180 x 120 cm',               '309',  1),
    ('edding 360 Boardmarker 4er-Set',        '309',  2),
    -- EDV-Labor 310
    ('LN-L14G4-001',                          '310',  1),
    ('DL-OPT7010-002',                        '310',  1),
    ('HDMI-Kabel 2 m',                        '310',  4),
    ('Logitech K120 Tastatur',                '310',  16),
    ('Logitech B100 Maus',                    '310',  16),
    ('Steckdosenleiste 6-fach mit Schalter',  '310',  8),
    ('Drehstuhl für EDV-Räume',               '310',  16),
    ('Whiteboard 180 x 120 cm',               '310',  1),
    ('edding 360 Boardmarker 4er-Set',        '310',  2),
    -- Netzwerklabor 311
    ('CS-C2960X-001',                         '311',  1),
    ('UB-U6PRO-001',                          '311',  1),
    ('APC-BX950-001',                         '311',  1),
    ('Patchkabel Cat6 2 m',                   '311',  20),
    ('Patchkabel Cat6 5 m',                   '311',  5),
    ('RJ45-Stecker Cat6',                     '311',  50),
    ('Knipex Crimpzange für RJ45',            '311',  8),
    ('Wera Kraftform Schraubendreher-Set',    '311',  4),
    ('Drehstuhl für EDV-Räume',               '311',  12),
    ('Whiteboard 180 x 120 cm',               '311',  1),
    -- Elektroniklabor 312
    ('FL-LIQ-001',                            '312',  1),
    ('RG-DS1054Z-001',                        '312',  1),
    ('RG-DS1054Z-002',                        '312',  1),
    ('PT-6225A-001',                          '312',  1),
    ('PT-6225A-002',                          '312',  1),
    ('WL-WE1010-001',                         '312',  1),
    ('GL-PD6-002',                            '312',  1),
    ('Logitech H390 Headset',                 '312',  8),
    ('Raspberry Pi 5 (8 GB)',                 '312',  16),
    ('Arduino Uno R4 WiFi',                   '312',  16),
    ('LEGO Education SPIKE Prime Set',        '312',  6),
    ('Breadboard 830 Kontakte',               '312',  20),
    ('Knipex Seitenschneider 160 mm',         '312',  8),
    ('Schutzbrille',                          '312',  16),
    ('Drehstuhl für EDV-Räume',               '312',  12),
    ('Whiteboard 180 x 120 cm',               '312',  1),
    -- Werkstatt W01
    ('FL-117-001',                            'W01',  1),
    ('WL-WE1010-002',                         'W01',  1),
    ('BO-GSR12V-001',                         'W01',  1),
    ('GL-PD6-001',                            'W01',  1),
    ('Kaltgerätekabel 1,8 m',                 'W01',  5),
    ('Kabeltrommel 25 m',                     'W01',  1),
    ('Knipex Seitenschneider 160 mm',         'W01',  10),
    ('Knipex Crimpzange für RJ45',            'W01',  2),
    ('Wera Kraftform Schraubendreher-Set',    'W01',  5),
    ('Schutzbrille',                          'W01',  10),
    ('Erste-Hilfe-Koffer',                    'W01',  1),
    -- Turnsaal TS1
    ('JB-CHG5-001',                           'TS1',  1),
    ('SE-XSW1-001',                           'TS1',  1),
    ('GL-PD6-003',                            'TS1',  1),
    ('Kabeltrommel 25 m',                     'TS1',  1),
    ('Fußball Größe 5',                       'TS1',  12),
    ('Volleyball V5M',                        'TS1',  12),
    ('Basketball Größe 7',                    'TS1',  10),
    ('Springschnur',                          'TS1',  25),
    ('Gymnastikmatte',                        'TS1',  30),
    ('Markierungshütchen (10er-Set)',         'TS1',  5),
    ('Weichbodenmatte 300 x 200 cm',          'TS1',  2),
    ('Casio HS-3V Stoppuhr',                  'TS1',  10),
    ('Erste-Hilfe-Koffer',                    'TS1',  1),
    -- Bibliothek BIB
    ('Globus 30 cm beleuchtet',               'BIB',  1),
    ('Österreichisches Wörterbuch',           'BIB',  30),
    ('PONS Schulwörterbuch Englisch',         'BIB',  25),
    ('Schülertisch 130 x 50 cm',              'BIB',  6),
    ('Schülersessel Größe 6',                 'BIB',  12),
    -- Konferenzzimmer KONF
    ('LN-L14G4-003',                          'KONF', 1),
    ('EP-EBW49-005',                          'KONF', 1),
    ('HP-M404DN-002',                         'KONF', 1),
    ('HDMI-Kabel 2 m',                        'KONF', 1),
    ('USB-C auf HDMI Adapter',                'KONF', 2),
    ('Steckdosenleiste 6-fach mit Schalter',  'KONF', 2),
    ('Logitech R400 Presenter',               'KONF', 1),
    ('Rollo-Leinwand 200 x 150 cm',           'KONF', 1),
    ('Drehstuhl für EDV-Räume',               'KONF', 20),
    ('Aktenkasten abschließbar',              'KONF', 2),
    ('Flipchart mobil',                       'KONF', 1),
    -- Sekretariat SEK
    ('DL-OPT7010-003',                        'SEK',  1),
    ('DL-P2422H-004',                         'SEK',  1),
    ('HP-M404DN-001',                         'SEK',  1),
    ('KY-TA2554-001',                         'SEK',  1),
    ('Logitech K120 Tastatur',                'SEK',  1),
    ('Logitech B100 Maus',                    'SEK',  1),
    ('HP 59A Tonerkartusche schwarz',         'SEK',  1),
    ('Kopierpapier A4 (500 Blatt)',           'SEK',  10),
    ('Leitz Ordner 180° A4 8 cm',             'SEK',  25),
    ('Aktenkasten abschließbar',              'SEK',  3),
    ('Erste-Hilfe-Koffer',                    'SEK',  1)
) AS v(Gegenstand, RaumID, Menge)
JOIN dbo.Gegenstand g
    ON g.Seriennummer = v.Gegenstand
    OR (g.Seriennummer IS NULL AND g.Name = v.Gegenstand)
JOIN dbo.Raum r ON r.ID = v.RaumID
WHERE NOT EXISTS (SELECT 1 FROM dbo.Raumbestand rb WHERE rb.GegenstandID = g.ID AND rb.RaumID = v.RaumID)
  -- Ein Gerät mit Seriennummer darf nur an einem Ort sein: nicht buchen, wenn es schon in einem Raum liegt oder gerade unterwegs ist
  AND (g.Seriennummer IS NULL OR (
          NOT EXISTS (SELECT 1 FROM dbo.Raumbestand rb WHERE rb.GegenstandID = g.ID)
      AND NOT EXISTS (SELECT 1 FROM dbo.Lagerbewegung l WHERE l.GegenstandID = g.ID AND l.BestaetigtAm IS NULL)));

/* ---------- Lagerbewegung ---------- */
-- Die Historie erklärt den Raumbestand oben: Ende August kommt alles per Wareneingang ins Hauptlager, Anfang September gibt das
-- Hauptlager jedem Raum seine Grundausstattung aus, danach folgen die einzelnen Vorgänge im September (Rückgaben, Reparatur,
-- abgelehnte und zurückgezogene sowie noch offene Transfers). Bucht man alle Bewegungen wie der LagerService, ergibt sich genau der Raumbestand.
-- Daher wird sie nur in eine leere Tabelle eingefügt: Wurde schon in der Anwendung gebucht, hat sich der Bestand inzwischen geändert.
IF NOT EXISTS (SELECT 1 FROM dbo.Lagerbewegung)
BEGIN
    CREATE TABLE #Bewegung (
        GegenstandID int          NOT NULL,
        Menge        int          NOT NULL,
        Bewegungsart nvarchar(20) NOT NULL,
        VonRaumID    varchar(8)   NOT NULL,
        NachRaumID   varchar(8)   NOT NULL,
        ErstelltAm   datetime     NOT NULL,
        BestaetigtAm datetime     NULL);

    -- Einzelne Vorgänge im September. Der Gegenstand wird wie beim Raumbestand über Seriennummer oder Bezeichnung gefunden.
    -- Offen = BestaetigtAm leer. Abgelehnte und zurückgezogene Transfers haben die Bewegungsart "Storniert" und in BestaetigtAm den Zeitpunkt der Stornierung.
    -- Bei einem Wareneingang sind Von- und Nach-Raum gleich und er ist sofort bestätigt.
    INSERT INTO #Bewegung (GegenstandID, Menge, Bewegungsart, VonRaumID, NachRaumID, ErstelltAm, BestaetigtAm)
    SELECT g.ID, v.Menge, v.Bewegungsart, v.Von, v.Nach, v.ErstelltAm, v.BestaetigtAm
    FROM (VALUES
        -- Beamer in 103 defekt: zur Reparatur in die Werkstatt, als Ersatz kommt ein Beamer aus dem Hauptlager
        ('EP-EBW49-006',                    1,  'Reparatur',    '103',  'W01',  '2026-09-08T08:20:00', '2026-09-08T10:05:00'),
        ('EP-EBW49-003',                    1,  'Ausgabe',      'HL',   '103',  '2026-09-08T09:10:00', '2026-09-08T11:40:00'),
        -- 309 will Tastaturen an 310 abgeben, 310 lehnt ab
        ('Logitech K120 Tastatur',          4,  'Storniert',    '309',  '310',  '2026-09-09T09:00:00', '2026-09-09T11:30:00'),
        -- Notebook aus 310 zurückgegeben und wieder im Hauptlager eingelagert
        ('LN-L14G4-002',                    1,  'Rueckgabe',    '310',  'UL',   '2026-09-10T09:30:00', '2026-09-10T12:00:00'),
        ('LN-L14G4-002',                    1,  'Einlagerung',  'UL',   'HL',   '2026-09-11T08:15:00', '2026-09-11T09:00:00'),
        -- Das Elektroniklabor gibt eine Lötstation an die Werkstatt ab
        ('WL-WE1010-002',                   1,  'Ausgabe',      '312',  'W01',  '2026-09-11T10:00:00', '2026-09-11T13:15:00'),
        ('HP-PB450-001',                    1,  'Rueckgabe',    '310',  'UL',   '2026-09-14T10:15:00', '2026-09-14T13:00:00'),
        ('HDMI-Kabel 2 m',                  1,  'Rueckgabe',    '102',  'UL',   '2026-09-15T07:50:00', '2026-09-15T09:30:00'),
        ('Kopierpapier A4 (500 Blatt)',     20, 'Wareneingang', 'HL',   'HL',   '2026-09-15T11:00:00', '2026-09-15T11:00:00'),
        ('HDMI-Kabel 2 m',                  1,  'Einlagerung',  'UL',   'HL',   '2026-09-16T08:00:00', '2026-09-16T08:45:00'),
        ('USB-C auf HDMI Adapter',          2,  'Rueckgabe',    'KONF', 'UL',   '2026-09-16T09:00:00', '2026-09-16T11:30:00'),
        ('Patchkabel Cat6 2 m',             3,  'Rueckgabe',    '309',  'UL',   '2026-09-17T12:40:00', '2026-09-18T08:10:00'),
        -- Der reparierte Beamer kommt ins Hauptlager
        ('EP-EBW49-006',                    1,  'Reparatur',    'W01',  'HL',   '2026-09-18T14:00:00', '2026-09-21T07:40:00'),
        ('Casio fx-991DE X Schulrechner',   4,  'Rueckgabe',    '101',  'UL',   '2026-09-21T11:05:00', '2026-09-21T14:20:00'),
        ('Breadboard 830 Kontakte',         10, 'Wareneingang', '312',  '312',  '2026-09-22T09:30:00', '2026-09-22T09:30:00'),
        ('Gymnastikmatte',                  2,  'Rueckgabe',    'TS1',  'UL',   '2026-09-22T15:30:00', '2026-09-23T07:45:00'),
        -- Das Hauptlager zieht einen Transfer an 205 zurück, 102 lehnt ein Display ab
        ('Raspberry Pi 5 (8 GB)',           5,  'Storniert',    'HL',   '205',  '2026-09-24T10:00:00', '2026-09-24T10:20:00'),
        ('BQ-RP6502-002',                   1,  'Storniert',    'HL',   '102',  '2026-09-25T08:30:00', '2026-09-25T12:00:00'),
        -- Noch offen: warten auf die Freigabe durch den Nach-Raum
        ('CS-ISR1100-001',                  1,  'Ausgabe',      'HL',   '311',  '2026-09-29T10:20:00', NULL),
        ('HDMI-Kabel 2 m',                  2,  'Ausgabe',      'HL',   '309',  '2026-09-30T08:05:00', NULL),
        ('Logitech R400 Presenter',         1,  'Ausgabe',      'HL',   'KONF', '2026-09-30T08:10:00', NULL),
        ('Bresser Erudit DLX Mikroskop',    1,  'Rueckgabe',    '205',  'UL',   '2026-09-30T13:40:00', NULL),
        ('Arduino Uno R4 WiFi',             6,  'Ausgabe',      '312',  '310',  '2026-09-30T14:10:00', NULL)
    ) AS v(Gegenstand, Menge, Bewegungsart, Von, Nach, ErstelltAm, BestaetigtAm)
    JOIN dbo.Gegenstand g
        ON g.Seriennummer = v.Gegenstand
        OR (g.Seriennummer IS NULL AND g.Name = v.Gegenstand);

    -- Bestand jedes Raums nach der Grundausstattung: der heutige Raumbestand ohne das, was die Vorgänge oben bewirkt haben.
    -- Wie im LagerService wird im Von-Raum abgebucht, sobald eine Bewegung angelegt ist (nicht beim Wareneingang),
    -- und im Nach-Raum erst mit der Bestätigung zugebucht. Eine stornierte Bewegung hebt sich auf.
    SELECT x.GegenstandID, x.RaumID, SUM(x.Menge) AS Menge
    INTO #Grundausstattung
    FROM (
        SELECT rb.GegenstandID, rb.RaumID, rb.Menge
        FROM dbo.Raumbestand rb
        UNION ALL
        -- Abgebuchtes wieder dazuzählen
        SELECT b.GegenstandID, b.VonRaumID, b.Menge
        FROM #Bewegung b
        WHERE b.VonRaumID <> b.NachRaumID AND b.Bewegungsart <> 'Storniert'
        UNION ALL
        -- Zugebuchtes wieder abziehen
        SELECT b.GegenstandID, b.NachRaumID, -b.Menge
        FROM #Bewegung b
        WHERE b.BestaetigtAm IS NOT NULL AND b.Bewegungsart <> 'Storniert'
    ) AS x
    GROUP BY x.GegenstandID, x.RaumID
    HAVING SUM(x.Menge) > 0;

    -- Ausgabe der Grundausstattung vom Hauptlager an die Räume (1. bis 4. September, vier Räume am Tag im Abstand von zwei Stunden).
    -- Jeder Raum bekommt alles auf einmal, die verantwortliche Person bestätigt eineinhalb Stunden später.
    INSERT INTO #Bewegung (GegenstandID, Menge, Bewegungsart, VonRaumID, NachRaumID, ErstelltAm, BestaetigtAm)
    SELECT a.GegenstandID, a.Menge, 'Ausgabe', 'HL', a.RaumID, z.Zeit, DATEADD(MINUTE, 90, z.Zeit)
    FROM (
        SELECT ga.GegenstandID, ga.RaumID, ga.Menge, DENSE_RANK() OVER (ORDER BY ga.RaumID) - 1 AS Nr
        FROM #Grundausstattung ga
        WHERE ga.RaumID <> 'HL'
    ) AS a
    CROSS APPLY (SELECT DATEADD(MINUTE, 120 * (a.Nr % 4), DATEADD(DAY, a.Nr / 4, CAST('2026-09-01T08:00:00' AS datetime))) AS Zeit) AS z;

    -- Wareneingang ins Hauptlager: je Gegenstand die gesamte Grundausstattung aller Räume samt Hauptlager
    -- (24. bis 27. August, 30 Gegenstände am Tag im Abstand von zehn Minuten)
    INSERT INTO #Bewegung (GegenstandID, Menge, Bewegungsart, VonRaumID, NachRaumID, ErstelltAm, BestaetigtAm)
    SELECT w.GegenstandID, w.Menge, 'Wareneingang', 'HL', 'HL', z.Zeit, z.Zeit
    FROM (
        SELECT ga.GegenstandID, SUM(ga.Menge) AS Menge, ROW_NUMBER() OVER (ORDER BY ga.GegenstandID) - 1 AS Nr
        FROM #Grundausstattung ga
        GROUP BY ga.GegenstandID
    ) AS w
    CROSS APPLY (SELECT DATEADD(MINUTE, 10 * (w.Nr % 30), DATEADD(DAY, w.Nr / 30, CAST('2026-08-24T08:00:00' AS datetime))) AS Zeit) AS z;

    -- Angelegt hat jede Bewegung die Person, die für den Von-Raum verantwortlich ist (Zuständigkeitsregel im LagerService).
    -- Sortiert eingefügt, damit die IDs der zeitlichen Reihenfolge folgen
    INSERT INTO dbo.Lagerbewegung (Menge, ErstelltAm, BestaetigtAm, BewegungsartID, VonRaumID, NachRaumID, GegenstandID, PersonID)
    SELECT b.Menge, b.ErstelltAm, b.BestaetigtAm, ba.ID, b.VonRaumID, b.NachRaumID, b.GegenstandID, von.PersonID
    FROM #Bewegung b
    JOIN dbo.Bewegungsart ba ON ba.Name = b.Bewegungsart
    JOIN dbo.Raum von ON von.ID = b.VonRaumID
    WHERE von.PersonID IS NOT NULL
    ORDER BY b.ErstelltAm, b.GegenstandID;

    DROP TABLE #Grundausstattung;
    DROP TABLE #Bewegung;
END
ELSE
BEGIN
    PRINT 'Lagerbewegung: Es gibt schon Lagerbewegungen, daher wird keine Historie eingefügt.';
END;

COMMIT TRANSACTION;

/* ---------- Kontrolle: Anzahl der Datensätze je Tabelle ---------- */
SELECT 'Rolle' AS Tabelle, COUNT(*) AS Anzahl FROM dbo.Rolle
UNION ALL SELECT 'Raumart',       COUNT(*) FROM dbo.Raumart
UNION ALL SELECT 'Bewegungsart',  COUNT(*) FROM dbo.Bewegungsart
UNION ALL SELECT 'Kategorie',     COUNT(*) FROM dbo.Kategorie
UNION ALL SELECT 'Hersteller',    COUNT(*) FROM dbo.Hersteller
UNION ALL SELECT 'Person',        COUNT(*) FROM dbo.Person
UNION ALL SELECT 'Raum',          COUNT(*) FROM dbo.Raum
UNION ALL SELECT 'Gegenstand',    COUNT(*) FROM dbo.Gegenstand
UNION ALL SELECT 'Raumbestand',   COUNT(*) FROM dbo.Raumbestand
UNION ALL SELECT 'Lagerbewegung', COUNT(*) FROM dbo.Lagerbewegung;
GO