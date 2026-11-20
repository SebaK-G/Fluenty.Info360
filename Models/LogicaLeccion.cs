using Fluenty.Services;

namespace Fluenty.Info360.Models;

// Sprint 2: el reproductor de lecciones.
// - Las tarjetas las arma GeneradorLecciones (de antemano, o la primera vez que se abre la lección).
// - Las preguntas se responden en el servidor: si se equivoca no avanza, le llega OTRA pregunta del mismo tema.
// - "Probalo vos" hay que aprobarlo. La lección 5 de cada unidad es un examen: si no llega a la nota, lo vuelve a rendir.
// - Hay un tope de pedidos a la IA por persona y por día, para que nadie gaste todos los créditos.
public static class LogicaLeccion
{
    // ============ Abrir una lección ============

    // La lección, solo si es suya, de un curso confirmado y está disponible o ya hecha (null si no)
    public static LeccionUsuario TraerLeccionParaHacer(int idLeccion, int idUsuario)
    {
        BD bd = new BD();
        LeccionUsuario leccion = bd.TraerLeccionUsuario(idLeccion, idUsuario);
        if (leccion == null || leccion.EstadoCurso != "Confirmado" || (leccion.Estado != "Disponible" && leccion.Estado != "Completada"))
        {
            return null;
        }
        return leccion;
    }

    // Si no estaba preparada de antemano, la arma ahora (tarda unos segundos). Devuelve el error, o "" si está lista.
    // Mientras hace esta, se preparan las 2 siguientes.
    public static async Task<string> Preparar(LeccionUsuario leccion, int idUsuario, GroqService groqService, bool iaPrendida)
    {
        BD bd = new BD();
        if (bd.TraerTarjetas(leccion.IdLeccionXUsuario).Count == 0)
        {
            if (!iaPrendida)
            {
                return "La IA está deshabilitada en este momento.";
            }
            string error = await new GeneradorLecciones(groqService).GenerarLeccion(bd, leccion);
            if (error != "")
            {
                return error;
            }
        }
        if (iaPrendida)
        {
            GeneradorLecciones.PrepararEnSegundoPlano(groqService, idUsuario, GeneradorLecciones.Siguientes(bd, leccion.IdCurso, idUsuario, leccion.IdLeccion, 2));
        }
        return "";
    }

    // Lo que muestra el reproductor: una tarjeta por Orden (en las preguntas, la versión que le toca)
    public static List<Tarjetas> ArmarTarjetas(LeccionUsuario leccion)
    {
        BD bd = new BD();
        List<Tarjetas> todas = bd.TraerTarjetas(leccion.IdLeccionXUsuario);
        List<int> ordenes = new List<int>();
        foreach (Tarjetas tarjeta in todas)
        {
            if (!Funciones.EstaEnLista(ordenes, tarjeta.Orden))
            {
                ordenes.Add(tarjeta.Orden);
            }
        }

        List<Tarjetas> aMostrar = new List<Tarjetas>();
        foreach (int orden in ordenes)
        {
            Tarjetas bien = null;
            Tarjetas pendiente = null;
            Tarjetas primera = null;
            foreach (Tarjetas tarjeta in todas)
            {
                if (tarjeta.Orden == orden)
                {
                    if (primera == null)
                    {
                        primera = tarjeta;
                    }
                    if (bien == null && tarjeta.Resultado == "Bien")
                    {
                        bien = tarjeta;
                    }
                    if (pendiente == null && tarjeta.Resultado == "")
                    {
                        pendiente = tarjeta;
                    }
                }
            }
            Tarjetas elegida;
            if (!primera.EsPregunta())
            {
                elegida = primera;
            }
            else if (bien != null)
            {
                elegida = bien;
            }
            else if (pendiente != null)
            {
                elegida = pendiente;
            }
            else
            {
                // Respondió mal todas: vuelve a salir la primera
                bd.ReiniciarTarjeta(primera.IdTarjeta);
                primera.Resultado = "";
                elegida = primera;
            }
            PrepararParaMostrar(elegida);
            aMostrar.Add(elegida);
        }
        return aMostrar;
    }

