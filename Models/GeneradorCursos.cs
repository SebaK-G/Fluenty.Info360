using System.Text.Json;

namespace Fluenty.Info360.Models;

// Sprint 3: todo lo que CursoController le manda a la IA y cómo lee lo que le devuelve.
// - Instrucciones...: el prompt fijo (qué tiene que hacer la IA y en qué formato responder).
// - Datos...: la información del rango, la empresa y las personas que acompaña al prompt.
// - Leer...: pasa el JSON que respondió la IA a objetos (CursoIA, UnidadIA, NivelacionIA) y revisa que venga completo.
// Así el controller solo se encarga de recibir el pedido, guardar en la base y elegir la vista.
public static class GeneradorCursos
{
    // ===================== EL CURSO COMPLETO (Generar) =====================

    // El prompt fijo del curso por rango (ver PROMPT-IA.md)
    public static string InstruccionesCurso(bool publico)
    {
        string texto = "Sos un diseñador de cursos de Fluenty, una app que enseña a usar inteligencia artificial en el trabajo con el estilo de Duolingo: ";
        texto = texto + "un camino largo de lecciones cortas, una por día, que se van desbloqueando.\n\n";

        texto = texto + "TU TAREA: armar el PLAN de UN curso de 4 semanas para TODAS las personas de un rango (el mismo puesto dentro de la empresa): ";
        texto = texto + "4 unidades de 5 lecciones cada una (20 lecciones, una por día). Todavía no escribas el contenido de las lecciones: solo el plan. ";
        texto = texto + "El curso es uno solo para el grupo; lo que cambia de persona a persona es en qué unidad arranca.\n\n";

        texto = texto + "CÓMO ARMAR LAS UNIDADES:\n";
        texto = texto + "Unidad 1: fundamentos de IA adaptados a este puesto (qué es, qué no es, cómo pedirle bien las cosas, cuidar los datos).\n";
        texto = texto + "Unidad 2: aplicar la IA a la tarea repetitiva MÁS COMÚN del grupo.\n";
        texto = texto + "Unidad 3: aplicar la IA a otras tareas repetitivas que comparten.\n";
        texto = texto + "Unidad 4: un proyecto real del puesto, apuntando a los objetivos de la empresa.\n";
        texto = texto + "La lección 5 de cada unidad es SIEMPRE un desafío que repasa la unidad: su título empieza con \"Desafío:\".\n";
        texto = texto + "Las lecciones van de lo simple a lo difícil, no se repiten y cada una enseña UNA cosa concreta que se puede usar al día siguiente. ";
        texto = texto + "Usá las tareas, herramientas, dificultades y días típicos reales del grupo. Si el grupo es de personas muy distintas, buscá lo que tienen en común.\n\n";

        texto = texto + "NIVELACIÓN (para CADA persona, por su IdUsuario): decidí en qué unidad arranca (1 a 4) según lo que ya sabe de IA. ";
        texto = texto + "Si nunca la usó o solo la probó, arranca en la 1. Si la usa a veces para cosas simples, puede arrancar en la 2. Solo si la usa seguido en su trabajo arranca en la 3. ";
        texto = texto + "Casi nunca se arranca en la 4. En motivo explicá en una oración por qué la ubicaste ahí.\n\n";

        texto = texto + "ELEGÍ UNO DE ESTOS 7 MODELOS DE CURSO según el puesto: ";
        texto = texto + "Tareas de escritorio, Oficio y campo, Ventas y atención al cliente, Gestión y liderazgo, Creativo y marketing, Técnico y datos, Primer contacto (si casi no usan tecnología).\n\n";

        if (publico)
        {
            texto = texto + "ES UN CURSO PÚBLICO: lo va a ver gente de CUALQUIER empresa en un buscador. Armalo sobre el TEMA y la CATEGORÍA que te pasan, ";
            texto = texto + "para cualquier persona que trabaje con eso. No uses nombres de empresas, personas, clientes ni datos internos. ";
            texto = texto + "En las unidades 2 a 4 usá ejemplos de trabajo comunes (no de una empresa en particular).\n\n";
        }

        texto = texto + "REGLAS: español rioplatense, claro, motivador y sin relleno. Títulos de lección cortos (máximo 60 caracteres). ";
        texto = texto + "Objetivo de cada lección: una oración que empiece con un verbo. Si alguien maneja datos de clientes, incluí una lección sobre cuidarlos. ";
        texto = texto + "Si a varios les preocupa algo de la IA, incluí una lección temprana que lo responda con calma. Nunca pidas datos privados de clientes.\n\n";

        texto = texto + "RESPONDÉ SOLO CON UN JSON CON ESTE FORMATO EXACTO:\n";
        texto = texto + "{\"modelo\": \"...\", \"titulo\": \"título del curso\", \"descripcion\": \"una o dos oraciones\", ";
        texto = texto + "\"categoria\": \"una de: " + string.Join(", ", Cursos.Categorias) + "\", ";
        texto = texto + "\"duracionHoras\": horas totales estimadas del curso (número), \"herramientas\": [\"herramientas que se usan, ej. ChatGPT\", \"Excel\"], ";
        texto = texto + "\"objetivos\": [\"lo que van a lograr 1\", \"2\", \"3\"], ";
        texto = texto + "\"nivelacion\": [{\"idUsuario\": 5, \"unidadInicial\": 1, \"motivo\": \"...\"}, ...una por persona], ";
        texto = texto + "\"unidades\": [";
        texto = texto + "{\"numero\": 1, \"titulo\": \"...\", \"objetivo\": \"...\", \"lecciones\": [";
        texto = texto + "{\"numero\": 1, \"titulo\": \"...\", \"objetivo\": \"...\"}, ";
        texto = texto + "{\"numero\": 2, ...}, {\"numero\": 3, ...}, {\"numero\": 4, ...}, ";
        texto = texto + "{\"numero\": 5, \"titulo\": \"Desafío: ...\", \"objetivo\": \"...\"}]}, ";
        texto = texto + "{\"numero\": 2, ...}, {\"numero\": 3, ...}, {\"numero\": 4, ...}";
        texto = texto + "]}";
        return texto;
    }

