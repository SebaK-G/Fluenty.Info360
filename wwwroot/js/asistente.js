// Pantalla Asistente (Views/Asistente/Index.cshtml)

// Que se vea el último mensaje: baja la página hasta el final
function bajarAlFinal() {
    window.scrollTo(0, document.body.scrollHeight);
}

// Manda la pregunta con fetch a AsistenteController.Enviar y muestra la respuesta sin recargar la página
function enviar() {
    let cuadro = document.getElementById("texto");
    let texto = cuadro.value;
    let error = document.getElementById("error-chat");

    // Validación del lado del cliente (el servidor vuelve a chequear)
    if (texto == "") {
        return;
    }
    if (texto.length > 2000) {
        error.innerHTML = "La pregunta puede tener hasta 2000 letras.";
        error.style.display = "block";
        return;
    }

    // Mientras la IA responde: se ve la pregunta y los puntitos
    let bienvenida = document.getElementById("bienvenida");
    if (bienvenida != null) {
        bienvenida.style.display = "none";
    }
    error.style.display = "none";
    // innerText (y no innerHTML) muestra lo que escribió tal cual, sin interpretarlo como HTML
    document.getElementById("texto-mi-pregunta").innerText = texto;
    document.getElementById("mi-pregunta").style.display = "flex";
    document.getElementById("escribiendo").style.display = "flex";
    document.getElementById("boton-enviar").disabled = true;
    cuadro.value = "";
    bajarAlFinal();

    let idChat = document.getElementById("IdChat").value;
    // encodeURIComponent deja el texto listo para ir en la URL (espacios, signos, saltos de línea)
    fetch('/Asistente/Enviar?IdChat=' + idChat + '&Texto=' + encodeURIComponent(texto), {
        method: 'GET',
        headers: { 'Content-Type': 'application/json' },
    })
    .then(response => response.json())
    .then(data => {
        mostrarRespuesta(data, texto);
    })
    .catch(error => {
        console.error(error);
        document.getElementById("mi-pregunta").style.display = "none";
        document.getElementById("escribiendo").style.display = "none";
        document.getElementById("boton-enviar").disabled = false;
        document.getElementById("texto").value = texto;
        let mensajeError = document.getElementById("error-chat");
        mensajeError.innerHTML = "No se pudo conectar. Probá de nuevo.";
        mensajeError.style.display = "block";
    });
}

// Agrega al chat la pregunta y la respuesta que devolvió el servidor
function mostrarRespuesta(data, texto) {
    document.getElementById("mi-pregunta").style.display = "none";
    document.getElementById("escribiendo").style.display = "none";
    document.getElementById("boton-enviar").disabled = false;
    let historial = document.getElementById("historial");

    // La pregunta quedó guardada en el chat (aunque la IA haya fallado)
    if (data.pregunta != null) {
        let html = '<div class="mensaje mensaje-mio"><div class="burbuja">';
        data.pregunta.forEach(parrafo => {
            html = html + "<p>" + parrafo + "</p>";
        });
        html = html + "</div></div>";
        historial.innerHTML = historial.innerHTML + html;
    } else {
        // No se guardó: le devolvemos el texto para que no lo tenga que escribir de nuevo
        document.getElementById("texto").value = texto;
    }

    if (data.ok) {
        let html = '<div class="mensaje mensaje-ia"><div class="avatar-ia">✦</div><div class="burbuja">';
        data.respuesta.forEach(parrafo => {
            html = html + "<p>" + parrafo + "</p>";
        });
        html = html + "</div></div>";
        historial.innerHTML = historial.innerHTML + html;
    } else {
        let error = document.getElementById("error-chat");
        error.innerHTML = data.error;
        error.style.display = "block";
    }

    // Si la pregunta abrió un chat nuevo: se agrega a la lista de la izquierda y se puede borrar
    if (data.chatNuevo) {
        document.getElementById("IdChat").value = data.idChat;
        document.getElementById("IdChatBorrar").value = data.idChat;
        document.getElementById("form-borrar").style.display = "block";
        document.getElementById("titulo-chat").innerHTML = data.tituloChat;
        let sinChats = document.getElementById("sin-chats");
        if (sinChats != null) {
            sinChats.style.display = "none";
        }
        let lista = document.getElementById("lista-chats");
        lista.innerHTML = '<a class="item-chat item-chat-activo" href="/Asistente/Index?IdChat=' + data.idChat + '"><strong>' + data.tituloChat + '</strong><p class="ayuda">Ahora</p></a>' + lista.innerHTML;
    }
    if (data.restantes != undefined) {
        document.getElementById("restantes").innerHTML = data.restantes;
    }
    bajarAlFinal();
}

// Enter manda, Shift + Enter hace un salto de línea
function teclaEnter(evento) {
    if (evento.key == "Enter" && !evento.shiftKey) {
        // Que el Enter no agregue un salto de línea en el cuadro
        evento.preventDefault();
        enviar();
    }
}

// El cuadro de texto crece mientras se escribe (hasta 160 píxeles de alto)
function ajustarAlto() {
    let cuadro = document.getElementById("texto");
    cuadro.style.height = "auto";
    let alto = cuadro.scrollHeight;
    if (alto > 160) {
        alto = 160;
    }
    cuadro.style.height = alto + "px";
}

// Las preguntas de ejemplo: al tocar una, se copia en el cuadro de texto
function usarSugerencia(numero) {
    let cuadro = document.getElementById("texto");
    cuadro.value = document.getElementById("sugerencia-" + numero).innerText;
    ajustarAlto();
    cuadro.focus();
}

function confirmarBorrado() {
    return confirm("¿Borrar este chat y todos sus mensajes?");
}

bajarAlFinal();
