using Microsoft.Data.Sqlite;
using server.Records;

namespace server.Handlers;

public static class OrderHandlers
{
    static void EnsureConnectionOpen(SqliteConnection connection)
    {
        if (connection.State != System.Data.ConnectionState.Open)
        {
            connection.Open();
        }
    }
    public static async Task<IResult> GetOrders(SqliteConnection connection)
    {
        EnsureConnectionOpen(connection);

        var sql = "SELECT * FROM order_view";
        using var command = new SqliteCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();
        var orders = new List<OrderRead>();
        while (await reader.ReadAsync())
        {
            var order = new OrderRead(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3),
                reader.GetInt32(4),
                reader.GetInt32(5),
                reader.GetDateTime(6)
            );
            orders.Add(order);
        }
        return Results.Ok(orders);
    }

    public static async Task<IResult> GetOrder(SqliteConnection connection, int id)
    {
        EnsureConnectionOpen(connection);

        var sql = "SELECT * FROM order_view WHERE id = $id";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$id", id);
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var order = new OrderRead(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3),
                reader.GetInt32(4),
                reader.GetInt32(5),
                reader.GetDateTime(6)
            );
            return Results.Ok(order);
        }
        return Results.NotFound(new { message = "Order not found" });
    }

    public static async Task<IResult> CreateOrder(SqliteConnection connection, HttpContext context, OrderCreate order)
    {
        EnsureConnectionOpen(connection);

        if (order.ProductId == 0 || order.Quantity == 0)
            return Results.BadRequest(new { error = "Product ID and quantity are required" });
        if (order.Quantity < 0)
            return Results.BadRequest(new { error = "Quantity cannot be negative" });
        if (order.ProductId < 0)
            return Results.BadRequest(new { error = "Product ID cannot be negative" });

        var customerId = context.Session.GetInt32("id");
        if (customerId == null)
            return Results.Unauthorized();

        var priceCommand = new SqliteCommand("SELECT price FROM products WHERE id = $id", connection);
        priceCommand.Parameters.AddWithValue("$id", order.ProductId);
        var price = await priceCommand.ExecuteScalarAsync();
        if (price == null)
            return Results.NotFound(new { message = "Product not found" });

        var sql = "INSERT INTO orders (customer_id, product_id, quantity, price) VALUES ($customer_id, $product_id, $quantity, $price)";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$customer_id", customerId);
        command.Parameters.AddWithValue("$product_id", order.ProductId);
        command.Parameters.AddWithValue("$quantity", order.Quantity);
        command.Parameters.AddWithValue("$price", price);
        await command.ExecuteNonQueryAsync();
        using var command2 = new SqliteCommand("SELECT last_insert_rowid()", connection);
        var id = (long?)await command2.ExecuteScalarAsync();
        Console.WriteLine($"Info: Order added to database");
        var total = order.Quantity * (long)price;
        return Results.Ok(new { customer_id = customerId, product_id = order.ProductId, quantity = order.Quantity, price, total, insertId = id });
    }

    public static async Task<IResult> DeleteOrder(SqliteConnection connection, int id)
    {
        EnsureConnectionOpen(connection);

        var sql = "DELETE FROM orders WHERE id = $id";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$id", id);
        var rowsAffected = await command.ExecuteNonQueryAsync();
        if (rowsAffected == 0)
            return Results.NotFound();
        Console.WriteLine($"Info: Order with id {id} deleted from database");
        return Results.Ok(new { message = $"Order with id:{id} deleted" });
    }

    public static async Task<IResult> GetOrdersByUser(int id, SqliteConnection connection)
    {
        EnsureConnectionOpen(connection);

        var sql = "SELECT * FROM order_view WHERE username = (SELECT username FROM users WHERE id = $id)";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$id", id);
        using var reader = await command.ExecuteReaderAsync();
        var orders = new List<OrderRead>();
        while (await reader.ReadAsync())
        {
            var order = new OrderRead(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3),
                reader.GetInt32(4),
                reader.GetInt32(5),
                reader.GetDateTime(6)
            );
            orders.Add(order);
        }
        return Results.Ok(orders);
    }

    public static async Task<IResult> GetOrderByUser(int id, int orderId, SqliteConnection connection)
    {
        EnsureConnectionOpen(connection);

        var sql = "SELECT * FROM order_view WHERE id = $orderId AND username = (SELECT username FROM users WHERE id = $id)";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$orderId", orderId);
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var order = new OrderRead(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3),
                reader.GetInt32(4),
                reader.GetInt32(5),
                reader.GetDateTime(6)
            );
            return Results.Ok(order);
        }
        return Results.NotFound(new { message = "Order not found" });
    }
}