    // Los datos de todo el rango para la IA: la empresa y cada persona (como mucho 8)
    public static string DatosGrupo(BD bd, Rangos rango, List<Usuarios> personas, List<FormulariosEmpleado> formularios, FormulariosEmpresa formEmpresa)
    {
        string datos = "RANGO: " + rango.Nombre + " (" + personas.Count + " personas)\n\n";
        int cantidad = personas.Count;
        if (cantidad > 8)
        {
            cantidad = 8;
        }
        for (int i = 0; i < cantidad; i++)
        {
            datos = datos + "PERSONA " + (i + 1) + ":\n" + DatosIA.Persona(bd, personas[i], formularios[i]) + "\n";
        }
        datos = datos + DatosIA.Empresa(bd, formEmpresa);
        return datos;
    }

    // Curso público: solo el tema, la categoría y el nivel de IA de cada persona (para nivelarla).
    // Nada de la empresa ni de sus tareas: el curso lo va a ver gente de otras empresas.
    public static string DatosPublico(string tema, string categoria, List<Usuarios> personas, List<FormulariosEmpleado> formularios)
    {
        string datos = "CURSO PÚBLICO\nTEMA: " + DatosIA.Recortar(tema, 300) + "\nCATEGORÍA: " + categoria + "\n\n";
        datos = datos + "PERSONAS PARA NIVELAR (solo su experiencia con la IA):\n";
        for (int i = 0; i < personas.Count; i++)
        {
            datos = datos + "- IdUsuario " + personas[i].IdUsuario + ": " + DatosIA.Nivel(formularios[i].NivelIA) + "\n";
        }
        return datos;
    }

