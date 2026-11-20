// Pantalla Calculadora de eficiencia (Views/Calculadora/Index.cshtml)

// Si la persona escribió a mano cuántas personas, no se lo pisamos al cambiar de tarea
let personasEditadas = false;

function marcarPersonasEditadas() {
    personasEditadas = true;
}

// Al elegir una tarea: cuántos de su equipo la marcaron en el formulario y cómo ayuda la IA.
// Los datos de cada tarea están en inputs ocultos con el Id de la tarea en su id.
function elegirTarea() {
    let idTarea = document.getElementById("tarea").value;
    let cantidad = document.getElementById("personas-tarea-" + idTarea).value;
    let ejemplo = document.getElementById("ejemplo-tarea-" + idTarea).value;
    let porcentaje = document.getElementById("porcentaje-tarea-" + idTarea).value;

    document.getElementById("info-tarea").innerHTML = "💡 " + ejemplo + " La IA ahorra cerca del " + porcentaje + "% del tiempo.";
    let infoPersonas = document.getElementById("info-personas");
    if (cantidad == 1) {
        infoPersonas.innerHTML = "1 persona de tu equipo marcó esta tarea en su formulario.";
    } else if (cantidad > 1) {
        infoPersonas.innerHTML = cantidad + " personas de tu equipo marcaron esta tarea en su formulario.";
    } else {
        infoPersonas.innerHTML = "Nadie de tu equipo marcó esta tarea en su formulario: poné cuántos la hacen.";
    }
    if (cantidad > 0 && !personasEditadas) {
        document.getElementById("personas").value = cantidad;
    }
}

// Validación del lado del cliente antes de mandar el formulario (el servidor vuelve a chequear lo mismo).
// Si devuelve false, el formulario no se manda.
function validarCalculo() {
    let personas = document.getElementById("personas").value;
    let horas = document.getElementById("horas").value;
    let error = document.getElementById("error-calculadora");
    if (personas < 1 || personas > 10000) {
        error.innerHTML = "Poné entre 1 y 10000 personas.";
        error.style.display = "block";
        return false;
    }
    if (horas == "") {
        error.innerHTML = "Poné cuántas horas por semana le dedica cada una.";
        error.style.display = "block";
        return false;
    }
    return true;
}

// Borra un cálculo guardado sin recargar la página: llama a CalculadoraController.Borrar con fetch
// y, si salió bien, esconde su fila de la tabla.
function borrarCalculo(idCalculo) {
    fetch('/Calculadora/Borrar?IdCalculo=' + idCalculo, {
        method: 'GET',
        headers: { 'Content-Type': 'application/json' },
    })
    .then(response => response.json())
    .then(data => {
        if (data.ok) {
            document.getElementById("calculo-" + idCalculo).style.display = "none";
        }
    })
    .catch(error => console.error(error));
}

// Al abrir la pantalla, se muestran los datos de la primera tarea
elegirTarea();
