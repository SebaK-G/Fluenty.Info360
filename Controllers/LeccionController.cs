using Microsoft.AspNetCore.Mvc;
using Fluenty.Info360.Models;
using Fluenty.Services;

namespace Fluenty.Info360.Controllers;

// Sprint 2: el reproductor de lecciones.
// - Las tarjetas las arma GeneradorLecciones (de antemano, o la primera vez que se abre la lección).
// - Las preguntas se responden en el servidor: si se equivoca no avanza, le llega OTRA pregunta del mismo tema.
// - "Probalo vos" hay que aprobarlo. La lección 5 de cada unidad es un examen: si no llega a la nota, lo vuelve a rendir.
// - Hay un tope de pedidos a la IA por persona y por día, para que nadie gaste todos los créditos.
public class LeccionController : Controller
{
    private readonly GroqService _groqService;
    private readonly GroqFeatureToggle _groqFeatureToggle;

    public LeccionController(GroqService groqService, GroqFeatureToggle groqFeatureToggle)
    {
        _groqService = groqService;
        _groqFeatureToggle = groqFeatureToggle;
    }

    // ============ Abrir una lección (Tarjeta = en qué tarjeta arranca; Falta = volvió porque le faltaba algo) ============
    public async Task<IActionResult> Index(int IdLeccion, int Tarjeta = 1, int Falta = 0)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        BD bd = new BD();
        LeccionUsuario leccion = bd.TraerLeccionUsuario(IdLeccion, idUsuario);

        // Solo se abre una lección propia, de un curso confirmado, que esté disponible o ya hecha
        if (leccion == null || leccion.EstadoCurso != "Confirmado" || (leccion.Estado != "Disponible" && leccion.Estado != "Completada"))
        {
            return RedirectToAction("Index", "Home");
        }

        // Si no estaba preparada de antemano, se arma ahora (tarda unos segundos)
        if (bd.TraerTarjetas(leccion.IdLeccionXUsuario).Count == 0)
        {
            string error = "La IA está deshabilitada en este momento.";
            if (_groqFeatureToggle.Enabled)
            {
                error = await new GeneradorLecciones(_groqService).GenerarLeccion(bd, leccion);
            }
            if (error != "")
            {
                ViewBag.Error = error;
                ViewBag.Leccion = leccion;
                return View("Error");
            }
        }

        // Mientras hace esta, se preparan las 2 siguientes
        if (_groqFeatureToggle.Enabled)
        {
            GeneradorLecciones.PrepararEnSegundoPlano(_groqService, idUsuario, GeneradorLecciones.Siguientes(bd, leccion.IdCurso, idUsuario, IdLeccion, 2));
        }