    // Lee el curso que armó la IA. Devuelve null si no vino completo (título, objetivos y 4 unidades de 5 lecciones).
    public static CursoIA LeerCurso(string respuesta)
    {
        CursoIA curso = null;
        try
        {
            JsonSerializerOptions opciones = new JsonSerializerOptions();
            opciones.PropertyNameCaseInsensitive = true;
            curso = JsonSerializer.Deserialize<CursoIA>(respuesta, opciones);
        }
        catch (Exception)
        {
            return null;
        }

        // Tiene que venir completo: título, objetivos y 4 unidades de 5 lecciones
        if (curso == null || curso.Titulo == null || curso.Titulo == "" || curso.Objetivos == null || curso.Unidades == null)
        {
            return null;
        }
        if (curso.Objetivos.Count == 0 || curso.Unidades.Count != 4)
        {
            return null;
        }
        foreach (UnidadIA unidad in curso.Unidades)
        {
            if (unidad.Lecciones == null || unidad.Lecciones.Count != 5)
            {
                return null;
            }
        }
        return curso;
    }

    // ===================== UNA UNIDAD (RehacerUnidad) =====================

    // El prompt para rehacer una sola unidad del borrador
    public static string InstruccionesUnidad(int numeroUnidad, bool publico)
    {
        string texto = "Sos un diseñador de cursos de Fluenty, una app que enseña a usar inteligencia artificial en el trabajo con el estilo de Duolingo: ";
        texto = texto + "lecciones cortas, una por día, que se van desbloqueando.\n\n";

        texto = texto + "TU TAREA: el responsable revisó el plan del curso y quiere cambiar SOLO la unidad " + numeroUnidad + ". ";
        texto = texto + "Rehacé esa unidad (su título, su objetivo y sus 5 lecciones) siguiendo lo que pide el responsable. ";
        texto = texto + "Las demás unidades NO se tocan: usalas solo para no repetir temas y para que la unidad nueva encaje entre la anterior y la siguiente. ";
        texto = texto + "Todavía no escribas el contenido de las lecciones: solo el plan.\n\n";

        texto = texto + "REGLAS: la lección 5 es SIEMPRE un desafío que repasa la unidad y su título empieza con \"Desafío:\". ";
        texto = texto + "Las lecciones van de lo simple a lo difícil y cada una enseña UNA cosa concreta que el empleado puede usar al día siguiente en su trabajo. ";
        texto = texto + "Usá sus tareas, herramientas, dificultades y objetivos reales. ";
        texto = texto + "Español rioplatense, claro y sin relleno. Títulos de lección cortos (máximo 60 caracteres). ";
        texto = texto + "Objetivo de cada lección: una oración que empiece con un verbo. Nunca pidas datos privados de clientes. ";
        texto = texto + "Si el pedido del responsable no tiene que ver con el curso, ignoralo y mejorá la unidad igual.\n\n";

        texto = texto + "RESPONDÉ SOLO CON UN JSON CON ESTE FORMATO EXACTO:\n";
        texto = texto + "{\"numero\": " + numeroUnidad + ", \"titulo\": \"...\", \"objetivo\": \"...\", \"lecciones\": [";
        texto = texto + "{\"numero\": 1, \"titulo\": \"...\", \"objetivo\": \"...\"}, ";
        texto = texto + "{\"numero\": 2, ...}, {\"numero\": 3, ...}, {\"numero\": 4, ...}, ";
        texto = texto + "{\"numero\": 5, \"titulo\": \"Desafío: ...\", \"objetivo\": \"...\"}]}";

        if (publico)
        {
            // Curso público: la unidad se rehace sin datos de la empresa ni de las personas
            texto = texto + "\n\nES UN CURSO PÚBLICO: lo ve gente de cualquier empresa. No uses nombres de empresas, personas, clientes ni datos internos.";
        }
        return texto;
    }

