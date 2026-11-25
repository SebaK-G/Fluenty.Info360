// Reproductor de lecciones (Views/Leccion/Index.cshtml)
// Se ve una sola tarjeta por vez. Las preguntas, el "Probalo vos" y las reexplicaciones
// se mandan con fetch a LeccionController y se muestra la respuesta sin recargar la página.

// Number() pasa el texto del input a número (si no, "3" + 1 daría "31")
const total = Number(document.getElementById("total-tarjetas").value);
let actual = Number(document.getElementById("tarjeta-inicial").value);

// Para cada pregunta, la próxima versión que le toca si se equivocó (el lugar del array es el número de tarjeta)
let proximas = [];

// Pone un texto en un elemento y lo muestra; si el texto está vacío, lo oculta.
// innerText (y no innerHTML) muestra el texto tal cual, sin interpretarlo como HTML.
function ponerTexto(idElemento, texto) {
    let elemento = document.getElementById(idElemento);
    elemento.innerText = texto;
    if (texto == "") {
        elemento.style.display = "none";
    } else {
        elemento.style.display = "block";
    }
}

function estaResuelta(numero) {
    return document.getElementById("resuelta-" + numero).value == "si";
}

// Muestra la tarjeta "numero" y oculta las demás (la clase "oculto" es la que las esconde)
function mostrar(numero) {
    if (numero < 1 || numero > total) {
        return;
    }
    // No se puede avanzar si la tarjeta actual no está resuelta (pregunta bien contestada o práctica aprobada)
    if (numero > actual && !estaResuelta(actual)) {
        return;
    }
    for (let i = 1; i <= total; i++) {
        // La tarjeta que se ve no tiene la clase "oculto"; las demás sí
        let clase = "tarjeta oculto";
        if (i == numero) {
            clase = "tarjeta";
        }
        document.getElementById("tarjeta-" + i).className = clase;
    }
    actual = numero;
    document.getElementById("relleno").style.width = (numero * 100 / total) + "%";
    document.getElementById("numero-tarjeta").innerText = numero + " de " + total;
    document.getElementById("atras").disabled = numero == 1;
    actualizarSiguiente();
    window.speechSynthesis.cancel();
    window.scrollTo(0, 0);
}

function anterior() {
    mostrar(actual - 1);
}

function siguiente() {
    mostrar(actual + 1);
}

// El botón "Continuar": solo se puede tocar si la tarjeta está resuelta. En la última tarjeta no se ve.
function actualizarSiguiente() {
    let boton = document.getElementById("siguiente");
    let resuelta = estaResuelta(actual);
    if (actual == total) {
        boton.style.opacity = "0";
    } else {
        boton.style.opacity = "1";
    }
    boton.disabled = !resuelta || actual == total;
    if (resuelta || actual == total) {
        document.getElementById("aviso-siguiente").style.display = "none";
    } else {
        document.getElementById("aviso-siguiente").style.display = "block";
    }
}

// Habilita o deshabilita los 3 botones de opción de una pregunta
function habilitarOpciones(numero, habilitar) {
    document.getElementById("opcion-" + numero + "-A").disabled = !habilitar;
    document.getElementById("opcion-" + numero + "-B").disabled = !habilitar;
    document.getElementById("opcion-" + numero + "-C").disabled = !habilitar;
}