    // Le arma a la tarjeta lo que la vista muestra (así la vista no tiene que calcular nada)
    private static void PrepararParaMostrar(Tarjetas tarjeta)
    {
        tarjeta.EsDePregunta = tarjeta.EsPregunta();
        tarjeta.Resuelta = (!tarjeta.EsDePregunta && tarjeta.Tipo != "practica") || tarjeta.Resultado == "Bien";
        tarjeta.TextoParaLeer = tarjeta.Titulo + ". " + tarjeta.Texto;
        tarjeta.PasosEjemplo = Funciones.Separar(tarjeta.Ejemplo, "\n");
        tarjeta.Etiqueta = "🤔 ¿Cuál es mejor?";
        if (tarjeta.Tipo == "verdaderofalso")
        {
            tarjeta.Etiqueta = "✔️ ¿Verdadero o falso?";
        }
        if (tarjeta.Tipo == "completar")
        {
            tarjeta.Etiqueta = "✏️ Completá la frase";
        }
        if (tarjeta.Tipo == "error")
        {
            tarjeta.Etiqueta = "🔎 Cazá el error";
        }
        if (tarjeta.Tipo == "mejorrespuesta")
        {
            tarjeta.Etiqueta = "🏅 ¿Qué respuesta sirve más?";
        }
    }

    // En qué tarjeta arranca (si el número no existe, en la primera)
    public static int TarjetaInicial(int tarjeta, int cantidad)
    {
        if (tarjeta < 1 || tarjeta > cantidad)
        {
            return 1;
        }
        return tarjeta;
    }

    public static int TopePedidosDia()
    {
        BD bd = new BD();
        int tope = bd.TraerConfiguracion("TopePedidosIADia");
        if (tope <= 0)
        {
            tope = 20;
        }
        return tope;
    }

    // La tarjeta solo si es de esa persona (null si no)
    public static Tarjetas TarjetaPropia(int idTarjeta, int idUsuario)
    {
        BD bd = new BD();
        if (bd.TraerDuenoTarjeta(idTarjeta) != idUsuario)
        {
            return null;
        }
        return bd.TraerTarjeta(idTarjeta);
    }

    // ============ Responder una pregunta ============
    public static async Task<RespuestaPregunta> Responder(int idTarjeta, string letra, int idUsuario, GroqService groqService, bool iaPrendida)
    {
        RespuestaPregunta respuesta = new RespuestaPregunta();
        respuesta.Error = "";
        respuesta.Reexplicacion = "";
        BD bd = new BD();
        Tarjetas tarjeta = TarjetaPropia(idTarjeta, idUsuario);
        if (tarjeta == null || !tarjeta.EsPregunta() || tarjeta.Resultado != "")
        {
            respuesta.Error = "Esta pregunta ya no se puede responder. Recargá la página.";
            return respuesta;
        }
        respuesta.Bien = letra == tarjeta.Correcta;
        respuesta.Correcta = tarjeta.Correcta;
        respuesta.Explicacion = tarjeta.Explicacion;
        if (respuesta.Bien)
        {
            bd.ResponderTarjeta(idTarjeta, "Bien");
            return respuesta;
        }
        bd.ResponderTarjeta(idTarjeta, "Mal");

        // Se equivocó: no avanza. Buscamos otra versión del mismo tema que todavía no haya respondido.
        List<Tarjetas> versiones = bd.TraerVersionesTarjeta(tarjeta.IdLeccionXUsuario, tarjeta.Orden);
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

        GeneradorLecciones generador = new GeneradorLecciones(groqService);
        int tope = TopePedidosDia();
        // Si ya usó todas las versiones, se arma una nueva en el momento (cuenta para el tope de pedidos)
        if (siguiente == null && iaPrendida && bd.UsarPedidoIA(idUsuario, tope))
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
        if (errores > 0 && errores % 3 == 0 && iaPrendida && bd.UsarPedidoIA(idUsuario, tope))
        {
            try
            {
                TextoIA nuevo = await generador.Reexplicar(bd, idUsuario, tarjeta.Titulo, tarjeta.Texto + " " + tarjeta.Explicacion, "error");
                if (nuevo != null)
                {
                    respuesta.Reexplicacion = nuevo.Texto;
                }
            }
            catch (Exception)
            {
                // Si falla, queda la explicación de la pregunta
            }
        }

        // De la pregunta nueva se manda solo lo que se muestra (nunca la respuesta correcta)
        PreguntaNueva nueva = new PreguntaNueva();
        nueva.IdTarjeta = siguiente.IdTarjeta;
        nueva.Titulo = siguiente.Titulo;
        nueva.Texto = siguiente.Texto;
        nueva.Contexto = siguiente.Contexto;
        nueva.OpcionA = siguiente.OpcionA;
        nueva.OpcionB = siguiente.OpcionB;
        nueva.OpcionC = siguiente.OpcionC;
        respuesta.Siguiente = nueva;
        return respuesta;
    }

