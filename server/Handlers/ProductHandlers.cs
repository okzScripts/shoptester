using Microsoft.Data.Sqlite;
using server.Records;

namespace server.Handlers;


public static class ProductHandlers
{
    static void EnsureConnectionOpen(SqliteConnection connection)
    {
        if (connection.State != System.Data.ConnectionState.Open)
        {
            connection.Open();
        }
    }

    public static async Task<IResult> GetProducts(SqliteConnection connection)
    {
        EnsureConnectionOpen(connection);
        var sql = "SELECT * FROM products_view";
        using var command = new SqliteCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();
        var products = new List<ProductRead>();
        while (await reader.ReadAsync())
        {
            var item = new ProductRead(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetString(3)
            );
            products.Add(item);
        }
        return Results.Ok(products);
    }

    public static async Task<IResult> GetProduct(SqliteConnection connection, int id)
    {
        EnsureConnectionOpen(connection);
        var sql = "SELECT * FROM products_view WHERE id = @id";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var product = new ProductRead(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetString(3)
            );
            return Results.Ok(product);
        }
        return Results.NotFound();
    }

    public static async Task<IResult> CreateProduct(SqliteConnection connection, ProductCreate product)
    {
        EnsureConnectionOpen(connection);
        if (string.IsNullOrWhiteSpace(product.Name) || product.Price == null || product.CategoryId == 0)
            return Results.BadRequest(new { error = "Name, price, and category_id are required" });
        if (product.Price < 0)
            return Results.BadRequest(new { error = "Price must be a positive number" });
        var sql = "INSERT INTO products (name, price, category_id) VALUES ($name, $price, $category_id)";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$name", product.Name);
        command.Parameters.AddWithValue("$price", product.Price);
        command.Parameters.AddWithValue("$category_id", product.CategoryId);

        try
        {

            await command.ExecuteNonQueryAsync();
            using var command2 = new SqliteCommand("SELECT last_insert_rowid()", connection);
            var id = (long?)await command2.ExecuteScalarAsync();
            Console.WriteLine($"Info: Product {product.Name} added to database");
            return Results.Json(new { name = product.Name, price = product.Price, category = product.CategoryId, insertId = id }, statusCode: 201);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            return Results.Conflict(new { error = "A product with the same name already exists." });
        }
    }

    public static async Task<IResult> UpdateProduct(SqliteConnection connection, int id, ProductPatch product)
    {
        EnsureConnectionOpen(connection);
        var updates = new List<string>();
        if (product.Name != null) updates.Add("name = $name");
        if (product.Price != null) updates.Add("price = $price");
        if (product.CategoryId != null) updates.Add("category_id = $categoryId");

        if (updates.Count == 0)
            return Results.BadRequest(new { error = "No fields to update" });

        var sql = $"UPDATE products SET {string.Join(", ", updates)} WHERE id = $id";
        using var command = new SqliteCommand(sql, connection);

        // Only add parameters for fields that were provided
        if (product.Name != null) command.Parameters.AddWithValue("$name", product.Name);
        if (product.Price != null) command.Parameters.AddWithValue("$price", product.Price);
        if (product.CategoryId != null) command.Parameters.AddWithValue("$categoryId", product.CategoryId);
        command.Parameters.AddWithValue("$id", id);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        if (rowsAffected == 0)
            return Results.NotFound(new { message = $"Product with id:{id} not found" });

        Console.WriteLine($"Info: Product ID:{id} updated in database");
        return Results.Ok(new { message = $"Product with id:{id} updated" });
    }

    public static async Task<IResult> DeleteProduct(SqliteConnection connection, int id)
    {
        EnsureConnectionOpen(connection);
        var sql = "DELETE FROM products WHERE id = $id";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$id", id);
        var rowsAffected = await command.ExecuteNonQueryAsync();
        if (rowsAffected == 0)
            return Results.NotFound();
        Console.WriteLine($"Info: Product with id {id} deleted from database");
        return Results.Ok(new { message = $"Product with id:{id} deleted" });
    }

    public static async Task<IResult> GetProductsByCategory(SqliteConnection connection, string category)
    {
        EnsureConnectionOpen(connection);
        var sql = "SELECT * FROM products_view WHERE LOWER(category) = LOWER($category)";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$category", category);
        using var reader = await command.ExecuteReaderAsync();
        var products = new List<ProductRead>();
        while (await reader.ReadAsync())
        {
            var item = new ProductRead(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetString(3)
            );
            Console.WriteLine(item);
            products.Add(item);
        }
        return products.Count > 0 ? Results.Ok(products) : Results.NotFound($"No products found in category: {category}");
    }
}