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