    // ============ "Probalo vos": la IA corrige y dice si aprobó ============
    // No usa el tope del día (si no, la persona quedaría trabada), pero cada práctica tiene como mucho 5 correcciones.
    public static async Task<RespuestaPractica> Practicar(int idTarjeta, string texto, int idUsuario, GroqService groqService, bool iaPrendida)
    {
        RespuestaPractica respuesta = new RespuestaPractica();
        respuesta.Error = "";
        respuesta.Ejemplo = "";
        BD bd = new BD();
        Tarjetas tarjeta = TarjetaPropia(idTarjeta, idUsuario);
        if (tarjeta == null || tarjeta.Tipo != "practica")
        {
            respuesta.Error = "No encontramos este ejercicio. Recargá la página.";
            return respuesta;
        }
        respuesta.Intentos = tarjeta.Intentos;
        if (tarjeta.Resultado == "Bien")
        {
            respuesta.Aprobado = true;
            respuesta.Devolucion = "¡Este ya lo tenías aprobado!";
            respuesta.Ejemplo = tarjeta.RespuestaBuena;
            return respuesta;
        }
        texto = Funciones.Limpiar(texto);
        if (texto.Length < 10)
        {
            respuesta.Devolucion = "Escribí tu pedido completo a la IA (al menos una oración) y lo corregimos.";
            return respuesta;
        }
        if (texto.Length > 800)
        {
            respuesta.Devolucion = "Tu pedido puede tener hasta 800 letras.";
            return respuesta;
        }

        // Si la IA no pudo corregir, no se le cuenta el intento
        respuesta.Devolucion = "No pudimos corregir tu respuesta ahora. Probá de nuevo en un ratito.";
        CorreccionIA correccion = null;
        if (iaPrendida)
        {
            try
            {
                correccion = await new GeneradorLecciones(groqService).CorregirPractica(tarjeta, texto, tarjeta.Intentos >= 3);
            }
            catch (Exception)
            {
                correccion = null;
            }
        }
        if (correccion == null)
        {
            return respuesta;
        }

        respuesta.Intentos = tarjeta.Intentos + 1;
        respuesta.Aprobado = correccion.Aprobado;
        respuesta.Devolucion = correccion.Devolucion;
        // Tope: a la quinta corrección se da por hecha, con el ejemplo a la vista
        if (!respuesta.Aprobado && respuesta.Intentos >= 5)
        {
            respuesta.Aprobado = true;
            respuesta.Devolucion = respuesta.Devolucion + " Ya lo intentaste varias veces, así que la damos por hecha: mirá bien el ejemplo, que es lo que buscábamos.";
        }
        if (respuesta.Aprobado)
        {
            bd.ActualizarPractica(idTarjeta, "Bien");
        }
        else
        {
            bd.ActualizarPractica(idTarjeta, "Mal");
        }
        // Después de 3 intentos se le muestra un ejemplo de pedido bien hecho, para que lo reescriba con sus palabras
        if (respuesta.Aprobado || respuesta.Intentos >= 3)
        {
            respuesta.Ejemplo = tarjeta.RespuestaBuena;
        }
        return respuesta;
    }

