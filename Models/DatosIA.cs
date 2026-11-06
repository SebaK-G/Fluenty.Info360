namespace Fluenty.Info360.Models;

// Sprint 2: arma los textos con los datos que le mandamos a la IA.
// Lo usan CursoController (para el plan del curso) y LeccionController (para las tarjetas).
public static class DatosIA
{
    // NivelIA: 1 = nunca la usó, 2 = la probó alguna vez, 3 = la usa a veces, 4 = la usa seguido en el trabajo
    public static string Nivel(int nivelIA)
    {
        string nivel = "principiante: nunca usó una IA";
        if (nivelIA == 2)
        {
            nivel = "principiante: probó una IA alguna vez";
        }
        if (nivelIA == 3)
        {
            nivel = "intermedio: usa la IA a veces para cosas simples";
        }
        if (nivelIA == 4)
        {
            nivel = "avanzado: usa la IA seguido en su trabajo";
        }
        return nivel;
    }

    // En qué unidad arranca si la IA no lo dice (o lo dice mal)
    public static int UnidadPorNivel(int nivelIA)
    {
        if (nivelIA == 4)
        {
            return 3;
        }
        if (nivelIA == 3)
        {
            return 2;
        }
        return 1;
    }

    // Todo lo que sabemos de una persona: su trabajo y cómo explicarle.
    // Si dejó algo vacío o puso cualquier cosa ("nada", "asd"), no se lo mandamos a la IA:
    // en su lugar va lo que el dueño contó que hace la gente de su rango.
    public static string Persona(BD bd, Usuarios persona, FormulariosEmpleado form)
    {
        Rangos rango = null;
        foreach (Rangos item in bd.TraerRangosEmpresa(persona.IdEmpresa))
        {
            if (persona.IdRango != null && item.IdRango == persona.IdRango.Value)
            {
                rango = item;
            }
        }

        string datos = "Nombre: " + persona.Nombre + " (IdUsuario " + persona.IdUsuario + ")\n";
        if (rango != null)
        {
            datos = datos + "Rango en la empresa: " + rango.Nombre + "\n";
            if (!EsFloja(rango.Tareas))
            {
                datos = datos + "Lo que hace la gente de su rango (lo contó su responsable): " + rango.Tareas + "\n";
            }
        }
        datos = Agregar(datos, "Puesto", persona.Puesto);
        datos = Agregar(datos, "Área", form.Area);
        datos = datos + "Nivel con la IA (lo dice la persona): " + Nivel(form.NivelIA) + "\n";
        datos = datos + "Herramientas que usa: " + NombresHerramientas(bd.TraerHerramientasUsuario(persona.IdUsuario)) + "\n";
        datos = Agregar(datos, "Tareas que más tiempo le llevan", form.Tareas);
        datos = Agregar(datos, "La tarea que más tiempo le come (con esto arranca su primera lección)", form.TareaConcreta);
        datos = Agregar(datos, "Cómo es un día típico de su trabajo", form.DiaTipico);
        datos = Agregar(datos, "Principal dificultad en su trabajo", form.DificultadPrincipal);
        datos = Agregar(datos, "Errores o dificultades en sus tareas", form.Dificultades);
        datos = Agregar(datos, "Qué le gustaría que la empresa mejore en su puesto", form.MejoraEmpresa);
        datos = Agregar(datos, "Fortalezas y debilidades", form.FortalezasDebilidades);
        datos = Agregar(datos, "Qué quiere mejorar personalmente", form.MejoraPersonal);
        datos = Agregar(datos, "Qué quiere conseguir con el curso", form.Objetivos);
        datos = Agregar(datos, "¿Maneja datos de clientes?", form.DatosClientes);
        datos = datos + "Minutos por día para capacitarse: " + form.TiempoXDia + "\n";
        datos = Agregar(datos, "Edad", form.RangoEdad);
        datos = Agregar(datos, "Qué tan cómodo se siente con la compu y el celular", form.ComodidadTecnologia);
        datos = Agregar(datos, "Cómo le gusta aprender", form.ComoAprende);
        datos = Agregar(datos, "Qué le gusta hacer fuera del trabajo", form.Hobbies);
        datos = Agregar(datos, "Qué le preocupa de la IA", form.PreocupacionIA);

        if (EsFloja(form.TareaConcreta) && EsFloja(form.Tareas))
        {
            datos = datos + "OJO: esta persona casi no contó de su trabajo (capaz la capacitación es obligatoria y no tiene muchas ganas). ";
            datos = datos + "Armá los ejemplos con lo que hace la gente de su rango y su puesto. No inventes datos personales (hobbies, nombres de clientes, números). ";
            datos = datos + "Lo más importante: que vea rápido que la IA le ahorra trabajo en algo que hace todos los días.\n";
        }
        return datos;
    }

