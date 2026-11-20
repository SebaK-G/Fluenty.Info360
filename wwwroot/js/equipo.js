// Pantalla Equipo (Views/Equipo/Index.cshtml)

// Guarda qué hace la gente de un rango sin recargar la página.
// Llama a EquipoController.GuardarTareas con fetch y muestra el mensaje que devuelve.
function guardarTareas(idRango) {
    let tareas = document.getElementById("Tareas-" + idRango).value;
    let mensaje = document.getElementById("Mensaje-" + idRango);

    // Validación del lado del cliente (el servidor vuelve a chequear lo mismo)
    if (tareas.length > 300) {
        mensaje.innerHTML = "Puede tener hasta 300 letras.";
        return;
    }

    mensaje.innerHTML = "Guardando...";
    // encodeURIComponent deja el texto listo para ir en la URL (espacios, comas, tildes)
    fetch('/Equipo/GuardarTareas?IdRango=' + idRango + '&Tareas=' + encodeURIComponent(tareas), {
        method: 'GET',
        headers: { 'Content-Type': 'application/json' },
    })
    .then(response => response.json())
    .then(data => {
        mensaje.innerHTML = data.mensaje;
    })
    .catch(error => {
        console.error(error);
        mensaje.innerHTML = "No se pudo guardar. Probá de nuevo.";
    });
}
