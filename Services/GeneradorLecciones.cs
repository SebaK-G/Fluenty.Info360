using System.Text.Json;
using Fluenty.Info360.Models;

namespace Fluenty.Services;

// Sprint 2: todo lo que la IA hace con las lecciones.
// - Arma las tarjetas de una lección para UNA persona (variadas y distintas según la unidad; la 5 es un examen).
// - Las prepara por adelantado, en segundo plano, para que el empleado no tenga que esperar.
// - Arma una versión nueva de una pregunta, una explicación más simple, y corrige "Probalo vos".
public class GeneradorLecciones
{
    private readonly GroqService _groqService;

    public GeneradorLecciones(GroqService groqService)
    {
        _groqService = groqService;
    }

    // ============ Preparar lecciones por adelantado (no frena a nadie: corre en segundo plano) ============
    // Se arman de a una (para no saturar a la IA) y solo las que todavía no tienen tarjetas.
    public static void PrepararEnSegundoPlano(GroqService groqService, int idUsuario, List<int> idsLecciones)
    {
        _ = Task.Run(async () =>
        {
            GeneradorLecciones generador = new GeneradorLecciones(groqService);
            foreach (int idLeccion in idsLecciones)
            {
                try
                {
                    BD bd = new BD();
                    LeccionUsuario leccion = bd.TraerLeccionUsuario(idLeccion, idUsuario);
                    if (leccion != null && bd.TraerTarjetas(leccion.IdLeccionXUsuario).Count == 0)
                    {
                        await generador.GenerarLeccion(bd, leccion);
                    }
                }
                catch (Exception)
                {
                    // Si falla, no pasa nada: se arma cuando la persona abra la lección
                }
            }
        });
    }

    // Las próximas "cantidad" lecciones de esa persona en ese curso, después de la lección idLeccionActual
    // (si idLeccionActual es 0, arranca desde la primera disponible)
    public static List<int> Siguientes(BD bd, int idCurso, int idUsuario, int idLeccionActual, int cantidad)
    {
        List<Lecciones> lecciones = bd.TraerLeccionesUsuario(idCurso, idUsuario);
        List<int> resultado = new List<int>();
        bool contar = false;
        foreach (Lecciones leccion in lecciones)
        {
            if (idLeccionActual == 0 && leccion.Estado == "Disponible")
            {
                contar = true;
            }
            if (contar && leccion.Estado != "YaSabe" && leccion.Estado != "Completada" && resultado.Count < cantidad)
            {
                resultado.Add(leccion.IdLeccion);
            }
            if (leccion.IdLeccion == idLeccionActual)
            {
                contar = true;
            }
        }
        return resultado;
    }

