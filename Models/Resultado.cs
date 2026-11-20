namespace Fluenty.Info360.Models;

// Lo que devuelven las acciones que se llaman con fetch desde JavaScript:
// si salió bien y un mensaje para mostrarle a la persona.
public class Resultado
{
    public bool Ok { get; set; }
    public string Mensaje { get; set; }
    // Un Id que hace falta después (por ejemplo, el IdCurso que se acaba de crear)
    public int Id { get; set; }

    public Resultado(bool ok, string mensaje)
    {
        Ok = ok;
        Mensaje = mensaje;
    }
}
