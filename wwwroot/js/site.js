function mostrarFormulario(tipo) {
    var login = document.getElementById('form-login');
    var registro = document.getElementById('form-registro');
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