    // ============ "Explicamelo más simple" (modo = simple) o "Dame otro ejemplo" (modo = ejemplo) ============
    public static async Task<RespuestaReexplicar> Reexplicar(int idTarjeta, string modo, int idUsuario, GroqService groqService, bool iaPrendida)
    {
        RespuestaReexplicar respuesta = new RespuestaReexplicar();
        BD bd = new BD();
        Tarjetas tarjeta = TarjetaPropia(idTarjeta, idUsuario);
        if (tarjeta == null)
        {
            respuesta.Mensaje = "No encontramos esta tarjeta. Recargá la página.";
            return respuesta;
        }
        int topeTarjeta = bd.TraerConfiguracion("TopeReexplicarTarjeta");
        if (tarjeta.VecesReexplicada >= topeTarjeta)
        {
            respuesta.Mensaje = "Ya pediste " + topeTarjeta + " veces en esta tarjeta. Seguí con la próxima, que también te va a ayudar.";
            return respuesta;
        }
        if (!iaPrendida)
        {
            respuesta.Mensaje = "La IA está deshabilitada en este momento.";
            return respuesta;
        }
        if (!bd.UsarPedidoIA(idUsuario, TopePedidosDia()))
        {
            respuesta.SinPedidos = true;
            respuesta.Mensaje = "Llegaste al tope de pedidos a la IA de hoy. ¡Volvé mañana! 🙂";
            return respuesta;
        }
        if (modo != "ejemplo")
        {
            modo = "simple";
        }

        TextoIA nuevo = null;
        try
        {
            string texto = tarjeta.Texto;
            if (tarjeta.Ejemplo != "")
            {
                texto = texto + "\nEjemplo: " + tarjeta.Ejemplo;
            }
            nuevo = await new GeneradorLecciones(groqService).Reexplicar(bd, idUsuario, tarjeta.Titulo, texto, modo);
        }
        catch (Exception)
        {
            nuevo = null;
        }
        if (nuevo == null)
        {
            respuesta.Mensaje = "No pudimos explicarlo de nuevo ahora. Probá en un ratito.";
            return respuesta;
        }
        respuesta.Ok = true;
        respuesta.Titulo = DatosIA.Recortar(nuevo.Titulo, 150);
        respuesta.Texto = DatosIA.Recortar(nuevo.Texto, 1500);
        bd.ActualizarTextoTarjeta(idTarjeta, respuesta.Titulo, respuesta.Texto);
        bd.SumarReexplicacion(idTarjeta);
        respuesta.RestantesTarjeta = topeTarjeta - tarjeta.VecesReexplicada - 1;
        respuesta.RestantesHoy = TopePedidosDia() - bd.TraerPedidosIAHoy(idUsuario);
        return respuesta;
    }

