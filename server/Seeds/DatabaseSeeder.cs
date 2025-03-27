namespace server.Seeds;
using Microsoft.Data.Sqlite;

public static class DatabaseSeeder
{
    public static async Task PopulateSampleData(SqliteConnection connection)
    {
        // Database setup
        var createTableRoles = @"
            CREATE TABLE IF NOT EXISTS roles (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE
            )";

        var createTableUsers = @"
            CREATE TABLE IF NOT EXISTS users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT NOT NULL UNIQUE,
                email TEXT NOT NULL UNIQUE,
                password TEXT NOT NULL,
                role_id INTEGER,
                FOREIGN KEY(role_id) REFERENCES roles(id)
                    ON DELETE SET NULL
                    ON UPDATE CASCADE
            )";

        var createTableCategories = @"
            CREATE TABLE IF NOT EXISTS categories (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE
            )";

        var createTableProducts = @"
            CREATE TABLE IF NOT EXISTS products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                price INTEGER NOT NULL,
                category_id INTEGER,
                FOREIGN KEY(category_id) REFERENCES categories(id)
                    ON DELETE SET NULL
                    ON UPDATE CASCADE
            )";

        var createProductsView = @"
            CREATE VIEW IF NOT EXISTS products_view AS
            SELECT products.id, products.name, products.price, categories.name AS category
            FROM products
            LEFT JOIN categories ON products.category_id = categories.id
            ";

        var createUserView = @"
            CREATE VIEW IF NOT EXISTS user_view AS
            SELECT users.id, users.username, users.email, roles.name AS role
            FROM users
            LEFT JOIN roles ON users.role_id = roles.id
            ";

        var createTableOrders = @"
            CREATE TABLE IF NOT EXISTS orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                customer_id INTEGER,
                product_id INTEGER,
                quantity INTEGER NOT NULL,
                price INTEGER NOT NULL,
                total INTEGER GENERATED ALWAYS AS (quantity * price) VIRTUAL,
                created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY(customer_id) REFERENCES users(id)
                    ON DELETE SET NULL
                    ON UPDATE CASCADE,
                FOREIGN KEY(product_id) REFERENCES products(id)
                    ON DELETE SET NULL
                    ON UPDATE CASCADE
            )";

        var createTableOrderView = @"
            CREATE VIEW IF NOT EXISTS order_view AS
            SELECT orders.id, users.username, products.name as item, orders.quantity, orders.price, orders.total, orders.created_at
            FROM orders
            LEFT JOIN users ON orders.customer_id = users.id
            LEFT JOIN products ON orders.product_id = products.id
            ";

        try
        {
            await connection.OpenAsync();
            Console.WriteLine("**Connected to database**\n");

            Console.WriteLine("Enabling foreign keys...");
            using (var command = new SqliteCommand("PRAGMA foreign_keys = ON", connection))
            {
                await command.ExecuteNonQueryAsync();
            }
            Console.WriteLine("Foreign keys enabled\n");

            Console.WriteLine("Creating table roles...if not exists");
            using (var command = new SqliteCommand(createTableRoles, connection))
            {
                await command.ExecuteNonQueryAsync();
            }
            Console.WriteLine("Creating table roles completed\n");

            Console.WriteLine("Creating table users...if not exists");
            using (var command = new SqliteCommand(createTableUsers, connection))
            {
                await command.ExecuteNonQueryAsync();
            }
            Console.WriteLine("Creating table users completed\n");

            Console.WriteLine("Creating table categories...if not exists");
            using (var command = new SqliteCommand(createTableCategories, connection))
            {
                await command.ExecuteNonQueryAsync();
            }
            Console.WriteLine("Creating table categories completed\n");

            Console.WriteLine("Creating table products...if not exists");
            using (var command = new SqliteCommand(createTableProducts, connection))
            {
                await command.ExecuteNonQueryAsync();
            }
            Console.WriteLine("Creating table products completed\n");

            Console.WriteLine("Creating table orders...if not exists");
            using (var command = new SqliteCommand(createTableOrders, connection))
            {
                await command.ExecuteNonQueryAsync();
            }
            Console.WriteLine("Creating table orders completed\n");

            Console.WriteLine("Creating view order_view...if not exists");
            using (var command = new SqliteCommand(createTableOrderView, connection))
            {
                await command.ExecuteNonQueryAsync();
            }
            Console.WriteLine("View order_view created\n");

            Console.WriteLine("Creating view user_view...if not exists");
            using (var command = new SqliteCommand(createUserView, connection))
            {
                await command.ExecuteNonQueryAsync();
            }
            Console.WriteLine("View user_view created\n");

            Console.WriteLine("Creating view products_view...if not exists");
            using (var command = new SqliteCommand(createProductsView, connection))
            {
                await command.ExecuteNonQueryAsync();
            }
            Console.WriteLine("View products_view created\n");

            Console.WriteLine("**Database setup completed**");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }




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