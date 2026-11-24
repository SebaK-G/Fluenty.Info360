// Pantalla Cursos (Views/Curso/Index.cshtml)

// Muestra u oculta el tema y la categoría según si el curso es público.
// Si es público, el tema pasa a ser obligatorio (required).
function mostrarPublico(idRango, publico) {
    let caja = document.getElementById("publico-" + idRango);
    if (publico) {
        caja.style.display = "flex";
    } else {
        caja.style.display = "none";
    }
    document.getElementById("Tema-" + idRango).required = publico;
}
