# Pedido a la IA para generar cursos (Fluenty)

## 1. Qué datos le mandamos

Cuando el empleador toca **"Generar curso"** en un empleado que se sumó con el link, el `CursoController` junta los **dos formularios** (preguntas del doc "Logica de los Cursos"):

**Formulario del empleado** (lo completa él al unirse con el link):

| Pregunta | Dónde se guarda |
|---|---|
| Puesto | `Usuarios.Puesto` |
| Nivel de IA (1 principiante, 2 intermedio, 3 avanzado) | `FormulariosEmpleado.NivelIA` |
| Herramientas que usa (casillas del catálogo) | `HerramientasXUsuario` → `Herramientas.Nombre` |
| 1. Principal dificultad en su trabajo | `FormulariosEmpleado.DificultadPrincipal` |
| 2. Qué le gustaría que la empresa mejore en su puesto | `FormulariosEmpleado.MejoraEmpresa` |
| 3. Fortalezas y debilidades | `FormulariosEmpleado.FortalezasDebilidades` |
| 4. Qué quiere mejorar personalmente | `FormulariosEmpleado.MejoraPersonal` |
| 5. Las 3 tareas repetitivas | `FormulariosEmpleado.Tareas` |
| 6. Errores o dificultades en esas tareas | `FormulariosEmpleado.Dificultades` |
| 7. Qué quiere conseguir con el curso | `FormulariosEmpleado.Objetivos` |
| Minutos por día para capacitarse | `FormulariosEmpleado.TiempoXDia` |

**Formulario de la empresa** (lo completa el empleador la primera vez que entra a Cursos):

| Pregunta | Dónde se guarda |
|---|---|
| 1. Dificultades del equipo | `FormulariosEmpresa.Dificultades` |
| 2. Herramientas digitales (¿usan IA?) (casillas del catálogo) | `HerramientasXEmpresa` → `Herramientas.Nombre` |
| 3. Objetivos de la capacitación | `FormulariosEmpresa.Objetivos` |

## 2. Cómo se lo mandamos (solicitud HTTP)

`POST https://api.groq.com/openai/v1/chat/completions`
Header: `Authorization: Bearer <API key guardada en User Secrets>`

```json
{
  "model": "openai/gpt-oss-120b",
  "temperature": 0.4,
  "max_tokens": 8000,
  "response_format": { "type": "json_object" },
  "messages": [
    { "role": "system", "content": "<INSTRUCCIONES FIJAS (punto 3)>" },
    { "role": "user",   "content": "<LOS DOS FORMULARIOS (punto 1)>" }
  ]
}
```

- `response_format: json_object` obliga a la IA a devolver un JSON válido.
- `temperature` baja (0.4) para que respete el formato y no invente de más.

## 3. Instrucciones fijas (el prompt)

El curso es **estilo Duolingo**: un camino de **4 unidades × 5 lecciones** (20 lecciones, una por día, unas 4 semanas). En este pedido la IA arma **solo el plan** (títulos y objetivos); el contenido de cada lección se genera más adelante, cuando el empleado la abre, para que cada pedido sea chico y completo.

> Sos un diseñador de cursos de Fluenty, una app que enseña a usar IA en el trabajo con el estilo de Duolingo.
>
> **Tu tarea:** armar el PLAN de un curso de 4 semanas: 4 unidades de 5 lecciones.
>
> **Unidades:** 1) fundamentos de IA adaptados a su puesto · 2) su primera tarea repetitiva · 3) su segunda y tercera tarea · 4) un proyecto real de su puesto con los objetivos de la empresa. La lección 5 de cada unidad es un **"Desafío:"** que repasa la unidad. Las lecciones van de simple a difícil, no se repiten y enseñan una cosa concreta usando sus tareas y herramientas reales.
>
> **Nivelación:** decidí en qué unidad arranca (1 a 4) según lo que ya sabe de IA y explicá por qué.
>
> **Modelo de curso:** elegí uno de los 7 del doc "Logica de los Cursos".
>
> **Reglas:** español rioplatense, claro y motivador; títulos cortos; objetivos que empiecen con un verbo; nunca pedir datos privados de clientes. Respondé SOLO con el JSON del punto 4.

## 4. Formato de respuesta que esperamos

