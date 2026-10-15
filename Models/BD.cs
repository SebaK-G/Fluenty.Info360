using Microsoft.Data.SqlClient;
using Dapper;

namespace Fluenty.Info360.Models;

public class BD
{
    private string _connectionString = @"Server=localhost;DataBase=NombreBase;Integrated Security=True;TrustServerCertificate=True;";

    public Usuario Login(string email, string contrasena)
    {
        Usuario usuario = null;

        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            string query = "SELECT * FROM Usuarios WHERE Email = @pEmail AND Contrasena = @pContrasena";
            usuario = connection.QueryFirstOrDefault<Usuario>(query, new { pEmail = email, pContrasena = contrasena });
        }

        return usuario;
    }
}