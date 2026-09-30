USE [Lagerverwaltung];
GO

SET NOCOUNT ON;
-- Falls ein Fehler vorkommt, wird die ganze Aktion zurückgenommen, um halbfertige Dateneinträge zu vermeiden
SET XACT_ABORT ON;

BEGIN TRANSACTION;

/* ---------- Rolle ---------- */
-- "LehrerIn" muss genau so heißen, da neu registrierte Benutzer diese Rolle bekommen (Rolle.LehrerIn)
INSERT INTO dbo.Rolle (Name, [Admin])
SELECT v.Name, v.[Admin]
FROM (VALUES
    (N'Administrator', 1),
    (N'LehrerIn',      0),
    (N'Laborvorstand', 0)
) AS v(Name, [Admin])
WHERE NOT EXISTS (SELECT 1 FROM dbo.Rolle r WHERE r.Name = v.Name);

/* ---------- Raumart (höchstens 20 Zeichen) ---------- */
INSERT INTO dbo.Raumart (Name)
SELECT v.Name
FROM (VALUES
    (N'Hauptlager'),
    (N'Umbuchungslager'),
    (N'Labor'),
    (N'Werkstatt')
) AS v(Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Raumart r WHERE r.Name = v.Name);

/* ---------- Bewegungsart (höchstens 20 Zeichen) ---------- */
INSERT INTO dbo.Bewegungsart (Name)
SELECT v.Name
FROM (VALUES
    (N'Wareneingang'),
    (N'Ausgabe'),
    (N'Rueckgabe'),
    (N'Einlagerung')
) AS v(Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Bewegungsart b WHERE b.Name = v.Name);

/* ---------- Kategorie (höchstens 30 Zeichen) ---------- */
INSERT INTO dbo.Kategorie (Name)
SELECT v.Name
FROM (VALUES
    (N'Monitore'),
    (N'Notebooks'),
    (N'Desktop-PCs'),
    (N'Netzwerktechnik'),
    (N'Kabel und Adapter'),
    (N'Eingabegeräte'),
    (N'Messgeräte')
) AS v(Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Kategorie k WHERE k.Name = v.Name);

/* ---------- Hersteller (höchstens 50 Zeichen) ---------- */
INSERT INTO dbo.Hersteller (Name)
SELECT v.Name
FROM (VALUES
    (N'Dell'),
    (N'HP'),
    (N'Lenovo'),
    (N'Cisco'),
    (N'Logitech'),
    (N'Fluke'),
    (N'Delock')
) AS v(Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Hersteller h WHERE h.Name = v.Name);