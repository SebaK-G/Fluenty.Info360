using Microsoft.Data.SqlClient;
using Dapper;

namespace Fluenty.Info360.Models;

public class BD
{
    private string _connectionString = @"Server=localhost;DataBase=Fluenty;Integrated Security=True;TrustServerCertificate=True;";

    public Usuarios Login(string email, string contrasena)
    {
        Usuarios usuario = null;

        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            string query = "SELECT * FROM Usuarios WHERE Email = @pEmail AND [Contraseña] = @pContrasena";
            usuario = connection.QueryFirstOrDefault<Usuarios>(query, new { pEmail = email, pContrasena = contrasena });
        }

        return usuario;
    }

    public void Signup(Usuarios usuario)
    {
        string query = "INSERT INTO Usuarios (Nombre, Apellido, IdEmpresa, Email, Dni, Telefono, Contraseña) VALUES (@pNombre, @pApellido, @pIdEmpresa, @pEmail, @pDni, @pTelefono, @pContrasena)";

        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Execute(query, new { pNombre = usuario.Nombre, pApellido = usuario.Apellido, pIdEmpresa = usuario.IdEmpresa, pEmail = usuario.Email, pDni = usuario.Dni, pTelefono = usuario.Telefono, pContrasena = usuario.Contraseña});
        }
    }
}