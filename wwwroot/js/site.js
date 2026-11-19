function mostrarFormulario(tipo) {
    var login = document.getElementById('form-login');
    var registro = document.getElementById('form-registro');
    // Solo la pantalla de acceso tiene estos formularios: en las demás no hacemos nada
    if (login == null || registro == null) {
        return;
    }
    if (tipo === 'login') {
        login.hidden = false;
        registro.hidden = true;
    } else {
        login.hidden = true;
        registro.hidden = false;
    }
}
window.onload = function () {
mostrarFormulario('login');
};

// Formularios: pinta de azul la opción elegida (le pone la clase "elegida" a su label).
// Cuando se toca un radio, se despinta el resto del mismo grupo.
document.addEventListener('change', function (evento) {
    var input = evento.target;
    if (input.closest == null || input.closest('.eleccion') == null) {
        return;
    }
    if (input.type === 'radio') {
        var grupo = document.querySelectorAll('input[name="' + input.name + '"]');
        for (var i = 0; i < grupo.length; i++) {
            grupo[i].closest('.eleccion').classList.remove('elegida');
        }
    }
    input.closest('.eleccion').classList.toggle('elegida', input.checked);
});