    // Suma una línea con el dato solo si la respuesta sirve (si está floja, no se la mandamos a la IA)
    private static string Agregar(string datos, string etiqueta, string valor)
    {
        if (EsFloja(valor))
        {
            return datos;
        }
        return datos + etiqueta + ": " + valor + "\n";
    }

    // ¿La respuesta está vacía o no dice nada? ("nada", "no sé", "asd", "xxxxx", "-")
    public static bool EsFloja(string respuesta)
    {
        if (respuesta == null)
        {
            return true;
        }
        string texto = respuesta.Trim().ToLower();
        texto = texto.Replace(";", "").Replace(".", "").Replace(",", "").Trim();
        if (texto.Length < 4)
        {
            return true;
        }
        string[] vacias = { "nada", "no se", "no sé", "nose", "ninguna", "ninguno", "no tengo", "ni idea", "nada en especial", "no aplica", "todo", "asd", "asdf", "qwerty", "test", "prueba", "hola" };
        foreach (string vacia in vacias)
        {
            if (texto == vacia)
            {
                return true;
            }
        }
        // Cosas como "aaaaaa" o "asdasdasd": muy pocas letras distintas
        // (solo en textos largos, porque "jefe" o "caja" tienen 3 letras distintas y están bien)
        List<char> distintas = new List<char>();
        foreach (char letra in texto)
        {
            if (letra != ' ' && !distintas.Contains(letra))
            {
                distintas.Add(letra);
            }
        }
        if (distintas.Count < 2 || (texto.Length >= 6 && distintas.Count < 4))
        {
            return true;
        }
        // Sin ninguna vocal no es una palabra de verdad ("sdfgh")
        bool tieneVocal = false;
        foreach (char letra in texto)
        {
            if ("aeiouáéíóú".Contains(letra))
            {
                tieneVocal = true;
            }
        }
        return !tieneVocal;
    }

    public static string Empresa(BD bd, FormulariosEmpresa formEmpresa)
    {
        if (formEmpresa == null)
        {
            return "";
        }
        string datos = "FORMULARIO DE LA EMPRESA (cómo quiere los cursos):\n";
        datos = datos + "Dificultades del equipo: " + formEmpresa.Dificultades + "\n";
        datos = datos + "Herramientas digitales que usan: " + NombresHerramientas(bd.TraerHerramientasEmpresa(formEmpresa.IdEmpresa)) + "\n";
        datos = datos + "Objetivos de la capacitación: " + formEmpresa.Objetivos + "\n";
        return datos;
    }

    // Junta los nombres de las herramientas tildadas: "ChatGPT, Excel o Google Sheets, Gmail u Outlook"
    public static string NombresHerramientas(List<Herramientas> herramientas)
    {
        string texto = "";
        foreach (Herramientas herramienta in herramientas)
        {
            if (texto != "")
            {
                texto = texto + ", ";
            }
            texto = texto + herramienta.Nombre;
        }
        if (texto == "")
        {
            texto = "ninguna";
        }
        return texto;
    }

    // Corta el texto para que entre en la columna de la base
    public static string Recortar(string texto, int maximo)
    {
        if (texto == null)
        {
            return "";
        }
        if (texto.Length > maximo)
        {
            return texto.Substring(0, maximo);
        }
        return texto;
    }
}
