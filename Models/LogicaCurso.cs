using Fluenty.Services;

namespace Fluenty.Info360.Models;

// Sprint 2: los cursos son POR RANGO. Cada uno ve y crea cursos solo para los rangos que tiene abajo.
// El curso es uno para todo el rango, pero la IA nivela a cada persona (en qué unidad arranca).
// El prompt y los datos para la IA los arma GeneradorCursos; acá está el resto de la lógica.
public static class LogicaCurso
{
    // ============ Permisos: los rangos de abajo ============

    // Los rangos que la persona tiene abajo (los únicos para los que puede crear y ver cursos)
    public static List<Rangos> RangosDeAbajo(int idEmpresa, int idRango)
    {
        BD bd = new BD();
        // El árbol se recorre en C# (Arbol.cs): cada rango puede tener rangos abajo, y esos otros más
        return Arbol.Debajo(bd.TraerRangosEmpresa(idEmpresa), idRango);
    }

    // El rango si está abajo de la persona; si no, null (así nadie toca rangos de arriba o de otra empresa)
    public static Rangos RangoDeAbajo(int idEmpresa, int idRango, int idRangoBuscado)
    {
        foreach (Rangos rango in RangosDeAbajo(idEmpresa, idRango))
        {
            if (rango.IdRango == idRangoBuscado)
            {
                return rango;
            }
        }
        return null;
    }

    // ============ Pantalla Cursos: cada rango de abajo con su gente y su curso ============

    public static List<GrupoRango> ArmarGrupos(int idEmpresa, int idRango)
    {
        BD bd = new BD();
        List<GrupoRango> grupos = new List<GrupoRango>();
        foreach (Rangos rango in RangosDeAbajo(idEmpresa, idRango))
        {
            GrupoRango grupo = new GrupoRango();
            grupo.Rango = rango;
            grupo.Curso = bd.TraerCursoPorRango(rango.IdRango);
            // La gente de este rango (el Stored Procedure ya filtra por el rango)
            grupo.Miembros = bd.TraerMiembrosRangos(rango.IdRango.ToString());
            foreach (EmpleadoCurso miembro in grupo.Miembros)
            {
                miembro.TextoActividad = miembro.Actividad();
                miembro.SinAsignar = false;
                if (miembro.TieneFormulario)
                {
                    grupo.ConFormulario++;
                    // Completó el formulario pero todavía no tiene el curso del rango: se lo puede sumar
                    if (grupo.Curso != null && miembro.IdCurso != grupo.Curso.IdCurso)
                    {
                        miembro.SinAsignar = true;
                    }
                }
            }
            grupos.Add(grupo);
        }
        return grupos;
    }

    // ============ Formulario de la empresa (lo completa el dueño una sola vez) ============

    // Devuelve el error para mostrar, o "" si se guardó
    public static string GuardarFormularioEmpresa(int idEmpresa, string dificultades, string objetivos, int[] herramientas)
    {
        if (Funciones.EstaVacio(dificultades) || Funciones.EstaVacio(objetivos))
        {
            return "Contanos las dificultades y los objetivos de la empresa.";
        }
        if (Funciones.Limpiar(dificultades).Length > 500 || Funciones.Limpiar(objetivos).Length > 500)
        {
            return "Cada respuesta puede tener hasta 500 letras.";
        }
        BD bd = new BD();
        // Si ya lo completó, no se guarda de nuevo
        if (bd.TraerFormularioEmpresa(idEmpresa) != null)
        {
            return "";
        }
        FormulariosEmpresa formulario = new FormulariosEmpresa();
        formulario.IdEmpresa = idEmpresa;
        formulario.Dificultades = Funciones.Limpiar(dificultades);
        formulario.Objetivos = Funciones.Limpiar(objetivos);
        bd.GuardarFormularioEmpresa(formulario);
        if (herramientas != null)
        {
            foreach (int idHerramienta in herramientas)
            {
                bd.AgregarHerramientaEmpresa(idEmpresa, idHerramienta);
            }
        }
        return "";
    }

