// Lucide-Icons: ersetzt alle <i data-lucide="name"></i> durch das passende SVG.
// Strichstärke 1.5 wie im Design-System, die Größe kommt aus site.css (.lucide)
lucide.createIcons({ attrs: { 'stroke-width': 1.5 } });

// Suchfeld über einer Tabelle (data-tabellen-suche="#tabellenID"): blendet Zeilen aus, die den Suchbegriff nicht enthalten.
// Ausgeblendete Zeilen bleiben im Formular, schon eingetragene Mengen werden also trotzdem mitgeschickt
document.querySelectorAll('[data-tabellen-suche]').forEach(function (suchfeld) {
    var zeilen = document.querySelectorAll(suchfeld.dataset.tabellenSuche + ' tbody tr');
    suchfeld.addEventListener('input', function () {
        var begriff = suchfeld.value.trim().toLowerCase();
        zeilen.forEach(function (zeile) {
            zeile.hidden = begriff !== '' && !zeile.textContent.toLowerCase().includes(begriff);
        });
    });
});

// Filter als Auswahlliste (data-auto-absenden): schickt das Formular sofort ab, wenn sich die Auswahl ändert
document.querySelectorAll('[data-auto-absenden]').forEach(function (auswahl) {
    auswahl.addEventListener('change', function () {
        auswahl.form.submit();
    });
});

// Auswahlliste mit Links als Werten (data-link-auswahl, z. B. "Weitere Kategorien"): öffnet den gewählten Link
document.querySelectorAll('[data-link-auswahl]').forEach(function (auswahl) {
    auswahl.addEventListener('change', function () {
        if (auswahl.value) {
            window.location.href = auswahl.value;
        }
    });
});

// Tabellenzeile mit data-href: ein Klick irgendwo in der Zeile öffnet den Link (Links und Buttons in der Zeile gehen vor)
document.querySelectorAll('tr[data-href]').forEach(function (zeile) {
    zeile.addEventListener('click', function (e) {
        if (!e.target.closest('a, button, input, select, label')) {
            window.location.href = zeile.dataset.href;
        }
    });
});
