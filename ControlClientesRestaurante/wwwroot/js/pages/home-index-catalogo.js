let categoriaActiva = 'todas';
const quitarAcentos = s => (s || '').toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');

function marcarCategoria(btn) {
    document.querySelectorAll('#cartaCategoryFilter .btn-ghost').forEach(b => {
        b.classList.remove('active');
        b.style.background = '';
        b.style.color = '';
    });
    if (btn) { btn.classList.add('active'); btn.style.background = '#1F120C'; btn.style.color = '#FFFFFF'; }
}

function filtrarCartaCategoria(cat, btn) {
    categoriaActiva = cat;
    const caja = document.getElementById('cartaBuscar');
    if (caja) caja.value = '';
    marcarCategoria(btn);
    aplicarFiltrosCarta();
}

// La búsqueda mira toda la carta (como en las cadenas): ignora la categoría mientras haya texto
function buscarEnCarta(valor, enfocar) {
    const caja = document.getElementById('cartaBuscar');
    if (caja && caja.value !== valor) caja.value = valor;
    if (valor.trim()) marcarCategoria(null);
    else { categoriaActiva = 'todas'; marcarCategoria(document.querySelector('#cartaCategoryFilter .btn-ghost')); }
    aplicarFiltrosCarta();
    if (enfocar && caja) caja.focus();
}

function aplicarFiltrosCarta() {
    const q = quitarAcentos((document.getElementById('cartaBuscar') || {}).value).trim();
    const buscando = q.length > 0;
    let visibles = 0;

    document.querySelectorAll('.dish-card-minimal').forEach(card => {
        const cat = card.getAttribute('data-categoria') || '';
        let visible = true;
        if (buscando) {
            const texto = (card.querySelector('.dish-minimal-name')?.textContent || '') + ' ' + (card.querySelector('.dish-minimal-desc')?.textContent || '') + ' ' + cat;
            visible = quitarAcentos(texto).includes(q);
        } else if (categoriaActiva !== 'todas' && !cat.toLowerCase().includes(categoriaActiva.toLowerCase())) {
            visible = false;
        }
        card.style.display = visible ? 'flex' : 'none';
        if (visible) visibles++;
    });

    document.querySelectorAll('.combo-card').forEach(c => {
        const visible = !buscando || quitarAcentos(c.querySelector('h4')?.textContent + ' ' + c.querySelector('p')?.textContent).includes(q);
        c.style.display = visible ? '' : 'none';
        if (visible && buscando) visibles++;
    });
    const fila = document.getElementById('combosRow');
    if (fila) fila.style.display = (buscando && ![...fila.querySelectorAll('.combo-card')].some(c => c.style.display !== 'none')) ? 'none' : '';

    document.getElementById('cartaSinResultados').hidden = !(buscando && visibles === 0);
    document.getElementById('cartaBuscarLimpiar').hidden = !buscando;
}

// Desde la lupa de la barra superior: /Carta?buscar=1 deja el cursor en la caja
document.addEventListener('DOMContentLoaded', () => {
    const q = new URLSearchParams(location.search).get('q');
    if (q) { buscarEnCarta(q); document.getElementById('cartaBuscar')?.scrollIntoView({ block: 'center' }); }
});