    // ============ Armar las tarjetas de una lección. Devuelve "" si salió bien, o el error ============
    public async Task<string> GenerarLeccion(BD bd, LeccionUsuario leccion)
    {
        Usuarios persona = bd.TraerEmpleado(leccion.IdUsuario);
        FormulariosEmpleado form = bd.TraerFormularioEmpleado(leccion.IdUsuario);
        if (persona == null || form == null)
        {
            return "No encontramos el formulario de esta persona para armar la lección.";
        }
        bool esExamen = leccion.Numero == 5;

        // ¿Es la primera lección que hace? (la lección 1 de la unidad donde arranca)
        bool esPrimera = false;
        foreach (CursoXUsuario asignacion in bd.TraerAsignacionesCurso(leccion.IdCurso))
        {
            if (asignacion.IdUsuario == leccion.IdUsuario && asignacion.UnidadInicial == leccion.NumeroUnidad && leccion.Numero == 1)
            {
                esPrimera = true;
            }
        }

        // Las lecciones de la unidad, para que avance en orden y el examen repase todo
        string unidad = "";
        foreach (Lecciones item in bd.TraerLeccionesCurso(leccion.IdCurso))
        {
            if (item.NumeroUnidad == leccion.NumeroUnidad)
            {
                unidad = unidad + "   Lección " + item.Numero + ": " + item.Titulo + " (" + item.Objetivo + ")\n";
            }
        }

        string datos = "CURSO: " + leccion.TituloCurso + "\n";
        datos = datos + "UNIDAD " + leccion.NumeroUnidad + " DE 4: " + leccion.TituloUnidad + "\n";
        datos = datos + "LAS LECCIONES DE ESTA UNIDAD:\n" + unidad;
        datos = datos + "LA LECCIÓN QUE TENÉS QUE ESCRIBIR: lección " + leccion.Numero + ", \"" + leccion.Titulo + "\". Objetivo: " + leccion.Objetivo + "\n";
        if (esExamen)
        {
            datos = datos + "ES EL EXAMEN DE LA UNIDAD.\n";
            if (leccion.Intentos > 0)
            {
                datos = datos + "Ya lo rindió " + leccion.Intentos + " vez/veces y no aprobó (última nota: " + leccion.Nota + "%). Hacé preguntas NUEVAS, distintas a las de antes.\n";
            }
        }
        datos = datos + "\nLA PERSONA QUE LA VA A HACER:\n" + DatosIA.Persona(bd, persona, form);

        ContenidoLeccionIA contenido = null;
        try
        {
            int intento = 1;
            while (intento <= 2 && contenido == null)
            {
                string respuesta = await _groqService.PreguntarAsync(Instrucciones(leccion.NumeroUnidad, esExamen, esPrimera), datos, true);
                contenido = LeerContenido(respuesta, esExamen);
                intento++;
            }
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        if (contenido == null)
        {
            return "La IA respondió con un formato incorrecto. Probá abrir la lección de nuevo.";
        }

        // Si mientras tanto alguien ya la armó (por ejemplo, la preparación en segundo plano), no se duplica
        if (bd.TraerTarjetas(leccion.IdLeccionXUsuario).Count > 0)
        {
            return "";
        }

        int orden = 1;
        foreach (TarjetaIA tarjetaIA in contenido.Tarjetas)
        {
            if (tarjetaIA.Tipo == "pregunta")
            {
                // Cada versión es una fila con el mismo Orden
                int version = 1;
                foreach (TarjetaIA versionIA in tarjetaIA.Versiones)
                {
                    versionIA.Tipo = tarjetaIA.Subtipo;
                    bd.AgregarTarjeta(ConvertirTarjeta(versionIA, leccion.IdLeccionXUsuario, orden, version));
                    version++;
                }
            }
            else
            {
                bd.AgregarTarjeta(ConvertirTarjeta(tarjetaIA, leccion.IdLeccionXUsuario, orden, 1));
            }
            orden++;
        }
        return "";
    }

    // ============ Una versión NUEVA de una pregunta (mismo tema, otro ejemplo) ============
    public async Task<Tarjetas> GenerarVersion(BD bd, Tarjetas pregunta, int nuevaVersion, int idUsuario)
    {
        Usuarios persona = bd.TraerEmpleado(idUsuario);
        FormulariosEmpleado form = bd.TraerFormularioEmpleado(idUsuario);

        string instrucciones = "Sos el profe de Fluenty. La persona se equivocó en esta pregunta y ya usó todas las versiones que teníamos. ";
        instrucciones = instrucciones + "Escribí UNA pregunta NUEVA sobre el MISMO tema, del mismo tipo (" + pregunta.Tipo + "), con un ejemplo distinto y bien pegado a su trabajo. ";
        instrucciones = instrucciones + "Que no se pueda contestar de memoria: cambiá el caso, no solo las palabras. ";
        instrucciones = instrucciones + "La explicación tiene que enseñar: por qué la correcta está bien, por qué las otras están mal, y un ejemplo de su trabajo. ";
        instrucciones = instrucciones + "Español rioplatense, sin markdown. Si el tipo es verdaderofalso, opcionA = \"Verdadero\", opcionB = \"Falso\" y opcionC vacía. ";
        instrucciones = instrucciones + "Respondé SOLO con un JSON: {\"titulo\": \"...\", \"texto\": \"la pregunta\", \"contexto\": \"un texto o respuesta de IA para analizar, si hace falta\", \"opcionA\": \"...\", \"opcionB\": \"...\", \"opcionC\": \"...\", \"correcta\": \"A\", \"explicacion\": \"...\"}";

        string datos = "LA PREGUNTA EN LA QUE SE EQUIVOCÓ:\n" + pregunta.Texto + "\n" + pregunta.Contexto + "\nA) " + pregunta.OpcionA + "\nB) " + pregunta.OpcionB + "\nC) " + pregunta.OpcionC;
        datos = datos + "\nLo que enseña: " + pregunta.Explicacion + "\n\n";
        if (persona != null && form != null)
        {
            datos = datos + "LA PERSONA:\n" + DatosIA.Persona(bd, persona, form);
        }

        string respuesta = await _groqService.PreguntarAsync(instrucciones, datos, true);
        TarjetaIA versionIA = Leer<TarjetaIA>(respuesta);
        if (versionIA == null || !PreguntaValida(versionIA))
        {
            return null;
        }
        versionIA.Tipo = pregunta.Tipo;
        Tarjetas nueva = ConvertirTarjeta(versionIA, pregunta.IdLeccionXUsuario, pregunta.Orden, nuevaVersion);
        nueva.IdTarjeta = bd.AgregarTarjeta(nueva);
        return nueva;
    }

    // ============ Explicar de nuevo: "simple" (más simple), "ejemplo" (otro ejemplo) o "error" (se equivocó 3 veces) ============
    public async Task<TextoIA> Reexplicar(BD bd, int idUsuario, string titulo, string texto, string modo)
    {
        Usuarios persona = bd.TraerEmpleado(idUsuario);
        FormulariosEmpleado form = bd.TraerFormularioEmpleado(idUsuario);

        string instrucciones = "Sos el profe de Fluenty. ";
        if (modo == "ejemplo")
        {
            instrucciones = instrucciones + "Reescribí esta explicación con OTRO ejemplo, distinto y bien pegado al trabajo de esta persona. ";
        }
        else if (modo == "error")
        {
            instrucciones = instrucciones + "La persona se equivocó 3 veces en este tema: explicáselo de nuevo DESDE CERO, de otra forma, ";
            instrucciones = instrucciones + "con una comparación de la vida diaria (si podés, con sus hobbies) y un ejemplo paso a paso de su trabajo. Con calma y buena onda, sin hacerla sentir mal. ";
        }
        else
        {
            instrucciones = instrucciones + "La persona no la entendió: reescribila MÁS SIMPLE, sin palabras técnicas, con una comparación de la vida diaria (si podés, con sus hobbies). ";
        }
        instrucciones = instrucciones + "Entre 40 y 90 palabras, español rioplatense, sin markdown. Respondé SOLO con un JSON: {\"titulo\": \"...\", \"texto\": \"...\"}";

        string datos = "LO QUE HAY QUE EXPLICAR:\nTítulo: " + titulo + "\nTexto: " + texto + "\n\n";
        if (persona != null && form != null)
        {
            datos = datos + "LA PERSONA:\n" + DatosIA.Persona(bd, persona, form);
        }
        string respuesta = await _groqService.PreguntarAsync(instrucciones, datos, true);
        TextoIA nuevo = Leer<TextoIA>(respuesta);
        if (nuevo == null || nuevo.Texto == null || nuevo.Texto == "")
        {
            return null;
        }
        return nuevo;
    }

    // ============ Corregir "Probalo vos". flexible = ya falló 3 veces y vio el ejemplo ============
    public async Task<CorreccionIA> CorregirPractica(Tarjetas practica, string respuestaPersona, bool flexible)
    {
        string instrucciones = "Sos un profe de Fluenty que corrige un ejercicio de uso de IA en el trabajo. ";
        instrucciones = instrucciones + "Decidí si la persona APROBÓ: aprueba si su pedido a la IA cumple la consigna y aplica lo que enseña la lección (aunque no sea perfecto). ";
        instrucciones = instrucciones + "No aprueba si está vacío, no tiene que ver, o le falta lo central de la consigna. ";
        if (flexible)
        {
            instrucciones = instrucciones + "Ya falló 3 veces y le mostramos un ejemplo: aprobalo si lo reescribió con sus palabras y se nota que entendió la idea (no tiene que ser igual al ejemplo, pero tampoco una copia textual). ";
        }
        instrucciones = instrucciones + "En la devolución (máximo 4 oraciones, español rioplatense, cálido, sin markdown): algo que hizo bien, qué le falta o qué mejorar, y cómo quedaría mejor. ";
        instrucciones = instrucciones + "Respondé SOLO con un JSON: {\"aprobado\": true, \"devolucion\": \"...\"}";

        string datos = "CONSIGNA: " + practica.Texto + "\nPISTA QUE TENÍA: " + practica.Explicacion + "\nEJEMPLO DE UN PEDIDO BIEN HECHO: " + practica.RespuestaBuena;
        datos = datos + "\nLO QUE ESCRIBIÓ LA PERSONA: " + DatosIA.Recortar(respuestaPersona, 800);
        string respuesta = await _groqService.PreguntarAsync(instrucciones, datos, true);
        return Leer<CorreccionIA>(respuesta);
    }

    // ================= Métodos de ayuda =================

    private Tarjetas ConvertirTarjeta(TarjetaIA tarjetaIA, int idLeccionXUsuario, int orden, int version)
    {
        Tarjetas tarjeta = new Tarjetas();
        tarjeta.IdLeccionXUsuario = idLeccionXUsuario;
        tarjeta.Orden = orden;
        tarjeta.Version = version;
        tarjeta.Tipo = DatosIA.Recortar(tarjetaIA.Tipo, 20);
        tarjeta.Titulo = DatosIA.Recortar(tarjetaIA.Titulo, 150);
        tarjeta.ParaQue = DatosIA.Recortar(tarjetaIA.ParaQue, 300);
        tarjeta.Texto = DatosIA.Recortar(tarjetaIA.Texto, 1500);
        tarjeta.Ejemplo = DatosIA.Recortar(tarjetaIA.Ejemplo, 1500);
        tarjeta.Contexto = DatosIA.Recortar(tarjetaIA.Contexto, 1000);
        tarjeta.PedidoMalo = DatosIA.Recortar(tarjetaIA.PedidoMalo, 500);
        tarjeta.RespuestaMala = DatosIA.Recortar(tarjetaIA.RespuestaMala, 1000);
        tarjeta.PedidoBueno = DatosIA.Recortar(tarjetaIA.PedidoBueno, 500);
        tarjeta.RespuestaBuena = DatosIA.Recortar(tarjetaIA.RespuestaBuena, 1000);
        tarjeta.OpcionA = DatosIA.Recortar(tarjetaIA.OpcionA, 300);
        tarjeta.OpcionB = DatosIA.Recortar(tarjetaIA.OpcionB, 300);
        tarjeta.OpcionC = DatosIA.Recortar(tarjetaIA.OpcionC, 300);
        tarjeta.Correcta = DatosIA.Recortar(tarjetaIA.Correcta, 1);
        tarjeta.Explicacion = DatosIA.Recortar(tarjetaIA.Explicacion, 1000);
        return tarjeta;
    }

    private T Leer<T>(string respuesta) where T : class
    {
        try
        {
            JsonSerializerOptions opciones = new JsonSerializerOptions();
            opciones.PropertyNameCaseInsensitive = true;
            return JsonSerializer.Deserialize<T>(respuesta, opciones);
        }
        catch (Exception)
        {
            return null;
        }
    }

    // Una pregunta sirve si tiene texto, al menos 2 opciones y la correcta es una de las que tiene
    private bool PreguntaValida(TarjetaIA pregunta)
    {
        Limpiar(pregunta);
        pregunta.Correcta = pregunta.Correcta.Trim().ToUpper();
        if (pregunta.Texto == "" || pregunta.OpcionA == "" || pregunta.OpcionB == "")
        {
            return false;
        }
        if (pregunta.Correcta == "A" || pregunta.Correcta == "B")
        {
            return true;
        }
        return pregunta.Correcta == "C" && pregunta.OpcionC != "";
    }

    // Los campos que no vinieron quedan vacíos en vez de null
    private void Limpiar(TarjetaIA tarjeta)
    {
        if (tarjeta.Tipo == null) { tarjeta.Tipo = ""; }
        if (tarjeta.Subtipo == null) { tarjeta.Subtipo = ""; }
        if (tarjeta.Titulo == null) { tarjeta.Titulo = ""; }
        if (tarjeta.ParaQue == null) { tarjeta.ParaQue = ""; }
        if (tarjeta.Texto == null) { tarjeta.Texto = ""; }
        if (tarjeta.Ejemplo == null) { tarjeta.Ejemplo = ""; }
        if (tarjeta.Contexto == null) { tarjeta.Contexto = ""; }
        if (tarjeta.PedidoMalo == null) { tarjeta.PedidoMalo = ""; }
        if (tarjeta.RespuestaMala == null) { tarjeta.RespuestaMala = ""; }
        if (tarjeta.PedidoBueno == null) { tarjeta.PedidoBueno = ""; }
        if (tarjeta.RespuestaBuena == null) { tarjeta.RespuestaBuena = ""; }
        if (tarjeta.OpcionA == null) { tarjeta.OpcionA = ""; }
        if (tarjeta.OpcionB == null) { tarjeta.OpcionB = ""; }
        if (tarjeta.OpcionC == null) { tarjeta.OpcionC = ""; }
        if (tarjeta.Correcta == null) { tarjeta.Correcta = ""; }
        if (tarjeta.Explicacion == null) { tarjeta.Explicacion = ""; }
        if (tarjeta.Versiones == null) { tarjeta.Versiones = new List<TarjetaIA>(); }
    }

    // Revisa que la lección venga completa: arranca y termina bien, tiene preguntas y práctica
    private ContenidoLeccionIA LeerContenido(string respuesta, bool esExamen)
    {
        ContenidoLeccionIA contenido = Leer<ContenidoLeccionIA>(respuesta);
        if (contenido == null || contenido.Tarjetas == null || contenido.Tarjetas.Count < 5 || contenido.Tarjetas.Count > 14)
        {
            return null;
        }
        string[] tiposQueExplican = { "gancho", "explicacion", "comparacion", "chat", "historia", "cierre", "practica" };
        int preguntas = 0;
        int practicas = 0;
        List<TarjetaIA> validas = new List<TarjetaIA>();
        foreach (TarjetaIA tarjeta in contenido.Tarjetas)
        {
            if (tarjeta == null)
            {
                continue;
            }
            Limpiar(tarjeta);
            tarjeta.Tipo = tarjeta.Tipo.Trim().ToLower();
            if (tarjeta.Tipo == "pregunta")
            {
                tarjeta.Subtipo = tarjeta.Subtipo.Trim().ToLower();
                if (!Tarjetas.TiposPregunta.Contains(tarjeta.Subtipo))
                {
                    tarjeta.Subtipo = "opcion";
                }
                List<TarjetaIA> versiones = new List<TarjetaIA>();
                foreach (TarjetaIA version in tarjeta.Versiones)
                {
                    if (version != null && PreguntaValida(version))
                    {
                        versiones.Add(version);
                    }
                }
                if (versiones.Count > 0)
                {
                    tarjeta.Versiones = versiones;
                    validas.Add(tarjeta);
                    preguntas++;
                }
            }
            else if (tiposQueExplican.Contains(tarjeta.Tipo) && tarjeta.Texto != "")
            {
                validas.Add(tarjeta);
                if (tarjeta.Tipo == "practica")
                {
                    practicas++;
                }
            }
        }
        int minimoPreguntas = 2;
        if (esExamen)
        {
            minimoPreguntas = 4;
        }
        if (preguntas < minimoPreguntas || practicas == 0 || validas[validas.Count - 1].Tipo != "cierre")
        {
            return null;
        }
        contenido.Tarjetas = validas;
        return contenido;
    }

    // ============ El prompt de las lecciones ============
    // Cambia según la unidad (cuánta explicación y cuánta práctica), si es el examen (lección 5)
    // y si es la primera lección de la persona (tiene que llevarse algo útil ese mismo día)
    private string Instrucciones(int numeroUnidad, bool esExamen, bool esPrimera)
    {
        string texto = "Sos el mejor profe de Fluenty, una app que enseña a usar inteligencia artificial en el trabajo, al estilo Duolingo. ";
        texto = texto + "Escribís UNA lección para UNA persona concreta. Lo más importante son las EXPLICACIONES: claras, concretas, con ejemplos de SU trabajo, ";
        texto = texto + "que le sirvan igual a alguien grande que casi no usa tecnología que a un joven, y que sea entretenido. Español rioplatense, cálido, sin markdown ni emojis.\n\n";

        texto = texto + "PRIMERO PENSÁ, DESPUÉS ESCRIBÍ: en \"plan\" decidí la idea central de la lección, el error más común que comete la gente con este tema, ";
        texto = texto + "la mejor comparación de la vida diaria para ESTA persona (usá sus hobbies si se puede) y para qué le sirve en su trabajo. Después escribí las tarjetas siguiendo ese plan.\n\n";

        texto = texto + "TIPOS DE TARJETA (elegí cuáles usar y en qué orden; NO uses siempre los mismos ni en el mismo orden):\n";
        texto = texto + "- gancho: una situación concreta de su trabajo que la lección va a resolver. titulo + texto.\n";
        texto = texto + "- explicacion: enseña UNA idea. paraQue = una frase de para qué le sirve en su trabajo (va primero); texto = la explicación (40 a 90 palabras, sin palabras técnicas sin explicar); ";
        texto = texto + "ejemplo = un ejemplo resuelto PASO A PASO de su trabajo (3 o 4 pasos, uno por línea, empezando con \"1.\", \"2.\"...).\n";
        texto = texto + "- comparacion: la idea explicada con algo de la vida diaria. titulo + texto.\n";
        texto = texto + "- chat: cómo se hace. texto = intro corta; pedidoMalo / respuestaMala = un pedido flojo a la IA y lo que contestaría; pedidoBueno / respuestaBuena = el pedido bien hecho para su caso y la respuesta.\n";
        texto = texto + "- historia: una mini historia (60 a 100 palabras) con un compañero o cliente de su trabajo que tiene este problema y lo resuelve con IA.\n";
        texto = texto + "- pregunta: subtipo = opcion (elegir la mejor de 3), verdaderofalso (opcionA \"Verdadero\", opcionB \"Falso\"), completar (texto con \"____\" y 3 opciones para llenarlo), ";
        texto = texto + "error (contexto = una respuesta de IA con UN error; las opciones son frases de esa respuesta), mejorrespuesta (contexto = un pedido; las opciones son 3 respuestas de la IA). ";
        texto = texto + "Cada pregunta trae \"versiones\": " + (esExamen ? "2" : "3") + " versiones del MISMO tema con casos DISTINTOS (si se equivoca le toca otra, así no la pasa de memoria). ";
        texto = texto + "En cada versión: titulo, texto (la pregunta), contexto (si hace falta), opcionA, opcionB, opcionC, correcta (\"A\", \"B\" o \"C\") y explicacion. ";
        texto = texto + "La explicacion de una pregunta ENSEÑA (50 a 90 palabras): por qué la correcta está bien, por qué las otras están mal, y cómo se aplica en su trabajo. Variá la letra correcta.\n";
        texto = texto + "- practica: \"Probalo vos\". texto = una consigna concreta para escribir su propio pedido a la IA sobre una tarea suya; explicacion = una pista; ";
        texto = texto + "respuestaBuena = un ejemplo de pedido bien hecho (se le muestra si falla 3 veces).\n";
        texto = texto + "- cierre: titulo = el resumen en una frase; texto = UNA acción concreta para hacer mañana en su trabajo. Siempre es la última.\n\n";

        if (esExamen)
        {
            texto = texto + "ESTA LECCIÓN ES EL EXAMEN DE LA UNIDAD " + numeroUnidad + ". Tiene que aprobarlo (70% de las preguntas bien a la primera) para pasar a la unidad siguiente. ";
            texto = texto + "Nada de explicaciones nuevas: arrancá con una historia corta que plantee un caso real de su trabajo, después 5 preguntas que repasen TODAS las lecciones de la unidad ";
            texto = texto + "(usá al menos 3 subtipos distintos y que no sean obvias), una practica que sea un caso completo de su trabajo para resolver de punta a punta, y el cierre. 8 tarjetas en total.\n\n";
        }
        else if (numeroUnidad == 1)
        {
            texto = texto + "ESTILO DE LA UNIDAD 1 (fundamentos): muy guiado. Arrancá con un gancho, explicá con calma (1 o 2 tarjetas de explicacion con ejemplo paso a paso), ";
            texto = texto + "usá una comparacion, mostrá un chat, 2 preguntas de subtipos distintos, una practica sencilla y el cierre. Entre 8 y 10 tarjetas.\n\n";
        }
        else if (numeroUnidad == 2)
        {
            texto = texto + "ESTILO DE LA UNIDAD 2 (aplicarlo a su tarea): ejemplo resuelto y práctica guiada. Gancho con su tarea real, 1 explicacion con ejemplo paso a paso, ";
            texto = texto + "un chat o una historia, 2 o 3 preguntas (subtipos distintos), una practica sobre su tarea real y el cierre. Entre 7 y 9 tarjetas.\n\n";
        }
        else
        {
            texto = texto + "ESTILO DE LAS UNIDADES 3 Y 4 (nivel avanzado): menos teoría y más hacer. Gancho con un caso real y difícil, una explicacion corta con trucos de usuario avanzado, ";
            texto = texto + "una historia o un chat, 3 preguntas exigentes (subtipos distintos, nada obvio), una practica exigente con un caso real y el cierre. Entre 7 y 9 tarjetas.\n\n";
        }

        if (esPrimera)
        {
            texto = texto + "ESTA ES SU PRIMERA LECCIÓN DEL CURSO (manda sobre el estilo de la unidad). Capaz la capacitación se la impuso su jefe y no tiene ganas: ";
            texto = texto + "si no siente que le sirvió, no vuelve. Tiene que terminar con algo que le ahorre trabajo HOY. ";
            texto = texto + "Arrancá con un gancho sobre la tarea que más tiempo le come (si no la contó, algo que hace la gente de su rango). ";
            texto = texto + "Incluí un chat cuyo pedidoBueno esté LISTO PARA COPIAR Y PEGAR y le resuelva esa tarea, con [corchetes] donde tiene que poner sus datos. ";
            texto = texto + "Explicá solo lo justo para entender por qué ese pedido funciona. En la practica, que adapte ese pedido a un caso suyo. ";
            texto = texto + "En el cierre, decile que copie el pedido y lo use hoy mismo. Nada de historia ni teoría de la IA. Entre 6 y 8 tarjetas.\n\n";
        }
        texto = texto + "SIEMPRE: vendé el beneficio concreto (\"esto te ahorra media hora con los presupuestos\"), no \"aprender IA\". ";
        texto = texto + "Si la persona contó poco de su trabajo, usá lo que hace su rango y no inventes datos personales.\n\n";

        texto = texto + "ASÍ SE VE UNA TARJETA BUENA (ejemplo para una administrativa a la que le gusta cocinar; NO copies el tema, copiá la calidad):\n";
        texto = texto + "{\"tipo\": \"explicacion\", \"titulo\": \"Darle contexto a la IA\", \"paraQue\": \"Te ahorra rehacer tres veces el mail de cobranza.\", ";
        texto = texto + "\"texto\": \"La IA no conoce a tus clientes ni tu empresa. Si le pedís 'escribí un mail de cobranza', inventa todo y queda genérico. Es como pedirle a alguien que te cocine algo rico sin decirle qué hay en la heladera: ";
        texto = texto + "va a improvisar. Cuanto más le contás (a quién le escribís, qué pasó, qué tono querés), menos tenés que corregir.\", ";
        texto = texto + "\"ejemplo\": \"1. Decí quién sos: 'Soy administrativa de una distribuidora'.\\n2. Contá la situación: 'El cliente debe la factura 245 hace 20 días'.\\n3. Pedí el tono: 'Amable pero firme, sin amenazar'.\\n4. Pedí el formato: 'Un mail corto, de 5 líneas'.\"}\n";
        texto = texto + "{\"tipo\": \"pregunta\", \"subtipo\": \"opcion\", \"versiones\": [{\"titulo\": \"¿Cuál pedido sale mejor?\", \"texto\": \"Tenés que avisarle a un proveedor que el pedido llegó incompleto. ¿Qué le pedís a la IA?\", ";
        texto = texto + "\"contexto\": \"\", \"opcionA\": \"Escribí un mail al proveedor\", \"opcionB\": \"Escribí un mail cordial a nuestro proveedor de envases avisando que del pedido 118 faltaron 40 cajas, pidiendo que las manden esta semana\", ";
        texto = texto + "\"opcionC\": \"Haceme un mail enojado porque falta mercadería\", \"correcta\": \"B\", \"explicacion\": \"La B le da todo lo que necesita: a quién, qué pasó, con datos concretos y qué querés lograr. ";
        texto = texto + "La A es tan vaga que la IA va a inventar el problema, y la C pide enojo, que rara vez ayuda con un proveedor. En tu trabajo, antes de pedirle algo a la IA, pensá: ¿le dije quién, qué pasó y qué quiero?\"}, ...]}\n\n";

        texto = texto + "ANTES DE RESPONDER, REVISÁ: ¿lo entendería alguien que nunca usó IA? ¿todos los ejemplos son de SU trabajo y SUS tareas? ¿quedó alguna palabra técnica sin explicar? ";
        texto = texto + "¿la comparación tiene sentido? ¿las preguntas tienen una sola respuesta correcta y no son obvias? ¿las versiones de cada pregunta usan casos distintos? ¿variaste los tipos de tarjeta?\n\n";

        texto = texto + "RESPONDÉ SOLO CON UN JSON CON ESTE FORMATO (los campos que una tarjeta no usa van vacíos):\n";
        texto = texto + "{\"plan\": {\"ideaCentral\": \"...\", \"errorComun\": \"...\", \"comparacion\": \"...\", \"paraQue\": \"...\"}, ";
        texto = texto + "\"tarjetas\": [{\"tipo\": \"gancho\", \"titulo\": \"...\", \"texto\": \"...\"}, ";
        texto = texto + "{\"tipo\": \"explicacion\", \"titulo\": \"...\", \"paraQue\": \"...\", \"texto\": \"...\", \"ejemplo\": \"1. ...\\n2. ...\"}, ";
        texto = texto + "{\"tipo\": \"pregunta\", \"subtipo\": \"opcion\", \"versiones\": [{\"titulo\": \"...\", \"texto\": \"...\", \"contexto\": \"\", \"opcionA\": \"...\", \"opcionB\": \"...\", \"opcionC\": \"...\", \"correcta\": \"A\", \"explicacion\": \"...\"}, ...]}, ";
        texto = texto + "{\"tipo\": \"practica\", \"titulo\": \"...\", \"texto\": \"...\", \"explicacion\": \"pista\", \"respuestaBuena\": \"ejemplo de pedido bien hecho\"}, ";
        texto = texto + "..., {\"tipo\": \"cierre\", \"titulo\": \"...\", \"texto\": \"...\"}]}";
        return texto;
    }
}
