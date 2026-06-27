// ─── Toast ──────────────────────────────────────────────────────────
function showToast(msg, type = 'info') {
    const t = document.getElementById('toast');
    if (!t) return;
    t.textContent = msg;
    t.className = `toast show ${type}`;
    setTimeout(() => t.classList.remove('show'), 3000);
}

// ─── Modal ──────────────────────────────────────────────────────────
function showModal(innerHtml) {
    const overlay = document.getElementById('modal-overlay');
    const box = document.getElementById('modal-box');
    if (!overlay || !box) return;
    box.innerHTML = innerHtml;
    overlay.style.display = 'flex';
    overlay.onclick = (e) => { if (e.target === overlay) closeModal(); };
}

function closeModal() {
    const overlay = document.getElementById('modal-overlay');
    if (overlay) overlay.style.display = 'none';
}

// ─── Escape cierra modal ─────────────────────────────────────────────
document.addEventListener('keydown', e => {
    if (e.key === 'Escape') closeModal();
});

// ─── Cambio de empresa ──────────────────────────────────────────────
function toggleCompanyMenu(e) {
    e.stopPropagation();
    document.getElementById('company-menu')?.classList.toggle('open');
}

function switchCompany(id) {
    const base = window.APP_BASE || '';
    fetch(base + '/Auth/SwitchCompany', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ companyId: id })
    })
    .then(r => r.json())
    .then(d => {
        if (d.success) location.href = base + '/Dashboard';
        else showToast(d.message || 'Error al cambiar empresa.', 'error');
    })
    .catch(() => showToast('Error de comunicación.', 'error'));
}

document.addEventListener('click', function(e) {
    const sw = document.getElementById('company-switcher');
    if (sw && !sw.contains(e.target))
        document.getElementById('company-menu')?.classList.remove('open');
});

// ─── Selector de tema ────────────────────────────────────────────────
(function initTheme() {
    const current = document.documentElement.getAttribute('data-theme') || 'light';

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem('wms-theme', theme);
        document.querySelectorAll('.theme-btn').forEach(btn =>
            btn.classList.toggle('active', btn.dataset.theme === theme));
    }

    document.querySelectorAll('.theme-btn').forEach(btn => {
        btn.classList.toggle('active', btn.dataset.theme === current);
        btn.addEventListener('click', () => applyTheme(btn.dataset.theme));
    });
})();

// ─── Ordenamiento de columnas (doble clic en encabezado) ─────────────
(function initTableSort() {
    document.querySelectorAll('.data-table').forEach(table => {
        const headers = Array.from(table.querySelectorAll('thead th'));
        let sortCol = -1;
        let sortAsc  = true;

        headers.forEach((th, colIdx) => {
            if (colIdx === 0) return; // saltar columna checkbox
            th.classList.add('sortable');
            th.title = 'Doble clic para ordenar';

            th.addEventListener('dblclick', () => {
                if (sortCol === colIdx) {
                    sortAsc = !sortAsc;
                } else {
                    sortCol = colIdx;
                    sortAsc = true;
                }

                headers.forEach(h => h.classList.remove('sort-asc', 'sort-desc'));
                th.classList.add(sortAsc ? 'sort-asc' : 'sort-desc');

                const tbody = table.querySelector('tbody');
                const rows  = Array.from(tbody.querySelectorAll('tr'));
                if (rows.length === 1 && rows[0].querySelector('.empty-row')) return;

                rows.sort((a, b) => {
                    const aText = (a.cells[colIdx]?.textContent ?? '').trim();
                    const bText = (b.cells[colIdx]?.textContent ?? '').trim();

                    // Intentar orden numérico
                    const aNum = parseFloat(aText.replace(/[^\d.-]/g, ''));
                    const bNum = parseFloat(bText.replace(/[^\d.-]/g, ''));
                    const cmp  = (!isNaN(aNum) && !isNaN(bNum))
                        ? aNum - bNum
                        : aText.localeCompare(bText, 'es', { sensitivity: 'base' });

                    return sortAsc ? cmp : -cmp;
                });

                rows.forEach(r => tbody.appendChild(r));
            });
        });
    });
})();
