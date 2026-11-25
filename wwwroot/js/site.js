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

// Formularios: pinta de azul las opciones elegidas (los div con clase "boton-chico eleccion", que tienen un radio o checkbox adentro).
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

// Formularios de 3 pasos: muestra solo el paso que se eligió y esconde los otros
function irAPaso(numero) {
    let pasos = document.getElementsByClassName("paso-formulario");
    for (let i = 0; i < pasos.length; i++) {
        pasos[i].className = "tarjeta paso-formulario oculto";
    }
    document.getElementById("paso-" + numero).className = "tarjeta paso-formulario";
    window.scrollTo(0, 0);
}

// Al mandar: si falta algo, se muestran los 3 pasos para que el navegador marque qué falta
function revisarPasos() {
    let pasos = document.getElementsByClassName("paso-formulario");
    let formulario = pasos[0].parentElement;
    if (!formulario.checkValidity()) {
        for (let i = 0; i < pasos.length; i++) {
            pasos[i].className = "tarjeta paso-formulario";
        }
    }
}

// Empleados: muestra solo los renglones cuyo nombre tiene lo que se escribió en el buscador
function buscarEmpleado() {
    let texto = document.getElementById("buscar-empleado").value.toLowerCase();
    let empleados = document.getElementsByClassName("empleado");
    for (let i = 0; i < empleados.length; i++) {
        let nombre = empleados[i].innerText.toLowerCase();
        if (nombre.includes(texto)) {
            empleados[i].style.display = "grid";
        } else {
            empleados[i].style.display = "none";
        }
    }
}