```json
{
  "modelo": "Tareas de escritorio",
  "titulo": "Mails y reportes con IA en 4 semanas",
  "descripcion": "Un camino diario para que la IA te ahorre horas en mails y reportes.",
  "objetivos": ["Responder mails en la mitad de tiempo", "Armar el reporte semanal con IA", "Revisar textos sin errores"],
  "unidadInicial": 2,
  "motivoNivel": "Ya usa ChatGPT para cosas simples, así que arranca aplicándolo a sus tareas.",
  "unidades": [
    { "numero": 1, "titulo": "Primeros pasos con IA", "objetivo": "...",
      "lecciones": [
        { "numero": 1, "titulo": "Qué es y qué no es la IA", "objetivo": "Entender..." },
        { "numero": 2, "titulo": "...", "objetivo": "..." },
        { "numero": 3, "titulo": "...", "objetivo": "..." },
        { "numero": 4, "titulo": "...", "objetivo": "..." },
        { "numero": 5, "titulo": "Desafío: tu primer pedido perfecto", "objetivo": "..." }
      ] },
    { "numero": 2, "...": "..." },
    { "numero": 3, "...": "..." },
    { "numero": 4, "...": "..." }
  ]
}
```

## 5. Cómo lo guardamos en la base

| JSON | Tabla / columna |
|---|---|
| `modelo`, `titulo`, `descripcion`, `objetivos` | `Cursos` (el contenido, de la empresa; Estado = "Borrador") |
| `unidadInicial`, `motivoNivel` + el empleado elegido | `CursoXUsuario` (a quién se le asigna el curso, su progreso y su nivelación) |
| cada elemento de `unidades` | `Unidades` (IdCurso, Numero, Titulo, Objetivo) |
| cada elemento de `lecciones` | `Lecciones` (IdUnidad, Numero, Titulo, Objetivo) |
| el estado de cada lección para el empleado | `LeccionXUsuario` (IdLeccion, IdUsuario, Estado, FechaCompletado) |

El **contenido** (Cursos, Unidades, Lecciones) está separado del **avance de cada persona** (CursoXUsuario, LeccionXUsuario). Así el mismo curso se puede asignar a varias personas, cada una con su nivelación y su progreso.

**Estado de cada lección según la nivelación** (en `LeccionXUsuario`): las unidades anteriores a `unidadInicial` quedan **YaSabe**; la lección 1 de la unidad inicial queda **Disponible**; el resto **Bloqueada**. Cuando el empleado termine una lección, se desbloquea la siguiente.

Antes de guardar, el controller **valida**: título, objetivos, `unidadInicial` entre 1 y 4, y exactamente 4 unidades de 5 lecciones. Si la respuesta viene mal, le pide a la IA una vez más. Los textos se recortan al largo máximo de cada columna.

El curso queda como **Borrador**: el empleador revisa el camino y toca **Confirmar** (Estado = "Confirmado"). Recién ahí el empleado pasa a la lista de Empleados y ve el curso en su panel. Si no le gusta todo, lo **descarta** y lo genera de nuevo; si solo quiere cambiar una parte, **rehace una unidad** (ver punto 6).

**La IA nunca escribe en la base:** solo devuelve texto. Lo guarda nuestro código con stored procedures.

## 6. Rehacer una sola unidad (con un pedido del empleador)

Mientras el curso es **Borrador**, debajo de cada unidad hay un cuadro de texto para pedirle un cambio a la IA, por ejemplo *"que sea más difícil"* o *"con más ejemplos de Excel"*. Así no hace falta rehacer todo el curso.

**Qué le mandamos:** los mismos datos del punto 1, más el plan actual completo (las 4 unidades con sus lecciones, para que no repita temas), el número de la unidad a rehacer y el pedido del empleador (máximo 300 caracteres).

**Instrucciones fijas:** las mismas reglas del curso (lección 5 = "Desafío:", de lo simple a lo difícil, español rioplatense, títulos cortos), pero solo para esa unidad. Las otras unidades no se tocan. Si el pedido no tiene que ver con el curso, la IA lo ignora y mejora la unidad igual. Está en `ArmarInstruccionesUnidad()` en `CursoController`.

**Formato de respuesta:**

```json
{
  "numero": 2,
  "titulo": "título de la unidad",
  "objetivo": "una oración",
  "lecciones": [
    { "numero": 1, "titulo": "...", "objetivo": "..." },
    { "numero": 2, "titulo": "...", "objetivo": "..." },
    { "numero": 3, "titulo": "...", "objetivo": "..." },
    { "numero": 4, "titulo": "...", "objetivo": "..." },
    { "numero": 5, "titulo": "Desafío: ...", "objetivo": "..." }
  ]
}
```

**Cómo se guarda:** se valida que traiga título y 5 lecciones (si no, se pide una vez más). Después se actualiza el título y objetivo de la unidad (`sp_ActualizarUnidad`), se borran sus 5 lecciones viejas (`sp_BorrarLeccionesUnidad`) (con eso se borra solo su estado en `LeccionXUsuario`, por el `ON DELETE CASCADE`) y se cargan las nuevas con el estado de nivelación de cada persona que tiene el curso. El resto del curso queda igual.