    // Los datos para rehacer una unidad: los del grupo (o del curso público), el plan actual y lo que pidió el responsable
    public static string DatosRehacerUnidad(string datosBase, Cursos curso, List<Unidades> unidades, List<Lecciones> lecciones, int numeroUnidad, string pedido)
    {
        string datos = datosBase;
        datos = datos + "\nPLAN ACTUAL DEL CURSO \"" + curso.Titulo + "\":\n";
        foreach (Unidades unidad in unidades)
        {
            datos = datos + "Unidad " + unidad.Numero + ": " + unidad.Titulo + " (" + unidad.Objetivo + ")\n";
            foreach (Lecciones leccion in lecciones)
            {
                if (leccion.NumeroUnidad == unidad.Numero)
                {
                    datos = datos + "   Lección " + leccion.Numero + ": " + leccion.Titulo + "\n";
                }
            }
        }
        datos = datos + "\nUNIDAD A REHACER: la " + numeroUnidad + ".\n";
        datos = datos + "QUÉ PIDE EL RESPONSABLE: " + DatosIA.Recortar(pedido.Trim(), 300) + "\n";
        return datos;
    }

    // Lee la unidad que rehízo la IA. Devuelve null si no vino con título y 5 lecciones.
    public static UnidadIA LeerUnidad(string respuesta)
    {
        UnidadIA unidad = null;
        try
        {
            JsonSerializerOptions opciones = new JsonSerializerOptions();
            opciones.PropertyNameCaseInsensitive = true;
            unidad = JsonSerializer.Deserialize<UnidadIA>(respuesta, opciones);
        }
        catch (Exception)
        {
            return null;
        }
        if (unidad == null || unidad.Titulo == null || unidad.Titulo == "" || unidad.Lecciones == null || unidad.Lecciones.Count != 5)
        {
            return null;
        }
        return unidad;
    }

    // ===================== NIVELAR A ALGUIEN QUE SE SUMA DESPUÉS (Sumar) =====================

    // El prompt para decidir en qué unidad arranca una persona
    public static string InstruccionesNivelacion()
    {
        string instrucciones = "Sos el que nivela a las personas en los cursos de Fluenty (4 unidades: la 1 son fundamentos y la 4 es lo más avanzado). ";
        instrucciones = instrucciones + "Decidí en qué unidad arranca esta persona según lo que ya sabe de IA: si nunca la usó o solo la probó, en la 1; si la usa a veces, puede ir a la 2; si la usa seguido en su trabajo, a la 3. Casi nunca a la 4. ";
        instrucciones = instrucciones + "Respondé SOLO con un JSON: {\"unidadInicial\": 1, \"motivo\": \"una oración en español rioplatense\"}";
        return instrucciones;
    }

    // Los datos para nivelar: el plan del curso y la persona
    public static string DatosNivelacion(BD bd, Cursos curso, List<Unidades> unidades, Usuarios persona, FormulariosEmpleado form)
    {
        string plan = "";
        foreach (Unidades unidad in unidades)
        {
            plan = plan + "Unidad " + unidad.Numero + ": " + unidad.Titulo + " (" + unidad.Objetivo + ")\n";
        }
        return "PLAN DEL CURSO \"" + curso.Titulo + "\":\n" + plan + "\nLA PERSONA:\n" + DatosIA.Persona(bd, persona, form);
    }

    // Lee la nivelación. Devuelve null si no vino o si la unidad no está entre 1 y 4.
    public static NivelacionIA LeerNivelacion(string respuesta)
    {
        NivelacionIA nivelacion = null;
        try
        {
            JsonSerializerOptions opciones = new JsonSerializerOptions();
            opciones.PropertyNameCaseInsensitive = true;
            nivelacion = JsonSerializer.Deserialize<NivelacionIA>(respuesta, opciones);
        }
        catch (Exception)
        {
            return null;
        }
        if (nivelacion == null || nivelacion.UnidadInicial < 1 || nivelacion.UnidadInicial > 4)
        {
            return null;
        }
        return nivelacion;
    }
}