    // ============ La IA genera UN curso para todo el rango (queda como borrador) ============
    // publico = true: además aparece en el buscador para todas las empresas. En ese caso la IA arma
    // el temario solo con el tema y la categoría, SIN datos de la empresa ni de las personas.
    // Devuelve Ok = true y en Id el curso (nuevo o el que el rango ya tenía), o Ok = false y el error.
    public static async Task<Resultado> Generar(int idEmpresa, int idRango, int idRangoCurso, bool publico, string tema, string categoria, GroqService groqService, bool iaPrendida)
    {
        Rangos rango = RangoDeAbajo(idEmpresa, idRango, idRangoCurso);
        if (rango == null)
        {
            return new Resultado(false, "Ese rango no está abajo del tuyo.");
        }
        BD bd = new BD();
        // Si el rango ya tiene curso, no se genera otro
        Cursos existente = bd.TraerCursoPorRango(idRangoCurso);
        if (existente != null)
        {
            Resultado yaTiene = new Resultado(true, "");
            yaTiene.Id = existente.IdCurso;
            return yaTiene;
        }
        if (!iaPrendida)
        {
            return new Resultado(false, "La IA está deshabilitada en este momento.");
        }

        // 1) La gente del rango que completó el formulario
        List<Usuarios> personas = bd.TraerPersonasConFormulario(idRangoCurso);
        List<FormulariosEmpleado> formularios = new List<FormulariosEmpleado>();
        foreach (Usuarios persona in personas)
        {
            formularios.Add(bd.TraerFormularioEmpleado(persona.IdUsuario));
        }
        if (personas.Count == 0)
        {
            return new Resultado(false, "Nadie del rango " + rango.Nombre + " completó el formulario todavía.");
        }
        tema = Funciones.Limpiar(tema);
        if (publico && (tema.Length < 5 || !Funciones.EstaEnArray(Cursos.Categorias, categoria)))
        {
            return new Resultado(false, "Para un curso público contá de qué trata (el tema) y elegí una categoría.");
        }

        // El prompt y los datos los arma GeneradorCursos
        string instrucciones = GeneradorCursos.InstruccionesCurso(publico);
        string datos;
        if (publico)
        {
            datos = GeneradorCursos.DatosPublico(tema, categoria, personas, formularios);
        }
        else
        {
            datos = GeneradorCursos.DatosGrupo(bd, rango, personas, formularios, bd.TraerFormularioEmpresa(idEmpresa));
        }

        // 2) Le pedimos el curso a la IA (si viene mal, probamos una vez más)
        CursoIA cursoIA = null;
        try
        {
            int intento = 1;
            while (intento <= 2 && cursoIA == null)
            {
                string respuesta = await groqService.PreguntarAsync(instrucciones, datos, true);
                cursoIA = GeneradorCursos.LeerCurso(respuesta);
                intento++;
            }
        }
        catch (Exception ex)
        {
            return new Resultado(false, ex.Message);
        }
        if (cursoIA == null)
        {
            return new Resultado(false, "La IA respondió con un formato incorrecto. Probá de nuevo.");
        }

        // 3) Guardamos el contenido: el curso del rango, sus 4 unidades y 20 lecciones
        int idCurso = GuardarCurso(cursoIA, formularios, publico, categoria);

        // 4) Se lo asignamos a cada persona, con SU nivelación (si la IA no la mandó bien, se usa su nivel de IA)
        List<Lecciones> lecciones = bd.TraerLeccionesCurso(idCurso);
        for (int i = 0; i < personas.Count; i++)
        {
            int unidadInicial = DatosIA.UnidadPorNivel(formularios[i].NivelIA);
            string motivo = "Arranca según su experiencia con la IA.";
            foreach (NivelacionIA nivelacion in cursoIA.Nivelacion)
            {
                if (nivelacion.IdUsuario == personas[i].IdUsuario && nivelacion.UnidadInicial >= 1 && nivelacion.UnidadInicial <= 4)
                {
                    unidadInicial = nivelacion.UnidadInicial;
                    motivo = nivelacion.Motivo;
                }
            }
            Asignar(idCurso, lecciones, personas[i].IdUsuario, unidadInicial, motivo);
        }

        // 5) Quien lo generó lo revisa antes de confirmarlo
        Resultado resultado = new Resultado(true, "");
        resultado.Id = idCurso;
        return resultado;
    }

