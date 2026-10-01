// Lucide-Icons: ersetzt alle <i data-lucide="name"></i> durch das passende SVG.
// Strichstärke 1.5 wie im Design-System, die Größe kommt aus site.css (.lucide)
lucide.createIcons({ attrs: { 'stroke-width': 1.5 } });

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

// Formular mit data-bestaetigen (z. B. Transfer ablehnen oder zurückziehen): erst nach einer Rückfrage abschicken
document.querySelectorAll('form[data-bestaetigen]').forEach(function (formular) {
    formular.addEventListener('submit', function (e) {
        if (!window.confirm(formular.dataset.bestaetigen)) {
            e.preventDefault();
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