    // ============ Terminar la lección: nota, XP, racha y (si es examen) aprobar o volver a rendir ============
    public static ResultadoLeccion Completar(LeccionUsuario leccion, int idUsuario, GroqService groqService, bool iaPrendida)
    {
        BD bd = new BD();
        ResultadoLeccion resultado = new ResultadoLeccion();
        resultado.Ofrecer = "";
        List<Tarjetas> tarjetas = bd.TraerTarjetas(leccion.IdLeccionXUsuario);

        // Para cada pregunta: ¿la terminó contestando bien? ¿la contestó bien a la primera?
        List<int> ordenesPregunta = new List<int>();
        List<int> ordenesAprendidas = new List<int>();
        int ordenPendiente = 0;
        foreach (Tarjetas tarjeta in tarjetas)
        {
            if (tarjeta.EsPregunta())
            {
                if (!Funciones.EstaEnLista(ordenesPregunta, tarjeta.Orden))
                {
                    ordenesPregunta.Add(tarjeta.Orden);
                }
                if (tarjeta.Resultado == "Bien" && !Funciones.EstaEnLista(ordenesAprendidas, tarjeta.Orden))
                {
                    ordenesAprendidas.Add(tarjeta.Orden);
                    // A la primera = la primera versión, respondida una sola vez (y bien)
                    if (tarjeta.Version == 1 && tarjeta.Intentos == 1)
                    {
                        resultado.AciertosPrimera++;
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
            if (!Funciones.EstaEnLista(ordenesAprendidas, orden) && (ordenPendiente == 0 || orden < ordenPendiente))
            {
                ordenPendiente = orden;
            }
        }
        // Si le falta algo, no termina: vuelve a esa tarjeta
        resultado.OrdenPendiente = ordenPendiente;
        if (ordenPendiente != 0)
        {
            return resultado;
        }

        resultado.Preguntas = ordenesPregunta.Count;
        resultado.Nota = 100;
        if (ordenesPregunta.Count > 0)
        {
            resultado.Nota = resultado.AciertosPrimera * 100 / ordenesPregunta.Count;
        }
        resultado.EsExamen = leccion.Numero == 5;
        resultado.NotaMinima = bd.TraerConfiguracion("NotaMinimaExamen");
        resultado.Aprobo = true;
        bd.GuardarNotaLeccion(leccion.IdLeccionXUsuario, resultado.Nota);

        // Examen desaprobado: no pasa de unidad. Se borra para armarle uno nuevo (con otras preguntas) la próxima vez.
        if (resultado.EsExamen && resultado.Nota < resultado.NotaMinima && leccion.Estado == "Disponible")
        {
            bd.BorrarTarjetas(leccion.IdLeccionXUsuario);
            if (iaPrendida)
            {
                List<int> examen = new List<int>();
                examen.Add(leccion.IdLeccion);
                GeneradorLecciones.PrepararEnSegundoPlano(groqService, idUsuario, examen);
            }
            resultado.Aprobo = false;
            return resultado;
        }

        // La primera vez que la termina: XP según cómo le fue, racha y se desbloquea la siguiente.
        // Repetir una lección ya hecha no suma de nuevo.
        if (leccion.Estado == "Disponible")
        {
            bd.CompletarLeccion(leccion.IdLeccionXUsuario);
            bd.ActualizarProgreso(leccion.IdCurso, idUsuario);

            // 5 por terminarla, 10 por cada pregunta bien a la primera, 4 por cada una que aprendió después de equivocarse
            resultado.XPGanado = 5 + resultado.AciertosPrimera * 10 + (ordenesAprendidas.Count - resultado.AciertosPrimera) * 4;
            if (resultado.EsExamen)
            {
                resultado.XPGanado = resultado.XPGanado + 20;
            }
            Usuarios yo = bd.TraerEmpleado(idUsuario);
            DateTime hoy = DateTime.Today;
            int racha = yo.Racha;
            // Si nunca hizo una lección (new DateTime()) o la última fue antes de ayer, la racha arranca de nuevo
            if (yo.UltimoDiaActivo == new DateTime() || yo.UltimoDiaActivo < hoy.AddDays(-1))
            {
                racha = 1;
                resultado.RachaSubio = true;
            }
            else if (yo.UltimoDiaActivo == hoy.AddDays(-1))
            {
                racha = racha + 1;
                resultado.RachaSubio = true;
            }
            bd.ActualizarXPRacha(idUsuario, yo.XP + resultado.XPGanado, racha, hoy);

            // Nivelar por lo que hace, no por lo que dijo en el formulario:
            // si la primera lección le salió perfecta puede saltar al examen; si le costó mucho, bajar una unidad
            leccion.Estado = "Completada";
            resultado.Ofrecer = QueNivelacionOfrecer(leccion, idUsuario, resultado.Nota);

            // Se preparan las próximas para que estén listas cuando las abra
            // (si le ofrecemos saltar o bajar, se preparan cuando elija)
            if (iaPrendida && resultado.Ofrecer == "")
            {
                GeneradorLecciones.PrepararEnSegundoPlano(groqService, idUsuario, GeneradorLecciones.Siguientes(bd, leccion.IdCurso, idUsuario, leccion.IdLeccion, 2));
            }
        }
        resultado.Proxima = bd.TraerProximaLeccion(leccion.IdCurso, idUsuario);
        return resultado;
    }

    // "saltar", "bajar" o "" (nada). Solo se ofrece en su primera lección (la 1 de la unidad donde arrancó),
    // si es la única que completó.
    public static string QueNivelacionOfrecer(LeccionUsuario leccion, int idUsuario, int nota)
    {
        if (leccion.Numero != 1 || leccion.Estado != "Completada")
        {
            return "";
        }
        BD bd = new BD();
        Inscripciones inscripcion = bd.TraerInscripcion(leccion.IdCurso, idUsuario);
        if (inscripcion == null || inscripcion.UnidadInicial != leccion.NumeroUnidad)
        {
            return "";
        }
        if (bd.ContarLeccionesCompletadas(leccion.IdCurso, idUsuario) != 1)
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

    // ============ Saltar al examen ("saltar") o arrancar una unidad antes ("bajar") ============
    // Devuelve el IdCurso para volver a él, o 0 si no se puede
    public static int Nivelar(int idLeccion, int idUsuario, string opcion, GroqService groqService, bool iaPrendida)
    {
        BD bd = new BD();
        LeccionUsuario leccion = bd.TraerLeccionUsuario(idLeccion, idUsuario);
        // Se vuelve a revisar acá: que no se pueda saltar mandando el formulario a mano
        if (leccion == null || leccion.Intentos == 0 || QueNivelacionOfrecer(leccion, idUsuario, leccion.Nota) != opcion)
        {
            return 0;
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
        if (iaPrendida)
        {
            GeneradorLecciones.PrepararEnSegundoPlano(groqService, idUsuario, GeneradorLecciones.Siguientes(bd, leccion.IdCurso, idUsuario, 0, 1));
        }
        return leccion.IdCurso;
    }
}
