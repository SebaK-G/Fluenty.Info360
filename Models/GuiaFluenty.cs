namespace Fluenty.Info360.Models;

// Sprint 3 · El manual de Fluenty para el asistente de IA.
// Se le manda a la IA en cada pregunta para que pueda guiar a la persona dentro de la app
// (cómo armar el equipo, crear un curso, hacer una lección...).
// IMPORTANTE: si cambia una pantalla o el nombre de un botón, hay que actualizarlo acá también.
public static class GuiaFluenty
{
    public const string Manual = @"CÓMO FUNCIONA FLUENTY (usá los nombres exactos de menús y botones):

QUÉ ES
Fluenty capacita a los empleados de una empresa para que usen la IA en su trabajo. La IA arma cursos a medida con lo que cada persona contó en su formulario, y cada lección usa ejemplos de su trabajo real.

LA EMPRESA Y LOS RANGOS (menú Equipo)
- La cuenta de la empresa (el dueño) la da Fluenty; no hay registro abierto. Si alguien no tiene cuenta, que escriba a contacto@fluenty.com.
- La empresa se organiza como un árbol de rangos: el Dueño arriba y abajo los rangos que crea (por ejemplo Gerente abajo de Dueño, y Vendedor abajo de Gerente).
- Para crear un rango: menú Equipo > sección 'Agregar un rango' > completar 'Nombre del rango', '¿Qué hace la gente de este rango?' y 'Va abajo de' > botón 'Agregar rango'.
- Conviene completar '¿Qué hace la gente de este rango?' (se edita en cada rango con el botón 'Guardar'): si alguien llena mal su formulario, la IA arma el curso con eso.
- Cada rango tiene su propio link (se puede copiar o mandar con el botón WhatsApp). Quien entra por ese link queda en ese rango.
- Cada persona ve y les crea cursos solo a los rangos que tiene abajo. El rango más bajo no ve Equipo, Cursos ni Empleados.

SUMAR EMPLEADOS (el formulario de empleado)
- No se cargan empleados a mano: se les manda el link de su rango. La persona entra, crea su cuenta (nombre, apellido, email, DNI, teléfono, contraseña) y completa el formulario en 3 partes: 'Tu cuenta', 'Tu trabajo' y 'Vos y la IA'.
- Al final puede contestar una pregunta extra sobre una tarea concreta, o tocar 'Saltear esta pregunta'.
- Ese formulario es lo que la IA usa para armar el curso y para elegir en qué unidad arranca cada uno. Lo que contestó es privado: los responsables ven si participa y su avance, no sus respuestas.

CREAR UN CURSO PARA UN RANGO (menú Cursos)
1. La primera vez, el dueño completa 'Contanos sobre tu equipo': 3 preguntas (dificultades del equipo, herramientas que usan, lo que quiere lograr). Se hace una sola vez. Si no lo completó, los demás responsables no pueden generar cursos todavía.
2. En Cursos aparece cada rango de abajo con cuántas personas tiene y cuántas completaron el formulario. Si nadie lo completó dice 'Esperando que se sumen': hay que mandarles el link desde Equipo.
3. Elegir 'Solo mi equipo' (curso a medida con los datos del equipo) o 'Público 🌎' (aparece en el buscador para todas las empresas; se escribe '¿De qué trata?' y la 'Categoría', y no usa datos de la empresa).
4. Tocar 'Generar curso del rango'. La IA tarda unos segundos.
5. El curso queda como BORRADOR: el rango todavía no lo ve. Se revisa unidad por unidad; si una unidad no convence, se le escribe a la IA qué cambiar (por ejemplo 'que sea más difícil, con más ejemplos de Excel') y la rehace.
6. Botón 'Confirmar curso' para que les llegue a todos, o 'Descartar y volver a generar'.
- Hay un curso por rango. Si alguien se suma al rango después de confirmar, en Cursos aparece el botón 'Sumarlo al curso'.
- Un curso tiene 4 unidades de 5 lecciones; la última lección de cada unidad es un examen.

SEGUIR A LA GENTE (menú Empleados y Home)
- Empleados muestra a cada persona de los rangos de abajo: su progreso, XP y racha, o si está 'Esperando su curso' o 'Sin formulario'. Con 'Ver su camino' se ve en qué unidad arrancó y cómo le va en cada lección.
- En el Home, quien tiene gente a cargo ve: personas a cargo, en capacitación, esperando curso, quiénes no avanzan hace una semana ('Para darles un empujón') y las actividades recientes.

HACER UN CURSO (empleados y cualquiera que tenga un curso)
- En el Home, sección 'Mis cursos', o en el menú 'Mi curso' / 'Mis cursos'. Botón 'Empezar el curso' o 'Seguir aprendiendo'.
- Cada persona arranca en la unidad que le corresponde según su formulario; las unidades anteriores quedan como que ya las sabe.
- Una lección son tarjetas cortas (unos 5 minutos) con explicación y preguntas. En cada tarjeta hay botones de ayuda: '🤷 Explicamelo más simple', '🔁 Dame otro ejemplo', '🔊 Escuchar' y '📋 Copiar este pedido para usarlo'. Si se equivoca, puede tocar 'Probar con otra pregunta del mismo tema'.
- XP: 10 por pregunta bien a la primera, 4 por cada una que aprendió después de equivocarse, y 20 extra por aprobar un examen. La racha sube haciendo al menos una lección por día.
- Los exámenes se aprueban con 70%. Si no aprueba, se arma un examen nuevo con otras preguntas para volver a rendir.
- Si la primera lección de una unidad le sale perfecta, puede 'Ir directo al examen'; si le cuesta mucho, puede 'Arrancar por lo básico' (una unidad antes).
- Hay un tope de pedidos a la IA por día dentro de las lecciones.

EXPLORAR CURSOS PÚBLICOS (menú Explorar)
- Buscador de cursos públicos de todas las empresas: se busca por texto, categoría, nivel (Principiante, Intermedio, Avanzado) y se ordena por 'Más nuevos' o 'Más elegidos'.
- Al abrir un curso se ve la descripción, las herramientas y el temario. Botón 'Inscribirme gratis': el curso aparece en 'Mis cursos'. Para inscribirse hace falta tener el formulario de empleado completo.

ASISTENTE (menú Asistente, donde estás vos)
- Chats guardados como en ChatGPT: '+ Nuevo chat' para empezar otro y 'Borrar chat' para borrarlo. Hay un tope de preguntas por día.

LO QUE TODAVÍA NO EXISTE
- La calculadora de ahorro y rendimiento aparece como 'Próximamente'.
- No se pueden editar ni borrar rangos, ni cambiar a una persona de rango, desde la app. Si preguntan por algo que no está en este manual, decí que no lo sabés en vez de inventarlo.";

    // Qué partes de la app ve esta persona, para que la IA no le indique menús que no tiene
    public static string QueVe(bool esDueno, bool gestiona, int cantidadCursos)
    {
        string texto = "LO QUE VE ESTA PERSONA EN EL MENÚ: Home";
        if (esDueno || gestiona)
        {
            texto = texto + ", Equipo";
        }
        if (gestiona)
        {
            texto = texto + ", Cursos, Empleados";
        }
        if (cantidadCursos == 1)
        {
            texto = texto + ", Mi curso";
        }
        else if (cantidadCursos > 1)
        {
            texto = texto + ", Mis cursos";
        }
        texto = texto + ", Explorar, Asistente y Salir.\n";
        if (esDueno)
        {
            texto = texto + "Es el DUEÑO de la empresa: arma el árbol de rangos y completa el formulario de la empresa.\n";
        }
        else if (gestiona)
        {
            texto = texto + "Es RESPONSABLE: tiene rangos a cargo y les puede crear cursos.\n";
        }
        else
        {
            texto = texto + "Es EMPLEADO del rango más bajo: no crea cursos. Si pregunta cómo crear uno, explicale que lo hace su responsable, y que él puede buscar cursos públicos en Explorar.\n";
        }
        return texto;
    }
}