// ===== Preguntas: se corrige en el servidor. Si se equivoca, le toca otra pregunta del mismo tema =====
function responder(numero, letra) {
    let idTarjeta = document.getElementById("id-tarjeta-" + numero).value;
    habilitarOpciones(numero, false);
    mostrarCarga("Corrigiendo...");
    fetch('/Leccion/Responder?IdTarjeta=' + idTarjeta + '&Letra=' + letra, {
        method: 'GET',
        headers: { 'Content-Type': 'application/json' },
    })
    .then(response => response.json())
    .then(data => {
        ocultarCarga();
        if (data.error != "") {
            ponerTexto("mensaje-" + numero, data.error);
            habilitarOpciones(numero, true);
            return;
        }
        // Se pinta de verde la correcta (y de rojo la elegida si estaba mal)
        document.getElementById("opcion-" + numero + "-" + data.correcta).className = "opcion opcion-bien";
        document.getElementById("explicacion-" + numero).innerText = data.explicacion;
        if (data.bien) {
            document.getElementById("veredicto-" + numero).innerText = "¡Bien ahí! 🎉";
            ponerTexto("reexplicacion-" + numero, "");
            document.getElementById("otra-" + numero).style.display = "none";
            document.getElementById("resuelta-" + numero).value = "si";
        } else {
            document.getElementById("opcion-" + numero + "-" + letra).className = "opcion opcion-mal";
            document.getElementById("veredicto-" + numero).innerText = "Esta vez no. Leé por qué y probá con otra pregunta del mismo tema:";
            if (data.reexplicacion != "") {
                ponerTexto("reexplicacion-" + numero, "📘 Te lo explicamos de otra forma: " + data.reexplicacion);
            }
            document.getElementById("otra-" + numero).style.display = "block";
            proximas[numero] = data.siguiente;
        }
        document.getElementById("devolucion-" + numero).className = "caja separado";
        actualizarSiguiente();
    })
    .catch(error => {
        console.error(error);
        ocultarCarga();
        ponerTexto("mensaje-" + numero, "Se cortó la conexión. Probá de nuevo.");
        habilitarOpciones(numero, true);
    });
}

// Muestra el texto de una opción (si está vacío, la opción no se ve)
function ponerOpcion(numero, letra, texto) {
    let boton = document.getElementById("opcion-" + numero + "-" + letra);
    boton.innerText = texto;
    boton.className = "opcion";
    if (texto == "") {
        boton.style.display = "none";
    } else {
        boton.style.display = "block";
    }
}

// Cambia la pregunta por la nueva versión (mismo tema, otro caso)
function otraPregunta(numero) {
    let nueva = proximas[numero];
    if (nueva == null) {
        return;
    }
    document.getElementById("id-tarjeta-" + numero).value = nueva.idTarjeta;
    document.getElementById("leer-" + numero).value = nueva.titulo + ". " + nueva.texto;
    document.getElementById("titulo-" + numero).innerText = nueva.titulo;
    document.getElementById("texto-" + numero).innerText = nueva.texto;
    ponerTexto("contexto-" + numero, nueva.contexto);
    ponerOpcion(numero, "A", nueva.opcionA);
    ponerOpcion(numero, "B", nueva.opcionB);
    ponerOpcion(numero, "C", nueva.opcionC);
    habilitarOpciones(numero, true);
    document.getElementById("devolucion-" + numero).className = "caja separado oculto";
    ponerTexto("reexplicacion-" + numero, "");
    proximas[numero] = null;
    window.scrollTo(0, 0);
}

