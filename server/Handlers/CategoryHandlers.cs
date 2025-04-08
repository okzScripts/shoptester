using Microsoft.Data.Sqlite;
using server.Records;

namespace server.Handlers;

public static class CategoryHandlers
{
    static void EnsureConnectionOpen(SqliteConnection connection)
    {
        if (connection.State != System.Data.ConnectionState.Open)
        {
            connection.Open();
        }
    }

    public static async Task<IResult> GetCategories(SqliteConnection connection)
    {
        EnsureConnectionOpen(connection);
        var sql = "SELECT * FROM categories";
        using var command = new SqliteCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();
        var categories = new List<CategoryRead>();
        while (await reader.ReadAsync())
        {
            var category = new CategoryRead(
                reader.GetInt32(0),
                reader.GetString(1)
            );

            categories.Add(category);
        }
        return Results.Ok(categories);
    }


    public static async Task<IResult> CreateCategory(SqliteConnection connection, CategoryCreate category)
    {
        EnsureConnectionOpen(connection);
        var sql = "INSERT INTO categories (name) VALUES ($name)";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$name", category.Name);
        try
        {
            await command.ExecuteNonQueryAsync();
            using var command2 = new SqliteCommand("SELECT last_insert_rowid()", connection);

            var id = (long?)await command2.ExecuteScalarAsync();
            Console.WriteLine($"Info: Category {category} added to database");
            return Results.Ok(new { category = category.Name, insertId = id });
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            return Results.Conflict(new { error = "A category with the same name already exists." });
        }
    }

    public static async Task<IResult> UpdateCategory(SqliteConnection connection, int id, CategoryPatch category)
    {
        EnsureConnectionOpen(connection);

        var sql = "UPDATE categories SET name = $name WHERE id = $id";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$name", category.Name);
        command.Parameters.AddWithValue("$id", id);
        var rowsAffected = await command.ExecuteNonQueryAsync();
        if (rowsAffected == 0)
            return Results.NotFound();
        Console.WriteLine($"Info: Category ID:{id} updated in database");
        return Results.Ok(new { message = $"Category with id:{id} updated" });
    }

    public static async Task<IResult> DeleteCategory(SqliteConnection connection, int id)
    {
        EnsureConnectionOpen(connection);
        var sql = "DELETE FROM categories WHERE id = $id";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$id", id);
        var rowsAffected = await command.ExecuteNonQueryAsync();
        if (rowsAffected == 0)
            return Results.NotFound();
        Console.WriteLine($"Info: Category with id {id} deleted from database");
        return Results.Ok(new { message = $"Category with id:{id} deleted" });

    }
}