    // Guarda el curso que armó la IA con sus unidades y lecciones. Devuelve el IdCurso.
    private static int GuardarCurso(CursoIA cursoIA, List<FormulariosEmpleado> formularios, bool publico, string categoria)
    {
        BD bd = new BD();
        string objetivos = "";
        foreach (string objetivo in cursoIA.Objetivos)
        {
            objetivos = objetivos + objetivo + "\n";
        }
        // El nivel del curso es el del que más sabe del grupo
        int nivelGrupo = 1;
        foreach (FormulariosEmpleado form in formularios)
        {
            if (form.NivelIA > nivelGrupo)
            {
                nivelGrupo = form.NivelIA;
            }
        }
        // El curso no guarda su rango ni su empresa: se saben por las personas a las que se les asigna
        Cursos curso = new Cursos();
        curso.Titulo = DatosIA.Recortar(cursoIA.Titulo, 100);
        curso.Descripcion = DatosIA.Recortar(cursoIA.Descripcion, 300);
        curso.Modelo = DatosIA.Recortar(cursoIA.Modelo, 50);
        curso.Nivel = nivelGrupo;
        curso.Objetivos = DatosIA.Recortar(objetivos, 600);
        curso.Publico = publico;
        if (publico)
        {
            curso.Categoria = categoria;
        }
        else
        {
            curso.Categoria = DatosIA.Recortar(cursoIA.Categoria, 50);
        }
        curso.DuracionHoras = 5;
        if (cursoIA.DuracionHoras > 0)
        {
            curso.DuracionHoras = cursoIA.DuracionHoras;
        }
        curso.Herramientas = DatosIA.Recortar(Funciones.Unir(cursoIA.Herramientas, " · "), 200);
        int idCurso = bd.CrearCurso(curso);

        foreach (UnidadIA unidadIA in cursoIA.Unidades)
        {
            Unidades unidad = new Unidades();
            unidad.IdCurso = idCurso;
            unidad.Numero = unidadIA.Numero;
            unidad.Titulo = DatosIA.Recortar(unidadIA.Titulo, 100);
            unidad.Objetivo = DatosIA.Recortar(unidadIA.Objetivo, 300);
            int idUnidad = bd.AgregarUnidad(unidad);

            foreach (LeccionIA leccionIA in unidadIA.Lecciones)
            {
                Lecciones leccion = new Lecciones();
                leccion.IdUnidad = idUnidad;
                leccion.Numero = leccionIA.Numero;
                leccion.Titulo = DatosIA.Recortar(leccionIA.Titulo, 100);
                leccion.Objetivo = DatosIA.Recortar(leccionIA.Objetivo, 300);
                bd.AgregarLeccion(leccion);
            }
        }
        return idCurso;
    }

    // ============ Sumar al curso del rango a alguien que se unió después ============
    // Devuelve el error para mostrar, o "" si quedó sumado (o ya lo estaba)
    public static async Task<string> Sumar(int idEmpresa, int idRango, int idCurso, int idUsuario, GroqService groqService, bool iaPrendida)
    {
        BD bd = new BD();
        Cursos curso = bd.TraerCurso(idCurso);
        if (curso == null || RangoDeAbajo(idEmpresa, idRango, curso.IdRango) == null)
        {
            return "Ese curso no es de un rango tuyo.";
        }
        Usuarios persona = bd.TraerEmpleado(idUsuario);
        FormulariosEmpleado form = bd.TraerFormularioEmpleado(idUsuario);
        if (persona == null || form == null || persona.IdRango != curso.IdRango)
        {
            return "Esa persona no es del rango o no completó el formulario.";
        }
        if (bd.EstaInscripto(idCurso, idUsuario))
        {
            return "";
        }

        // La IA decide en qué unidad arranca esta persona (si falla, se usa su nivel de IA)
        int unidadInicial = DatosIA.UnidadPorNivel(form.NivelIA);
        string motivo = "Arranca según su experiencia con la IA.";
        if (iaPrendida)
        {
            try
            {
                string instrucciones = GeneradorCursos.InstruccionesNivelacion();
                string datos = GeneradorCursos.DatosNivelacion(bd, curso, bd.TraerUnidadesCurso(idCurso), persona, form);
                string respuesta = await groqService.PreguntarAsync(instrucciones, datos, true);
                NivelacionIA nivelacion = GeneradorCursos.LeerNivelacion(respuesta);
                if (nivelacion != null)
                {
                    unidadInicial = nivelacion.UnidadInicial;
                    motivo = nivelacion.Motivo;
                }
            }
            catch (Exception)
            {
                // Si la IA falla, queda la nivelación por su nivel de IA
            }
        }
        Asignar(idCurso, bd.TraerLeccionesCurso(idCurso), idUsuario, unidadInicial, motivo);

        // Si el curso ya está confirmado, se le deja preparada su primera lección
        if (curso.Estado == "Confirmado" && iaPrendida)
        {
            GeneradorLecciones.PrepararEnSegundoPlano(groqService, idUsuario, GeneradorLecciones.Siguientes(bd, idCurso, idUsuario, 0, 1));
        }
        return "";
    }

