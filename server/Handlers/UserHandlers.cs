using Microsoft.Data.Sqlite;
using server.Records;

namespace server.Handlers;

public static class UserHandlers
{
    static void EnsureConnectionOpen(SqliteConnection connection)
    {
        if (connection.State != System.Data.ConnectionState.Open)
        {
            connection.Open();
        }
    }
    public static async Task<IResult> GetUsers(SqliteConnection connection)
    {
        EnsureConnectionOpen(connection);
        var sql = "SELECT * FROM user_view";
        using var command = new SqliteCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();
        var users = new List<UserRead>();
        while (await reader.ReadAsync())
        {
            var user = new UserRead(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3)
            );
            users.Add(user);
        }
        return Results.Ok(users);
    }

    public static async Task<IResult> GetUser(SqliteConnection connection, int id)
    {
        EnsureConnectionOpen(connection);
        var sql = "SELECT * FROM user_view WHERE id = $id";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$id", id);
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var user = new UserRead(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3)
            );
            return Results.Ok(user);
        }
        return Results.NotFound(new { message = "User not found" });
    }

    public static async Task<IResult> CreateUser(SqliteConnection connection, UserCreate user)
    {
        EnsureConnectionOpen(connection);
        if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.Password) || user.RoleId == null)
            return Results.BadRequest(new { error = "Username, email, password and role are required" });
        var sql = "INSERT INTO users (username, email, password, role_id) VALUES ($username, $email, $password, $role_id)";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$username", user.Username);
        command.Parameters.AddWithValue("$email", user.Email);
        command.Parameters.AddWithValue("$password", user.Password);
        command.Parameters.AddWithValue("$role_id", user.RoleId);

        try
        {
            await command.ExecuteNonQueryAsync();
            using var command2 = new SqliteCommand("SELECT last_insert_rowid()", connection);
            var id = (long?)await command2.ExecuteScalarAsync();
            Console.WriteLine($"Info: User {user.Username} added to database");
            return Results.Ok(new { username = user.Username, email = user.Email, password = user.Password, role = user.RoleId, insertId = id });
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            return Results.Conflict(new { error = "A user with the same email already exists." });
        }
    }

    public static async Task<IResult> UpdateUser(SqliteConnection connection, int id, UserPatch user)
    {
        EnsureConnectionOpen(connection);

        // Build SQL query based on which fields are provided
        var updates = new List<string>();
        if (user.Username != null) updates.Add("username = $username");
        if (user.Email != null) updates.Add("email = $email");
        if (user.Password != null) updates.Add("password = $password");
        if (user.RoleId != null) updates.Add("role_id = $role_id");

        if (updates.Count == 0)
            return Results.BadRequest("No fields to update");

        var sql = $"UPDATE users SET {string.Join(", ", updates)} WHERE id = $id";
        using var command = new SqliteCommand(sql, connection);

        // Only add parameters for fields that were provided
        if (user.Username != null) command.Parameters.AddWithValue("$username", user.Username);
        if (user.Email != null) command.Parameters.AddWithValue("$email", user.Email);
        if (user.Password != null) command.Parameters.AddWithValue("$password", user.Password);
        if (user.RoleId != null) command.Parameters.AddWithValue("$role_id", user.RoleId);
        command.Parameters.AddWithValue("$id", id);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        if (rowsAffected == 0)
            return Results.NotFound();

        Console.WriteLine($"Info: User ID:{id} updated in database");
        return Results.Ok(new { message = $"User with id:{id} updated" });
    }

    public static async Task<IResult> DeleteUser(SqliteConnection connection, int id)
    {
        EnsureConnectionOpen(connection);
        var sql = "DELETE FROM users WHERE id = $id";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$id", id);
        var rowsAffected = await command.ExecuteNonQueryAsync();
        if (rowsAffected == 0)
            return Results.NotFound();
        Console.WriteLine($"Info: User with id {id} deleted from database");
        return Results.Ok(new { message = $"User with id:{id} deleted" });
    }
}