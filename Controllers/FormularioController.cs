using Microsoft.AspNetCore.Mvc;
using Fluenty.Info360.Models;

namespace Fluenty.Info360.Controllers;

public class FormularioController : Controller
{
    // ============ Sprint 2: cada uno se une con el link de SU RANGO ============
    public IActionResult Unirse(string Codigo)
    {
        BD bd = new BD();
        Rangos rango = bd.TraerRangoPorCodigo(Codigo);
        if (rango == null)
        {
            return View("LinkInvalido");
        }
        ViewBag.Rango = rango;
        // Las casillas de herramientas salen del catálogo
        ViewBag.Herramientas = bd.TraerHerramientas();
        return View();
    }

    // Herramientas llega con el IdHerramienta de cada casilla tildada (puede venir vacío).
    // Sprint 2: el formulario es más corto (casi todo opciones) porque capaz al empleado lo obligan y no tiene ganas.
    // Las preguntas que se sacaron quedan vacías en la base; la IA usa lo que el dueño contó del rango.
    [HttpPost]
    public IActionResult Unirse(string Codigo, string Nombre, string Apellido, string Email, string Dni, string Telefono, string Contrasena,
        string Puesto, string Area, string[] Tareas, string OtraTarea, string TareaConcreta, int[] Herramientas, string DatosClientes,
        int NivelIA, string ComodidadTecnologia, string RangoEdad, string[] PreocupacionIA, int TiempoXDia, string Hobbies)
    {
        BD bd = new BD();
        Rangos rango = bd.TraerRangoPorCodigo(Codigo);
        if (rango == null)
        {
            return View("LinkInvalido");
        }

        if (bd.ExisteUsuarioDuplicado(Email, Dni, Telefono))
        {
            ViewBag.Rango = rango;
            ViewBag.Herramientas = bd.TraerHerramientas();
            ViewBag.Mensaje = "Ya existe un usuario con ese email, DNI o teléfono.";
            return View();
        }

        // Las tareas que tocó, más la que escribió en "Otra"
        string tareas = Juntar(Tareas);
        if (!DatosIA.EsFloja(OtraTarea))
        {
            if (tareas != "")
            {
                tareas = tareas + "; ";
            }
            tareas = tareas + OtraTarea.Trim();
        }

        // 1. Creamos su usuario con rol Empleado, en la empresa y el rango del link
        Usuarios usuario = new Usuarios();
        usuario.Nombre = Nombre;
        usuario.Apellido = Apellido;
        usuario.Email = Email;
        usuario.IdEmpresa = rango.IdEmpresa;
        usuario.IdRango = rango.IdRango;
        usuario.Dni = Dni;
        usuario.Telefono = Telefono;
        usuario.Contrasena = Contrasena;
        usuario.Rol = "Empleado";
        usuario.Puesto = Puesto;
        int idUsuario = bd.Signup(usuario);

        // 2. Guardamos su formulario (lo opcional que no completó queda vacío)
        FormulariosEmpleado formulario = new FormulariosEmpleado();
        formulario.IdUsuario = idUsuario;
        formulario.NivelIA = NivelIA;
        formulario.Area = Area;
        formulario.Tareas = DatosIA.Recortar(tareas, 250);
        formulario.TareaConcreta = DatosIA.Recortar(Texto(TareaConcreta), 500);
        formulario.DatosClientes = DatosClientes;
        formulario.ComodidadTecnologia = ComodidadTecnologia;
        formulario.RangoEdad = RangoEdad;
        formulario.PreocupacionIA = DatosIA.Recortar(Juntar(PreocupacionIA), 250);
        formulario.TiempoXDia = TiempoXDia;
        formulario.Hobbies = Texto(Hobbies);
        formulario.Dificultades = "";
        formulario.Objetivos = "";
        formulario.DificultadPrincipal = "";
        formulario.MejoraEmpresa = "";
        formulario.FortalezasDebilidades = "";
        formulario.MejoraPersonal = "";
        formulario.ComoAprende = "";
        formulario.DiaTipico = "";
        bd.AgregarFormularioEmpleado(formulario);

        // 3. Las herramientas que tildó, en la tabla intermedia HerramientasXUsuario
        if (Herramientas != null)
        {
            foreach (int idHerramienta in Herramientas)
            {
                bd.AgregarHerramientaUsuario(idUsuario, idHerramienta);
            }
        }

        // 4. Si no contó qué es lo que más tiempo le come (o puso "nada", "asd"...),
        //    le hacemos UNA pregunta más, con opciones para tocar. Se puede saltear.
        if (DatosIA.EsFloja(TareaConcreta))
        {
            HttpContext.Session.SetString("IdUsuarioNuevo", idUsuario.ToString());
            HttpContext.Session.SetString("NombreNuevo", Nombre);
            return RedirectToAction("UnaMas");
        }

        ViewBag.Nombre = Nombre;
        return View("Listo");
    }