    // Le asigna el curso a una persona: Inscripciones con su nivelación y el estado de cada lección
    private static void Asignar(int idCurso, List<Lecciones> lecciones, int idUsuario, int unidadInicial, string motivo)
    {
        BD bd = new BD();
        Inscripciones asignacion = new Inscripciones();
        asignacion.IdCurso = idCurso;
        asignacion.IdUsuario = idUsuario;
        asignacion.UnidadInicial = unidadInicial;
        asignacion.MotivoNivel = DatosIA.Recortar(motivo, 300);
        asignacion.Origen = "Equipo";
        int idInscripcion = bd.AsignarCurso(asignacion);
        foreach (Lecciones leccion in lecciones)
        {
            bd.AgregarLeccionUsuario(leccion.IdLeccion, idInscripcion, Lecciones.EstadoInicial(leccion.NumeroUnidad, leccion.Numero, unidadInicial));
        }
        bd.ActualizarProgreso(idCurso, idUsuario);
    }

    // ============ Ver un curso ============
    // - Como alumno: su propio curso (confirmado), con las lecciones para hacer.
    // - Como responsable: el curso de un rango de abajo, mirando el camino de cada persona.
    // Devuelve null si no lo puede ver.
    public static CaminoCurso ArmarCamino(int idCurso, int idUsuarioElegido, int idUsuario, int idEmpresa, int idRango)
    {
        BD bd = new BD();
        Cursos curso = bd.TraerCurso(idCurso);
        if (curso == null)
        {
            return null;
        }
        CaminoCurso camino = new CaminoCurso();
        camino.Curso = curso;
        Inscripciones propia = bd.TraerInscripcion(idCurso, idUsuario);
        bool esResponsable = RangoDeAbajo(idEmpresa, idRango, curso.IdRango) != null;

        if (idUsuarioElegido == 0 && propia != null && curso.Estado == "Confirmado")
        {
            camino.Asignacion = propia;
            camino.ModoAlumno = true;
        }
        else if (esResponsable)
        {
            // Las personas del equipo que tienen el curso (sin las de otras empresas que se sumaron desde el buscador)
            camino.Personas = bd.TraerPersonasCurso(idCurso);
            if (idUsuarioElegido == 0 && camino.Personas.Count > 0)
            {
                idUsuarioElegido = camino.Personas[0].IdUsuario;
            }
            foreach (Usuarios persona in camino.Personas)
            {
                persona.Elegida = persona.IdUsuario == idUsuarioElegido;
                if (persona.Elegida)
                {
                    camino.Asignacion = bd.TraerInscripcion(idCurso, idUsuarioElegido);
                }
            }
        }
        else
        {
            return null;
        }
        if (camino.Personas == null)
        {
            camino.Personas = new List<Usuarios>();
        }

        camino.EsInscripcionPublica = camino.ModoAlumno && camino.Asignacion.Origen == "Publico";
        camino.Subtitulo = ArmarSubtitulo(curso, camino.EsInscripcionPublica);
        camino.UnidadInicial = 1;
        camino.NombreEmpleado = "";
        List<Lecciones> lecciones = new List<Lecciones>();
        if (camino.Asignacion != null)
        {
            Usuarios persona = bd.TraerEmpleado(camino.Asignacion.IdUsuario);
            camino.NombreEmpleado = persona.Nombre + " " + persona.Apellido;
            camino.XP = persona.XP;
            camino.Racha = persona.Racha;
            camino.UnidadInicial = camino.Asignacion.UnidadInicial;
            lecciones = bd.TraerLeccionesUsuario(idCurso, camino.Asignacion.IdUsuario);
            if (camino.ModoAlumno)
            {
                camino.TextoArranque = "Arrancás en la Unidad " + camino.UnidadInicial;
            }
            else
            {
                camino.TextoArranque = camino.NombreEmpleado + " arranca en la Unidad " + camino.UnidadInicial;
            }
        }
        camino.Unidades = ArmarUnidades(bd.TraerUnidadesCurso(idCurso), lecciones, camino.UnidadInicial, camino.ModoAlumno);
        return camino;
    }

