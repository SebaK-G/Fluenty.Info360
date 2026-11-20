namespace Fluenty.Info360.Models;

// Sprint 2: cada persona se suma con el link de SU RANGO y completa su formulario.
// El formulario es corto (casi todo opciones) porque capaz al empleado lo obligan y no tiene ganas.
// Las preguntas que se sacaron quedan vacías en la base; la IA usa lo que el dueño contó del rango.
public static class LogicaFormulario
{
    // Revisa los datos del lado del servidor (la vista ya tiene required y maxlength, pero se pueden saltear).
    // Devuelve el error para mostrar, o "" si está todo bien.
    public static string Validar(Usuarios usuario, FormulariosEmpleado formulario)
    {
        if (Funciones.EstaVacio(usuario.Nombre) || Funciones.EstaVacio(usuario.Apellido) || Funciones.EstaVacio(usuario.Email)
            || Funciones.EstaVacio(usuario.Dni) || Funciones.EstaVacio(usuario.Telefono) || Funciones.EstaVacio(usuario.Contrasena)
            || Funciones.EstaVacio(usuario.Puesto) || Funciones.EstaVacio(formulario.Area))
        {
            return "Completá todos los datos obligatorios.";
        }
        if (usuario.Nombre.Length > 50 || usuario.Apellido.Length > 50 || usuario.Email.Length > 100 || usuario.Dni.Length > 10
            || usuario.Telefono.Length > 20 || usuario.Contrasena.Length > 50 || usuario.Puesto.Length > 50)
        {
            return "Algún dato es demasiado largo.";
        }
        // IndexOf devuelve -1 si no encuentra el @
        if (usuario.Email.IndexOf("@") < 0)
        {
            return "El email no es válido.";
        }
        if (formulario.NivelIA < 1 || formulario.NivelIA > 4)
        {
            return "Contanos cuánto usás la IA.";
        }
        if (formulario.TiempoXDia != 5 && formulario.TiempoXDia != 10 && formulario.TiempoXDia != 15)
        {
            return "Elegí cuánto tiempo por día podés dedicarle.";
        }
        if (Funciones.EstaVacio(formulario.DatosClientes) || Funciones.EstaVacio(formulario.ComodidadTecnologia) || Funciones.EstaVacio(formulario.RangoEdad))
        {
            return "Respondé todas las preguntas obligatorias.";
        }
        BD bd = new BD();
        if (bd.ExisteUsuarioRepetido(usuario.Email, usuario.Dni, usuario.Telefono))
        {
            return "Ya existe un usuario con ese email, DNI o teléfono.";
        }
        return "";
    }

    // Crea el usuario (rol Empleado, en el rango del link), guarda su formulario y sus herramientas.
    // Devuelve el IdUsuario nuevo.
    public static int Registrar(Usuarios usuario, FormulariosEmpleado formulario, string[] tareas, string otraTarea, int[] herramientas, string[] preocupaciones)
    {
        BD bd = new BD();
        usuario.Rol = "Empleado";
        int idUsuario = bd.CrearUsuario(usuario);

        // Las tareas que tocó, más la que escribió en "Otra"
        string tareasJuntas = Juntar(tareas);
        if (!DatosIA.EsFloja(otraTarea))
        {
            if (tareasJuntas != "")
            {
                tareasJuntas = tareasJuntas + "; ";
            }
            tareasJuntas = tareasJuntas + Funciones.Limpiar(otraTarea);
        }

        // Lo opcional que no completó queda vacío
        formulario.IdUsuario = idUsuario;
        formulario.Tareas = DatosIA.Recortar(tareasJuntas, 250);
        formulario.TareaConcreta = DatosIA.Recortar(Funciones.Limpiar(formulario.TareaConcreta), 500);
        formulario.PreocupacionIA = DatosIA.Recortar(Juntar(preocupaciones), 250);
        formulario.Hobbies = DatosIA.Recortar(Funciones.Limpiar(formulario.Hobbies), 200);
        formulario.Dificultades = "";
        formulario.Objetivos = "";
        formulario.DificultadPrincipal = "";
        formulario.MejoraEmpresa = "";
        formulario.FortalezasDebilidades = "";
        formulario.MejoraPersonal = "";
        formulario.ComoAprende = "";
        formulario.DiaTipico = "";
        bd.AgregarFormularioEmpleado(formulario);

        // Las herramientas que tildó, en la tabla intermedia HerramientasXUsuario
        if (herramientas != null)
        {
            foreach (int idHerramienta in herramientas)
            {
                bd.AgregarHerramientaUsuario(idUsuario, idHerramienta);
            }
        }
        return idUsuario;
    }

    // Si no contó qué es lo que más tiempo le come (o puso "nada", "asd"...), le hacemos UNA pregunta más
    public static bool HaceFaltaUnaMas(FormulariosEmpleado formulario)
    {
        return DatosIA.EsFloja(formulario.TareaConcreta);
    }

    // Las opciones de la pregunta de más: si tocó tareas, elige entre esas; si no, entre las más comunes
    public static List<string> OpcionesUnaMas(int idUsuario)
    {
        BD bd = new BD();
        FormulariosEmpleado formulario = bd.TraerFormularioEmpleado(idUsuario);
        List<string> opciones = new List<string>();
        if (formulario != null && !DatosIA.EsFloja(formulario.Tareas))
        {
            // Las tareas se guardan juntas separadas por ";"
            opciones = Funciones.Separar(formulario.Tareas, ";");
        }
        if (opciones.Count < 2)
        {
            opciones = new List<string>();
            opciones.Add("Responder mensajes o mails");
            opciones.Add("Armar presupuestos o cotizaciones");
            opciones.Add("Pasar o cargar datos a planillas");
            opciones.Add("Armar reportes o resúmenes");
            opciones.Add("Atender y responder dudas de clientes");
            opciones.Add("Organizar la agenda, turnos o pedidos");
        }
        return opciones;
    }

    // Guarda la respuesta de la pregunta de más (si contestó algo que sirva)
    public static void GuardarUnaMas(int idUsuario, string opcion, string otra, string detalle)
    {
        string tarea = opcion;
        if (!DatosIA.EsFloja(otra))
        {
            tarea = Funciones.Limpiar(otra);
        }
        if (!DatosIA.EsFloja(tarea))
        {
            if (!DatosIA.EsFloja(detalle))
            {
                tarea = tarea + ": " + Funciones.Limpiar(detalle);
            }
            BD bd = new BD();
            bd.ActualizarTareaConcreta(idUsuario, DatosIA.Recortar(tarea, 500));
        }
    }

    // Junta las casillas tildadas en un solo texto: "Con ejemplos prácticos; Directo al grano"
    private static string Juntar(string[] opciones)
    {
        List<string> lista = new List<string>();
        if (opciones != null)
        {
            foreach (string opcion in opciones)
            {
                lista.Add(opcion);
            }
        }
        return Funciones.Unir(lista, "; ");
    }
}