        CargarLeccion(bd, leccion, idUsuario, Tarjeta);
        if (Falta == 1)
        {
            ViewBag.Mensaje = "Todavía te falta contestar bien alguna pregunta o aprobar el \"Probalo vos\". Te llevamos a esa tarjeta.";
        }
        return View("Index");
    }

    // ============ Responder una pregunta (lo llama el navegador con fetch y recibe JSON) ============
    [HttpPost]
    public async Task<IActionResult> Responder(int IdTarjeta, string Letra)
    {
        BD bd = new BD();
        Tarjetas tarjeta = TarjetaPropia(bd, IdTarjeta);
        if (tarjeta == null || !tarjeta.EsPregunta() || tarjeta.Resultado != "")
        {
            return Json(new { error = "Esta pregunta ya no se puede responder. Recargá la página." });
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        bool bien = Letra == tarjeta.Correcta;
        bd.ResponderTarjeta(IdTarjeta, bien ? "Bien" : "Mal");

        if (bien)
        {
            return Json(new { bien = true, correcta = tarjeta.Correcta, explicacion = tarjeta.Explicacion });
        }

        // Se equivocó: no avanza. Buscamos otra versión del mismo tema que todavía no haya respondido.
        List<Tarjetas> versiones = new List<Tarjetas>();
        foreach (Tarjetas item in bd.TraerTarjetas(tarjeta.IdLeccionXUsuario))
        {
            if (item.Orden == tarjeta.Orden)
            {
                versiones.Add(item);
            }
        }
        int errores = 0;
        int ultimaVersion = 0;
        Tarjetas siguiente = null;
        foreach (Tarjetas item in versiones)
        {
            if (item.Resultado == "Mal")
            {
                errores++;
            }
            if (item.Version > ultimaVersion)
            {
                ultimaVersion = item.Version;
            }
            if (siguiente == null && item.Resultado == "")
            {
                siguiente = item;
            }
        }

        GeneradorLecciones generador = new GeneradorLecciones(_groqService);

        // Si ya usó todas las versiones, se arma una nueva en el momento (cuenta para el tope de pedidos)
        if (siguiente == null && _groqFeatureToggle.Enabled && bd.UsarPedidoIA(idUsuario, TopePedidosDia(bd)))
        {
            try
            {
                siguiente = await generador.GenerarVersion(bd, tarjeta, ultimaVersion + 1, idUsuario);
            }
            catch (Exception)
            {
                siguiente = null;
            }
        }
        // Si no se pudo (tope o error de la IA), vuelve a salir una de las que ya respondió, la más vieja
        if (siguiente == null)
        {
            foreach (Tarjetas item in versiones)
            {
                if (siguiente == null && item.IdTarjeta != tarjeta.IdTarjeta)
                {
                    siguiente = item;
                }
            }
            if (siguiente == null)
            {
                siguiente = tarjeta;
            }
            bd.ReiniciarTarjeta(siguiente.IdTarjeta);
        }

        // Cada 3 errores en el mismo tema, además le explicamos el tema de nuevo, de otra forma
        string reexplicacion = "";
        if (errores > 0 && errores % 3 == 0 && _groqFeatureToggle.Enabled && bd.UsarPedidoIA(idUsuario, TopePedidosDia(bd)))
        {
            try
            {
                TextoIA nuevo = await generador.Reexplicar(bd, idUsuario, tarjeta.Titulo, tarjeta.Texto + " " + tarjeta.Explicacion, "error");
                if (nuevo != null)
                {
                    reexplicacion = nuevo.Texto;
                }
            }
            catch (Exception)
            {
                // Si falla, queda la explicación de la pregunta
            }
        }

        return Json(new
        {
            bien = false,
            correcta = tarjeta.Correcta,
            explicacion = tarjeta.Explicacion,
            reexplicacion = reexplicacion,
            siguiente = new
            {
                idTarjeta = siguiente.IdTarjeta,
                tipo = siguiente.Tipo,
                titulo = siguiente.Titulo,
                texto = siguiente.Texto,
                contexto = siguiente.Contexto,
                opcionA = siguiente.OpcionA,
                opcionB = siguiente.OpcionB,
                opcionC = siguiente.OpcionC
            }
        });
    }

    // ============ "Probalo vos": la IA corrige y dice si aprobó (JSON) ============
    // No usa el tope del día (si no, la persona quedaría trabada), pero cada práctica tiene como mucho 5 correcciones.
    [HttpPost]
    public async Task<IActionResult> Practicar(int IdTarjeta, string Respuesta)
    {
        BD bd = new BD();
        Tarjetas tarjeta = TarjetaPropia(bd, IdTarjeta);
        if (tarjeta == null || tarjeta.Tipo != "practica")
        {
            return Json(new { error = "No encontramos este ejercicio. Recargá la página." });
        }
        if (tarjeta.Resultado == "Bien")
        {
            return Json(new { aprobado = true, devolucion = "¡Este ya lo tenías aprobado!", intentos = tarjeta.Intentos, ejemplo = tarjeta.RespuestaBuena });
        }
        if (Respuesta == null || Respuesta.Trim().Length < 10)
        {
            return Json(new { aprobado = false, devolucion = "Escribí tu pedido completo a la IA (al menos una oración) y lo corregimos.", intentos = tarjeta.Intentos, ejemplo = "" });
        }

        int intentos = tarjeta.Intentos + 1;
        bool aprobado = false;
        string devolucion = "No pudimos corregir tu respuesta ahora. Probá de nuevo en un ratito.";
        if (_groqFeatureToggle.Enabled)
        {
            try
            {
                CorreccionIA correccion = await new GeneradorLecciones(_groqService).CorregirPractica(tarjeta, Respuesta.Trim(), tarjeta.Intentos >= 3);
                if (correccion != null)
                {
                    aprobado = correccion.Aprobado;
                    devolucion = correccion.Devolucion;
                }
                else
                {
                    intentos = tarjeta.Intentos;
                }
            }
            catch (Exception)
            {
                intentos = tarjeta.Intentos;
            }
        }
        // Si la IA no pudo corregir, no le cuenta el intento
        if (intentos == tarjeta.Intentos)
        {
            return Json(new { aprobado = false, devolucion = devolucion, intentos = intentos, ejemplo = "" });
        }

        // Tope: a la quinta corrección se da por hecha, con el ejemplo a la vista
        if (!aprobado && intentos >= 5)
        {
            aprobado = true;
            devolucion = devolucion + " Ya lo intentaste varias veces, así que la damos por hecha: mirá bien el ejemplo, que es lo que buscábamos.";
        }
        bd.ActualizarPractica(IdTarjeta, aprobado ? "Bien" : "Mal");

        // Después de 3 intentos se le muestra un ejemplo de pedido bien hecho, para que lo reescriba con sus palabras
        string ejemplo = "";
        if (aprobado || intentos >= 3)
        {
            ejemplo = tarjeta.RespuestaBuena;
        }
        return Json(new { aprobado = aprobado, devolucion = devolucion, intentos = intentos, ejemplo = ejemplo });
    }

    // ============ "Explicamelo más simple" (Modo = simple) o "Dame otro ejemplo" (Modo = ejemplo) (JSON) ============
    [HttpPost]
    public async Task<IActionResult> Reexplicar(int IdTarjeta, string Modo)
    {
        BD bd = new BD();
        Tarjetas tarjeta = TarjetaPropia(bd, IdTarjeta);
        if (tarjeta == null)
        {
            return Json(new { ok = false, mensaje = "No encontramos esta tarjeta. Recargá la página." });
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        int topeTarjeta = bd.TraerConfiguracion("TopeReexplicarTarjeta");
        if (tarjeta.VecesReexplicada >= topeTarjeta)
        {
            return Json(new { ok = false, mensaje = "Ya pediste " + topeTarjeta + " veces en esta tarjeta. Seguí con la próxima, que también te va a ayudar." });
        }
        if (!_groqFeatureToggle.Enabled)
        {
            return Json(new { ok = false, mensaje = "La IA está deshabilitada en este momento." });
        }
        if (!bd.UsarPedidoIA(idUsuario, TopePedidosDia(bd)))
        {
            return Json(new { ok = false, sinPedidos = true, mensaje = "Llegaste al tope de pedidos a la IA de hoy. ¡Volvé mañana! 🙂" });
        }

        TextoIA nuevo = null;
        try
        {
            string texto = tarjeta.Texto;
            if (tarjeta.Ejemplo != "")
            {
                texto = texto + "\nEjemplo: " + tarjeta.Ejemplo;
            }
            nuevo = await new GeneradorLecciones(_groqService).Reexplicar(bd, idUsuario, tarjeta.Titulo, texto, Modo == "ejemplo" ? "ejemplo" : "simple");
        }
        catch (Exception)
        {
            nuevo = null;
        }
        if (nuevo == null)
        {
            return Json(new { ok = false, mensaje = "No pudimos explicarlo de nuevo ahora. Probá en un ratito." });
        }
        string titulo = DatosIA.Recortar(nuevo.Titulo, 150);
        string textoNuevo = DatosIA.Recortar(nuevo.Texto, 1500);
        bd.ActualizarTextoTarjeta(IdTarjeta, titulo, textoNuevo);
        bd.SumarReexplicacion(IdTarjeta);
        return Json(new
        {
            ok = true,
            titulo = titulo,
            texto = textoNuevo,
            restantesTarjeta = topeTarjeta - tarjeta.VecesReexplicada - 1,
            restantesHoy = TopePedidosDia(bd) - bd.TraerPedidosIAHoy(idUsuario)
        });
    }

    // ============ Terminar la lección: nota, XP, racha y (si es examen) aprobar o volver a rendir ============
    [HttpPost]
    public IActionResult Completar(int IdLeccion)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        BD bd = new BD();
        LeccionUsuario leccion = bd.TraerLeccionUsuario(IdLeccion, idUsuario);
        if (leccion == null || (leccion.Estado != "Disponible" && leccion.Estado != "Completada"))
        {
            return RedirectToAction("Index", "Home");
        }
        List<Tarjetas> tarjetas = bd.TraerTarjetas(leccion.IdLeccionXUsuario);
        if (tarjetas.Count == 0)
        {
            return RedirectToAction("Index", new { IdLeccion = IdLeccion });
        }

        // Para cada pregunta: ¿la terminó contestando bien? ¿la contestó bien a la primera?
        List<int> ordenesPregunta = new List<int>();
        List<int> ordenesAprendidas = new List<int>();
        int aciertosPrimera = 0;
        int ordenPendiente = 0;
        foreach (Tarjetas tarjeta in tarjetas)
        {
            if (tarjeta.EsPregunta())
            {
                if (!ordenesPregunta.Contains(tarjeta.Orden))
                {
                    ordenesPregunta.Add(tarjeta.Orden);
                }
                if (tarjeta.Resultado == "Bien" && !ordenesAprendidas.Contains(tarjeta.Orden))
                {
                    ordenesAprendidas.Add(tarjeta.Orden);
                    // A la primera = la primera versión, respondida una sola vez (y bien)
                    if (tarjeta.Version == 1 && tarjeta.Intentos == 1)
                    {
                        aciertosPrimera++;
                    }
                }
            }
            if (tarjeta.Tipo == "practica" && tarjeta.Resultado != "Bien" && ordenPendiente == 0)
            {
                ordenPendiente = tarjeta.Orden;
            }
        }
        foreach (int orden in ordenesPregunta)
        {
            if (!ordenesAprendidas.Contains(orden) && (ordenPendiente == 0 || orden < ordenPendiente))
            {
                ordenPendiente = orden;
            }
        }
        // Si le falta algo, no termina: vuelve a esa tarjeta
        if (ordenPendiente != 0)
        {
            return RedirectToAction("Index", new { IdLeccion = IdLeccion, Tarjeta = ordenPendiente, Falta = 1 });
        }

        int nota = 100;
        if (ordenesPregunta.Count > 0)
        {
            nota = aciertosPrimera * 100 / ordenesPregunta.Count;
        }
        bool esExamen = leccion.Numero == 5;
        int notaMinima = bd.TraerConfiguracion("NotaMinimaExamen");
        bd.GuardarNotaLeccion(leccion.IdLeccionXUsuario, nota);

        Usuarios yo = bd.TraerEmpleado(idUsuario);
        ViewBag.Leccion = leccion;
        ViewBag.Nota = nota;
        ViewBag.AciertosPrimera = aciertosPrimera;
        ViewBag.Preguntas = ordenesPregunta.Count;
        ViewBag.EsExamen = esExamen;
        ViewBag.NotaMinima = notaMinima;
        ViewBag.Aprobo = true;
        ViewBag.XPGanado = 0;
        ViewBag.RachaSubio = false;
        ViewBag.Proxima = null;
        ViewBag.Ofrecer = "";

        // Examen desaprobado: no pasa de unidad. Se borra para armarle uno nuevo (con otras preguntas) la próxima vez.
        if (esExamen && nota < notaMinima && leccion.Estado == "Disponible")
        {
            bd.BorrarTarjetas(leccion.IdLeccionXUsuario);
            if (_groqFeatureToggle.Enabled)
            {
                List<int> examen = new List<int>();
                examen.Add(IdLeccion);
                GeneradorLecciones.PrepararEnSegundoPlano(_groqService, idUsuario, examen);
            }
            ViewBag.Aprobo = false;
            ViewBag.XP = yo.XP;
            ViewBag.Racha = yo.Racha;
            return View("Completada");
        }

        // La primera vez que la termina: XP según cómo le fue, racha y se desbloquea la siguiente.
        // Repetir una lección ya hecha no suma de nuevo.
        if (leccion.Estado == "Disponible")
        {
            bd.CompletarLeccion(leccion.IdLeccionXUsuario);
            bd.ActualizarProgreso(leccion.IdCurso, idUsuario);

            // 5 por terminarla, 10 por cada pregunta bien a la primera, 4 por cada una que aprendió después de equivocarse
            int xpGanado = 5 + aciertosPrimera * 10 + (ordenesAprendidas.Count - aciertosPrimera) * 4;
            if (esExamen)
            {
                xpGanado = xpGanado + 20;
            }
            DateTime hoy = DateTime.Today;
            int racha = yo.Racha;
            if (yo.UltimoDiaActivo == null || yo.UltimoDiaActivo.Value.Date < hoy.AddDays(-1))
            {
                racha = 1;
                ViewBag.RachaSubio = true;
            }
            else if (yo.UltimoDiaActivo.Value.Date == hoy.AddDays(-1))
            {
                racha = racha + 1;
                ViewBag.RachaSubio = true;
            }
            bd.ActualizarXPRacha(idUsuario, yo.XP + xpGanado, racha, hoy);
            yo = bd.TraerEmpleado(idUsuario);
            ViewBag.XPGanado = xpGanado;

            // Nivelar por lo que hace, no por lo que dijo en el formulario:
            // si la primera lección le salió perfecta puede saltar al examen; si le costó mucho, bajar una unidad
            leccion.Estado = "Completada";
            ViewBag.Ofrecer = QueNivelacionOfrecer(bd, leccion, idUsuario, nota);

            // Se preparan las próximas para que estén listas cuando las abra
            // (si le ofrecemos saltar o bajar, se preparan cuando elija)
            if (_groqFeatureToggle.Enabled && ViewBag.Ofrecer == "")
            {
                GeneradorLecciones.PrepararEnSegundoPlano(_groqService, idUsuario, GeneradorLecciones.Siguientes(bd, leccion.IdCurso, idUsuario, IdLeccion, 2));
            }
        }

        foreach (Lecciones item in bd.TraerLeccionesUsuario(leccion.IdCurso, idUsuario))
        {
            if (item.Estado == "Disponible" && ViewBag.Proxima == null)
            {
                ViewBag.Proxima = item;
            }
        }
        ViewBag.XP = yo.XP;
        ViewBag.Racha = yo.Racha;
        return View("Completada");
    }

    // ============ Le fue perfecto en su primera lección: saltea las lecciones 2 a 4 y va al examen de la unidad ============
    [HttpPost]
    public IActionResult Saltar(int IdLeccion)
    {
        return Nivelar(IdLeccion, "saltar");
    }

    // ============ Le costó mucho su primera lección: arranca una unidad antes ============
    [HttpPost]
    public IActionResult Bajar(int IdLeccion)
    {
        return Nivelar(IdLeccion, "bajar");
    }

    // ================= Métodos de ayuda =================

    private IActionResult Nivelar(int idLeccion, string opcion)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        BD bd = new BD();
        LeccionUsuario leccion = bd.TraerLeccionUsuario(idLeccion, idUsuario);
        // Se vuelve a revisar acá: que no se pueda saltar mandando el formulario a mano
        if (leccion == null || leccion.Nota == null || QueNivelacionOfrecer(bd, leccion, idUsuario, leccion.Nota.Value) != opcion)
        {
            return RedirectToAction("Index", "Home");
        }
        if (opcion == "saltar")
        {
            bd.SaltarAlExamen(leccion.IdCurso, idUsuario, leccion.NumeroUnidad);
        }
        else
        {
            bd.BajarUnidad(leccion.IdCurso, idUsuario, leccion.NumeroUnidad);
        }
        bd.ActualizarProgreso(leccion.IdCurso, idUsuario);
        if (_groqFeatureToggle.Enabled)
        {
            GeneradorLecciones.PrepararEnSegundoPlano(_groqService, idUsuario, GeneradorLecciones.Siguientes(bd, leccion.IdCurso, idUsuario, 0, 1));
        }
        return RedirectToAction("Ver", "Curso", new { IdCurso = leccion.IdCurso });
    }

    // "saltar", "bajar" o "" (nada). Solo se ofrece en su primera lección (la 1 de la unidad donde arrancó),
    // si es la única que completó.
    private string QueNivelacionOfrecer(BD bd, LeccionUsuario leccion, int idUsuario, int nota)
    {
        if (leccion.Numero != 1 || leccion.Estado != "Completada")
        {
            return "";
        }
        int unidadInicial = 0;
        foreach (CursoXUsuario asignacion in bd.TraerAsignacionesCurso(leccion.IdCurso))
        {
            if (asignacion.IdUsuario == idUsuario)
            {
                unidadInicial = asignacion.UnidadInicial;
            }
        }
        if (unidadInicial != leccion.NumeroUnidad)
        {
            return "";
        }
        int completadas = 0;
        foreach (Lecciones item in bd.TraerLeccionesUsuario(leccion.IdCurso, idUsuario))
        {
            if (item.Estado == "Completada")
            {
                completadas++;
            }
        }
        if (completadas != 1)
        {
            return "";
        }
        if (nota == 100)
        {
            return "saltar";
        }
        if (nota < 50 && leccion.NumeroUnidad > 1)
        {
            return "bajar";
        }
        return "";
    }

    // Arma lo que muestra el reproductor: una tarjeta por Orden (en las preguntas, la versión que le toca)
    private void CargarLeccion(BD bd, LeccionUsuario leccion, int idUsuario, int tarjetaInicial)
    {
        List<Tarjetas> todas = bd.TraerTarjetas(leccion.IdLeccionXUsuario);
        List<Tarjetas> aMostrar = new List<Tarjetas>();
        List<int> ordenes = new List<int>();
        foreach (Tarjetas tarjeta in todas)
        {
            if (!ordenes.Contains(tarjeta.Orden))
            {
                ordenes.Add(tarjeta.Orden);
            }
        }
        foreach (int orden in ordenes)
        {
            Tarjetas bien = null;
            Tarjetas pendiente = null;
            Tarjetas primera = null;
            foreach (Tarjetas tarjeta in todas)
            {
                if (tarjeta.Orden == orden)
                {
                    if (primera == null) { primera = tarjeta; }
                    if (bien == null && tarjeta.Resultado == "Bien") { bien = tarjeta; }
                    if (pendiente == null && tarjeta.Resultado == "") { pendiente = tarjeta; }
                }
            }
            if (!primera.EsPregunta())
            {
                aMostrar.Add(primera);
            }
            else if (bien != null)
            {
                aMostrar.Add(bien);
            }
            else if (pendiente != null)
            {
                aMostrar.Add(pendiente);
            }
            else
            {
                // Respondió mal todas: vuelve a salir la primera
                bd.ReiniciarTarjeta(primera.IdTarjeta);
                primera.Resultado = "";
                aMostrar.Add(primera);
            }
        }

        Usuarios yo = bd.TraerEmpleado(idUsuario);
        int tope = TopePedidosDia(bd);
        ViewBag.Leccion = leccion;
        ViewBag.Tarjetas = aMostrar;
        ViewBag.TarjetaInicial = tarjetaInicial;
        ViewBag.EsExamen = leccion.Numero == 5;
        ViewBag.XP = yo.XP;
        ViewBag.Racha = yo.Racha;
        ViewBag.PedidosRestantes = tope - bd.TraerPedidosIAHoy(idUsuario);
        ViewBag.TopeReexplicar = bd.TraerConfiguracion("TopeReexplicarTarjeta");
    }

    private int TopePedidosDia(BD bd)
    {
        int tope = bd.TraerConfiguracion("TopePedidosIADia");
        if (tope <= 0)
        {
            tope = 20;
        }
        return tope;
    }

    // Devuelve la tarjeta solo si es del usuario logueado
    private Tarjetas TarjetaPropia(BD bd, int idTarjeta)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return null;
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        if (bd.TraerDuenoTarjeta(idTarjeta) != idUsuario)
        {
            return null;
        }
        return bd.TraerTarjeta(idTarjeta);
    }
}