    // "Modelo · Rango Vendedor · 🌎 Público · Categoría"
    private static string ArmarSubtitulo(Cursos curso, bool esInscripcionPublica)
    {
        string subtitulo = curso.Modelo;
        if (esInscripcionPublica)
        {
            // Se sumó desde el buscador: el curso no es de su rango
            subtitulo = subtitulo + " · 🌎 Curso público";
            if (curso.Categoria != "")
            {
                subtitulo = subtitulo + " · " + curso.Categoria;
            }
            return subtitulo;
        }
        BD bd = new BD();
        Rangos rango = bd.TraerRango(curso.IdRango);
        if (rango != null)
        {
            subtitulo = subtitulo + " · Rango " + rango.Nombre;
        }
        if (curso.Publico)
        {
            subtitulo = subtitulo + " · 🌎 Público · " + curso.Categoria;
        }
        return subtitulo;
    }

    // Cada unidad con su color, su etiqueta y sus lecciones (con el ícono de cada una)
    private static List<Unidades> ArmarUnidades(List<Unidades> unidades, List<Lecciones> lecciones, int unidadInicial, bool modoAlumno)
    {
        foreach (Unidades unidad in unidades)
        {
            unidad.Clase = "unidad unidad-color-" + unidad.Numero;
            unidad.Etiqueta = "";
            if (unidad.Numero < unidadInicial)
            {
                unidad.Clase = "unidad unidad-sabida";
                unidad.Etiqueta = "Ya lo sabe";
            }
            if (unidad.Numero == unidadInicial)
            {
                unidad.Etiqueta = "Arranca acá";
            }
            unidad.Lecciones = new List<Lecciones>();
            foreach (Lecciones leccion in lecciones)
            {
                if (leccion.NumeroUnidad == unidad.Numero)
                {
                    leccion.Icono = "🔒";
                    if (leccion.Estado == "Disponible")
                    {
                        leccion.Icono = "▶";
                    }
                    if (leccion.Estado == "Completada" || leccion.Estado == "YaSabe")
                    {
                        leccion.Icono = "✓";
                    }
                    // La lección 5 es el examen
                    if (leccion.Numero == 5 && leccion.Estado != "Completada")
                    {
                        leccion.Icono = "🏆";
                    }
                    leccion.SePuedeHacer = modoAlumno && (leccion.Estado == "Disponible" || leccion.Estado == "Completada");
                    unidad.Lecciones.Add(leccion);
                }
            }
        }
        return unidades;
    }