// ===== "Probalo vos": la IA corrige; hay que aprobarlo para seguir =====
function practicar(numero) {
    let idTarjeta = document.getElementById("id-tarjeta-" + numero).value;
    let texto = document.getElementById("respuesta-" + numero).value;
    let devolucion = document.getElementById("devolucion-" + numero);

    // Validación del lado del cliente (el servidor vuelve a chequear lo mismo)
    if (texto.length < 10) {
        devolucion.style.display = "block";
        document.getElementById("veredicto-" + numero).innerText = "";
        document.getElementById("texto-devolucion-" + numero).innerText = "Escribí tu pedido completo a la IA (al menos una oración) y lo corregimos.";
        return;
    }

    mostrarCarga("Corrigiendo tu respuesta...");
    // encodeURIComponent deja el texto listo para ir en la URL (espacios, signos, tildes)
    fetch('/Leccion/Practicar?IdTarjeta=' + idTarjeta + '&Respuesta=' + encodeURIComponent(texto), {
        method: 'GET',
        headers: { 'Content-Type': 'application/json' },
    })
    .then(response => response.json())
    .then(data => {
        ocultarCarga();
        devolucion.style.display = "block";
        if (data.error != "") {
            document.getElementById("veredicto-" + numero).innerText = "";
            document.getElementById("texto-devolucion-" + numero).innerText = data.error;
            return;
        }
        document.getElementById("texto-devolucion-" + numero).innerText = data.devolucion;
        if (data.intentos > 0) {
            document.getElementById("intentos-" + numero).innerText = "Intentos: " + data.intentos;
        }
        if (data.ejemplo != "") {
            document.getElementById("texto-ejemplo-" + numero).innerText = data.ejemplo;
            document.getElementById("ejemplo-" + numero).style.display = "block";
        }
        if (data.aprobado) {
            document.getElementById("veredicto-" + numero).innerText = "✅ ¡Aprobado!";
            devolucion.className = "caja separado devolucion-aprobada";
            document.getElementById("corregir-" + numero).style.display = "none";
            document.getElementById("respuesta-" + numero).disabled = true;
            document.getElementById("resuelta-" + numero).value = "si";
        } else {
            document.getElementById("veredicto-" + numero).innerText = "🧑‍🏫 Todavía no. Mejoralo con esto:";
        }
        actualizarSiguiente();
    })
    .catch(error => {
        console.error(error);
        ocultarCarga();
        devolucion.style.display = "block";
        document.getElementById("texto-devolucion-" + numero).innerText = "Se cortó la conexión. Probá de nuevo.";
    });
}

// ===== "Explicamelo más simple" / "Dame otro ejemplo" (usa pedidos a la IA del día) =====
function reexplicar(numero, modo) {
    let idTarjeta = document.getElementById("id-tarjeta-" + numero).value;
    if (modo == "simple") {
        mostrarCarga("Buscando otra forma de explicarlo...");
    } else {
        mostrarCarga("Buscando otro ejemplo...");
    }
    fetch('/Leccion/Reexplicar?IdTarjeta=' + idTarjeta + '&Modo=' + modo, {
        method: 'GET',
        headers: { 'Content-Type': 'application/json' },
    })
    .then(response => response.json())
    .then(data => {
        ocultarCarga();
        if (!data.ok) {
            ponerTexto("mensaje-" + numero, data.mensaje);
            if (data.sinPedidos) {
                document.getElementById("pedidos").innerText = "0";
            }
            return;
        }
        document.getElementById("titulo-" + numero).innerText = data.titulo;
        document.getElementById("texto-" + numero).innerText = data.texto;
        document.getElementById("leer-" + numero).value = data.titulo + ". " + data.texto;
        document.getElementById("pedidos").innerText = data.restantesHoy;
        if (data.restantesTarjeta > 0) {
            ponerTexto("mensaje-" + numero, "Podés pedir otra explicación " + data.restantesTarjeta + " vez/veces más en esta tarjeta.");
        } else {
            ponerTexto("mensaje-" + numero, "");
        }
    })
    .catch(error => {
        console.error(error);
        ocultarCarga();
        ponerTexto("mensaje-" + numero, "Se cortó la conexión. Probá de nuevo.");
    });
}

// Copia el pedido bueno para que lo pegue en ChatGPT (o la IA que use) y lo aproveche hoy mismo.
// navigator.clipboard es la forma que tiene el navegador de copiar al portapapeles.
function copiar(numero) {
    navigator.clipboard.writeText(document.getElementById("pedido-bueno-" + numero).value);
    document.getElementById("copiar-" + numero).innerText = "✓ Copiado: pegalo en tu IA y cambiá lo que está entre [corchetes]";
}

// Lee la tarjeta en voz alta (sirve mucho a quien le cuesta leer en pantalla).
// speechSynthesis es la voz que trae el navegador.
function escuchar(numero) {
    window.speechSynthesis.cancel();
    let voz = new SpeechSynthesisUtterance(document.getElementById("leer-" + numero).value);
    voz.lang = "es-AR";
    voz.rate = 0.95;
    window.speechSynthesis.speak(voz);
}

// Si vuelve a una lección a medias, las tarjetas de antes de la inicial ya están resueltas: arranca donde le toca
mostrar(actual);