    // ============ La pregunta de más (solo si dejó floja la tarea que más tiempo le come) ============
    public IActionResult UnaMas()
    {
        string idTexto = HttpContext.Session.GetString("IdUsuarioNuevo");
        if (idTexto == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        BD bd = new BD();
        FormulariosEmpleado formulario = bd.TraerFormularioEmpleado(int.Parse(idTexto));

        // Si tocó tareas, elige entre esas; si no, entre las más comunes
        List<string> opciones = new List<string>();
        if (formulario != null && !DatosIA.EsFloja(formulario.Tareas))
        {
            foreach (string tarea in formulario.Tareas.Split(';'))
            {
                if (tarea.Trim() != "")
                {
                    opciones.Add(tarea.Trim());
                }
            }
        }
        if (opciones.Count < 2)
        {
            opciones.Clear();
            opciones.Add("Responder mensajes o mails");
            opciones.Add("Armar presupuestos o cotizaciones");
            opciones.Add("Pasar o cargar datos a planillas");
            opciones.Add("Armar reportes o resúmenes");
            opciones.Add("Atender y responder dudas de clientes");
            opciones.Add("Organizar la agenda, turnos o pedidos");
        }
        ViewBag.Opciones = opciones;
        ViewBag.Nombre = HttpContext.Session.GetString("NombreNuevo");
        return View();
    }

    [HttpPost]
    public IActionResult UnaMas(string Opcion, string Otra, string Detalle)
    {
        string idTexto = HttpContext.Session.GetString("IdUsuarioNuevo");
        if (idTexto == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        string tarea = Opcion;
        if (!DatosIA.EsFloja(Otra))
        {
            tarea = Otra.Trim();
        }
        if (!DatosIA.EsFloja(tarea))
        {
            if (!DatosIA.EsFloja(Detalle))
            {
                tarea = tarea + ": " + Detalle.Trim();
            }
            BD bd = new BD();
            bd.ActualizarTareaConcreta(int.Parse(idTexto), DatosIA.Recortar(tarea, 500));
        }
        return Terminar();
    }

    // "Saltear": no contesta y listo (el curso se arma con lo que hace su rango)
    public IActionResult Saltear()
    {
        return Terminar();
    }

    private IActionResult Terminar()
    {
        ViewBag.Nombre = HttpContext.Session.GetString("NombreNuevo");
        HttpContext.Session.Remove("IdUsuarioNuevo");
        HttpContext.Session.Remove("NombreNuevo");
        return View("Listo");
    }

    // Un campo opcional que no completó llega en null: lo guardamos vacío
    private string Texto(string valor)
    {
        if (valor == null)
        {
            return "";
        }
        return valor.Trim();
    }

    // Junta las casillas tildadas en un solo texto: "Con ejemplos prácticos; Directo al grano"
    private string Juntar(string[] opciones)
    {
        string texto = "";
        if (opciones != null)
        {
            foreach (string opcion in opciones)
            {
                if (texto != "")
                {
                    texto = texto + "; ";
                }
                texto = texto + opcion;
            }
        }
        return texto;
    }
}