    // ============ Rehacer UNA unidad del borrador con un pedido ("que sea más difícil") ============
    // Devuelve el error para mostrar, o "" si se rehízo
    public static async Task<string> RehacerUnidad(int idEmpresa, int idRango, int idCurso, int numeroUnidad, string pedido, GroqService groqService, bool iaPrendida)
    {
        BD bd = new BD();
        Cursos curso = bd.TraerCurso(idCurso);
        if (curso == null)
        {
            return "Ese curso no existe.";
        }
        Rangos rango = RangoDeAbajo(idEmpresa, idRango, curso.IdRango);
        if (rango == null)
        {
            return "Ese curso no es de un rango tuyo.";
        }
        if (curso.Estado != "Borrador")
        {
            return "Este curso ya está confirmado: solo se pueden cambiar unidades de un borrador.";
        }
        if (Funciones.EstaVacio(pedido))
        {
            return "Escribí qué querés cambiar de la unidad " + numeroUnidad + ".";
        }
        if (Funciones.Limpiar(pedido).Length > 300)
        {
            return "El pedido puede tener hasta 300 letras.";
        }
        if (!iaPrendida)
        {
            return "La IA está deshabilitada en este momento.";
        }

        List<Inscripciones> asignaciones = bd.TraerAsignacionesCurso(idCurso);
        List<Unidades> unidades = bd.TraerUnidadesCurso(idCurso);
        List<Lecciones> lecciones = bd.TraerLeccionesCurso(idCurso);
        Unidades unidadVieja = null;
        foreach (Unidades unidad in unidades)
        {
            if (unidad.Numero == numeroUnidad)
            {
                unidadVieja = unidad;
            }
        }
        if (unidadVieja == null)
        {
            return "Esa unidad no existe.";
        }

        // Los datos del grupo: la gente que tiene el curso
        List<Usuarios> personas = new List<Usuarios>();
        List<FormulariosEmpleado> formularios = new List<FormulariosEmpleado>();
        foreach (Inscripciones item in asignaciones)
        {
            FormulariosEmpleado form = bd.TraerFormularioEmpleado(item.IdUsuario);
            if (form != null)
            {
                personas.Add(bd.TraerEmpleado(item.IdUsuario));
                formularios.Add(form);
            }
        }
        string instrucciones = GeneradorCursos.InstruccionesUnidad(numeroUnidad, curso.Publico);
        string datosBase;
        if (curso.Publico)
        {
            // Curso público: la unidad se rehace sin datos de la empresa ni de las personas
            datosBase = "CURSO PÚBLICO de la categoría " + curso.Categoria + ".\n";
        }
        else
        {
            datosBase = GeneradorCursos.DatosGrupo(bd, rango, personas, formularios, bd.TraerFormularioEmpresa(curso.IdEmpresa));
        }
        string datos = GeneradorCursos.DatosRehacerUnidad(datosBase, curso, unidades, lecciones, numeroUnidad, Funciones.Limpiar(pedido));

        UnidadIA unidadIA = null;
        try
        {
            int intento = 1;
            while (intento <= 2 && unidadIA == null)
            {
                string respuesta = await groqService.PreguntarAsync(instrucciones, datos, true);
                unidadIA = GeneradorCursos.LeerUnidad(respuesta);
                intento++;
            }
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        if (unidadIA == null)
        {
            return "La IA respondió con un formato incorrecto. Probá de nuevo.";
        }

        // Reemplazamos la unidad y sus 5 lecciones; al borrar las viejas se borra solo su estado (ON DELETE CASCADE)
        unidadVieja.Titulo = DatosIA.Recortar(unidadIA.Titulo, 100);
        unidadVieja.Objetivo = DatosIA.Recortar(unidadIA.Objetivo, 300);
        bd.ActualizarUnidad(unidadVieja);
        bd.BorrarLeccionesUnidad(unidadVieja.IdUnidad);

        int numero = 1;
        foreach (LeccionIA leccionIA in unidadIA.Lecciones)
        {
            Lecciones leccion = new Lecciones();
            leccion.IdUnidad = unidadVieja.IdUnidad;
            leccion.Numero = numero;
            leccion.Titulo = DatosIA.Recortar(leccionIA.Titulo, 100);
            leccion.Objetivo = DatosIA.Recortar(leccionIA.Objetivo, 300);
            int idLeccion = bd.AgregarLeccion(leccion);
            foreach (Inscripciones item in asignaciones)
            {
                bd.AgregarLeccionUsuario(idLeccion, item.IdInscripcion, Lecciones.EstadoInicial(numeroUnidad, numero, item.UnidadInicial));
            }
            numero++;
        }
        return "";
    }

    // ============ Confirmar: el curso le llega a todo el rango ============
    // Devuelve el curso confirmado, o null si no es de un rango de abajo
    public static Cursos Confirmar(int idEmpresa, int idRango, int idCurso, GroqService groqService, bool iaPrendida)
    {
        BD bd = new BD();
        Cursos curso = bd.TraerCurso(idCurso);
        if (curso == null || RangoDeAbajo(idEmpresa, idRango, curso.IdRango) == null)
        {
            return null;
        }
        bd.ConfirmarCurso(idCurso);

        // Se le deja preparada a cada persona su primera lección, así no espera cuando la abre.
        // Es una sola por persona para no gastar créditos en lecciones que capaz nadie abre;
        // mientras hace una, se preparan las 2 siguientes.
        if (iaPrendida)
        {
            foreach (Inscripciones asignacion in bd.TraerAsignacionesCurso(idCurso))
            {
                GeneradorLecciones.PrepararEnSegundoPlano(groqService, asignacion.IdUsuario, GeneradorLecciones.Siguientes(bd, idCurso, asignacion.IdUsuario, 0, 1));
            }
        }
        return curso;
    }

    // ============ Descartar el borrador para generarlo de nuevo ============
    public static void Descartar(int idEmpresa, int idRango, int idCurso)
    {
        BD bd = new BD();
        Cursos curso = bd.TraerCurso(idCurso);
        if (curso != null && curso.Estado == "Borrador" && RangoDeAbajo(idEmpresa, idRango, curso.IdRango) != null)
        {
            bd.BorrarCurso(idCurso);
        }
    }
}
