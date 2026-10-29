# Pedido a la IA para generar cursos (Fluenty)

## 1. Qué datos le mandamos

Cuando el empleador toca **"Crear curso"** en un empleado, el `CursoController` junta:

| Dato | De dónde sale |
|---|---|
| Puesto | `Empleados.Puesto` |
| Nivel de IA (1 principiante, 2 intermedio, 3 avanzado) | `FormulariosEmpleado.NivelIA` |
| Las 3 tareas repetitivas | `FormulariosEmpleado.Tareas` |
| Herramientas que usa | `FormulariosEmpleado.Herramientas` |
| Dificultades / errores frecuentes | `FormulariosEmpleado.Dificultades` |
| Qué quiere lograr | `FormulariosEmpleado.Objetivos` |
| Minutos por día para capacitarse | `FormulariosEmpleado.TiempoXDia` |
| Lo que dijo el empleador del empleado | `Invitaciones.InfoEmpleador` |
| Dificultades, herramientas y objetivos de la empresa (si los cargó) | `FormulariosEmpleador` |

## 2. Cómo se lo mandamos (solicitud HTTP)

`POST https://api.groq.com/openai/v1/chat/completions`
Header: `Authorization: Bearer <API key guardada en User Secrets>`

```json
{
  "model": "llama-3.3-70b-versatile",
  "temperature": 0.4,
  "max_tokens": 6000,
  "response_format": { "type": "json_object" },
  "messages": [
    { "role": "system", "content": "<INSTRUCCIONES FIJAS (punto 3)>" },
    { "role": "user",   "content": "<DATOS DEL EMPLEADO Y LA EMPRESA (punto 1)>" }
  ]
}
```

- `response_format: json_object` obliga a la IA a devolver un JSON válido.
- `temperature` baja (0.4) para que respete el formato y no invente de más.

## 3. Instrucciones fijas (el prompt)

> Sos un diseñador de cursos cortos de Fluenty. Tu trabajo es enseñarle a un empleado a usar inteligencia artificial en SU trabajo real.
>
> **Cómo pensar el curso:**
> 1. Leé las 3 tareas repetitivas del empleado. Para cada una pensá cómo la IA le ahorra tiempo o le evita los errores que contó.
> 2. Usá las herramientas que ya usa (Excel, Gmail, WhatsApp, etc.) y apuntá a los objetivos del empleado y de la empresa.
> 3. Dale ejemplos de pedidos (prompts) concretos que pueda copiar y usar en ChatGPT, Gemini o la IA que use la empresa.
> 4. Adaptá la dificultad a su nivel de IA y el largo al tiempo por día que tiene.
>
> **Elegí uno de estos 7 modelos de curso** según el puesto: Tareas de escritorio · Oficio y campo · Ventas y atención al cliente · Gestión y liderazgo · Creativo y marketing · Técnico y datos · Primer contacto (si es principiante y casi no usa tecnología).
>
> **El curso tiene 5 partes:**
> 1. Explicación simple y ordenada, con sus tareas reales.
> 2. 3 preguntas de opción múltiple (A, B, C) sobre la parte 1.
> 3. Cómo optimizar sus tareas con IA, con ejemplos de prompts.
> 4. 3 preguntas de opción múltiple (A, B, C) sobre la parte 3.
> 5. Un ejercicio de práctica corto con una situación real de su puesto.
>
> **Reglas:** español rioplatense, claro y sin relleno. Nunca pidas ni uses datos privados de clientes. Cada texto de parte, como máximo 2500 caracteres. Respondé SOLO con el JSON del formato pedido, sin texto antes ni después.

## 4. Formato de respuesta que esperamos

```json
{
  "modelo": "Tareas de escritorio",
  "titulo": "Mails y reportes en la mitad de tiempo",
  "descripcion": "Cómo usar ChatGPT para responder mails y armar el reporte semanal.",
  "partes": [
    { "numero": 1, "titulo": "Lo que tenés que saber", "contenido": "texto...", "preguntas": [] },
    { "numero": 2, "titulo": "Comprobá lo que aprendiste", "contenido": "",
      "preguntas": [
        { "texto": "¿...?", "opcionA": "...", "opcionB": "...", "opcionC": "...", "correcta": "B" }
      ] },
    { "numero": 3, "titulo": "Cómo optimizar tus tareas", "contenido": "texto...", "preguntas": [] },
    { "numero": 4, "titulo": "Comprobá otra vez", "contenido": "", "preguntas": [ "...3 preguntas..." ] },
    { "numero": 5, "titulo": "Ejercicio de práctica", "contenido": "consigna...", "preguntas": [] }
  ]
}
```

## 5. Cómo lo guardamos en la base

| JSON | Tabla / columna |
|---|---|
| `modelo`, `titulo`, `descripcion` + el empleado elegido | `Cursos` (Modelo, Titulo, Descripcion, **IdEmpleado** → así queda asignado) |
| cada elemento de `partes` | `PartesCurso` (IdCurso, Numero, Titulo, Contenido) |
| cada elemento de `preguntas` | `Preguntas` (IdCurso, NumeroParte, Texto, OpcionA, OpcionB, OpcionC, Correcta) |

Antes de guardar, el controller **valida**: que haya título, 5 partes y preguntas en las partes 2 y 4 con correcta A, B o C. Si la respuesta viene mal, le pide a la IA una vez más. Los textos se recortan al largo máximo de cada columna.

**La IA nunca escribe en la base:** solo devuelve texto. Lo guarda nuestro código con stored procedures.
