// Formato de pesos colombianos para usar en las pantallas
window.cop = (n) => new Intl.NumberFormat('es-CO', { style: 'currency', currency: 'COP', maximumFractionDigits: 0 }).format(n || 0);

// Confirmación para formularios peligrosos: <form data-confirm="¿Seguro?">
document.addEventListener('submit', (e) => {
    const msg = e.target.getAttribute('data-confirm');
    if (msg && !confirm(msg)) e.preventDefault();
});

// Las alertas de éxito se cierran solas
setTimeout(() => {
    document.querySelectorAll('.alert-success').forEach(a => bootstrap.Alert.getOrCreateInstance(a).close());
}, 6000);
