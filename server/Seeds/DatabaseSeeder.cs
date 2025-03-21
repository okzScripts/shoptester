namespace server.Seeds;
using Microsoft.Data.Sqlite;

public static class DatabaseSeeder
{
    public static async Task PopulateSampleData(SqliteConnection connection)
    {
        Console.WriteLine("Populating database with sample data...\n");

        // Sample Roles
        var roles = new[] { "admin", "user" };
        foreach (var role in roles)
        {
            try
            {
                using var command = new SqliteCommand(
                    "INSERT INTO roles (name) VALUES ($name)",
                    connection
                );
                command.Parameters.AddWithValue("$name", role);
                await command.ExecuteNonQueryAsync();
                Console.WriteLine($"Added role: {role}");
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                Console.WriteLine($"Role {role} already exists");
            }
        }
        Console.WriteLine();

        // Sample Categories
        var categories = new[] { "Electronics", "Books", "Clothing", "Food" };
        foreach (var category in categories)
        {
            try
            {
                using var command = new SqliteCommand(
                    "INSERT INTO categories (name) VALUES ($name)",
                    connection
                );
                command.Parameters.AddWithValue("$name", category);
                await command.ExecuteNonQueryAsync();
                Console.WriteLine($"Added category: {category}");
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                Console.WriteLine($"Category {category} already exists");
            }
        }
        Console.WriteLine();

        // Sample Products
        var products = new[]
        {
        (Name: "Laptop", Price: 999, Category: "Electronics"),
        (Name: "Smartphone", Price: 499, Category: "Electronics"),
        (Name: "The Great Gatsby", Price: 10, Category: "Books"),
        (Name: "1984", Price: 12, Category: "Books"),
        (Name: "T-Shirt", Price: 20, Category: "Clothing"),
        (Name: "Jeans", Price: 45, Category: "Clothing"),
        (Name: "Pizza", Price: 15, Category: "Food"),
        (Name: "Burger", Price: 8, Category: "Food"),
        (Name: "Headphones", Price: 50, Category: "Electronics"),
        (Name: "Mouse", Price: 20, Category: "Electronics"),
        (Name: "Keyboard", Price: 30, Category: "Electronics"),
        (Name: "The Catcher in the Rye", Price: 15, Category: "Books"),
        (Name: "To Kill a Mockingbird", Price: 18, Category: "Books"),
        (Name: "Dress", Price: 35, Category: "Clothing"),
        (Name: "Sweater", Price: 25, Category: "Clothing"),
        (Name: "Pasta", Price: 12, Category: "Food"),
        (Name: "Salad", Price: 10, Category: "Food"),
        (Name: "Monitor", Price: 150, Category: "Electronics"),
        (Name: "Tablet", Price: 80, Category: "Electronics"),
        (Name: "War and Peace", Price: 20, Category: "Books"),
        (Name: "The Odyssey", Price: 22, Category: "Books"),
        (Name: "Jacket", Price: 55, Category: "Clothing"),
        (Name: "Shoes", Price: 40, Category: "Clothing"),
        (Name: "Ice Cream", Price: 5, Category: "Food"),
        (Name: "Cake", Price: 18, Category: "Food"),
        (Name: "Printer", Price: 100, Category: "Electronics"),
        (Name: "Camera", Price: 200, Category: "Electronics"),
        (Name: "The Divine Comedy", Price: 25, Category: "Books")
    };

        foreach (var product in products)
        {
            try
            {
                using var command = new SqliteCommand(@"
                INSERT INTO products (name, price, category_id)
                SELECT $name, $price, id
                FROM categories
                WHERE name = $category",
                    connection
                );
                command.Parameters.AddWithValue("$name", product.Name);
                command.Parameters.AddWithValue("$price", product.Price);
                command.Parameters.AddWithValue("$category", product.Category);
                await command.ExecuteNonQueryAsync();
                Console.WriteLine($"Added product: {product.Name} (${product.Price}) in {product.Category}");
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                Console.WriteLine($"Product {product.Name} already exists");
            }
        }
        Console.WriteLine();

        // Sample Users
        var users = new[]
        {
        (Username: "admin", Email: "admin@admin.com", Password: "admin123", Role: "admin"),
        (Username: "john", Email: "john@email.com", Password: "john123", Role: "user"),
        (Username: "jane", Email: "jane@email.com", Password: "jane123", Role: "user")
    };

        foreach (var user in users)
        {
            try
            {
                using var command = new SqliteCommand(@"
                INSERT INTO users (username, email, password, role_id)
                SELECT $username, $email, $password, id
                FROM roles
                WHERE name = $role",
                    connection
                );
                command.Parameters.AddWithValue("$username", user.Username);
                command.Parameters.AddWithValue("$email", user.Email);
                command.Parameters.AddWithValue("$password", user.Password);
                command.Parameters.AddWithValue("$role", user.Role);
                await command.ExecuteNonQueryAsync();
                Console.WriteLine($"Added user: {user.Username} with role {user.Role}");
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                Console.WriteLine($"User {user.Username} already exists");
            }
        }

        Console.WriteLine("\nSample data population completed!");
    }
}