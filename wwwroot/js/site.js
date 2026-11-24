// Funciones que usan varias pantallas. Este archivo lo carga _Layout.cshtml en todas las páginas.

// Muestra el cartel de "cargando" mientras la IA trabaja.
// Las pantallas que lo usan tienen un <div id="carga">. Si mandan un texto, se muestra en id="texto-carga".
function mostrarCarga(texto) {
    document.getElementById("carga").style.display = "flex";
    let textoCarga = document.getElementById("texto-carga");
    if (texto != undefined && textoCarga != null) {
        textoCarga.innerHTML = texto;
    }
}

function ocultarCarga() {
    let carga = document.getElementById("carga");
    if (carga != null) {
        carga.style.display = "none";
    }
}

// Si la persona vuelve con el botón "atrás" del navegador, la página se muestra como la dejó:
// por eso cerramos el cartel de carga cada vez que la página se vuelve a mostrar.
window.onpageshow = function () {
    ocultarCarga();
};

// Formularios: pinta de azul las opciones elegidas (los <label class="boton-chico eleccion"> con un radio o checkbox adentro).
// La clase "boton-chico-activo" es la misma que marca el botón chico elegido en toda la app.
// Cada input llama a esta función cuando cambia (onchange).
function pintarElecciones() {
    let opciones = document.getElementsByClassName("eleccion");
    for (let i = 0; i < opciones.length; i++) {
        let input = opciones[i].getElementsByTagName("input")[0];
        if (input.checked) {
            opciones[i].className = "boton-chico eleccion boton-chico-activo";
        } else {
            opciones[i].className = "boton-chico eleccion";
        }
    }
}

// Desactiva un botón (por ejemplo, para que no se mande dos veces un formulario mientras carga)
function desactivarBoton(idBoton) {
    document.getElementById(idBoton).disabled = true;
}
