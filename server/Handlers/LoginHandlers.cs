using Microsoft.Data.Sqlite;
using server.Records;

namespace server.Handlers;

public static class LoginHandlers
{
    static void EnsureConnectionOpen(SqliteConnection connection)
    {
        if (connection.State != System.Data.ConnectionState.Open)
        {
            connection.Open();
        }
    }

    public static async Task<IResult> Login(HttpContext context, SqliteConnection connection, UserLogin user)
    {
        EnsureConnectionOpen(connection);

        if (string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.Password))
        {
            return Results.BadRequest(new { error = "Email and password are required" });
        }

        if (context.Session.GetInt32("id") != null)
        {
            return Results.Ok(new { message = "Already logged in" });
        }

        var sql = "SELECT * FROM users WHERE email = $email AND password = $password";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$email", user.Email);
        command.Parameters.AddWithValue("$password", user.Password);
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var id = reader.GetInt32(0);
            var username = reader.GetString(1);
            var email = reader.GetString(2);
            var role = reader.GetString(4);

            context.Session.SetInt32("id", id);
            context.Session.SetString("username", username);
            context.Session.SetString("email", email);
            if (role == "1")
            {
                role = "admin";
            }
            else
            {
                role = "user";
            }
            context.Session.SetString("role", role);
            return Results.Ok(new { id, username, email, role });
        }

        return Results.NotFound(new { message = "User not found" });
    }

    public static async Task<IResult> GetLogin(HttpContext context)
    {
        return await Task.Run(() =>
        {
            var id = context.Session.GetInt32("id");
            var username = context.Session.GetString("username");
            var email = context.Session.GetString("email");
            var role = context.Session.GetString("role");

            if (id == null)
            {
                return Results.Json(new { message = "Not logged in" }, statusCode: 401);
            }

            return Results.Ok(new { id, username, email, role });
        });
    }

    public static async Task<IResult> Logout(HttpContext context)
    {
        return await Task.Run(() =>
        {
            context.Session.Clear();
            return Results.Ok(new { message = "Logged out" });
        });
    }
}